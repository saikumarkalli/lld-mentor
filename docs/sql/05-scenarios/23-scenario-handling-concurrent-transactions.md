# Scenario: Handling Concurrent Transactions (Race Conditions)

## 1. The Business Problem (The Double-Spend)
We are designing the database for a highly anticipated concert ticket sale.
There is exactly **1 VIP Ticket** left in the `Tickets` table.
At 12:00:00.000 PM, Alice and Bob both click "Buy" on the website at the exact same millisecond.

**The Flawed Application Logic (C#):**
```csharp
// 1. Check if a ticket exists
var ticket = db.Tickets.FirstOrDefault(t => t.Id == 1 && t.Status == "Available");

if (ticket != null) 
{
    // 2. Charge the credit card
    ChargeCreditCard(); 

    // 3. Mark the ticket as sold
    ticket.Status = "Sold";
    ticket.Owner = "Alice"; // Or Bob
    db.SaveChanges();
}
```

**The Devastation (Race Condition):**
- Thread A (Alice) runs Step 1. The DB says "Available".
- Thread B (Bob) runs Step 1. The DB says "Available".
- Thread A charges Alice $500.
- Thread B charges Bob $500.
- Thread A updates the DB to "Sold to Alice".
- Thread B updates the DB to "Sold to Bob".
Bob gets the ticket. Alice was charged $500 but gets nothing. The company is sued.

## 2. The Architectural Solutions

How do we use database locking mechanisms to prevent this?

### Solution A: Pessimistic Locking (The Bouncer)
We tell the database to physically lock the row the moment we look at it, so nobody else can even *read* it until we are done.

**SQL Implementation (SQL Server):**
We use the `WITH (UPDLOCK)` hint.
```sql
BEGIN TRAN;
-- Lock the row. If Bob tries to run this, his thread freezes and waits.
SELECT * FROM Tickets WITH (UPDLOCK) WHERE Id = 1 AND Status = 'Available';

-- (Do business logic)

UPDATE Tickets SET Status = 'Sold' WHERE Id = 1;
COMMIT TRAN;
```
*Pros:* Mathematically guarantees safety.
*Cons:* Destroys performance. If 10,000 people click "Buy", 9,999 threads are frozen in the C# application waiting for the lock, causing thread pool exhaustion and crashing the web server.

### Solution B: Optimistic Locking (The Version Check)
We don't lock anything. We let everyone read the ticket instantly. But when they try to `UPDATE` it, we force the database to check if someone else changed it first.

**Implementation (Entity Framework Core):**
We add a `RowVersion` (or Timestamp) column to the `Tickets` table.
1. Alice and Bob both read the ticket. They both get `RowVersion = 1`.
2. Alice's thread fires the update first:
   `UPDATE Tickets SET Status='Sold' WHERE Id=1 AND RowVersion=1`.
   Result: **1 Row Updated**. The database automatically increments the ticket to `RowVersion = 2`.
3. Bob's thread fires his update a millisecond later:
   `UPDATE Tickets SET Status='Sold' WHERE Id=1 AND RowVersion=1`.
   Result: **0 Rows Updated**. (Because the RowVersion is now 2).
4. EF Core sees that 0 rows were updated, panics, and throws a `DbUpdateConcurrencyException`. Bob's thread catches the exception, cancels the credit card charge, and tells Bob "Sorry, sold out."

*Pros:* No locks. Infinite read scalability. Perfect for modern web apps.
*Cons:* The application code must be written to catch and handle the exceptions (retry logic or user error messages).

### Solution C: Idempotent Queues (Event-Driven)
The absolute best enterprise architecture. We don't let the web servers touch the SQL database at all.
1. Alice and Bob click buy.
2. The Web Servers drop a message onto a RabbitMQ or Kafka queue: `{"Action": "Buy", "UserId": "Alice"}`.
3. A single, single-threaded Background Worker pulls messages off the queue one by one.
4. Because the worker is single-threaded, it processes Alice first, assigns the ticket, and saves to SQL. It then processes Bob, sees the ticket is gone, and emails him a rejection.
*Pros:* Zero database locks. Zero race conditions. Massive scalability.

---

## Mock Interview Block

**Interviewer:** In a scenario where 5,000 users are attempting to purchase 50 limited-edition sneakers simultaneously, would you choose Pessimistic Locking or Optimistic Locking at the database layer? Explain why.
**Candidate:** I would choose **Optimistic Locking**. 
If I choose Pessimistic Locking (using `UPDLOCK` or `FOR UPDATE`), the database will serialize the 5,000 requests. 4,999 application threads will be forcefully blocked waiting for database locks to release. This will quickly exhaust the Web Server's thread pool, causing the entire website to crash (Error 503) for all users, even those just trying to browse the homepage.
Optimistic Locking uses a `RowVersion` token to ensure data integrity at the exact moment of the `UPDATE`. It allows all 5,000 users to read the sneaker inventory instantly without blocking. 50 users will successfully update the rows, and the remaining 4,950 users will trigger a `ConcurrencyException` in the application tier. We catch that exception and cleanly return a "Sold Out" UI to the user, keeping the system highly performant and online.

**Interviewer:** A junior developer tries to solve the Double-Spend problem by wrapping the C# logic in a standard `BEGIN TRAN` and `COMMIT TRAN` block, using the default `Read Committed` isolation level. Does this solve the race condition?
**Candidate:** No, it does not solve the race condition. 
At the default `Read Committed` isolation level, the database only locks rows during the actual `UPDATE` statement. When the developer runs the initial `SELECT` statement to check if the ticket is available, the database releases the read lock immediately after returning the data. Therefore, Thread A and Thread B can *both* read the ticket as "Available" at the exact same time inside their respective transactions, leading directly to the double-spend. To solve it using transactions, you must explicitly elevate the lock using an `UPDLOCK` hint, or elevate the entire transaction isolation level to `Serializable`, both of which severely degrade performance.

**Interviewer:** We implemented Optimistic Concurrency using a `RowVersion` column. A customer loads a product editing screen. They go to lunch for an hour. Meanwhile, an admin updates the product description and saves it. The customer returns from lunch, changes the price, and clicks "Save". The Optimistic lock throws a `ConcurrencyException` because the RowVersions don't match, and the customer's price change is rejected. The customer is furious. How do you architect the UI and Backend to handle this specific UX failure gracefully?
**Candidate:** Rejecting the customer's entire input is a poor UX. We must implement **Concurrency Conflict Resolution**.
When the C# API catches the `DbUpdateConcurrencyException`, it should not just return a 500 error. It should catch the exception, query the database to fetch the *new* (Admin) version of the row, and return an HTTP `409 Conflict` to the frontend, including both the Admin's version and the Customer's version in the JSON payload.
The Frontend UI intercepts the 409 and displays a "Diff/Merge" modal to the customer. It says: "While you were away, an Admin changed the description to X. You are trying to change the price to Y. Do you want to overwrite their changes, or merge your price change?" 
If the user clicks "Merge", the frontend issues a new `PUT` request containing the *new* `RowVersion` (from the Admin) along with the updated price, ensuring a safe, intentional update.
