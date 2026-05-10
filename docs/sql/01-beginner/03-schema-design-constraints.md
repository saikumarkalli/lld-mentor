# Schema Design & Constraints — Complete Deep Dive

## Part 1 — Protecting the Data Integrity

### 1. Plain English Explanation
**WHAT:** Schema Design is the process of deciding what tables to create and what columns they should have. **Constraints** are strict rules you enforce at the database level to ensure bad data never enters the system. 
You can write validation logic in your C# API ("Age must be > 18"), but bugs happen, or someone might bypass the API and write to the database directly. Database constraints are the final, unbreakable line of defense ensuring Data Integrity.

**WHY:** If your C# code accidentally tries to save a negative salary for an employee, or tries to delete a user who still has active orders, the database will throw an exception and block the operation. Without constraints, a single bug in your code can silently corrupt millions of rows of data permanently.

### 2. Core Constraints

#### 1. Primary Key (PK)
A column (or combination of columns) that uniquely identifies every single row in a table. It cannot be `NULL`.
*Example:* `UserId`. Every table should have a Primary Key.

#### 2. Foreign Key (FK)
A column in Table B that references the Primary Key in Table A. It enforces **Referential Integrity**.
*Example:* The `Orders` table has a `UserId` column. The FK guarantees that you cannot insert an Order for `UserId 999` if user 999 does not exist in the `Users` table. It also prevents you from deleting User 5 if User 5 has orders, preventing "Orphaned Records".

#### 3. UNIQUE Constraint
Ensures all values in a column are different. (Unlike a Primary Key, you can have multiple UNIQUE constraints on a table, and they can usually accept one `NULL` value).
*Example:* `EmailAddress`. Two users cannot register with the same email.

#### 4. CHECK Constraint
Enforces a specific business rule on a column's value.
*Example:* `CHECK (Salary > 0)`.

#### 5. DEFAULT Constraint
If the `INSERT` statement doesn't provide a value, the database inserts this automatically.
*Example:* `DEFAULT (GETUTCDATE())` for a `CreatedAt` column.

### 3. Production Relevance: The UUID vs INT Debate

The most critical decision in Schema Design is choosing the Data Type for your Primary Key.

**Option A: Auto-Incrementing Integer (`INT` or `BIGINT`)**
- *Pros:* Extremely fast. Takes only 4-8 bytes. Because they increment sequentially (1, 2, 3), new rows are always added to the very end of the physical hard drive file. This prevents **Index Fragmentation**.
- *Cons:* Not safe for distributed systems. If you have two databases syncing, they might both generate `Id = 5`, causing a collision. Easy for hackers to scrape your API (`/users/1`, `/users/2`).

**Option B: UUID / GUID (`UNIQUEIDENTIFIER`)**
- *Pros:* Globally unique (e.g., `a1b2c3d4-...`). You can generate the ID in your C# code *before* saving to the database, which is crucial for modern Event-Driven architectures. Immune to API scraping.
- *Cons:* Takes 16 bytes (huge). Because UUIDs are random, the database cannot just append the row to the end of the file. It has to slice open the physical hard drive file in the middle, jam the new row in, and push everything else down. This causes massive **Page Splits** and kills write performance.

*The Modern Solution:* Use **Sequential GUIDs** (like `NEWSEQUENTIALID()` in SQL Server, or UUIDv7). They look random but are mathematically generated in sequential order, giving you the distributed safety of a GUID with the blazing performance of an Integer.

### 4. Architectural Trade-offs: Soft Deletes

**Hard Delete:** `DELETE FROM Users WHERE Id = 5`. The row is gone forever.
**Soft Delete:** Adding an `IsDeleted BIT` column. You run `UPDATE Users SET IsDeleted = 1`. 

| Strategy | Data Recovery | Performance / Complexity |
| :--- | :--- | :--- |
| **Hard Delete** | Impossible (without restoring backups). | Simple. Fast. Keeps database small. |
| **Soft Delete** | Easy. Just flip the bit back to 0. | **High Complexity.** EVERY single `SELECT` query in your entire app must now include `WHERE IsDeleted = 0`. Slows down indexes. |

