# Database Per Service Pattern — Complete Deep Dive

## Part 1 — The Golden Rule of Microservices

### 1. Plain English Explanation
**WHAT:** The Database Per Service pattern dictates that each microservice must have its own private database. No other service is allowed to connect directly to that database. If the `OrderService` wants to know a customer's name, it cannot run a SQL query against the `CustomerDatabase`. It must make a network request to the `CustomerService` API, or subscribe to `Customer` events.

**WHY:** This is the most frequently broken rule in microservices, and breaking it ruins the entire architecture. If 5 microservices all share the same SQL database, and the `CustomerTeam` renames a column from `FirstName` to `GivenName`, they instantly crash the other 4 microservices. This is called a **Distributed Monolith**. By forcing every service to have a private database, you guarantee perfect **Autonomy**—teams can change their database schema anytime without asking anyone for permission.

### 2. Real-World Analogy
Imagine three businesses in a strip mall: A Bakery, a Bank, and a Gym.
- **Shared Database:** All three businesses share the exact same physical filing cabinet in the parking lot. The baker goes out to get a recipe, but the banker is organizing files, so the baker has to wait. One day, the banker buys a lock for the cabinet, and the baker and gym owner are permanently locked out. Total disaster.
- **Database Per Service:** The Bakery keeps its recipes in a safe inside the Bakery. The Bank has its own vault. If the Gym owner wants to know if a customer's credit card is valid, they don't break into the Bank's vault; they walk over to the Bank teller and *ask* politely (via an API). The Bank can reorganize its vault anytime it wants.

### 3. Core Concepts

#### 1. Polyglot Persistence
Because the databases are completely isolated, you can choose the best database technology for the specific job.
- `Catalog Service`: Uses **MongoDB** (NoSQL) because products have highly variable, dynamic attributes.
- `Order Service`: Uses **PostgreSQL** (SQL) because financial transactions require strict ACID guarantees.
- `Search Service`: Uses **ElasticSearch** for lightning-fast text matching.
- `Recommendation Service`: Uses **Neo4j** (Graph) to map relationships between users.

#### 2. The Great Problem: "How do I do a SQL JOIN?"
In a monolith, to show an Order History screen, you write:
`SELECT * FROM Orders JOIN Users ON Orders.UserId = Users.Id`.
In microservices, `Orders` and `Users` are on different servers. **You cannot do a SQL JOIN.**

**Solution A: API Composition (The Gateway)**
The API Gateway makes a call to `/orders`. It gets an array of Orders containing `userId=5`. The Gateway then makes a second call to `/users/5`, merges the JSON in memory, and sends it to the UI.
*Pros:* Easy to build.
*Cons:* Slow. Does not work for massive datasets or complex filtering (e.g., "Get all orders for users who live in New York").

**Solution B: Data Replication via Events (CQRS)**
The `UserService` publishes a RabbitMQ event: `UserMovedToNewYork { UserId: 5 }`.
The `OrderService` subscribes to this event. When it receives the event, it saves a *copy* of the user's city in its own Order database. 
Now, when the UI asks for "Orders in New York," the `OrderService` can query its own database directly without ever calling the `UserService`.
*Pros:* Extremely fast reads. Zero synchronous coupling.
*Cons:* Data duplication and Eventual Consistency.

### 4. Production Relevance: Bounded Contexts
Does "Database Per Service" literally mean you have to run 50 different PostgreSQL servers?
No. You can use the same physical database server, but create 50 different *Logical Databases* or *Schemas*.
- `Server 1`: Contains Database `OrdersDb` and Database `UsersDb`.
- **Crucial Rule:** The `OrderService` connection string uses a database user that ONLY has permissions to `OrdersDb`. If the `OrderService` attempts to run `SELECT * FROM UsersDb.Users`, the database throws an Access Denied error. This enforces the boundary at the infrastructure level.

### 5. Architectural Trade-offs

