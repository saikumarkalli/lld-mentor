# SQL Anti-Patterns: Cursors & N+1 Queries — Complete Deep Dive

## Part 1 — Why Applications Destroy Databases

### 1. Plain English Explanation
**WHAT:** An Anti-Pattern is a common, intuitive way of writing code that turns out to be a performance catastrophe in production. In the database world, 90% of performance issues are not caused by bad hardware; they are caused by application code (usually C# or Java) forcing the database to behave in ways it was not designed for.

**WHY:** Relational databases are built for **Set-Based Theory**. They are designed to grab 10,000 rows all at once, manipulate them in memory, and return them as a single block. 
Object-Oriented programming (like C#) is built for **Iterative Processing** (`for` loops). When a developer writes database code like a `for` loop (processing one row at a time), they destroy the database's performance.

### 2. Anti-Pattern #1: The N+1 Query Problem

This is the most famous bug in modern software, entirely caused by ORMs (like Entity Framework).

**The Scenario:** You want to load 100 Authors, and print their Books.
**The Bad C# Code:**
```csharp
var authors = dbContext.Authors.ToList(); // Query 1: SELECT * FROM Authors (Returns 100 rows)

foreach(var author in authors) 
{
    // For every loop iteration, EF runs another query behind the scenes!
    var books = author.Books.ToList(); // Query N: SELECT * FROM Books WHERE AuthorId = X
    Console.WriteLine(books.Count);
}
```

**The Devastation:** To load 100 authors, the application executed **101 separate SQL queries** over the network. If network latency is 2ms, `101 * 2ms = 202ms` just in network travel. If there were 10,000 authors, the application crashes.

**The Fix (Eager Loading):**
Force the database to use a `JOIN` and return everything in 1 query.
```csharp
// The .Include() forces a JOIN. 1 Query total.
var authors = dbContext.Authors.Include(a => a.Books).ToList(); 
```

### 3. Anti-Pattern #2: Database Cursors (Row-by-Agonizing-Row)

A developer needs to apply a complex 10% discount to 1 million orders. They write a Stored Procedure using a `CURSOR`.

```sql
DECLARE order_cursor CURSOR FOR SELECT Id, Total FROM Orders;
OPEN order_cursor;
FETCH NEXT FROM order_cursor INTO @Id, @Total;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Do complex math
    UPDATE Orders SET Total = @Total * 0.9 WHERE Id = @Id;
    FETCH NEXT FROM order_cursor INTO @Id, @Total;
END
```

**The Devastation:** A cursor forces the massive, set-based SQL engine to act like a C# `foreach` loop. It physically loads Row 1 into memory, locks it, updates it, logs it, and moves to Row 2. This is notoriously known as RBAR (Row-By-Agonizing-Row). It will take hours to process 1 million rows, and lock the entire table the whole time.

**The Fix (Set-Based Logic):**
Databases are built for Sets. Do it in one statement.
```sql
UPDATE Orders SET Total = Total * 0.9;
```
This executes in milliseconds.

### 4. Anti-Pattern #3: Functions in the WHERE Clause (Non-SARGable)

A developer wants to find everyone whose last name starts with "Sm".
**Bad:** `WHERE SUBSTRING(LastName, 1, 2) = 'Sm'`
**Devastation:** The database has a B-Tree index on `LastName`. But because you wrapped the column in a function, the engine can't use the B-Tree. It must do a full Table Scan, pull every row into CPU memory, apply the `SUBSTRING` function, and then check it.
**Fix:** Make it SARGable. `WHERE LastName LIKE 'Sm%'`. The engine instantly uses an Index Seek.

### 5. Architectural Trade-offs

| ORM Strategy | Developer Velocity | Database Performance |
| :--- | :--- | :--- |
| **Lazy Loading (EF Core Default in the past)** | High (Devs don't have to think about SQL). | **Catastrophic** (Causes N+1 queries everywhere). |
| **Eager Loading (`.Include()`)** | Medium | Good (1 Query via JOINs). |
| **Raw SQL / Dapper** | Low (Must write raw SQL strings). | **Perfect** (Total control over execution). |

### Mock Interview Block

**Interviewer (Junior):** What does the acronym "RBAR" stand for, and why is it bad in SQL?
**Candidate:** RBAR stands for "Row-By-Agonizing-Row." It refers to using Cursors or `WHILE` loops in SQL to process data one row at a time. It is terrible for performance because SQL is designed for "Set-Based" logic—manipulating entire blocks of data simultaneously. RBAR bypasses the engine's optimizations and causes massive CPU and locking overhead.

**Interviewer (Mid):** You are reviewing a pull request for a C# Web API. The developer is querying a list of `Orders`. Inside a `foreach` loop over those orders, they access `order.Customer.Name`. What database performance issue will this cause in production?
**Candidate:** This will cause the **N+1 Query Problem**. Because the `Customer` relationship was not explicitly loaded in the initial database query, the ORM (like Entity Framework) will trigger a "Lazy Load." For every iteration of the `foreach` loop, it will send a brand new, synchronous `SELECT` query across the network to fetch the customer. If there are 1,000 orders, it results in 1,001 network round-trips. The fix is to use Eager Loading (e.g., `.Include(o => o.Customer)`) so the database performs a SQL JOIN and returns all data in a single network trip.

**Interviewer (Senior):** A developer writes a query: `SELECT * FROM Logs WHERE CAST(LogDate AS DATE) = '2023-10-01'`. The `Logs` table has 50 million rows and a Non-Clustered Index on `LogDate`. The query takes 40 seconds and causes a full Clustered Index Scan. Explain why the index is being ignored and how to rewrite it efficiently.
**Candidate:** The query is Non-SARGable (Search-Argument-Able). By wrapping the indexed `LogDate` column inside the `CAST()` function, the Query Optimizer cannot use the B-Tree index to seek the date. It forces the engine to read all 50 million rows off the disk, cast them to a `DATE` in memory, and then perform the comparison.
To fix this, we must remove the function from the column side of the equation. We rewrite it as a range query: `WHERE LogDate >= '2023-10-01 00:00:00' AND LogDate < '2023-10-02 00:00:00'`. This allows the engine to instantly perform an Index Seek, dropping the execution time to milliseconds.

**Interviewer (Architect):** We are migrating a legacy system. It has a massive Stored Procedure that uses a Cursor to iterate over 500,000 invoices. For each invoice, it executes complex logic (checking 5 other tables) to calculate a penalty fee, and updates the invoice. The SP currently takes 4 hours to run. The business logic is too complex to write as a single inline `UPDATE` statement. How do you re-architect this to eliminate the Cursor and scale the processing?
**Candidate:** If the logic is truly too complex for a set-based SQL `UPDATE`, keeping it inside a database Cursor is an architectural failure. The database is meant for storage and retrieval, not complex, stateful computational loops.
I would completely remove the computation from the database layer. I would architect an Event-Driven background worker in C# (or Go). 
1. The worker pulls batches of 5,000 Invoice IDs from the database.
2. It fetches the required data into application memory.
3. It utilizes C#'s multi-threading (`Parallel.ForEach` or `Task.WhenAll`) to calculate the penalty fees for the 5,000 invoices simultaneously across multiple CPU cores.
4. It uses bulk-insert/bulk-update tools (like `SqlBulkCopy` or TVPs) to push the 5,000 updates back to the database in a single network round-trip.
By moving the compute to the horizontally scalable application tier, we eliminate the database locks and drop the processing time from 4 hours to a few minutes.