### 5. Common Mistakes and Misconceptions
- **Mistake:** Enforcing uniqueness in C# instead of SQL. A developer writes `if (!db.Users.Any(u => u.Email == newEmail)) { db.Users.Add(...) }`. Under heavy load, two users can hit the API at the exact same millisecond. Both `if` checks pass, and you insert duplicate emails. You MUST put a `UNIQUE` constraint on the SQL column. The database enforces locks to guarantee thread-safety.
- **Misconception:** "Foreign Keys slow down the database."
  **Reality:** Foreign keys do require the database to check the parent table on `INSERT`, adding microscopic overhead. However, removing Foreign Keys causes data corruption. Fixing corrupted, orphaned data costs thousands of times more money and time than the tiny milliseconds lost validating a Foreign Key.

### Mock Interview Block

**Interviewer (Junior):** What is the purpose of a Foreign Key?
**Candidate:** A Foreign Key creates a link between two tables and enforces referential integrity. It ensures that a value in a child table (like `UserId` in the `Orders` table) must exist in the parent table (`Users`). It prevents developers from inserting bad data, and prevents them from deleting a parent record if child records still rely on it.

**Interviewer (Mid):** You are building a system where users upload files. You notice developers are checking if the file name exists using C# logic before inserting. Why is this dangerous, and how do you fix it at the schema level?
**Candidate:** Checking uniqueness in the application code is vulnerable to Race Conditions. Two threads can check the database simultaneously, both see that the filename doesn't exist, and both perform an insert, creating duplicates. To fix this, I would add a `UNIQUE CONSTRAINT` on the `FileName` column in the database schema. The database engine guarantees thread-safe, ACID-compliant uniqueness.

**Interviewer (Senior):** Your team is designing a massive, globally distributed application with microservices. The Lead Developer wants to use standard Auto-Incrementing Integers (`IDENTITY`) for all Primary Keys because they are fast. Explain the architectural dangers of using Integers in a distributed system, and propose a better alternative.
**Candidate:** Auto-incrementing integers are terrible for distributed systems for two reasons. First, they cause collisions. If an offline mobile app generates data, or two separate database shards generate data, they will both generate `Id = 1`. When they sync, the data crashes. Second, they couple the application to the database; the C# code doesn't know the ID of an object until *after* the database saves it, making Event-Driven architectures difficult.
I would mandate the use of **UUIDs (GUIDs)**. However, because completely random GUIDs cause catastrophic index fragmentation (Page Splits), I would enforce the use of **Sequential GUIDs** (like UUIDv7). This provides global uniqueness allowing clients to generate IDs upfront, while maintaining sequential sorting to protect the database's physical write performance.

**Interviewer (Architect):** We implemented "Soft Deletes" (`IsDeleted = 1`) across our entire database of 500 million rows. Now, our queries are incredibly slow, because the SQL optimizer is ignoring our indexes. We cannot remove the Soft Delete feature because compliance mandates we keep the data for 7 years. How do you re-architect the schema to restore performance without losing the deleted data?
**Candidate:** Soft Deletes destroy performance because every query is forced to add `WHERE IsDeleted = 0`. If 90% of your database is "deleted", your indexes become massively bloated with dead data, leading to heavy fragmentation and poor cache utilization.
The architectural fix is to implement **Temporal Tables (History Tables)** or an **Archiving Strategy**.
We remove the `IsDeleted` column entirely. When a user is deleted, we use an `AFTER DELETE` database trigger (or application logic) to move that entire row into a completely separate `UsersArchive` database table. The main `Users` table only ever contains active data, remaining small, blazing fast, and index-optimized. The `UsersArchive` table satisfies the 7-year compliance mandate, and is only queried by auditors, keeping the production hot-path perfectly clean.