| Feature | Shared Database | Database Per Service |
| :--- | :--- | :--- |
| **Coupling** | **High.** Schema changes break other teams. | **Zero.** Teams have total autonomy. |
| **Data Integrity** | Perfect (Foreign Keys, ACID). | Hard. Requires Eventual Consistency. |
| **Distributed Joins** | N/A (Standard SQL JOINs). | Extremely complex (API Composition / CQRS). |
| **Infrastructure Cost** | Low (One giant DB). | High (Many DBs to backup and monitor). |

### 6. Common Mistakes and Misconceptions
- **Mistake:** Trying to enforce Foreign Key constraints across microservices. E.g., The `Order` table has a `UserId` column, and you try to make it a Foreign Key to the `User` table. You cannot have cross-database foreign keys. The `UserId` in the `Order` table is just a dumb Integer. Your application code is fully responsible for handling the edge case where `UserId 5` is deleted from the `UserService`, but still exists as a dead integer in the `OrderService`.
- **Misconception:** "Microservices must never duplicate data."
  **Reality:** Microservices duplicate data *constantly*. Duplicating a user's First Name and Email into the `OrderService` database so the Order team can send receipts without querying the `UserService` is highly encouraged. Disk space is cheap; network latency is expensive.

### Mock Interview Block

**Interviewer (Junior):** What is the Database Per Service pattern?
**Candidate:** It is the architectural rule that every microservice must manage its own private database. No other service is allowed to connect to it or read its tables directly. The only way to access that data is through the owning service's public API or by listening to its events.

**Interviewer (Mid):** If every service has a separate database, how do you handle complex queries that require data from two different services, since you can no longer write a SQL JOIN?
**Candidate:** There are two main approaches. The first is **API Composition**, where an API Gateway queries both services separately and joins the data together in memory before returning it to the client. The second approach is **Data Replication**, where services publish events when their data changes. Other services listen to those events and keep a localized, read-optimized copy of that data in their own database, allowing them to query everything locally.

**Interviewer (Senior):** Your team split a monolith into 10 microservices, but they all still connect to the same central SQL Server database. The deployment is a nightmare because any change to a table locks up the database and breaks 3 other services. Management says standing up 10 separate SQL Server clusters is too expensive. How do you implement Database Per Service without exploding infrastructure costs?
**Candidate:** The pattern is about *logical* isolation, not physical hardware. We can keep the single, powerful SQL Server cluster. However, we will create 10 completely separate schemas (or databases) within that cluster. We will create 10 different SQL User accounts, and grant each account permissions to exactly one schema. We update the connection strings in the microservices so they can only access their specific schema. This immediately enforces the isolation boundary, preventing cross-database joins and unauthorized table modifications, without spending a dime on new servers.

**Interviewer (Architect):** We are designing a multi-tenant SaaS application using the Database Per Service pattern. We have a `TenantService` and an `OrderService`. When a company (Tenant) deletes their account, we must delete all of their Orders to comply with GDPR. Because there are no Foreign Keys or cascading deletes across microservices, how do you architect a reliable system to ensure millions of orphaned Order records aren't left behind?
**Candidate:** Relying on a synchronous HTTP call (e.g., `TenantService` calls `DELETE /orders?tenantId=X`) is fragile. If the `OrderService` is down or times out while deleting 5 million rows, the deletion fails, and we are in violation of GDPR.
We must use an **Event-Driven Saga**. 
When the account is deleted, the `TenantService` marks the Tenant as `PendingDeletion` in its DB and publishes a `TenantDeletedEvent` to Kafka.
The `OrderService` consumes this event. Because deleting millions of rows takes time, it processes the deletion asynchronously in batches. Once completed, the `OrderService` publishes an `OrdersPurgedEvent`.
The `TenantService` listens for that completion event. Only when it receives the confirmation does it permanently hard-delete the Tenant record. If the `OrderService` crashes halfway through, Kafka's at-least-once delivery guarantees it will resume and finish the job when it restarts.
