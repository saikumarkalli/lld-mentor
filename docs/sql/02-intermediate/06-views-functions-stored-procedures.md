# Views, Functions, & Stored Procedures — Complete Deep Dive

## Part 1 — Moving Logic to the Data

### 1. Plain English Explanation
**WHAT:** Relational databases are not just dumb storage cabinets; they are powerful computational engines. 
- **Views:** A saved SQL query that acts like a "virtual table". 
- **User-Defined Functions (UDFs):** A saved piece of logic that takes parameters and returns a single value or a table, mostly used inside `SELECT` statements.
- **Stored Procedures (SPs):** A full "program" saved in the database. It can take parameters, run loops, execute `INSERT/UPDATE/DELETE`, handle errors (Try/Catch), and return data.

**WHY:** If you have a complex formula to calculate a user's "Loyalty Tier", and you have a Web App, a Mobile App, and a Reporting tool that all need this tier, writing that logic in C# means the Reporting tool (which doesn't use C#) can't see it. By putting the logic directly into a View or Stored Procedure, the logic is centralized in the database. 

### 2. Deep Dive into the Tools

#### 1. Views
Views do not store data (usually). When you run `SELECT * FROM ActiveUsersView`, the engine seamlessly executes the underlying `SELECT * FROM Users WHERE IsActive = 1` query.
- **Primary Use:** **Security and Simplification.** If you have an `Employees` table with a `SocialSecurityNumber` column, you don't want the interns writing reports to see it. You create an `InternEmployeeView` that specifically excludes that column. You deny the interns access to the raw table, and grant them access *only* to the view.

#### 2. Functions (UDFs)
Functions must be deterministic (passing the same inputs always yields the same output) and they **cannot change database state** (No `INSERT` or `UPDATE`).
- **Scalar Function:** Returns one value. (e.g., `dbo.CalculateTax(100.00)` returns `108.00`).
- **Table-Valued Function (TVF):** Returns a table. You can use it in a `JOIN`.
*Warning:* Scalar functions in a `SELECT` statement execute row-by-row. If your table has 1 million rows, the function executes 1 million times, completely killing performance.

#### 3. Stored Procedures (SPs)
The heavy lifters. They allow you to batch multiple operations together. 
```sql
CREATE PROCEDURE ProcessCheckout (@UserId INT, @Total DECIMAL)
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;
            INSERT INTO Orders (UserId, Total) VALUES (@UserId, @Total);
            UPDATE Users SET LoyaltyPoints = LoyaltyPoints + 10 WHERE Id = @UserId;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        -- Handle Error
    END CATCH
END
```

### 3. Production Relevance: The ORM vs SP Debate
Historically (1990s - 2000s), standard practice was to put *all* business logic into Stored Procedures.
Today, with modern ORMs like **Entity Framework Core**, this has shifted. Business logic lives in C#. The ORM generates the raw SQL dynamically. 
**When to still use Stored Procedures today:**
1. **Security boundaries:** The DBA refuses to give the web app `INSERT` permissions on raw tables.
2. **Massive Data Operations:** If you need to update 5 million rows, doing it in C# means pulling 5 million rows across the network into RAM, updating them, and sending them back. A Stored Procedure executes instantly directly on the database CPU with zero network travel.

### 4. Architectural Trade-offs

| Tool | State Changes | Network Impact | Best For |
| :--- | :--- | :--- | :--- |
| **View** | No | Standard | Masking complex joins and securing column access. |
| **Function** | No | Standard | Reusable mathematical calculations. |
| **Stored Procedure** | **Yes** | **Lowest** (1 call triggers many actions) | Batch jobs, heavy state mutations, legacy systems. |
| **ORM (EF Core)** | Yes | Higher (Generates SQL on the fly) | Modern Agile development, keeping logic in Git. |

### 5. Common Mistakes and Misconceptions
- **Mistake:** Putting business rules (like "Premium Users get a 10% discount") inside a Stored Procedure, while the rest of the logic is in C#. This creates **Split Brain Architecture**. When a developer needs to change the discount logic, they search the C# code, can't find it, and assume the feature doesn't exist. Keep business logic in exactly one tier (usually the application layer).
- **Misconception:** "Stored Procedures prevent SQL Injection, while inline SQL does not."
  **Reality:** SPs prevent SQL injection because they enforce **Parameterization** (treating inputs as raw text, never executing them). However, modern ORMs (EF Core, Dapper) *also* automatically use parameterized queries under the hood. SPs do not have a magical security advantage over Dapper; they both use the exact same secure parameterization techniques.

### Mock Interview Block

**Interviewer (Junior):** What is the difference between a View and a Table?
**Candidate:** A table physically stores data on the hard drive. A View is just a saved SQL query. When you select from a view, the database engine executes the underlying query against the physical tables in real-time. Views are great for simplifying complex joins so developers can query them easily.

**Interviewer (Mid):** You need to calculate a complex "Risk Score" for 500,000 customers. A developer writes a Scalar SQL Function `dbo.GetRiskScore(CustomerId)` and runs `SELECT Name, dbo.GetRiskScore(Id) FROM Customers`. The query takes 4 minutes. Why is it so slow, and how do you fix it?
**Candidate:** Scalar functions in the `SELECT` clause are a notorious anti-pattern because they execute iteratively. The database engine acts like a `foreach` loop, executing that function 500,000 separate times, destroying set-based performance. To fix it, we must rewrite the logic as an `Inline Table-Valued Function (TVF)` and `CROSS APPLY` it to the query, or simply unpack the function's logic directly into the main `SELECT` statement using `CASE` statements. This allows the engine's query optimizer to process the 500,000 rows as a single set.

**Interviewer (Senior):** Historically, DBAs mandated that applications only communicate with the database via Stored Procedures. Modern Agile teams prefer using ORMs like Entity Framework Core. What are the architectural downsides of putting all your business logic into Stored Procedures?
**Candidate:** Putting business logic in Stored Procedures causes several architectural issues. 
First, **Version Control and CI/CD:** C# code is easily branched, merged, and tested in Git. Managing database schema state and SP rollbacks across different environments is notoriously difficult. 
Second, **Testing:** You cannot easily write isolated Unit Tests for a Stored Procedure without standing up a real SQL database. C# logic can be tested in milliseconds using Mocks. 
Third, **Vendor Lock-in:** SPs are written in proprietary dialects (T-SQL vs PL/pgSQL). If you migrate from SQL Server to PostgreSQL, you have to rewrite thousands of lines of logic. If the logic is in EF Core, the ORM handles the translation automatically.

**Interviewer (Architect):** We have a microservice architecture. The `BillingService` database receives 10,000 small `INSERT` statements per second from our C# API, representing individual IoT sensor charges. The network latency between our Kubernetes cluster and the Database server is 2 milliseconds. The C# API threads are starving because they spend 2ms waiting for every single insert. How do you utilize Stored Procedures to solve this network latency bottleneck?
**Candidate:** Sending 10,000 individual `INSERT` statements requires 10,000 network round-trips. `10,000 * 2ms = 20,000ms` of network latency per second. The application cannot keep up.
The architectural fix is **Batching via User-Defined Table Types (TVPs)** and Stored Procedures. 
In the C# application, we collect the 10,000 records into memory over the span of 1 second. We then serialize them into a C# `DataTable` or structured array. We make exactly *one* network call to the database, passing the entire array as a single parameter to a Stored Procedure. The SP uses an `INSERT INTO ... SELECT * FROM @IncomingTableParameter` to write all 10,000 rows to the disk instantly. We reduced the network penalty from 20,000 milliseconds to 2 milliseconds.
