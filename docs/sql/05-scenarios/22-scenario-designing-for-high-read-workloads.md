# Scenario: Designing for High-Read Workloads

## 1. The Business Requirements
We are designing the backend for a massive media site (like Reddit or a News Portal).
1. The site gets 100,000 visitors per minute (Reads).
2. The site gets 50 new articles or comments per minute (Writes).
3. The Homepage shows the "Top 20 Trending Articles" across all categories.
4. Users must be able to view articles instantly (sub-50ms latency).
5. The relational database (SQL Server) is currently hitting 95% CPU and causing the site to crash.

## 2. The Anti-Pattern: Hitting the Database Directly
The junior developers wrote an API endpoint `GET /api/homepage/trending`. 
Inside this endpoint, EF Core runs:
```sql
SELECT TOP 20 * 
FROM Articles a
JOIN Comments c ON a.Id = c.ArticleId
WHERE a.PublishDate > GETDATE() - 1
GROUP BY a.Id, a.Title
ORDER BY COUNT(c.Id) DESC;
```
**The Devastation:** This is a phenomenally expensive query. It has to scan all articles from yesterday, join thousands of comments, group them, count them, sort them, and take the top 20. 
Because the site gets 100,000 visitors per minute, this query is executing 1,600 times a second. The database CPU instantly catches fire.

## 3. The Architecture Fix: Multi-Tiered Caching

A relational database should **never** serve heavy analytical dashboard queries directly to thousands of concurrent users. We must shield the database using caches.

### Tier 1: The Application Cache (In-Memory)
The absolute fastest place to get data is the RAM of the C# Web Server itself.
We implement `IMemoryCache` in .NET.
When Request #1 hits the server, it runs the slow SQL query (takes 2 seconds). The C# server saves the "Top 20 Articles" list in its local RAM for 60 seconds.
For the next 59 seconds, the other 99,999 requests don't even talk to the database. The C# server just hands them the list from RAM (takes 0.01 milliseconds).

### Tier 2: The Distributed Cache (Redis)
*Problem:* If we have 50 Web Servers behind a load balancer, Request #1 hits Server A (runs the slow query). Request #2 hits Server B (also has to run the slow query because its local RAM is empty).
*Solution:* We introduce **Redis** (a distributed Key-Value NoSQL database running entirely in RAM).
Now, Server A checks Redis. If empty, Server A runs the SQL query and saves the JSON to Redis. Servers B through Z all check Redis and instantly get the JSON. The SQL database is now completely protected.

### Tier 3: Asynchronous Materialization (CQRS)
*Problem:* Even with Redis, once every 60 seconds the cache expires, and one poor user has to wait 2 seconds for the heavy SQL query to run.
*Solution:* We permanently sever the UI from the Read process.
1. The UI API simply executes `return await redis.GetAsync("homepage_trending");` (Always takes 2ms).
2. We create a **Background Worker Service** (a cron job).
3. Every 60 seconds, exactly *one thread* in the background worker runs the heavy SQL query, and overwrites the Redis key.
**Result:** The UI never waits. The database only ever receives exactly 1 query per minute, guaranteeing it uses 0.1% CPU, no matter if there are 10 users or 10 million users.

## 4. The Trade-off: Eventual Consistency
By using a cache, the Homepage is now "Eventual Consistent." If a user posts a comment on an article, the article's ranking score won't update on the homepage for up to 60 seconds. 
*Architectural Decision:* For a media site's homepage, 60 seconds of stale data is perfectly acceptable to the business. If this were a banking ledger, caching the balance for 60 seconds would be illegal.

---

## Mock Interview Block

**Interviewer:** Your team has implemented Redis to cache the "Top 20 Articles" list. However, every time the cache expires at the 60-second mark, the SQL database CPU spikes to 100% for exactly 3 seconds, and then drops back to 0%. This causes temporary connection timeouts. What is causing this, and how do you prevent it?
**Candidate:** This is the "Cache Stampede" (or Thundering Herd) problem. When the cache expires at exactly 60.00 seconds, the next 1,000 concurrent web requests all check Redis simultaneously, all see the cache is missing, and all 1,000 threads independently fire the heavy SQL query at the database at the exact same millisecond, crushing the CPU.
To prevent this, we must implement a **Locking Mechanism** (like a Distributed Lock in Redis) or use a `Double-Checked Locking` pattern in C#. When the cache expires, only the very first thread is allowed to acquire the lock and query the SQL database. The other 999 threads are forced to either wait for the new cache, or we can serve them the "stale" cache data while the background thread fetches the fresh data.

**Interviewer:** We have a High-Read system, but we also have strict SLA requirements that the data shown to the user must never be more than 1 second stale. A 60-second Redis background worker won't work. How can we architecture a highly scalable read system that is instantly updated when a write occurs?
**Candidate:** We must implement an **Event-Driven Cache Aside** pattern, effectively building a CQRS system.
When the user's C# API performs an `INSERT` or `UPDATE` into the SQL Database, we do not wait for a background polling job. Instead, the moment the SQL transaction commits, the C# API (or a Change Data Capture tool like Debezium attached to the SQL logs) publishes an `ArticleUpdatedEvent` to a message broker like RabbitMQ.
A dedicated "Projection Worker" consumes this event instantly, calculates the new ranking logic, and forcefully overwrites the Redis `homepage_trending` key. 
Because this is driven by events rather than a 60-second timer, the Redis cache is updated within milliseconds of the database write occurring, satisfying the 1-second SLA while keeping read queries entirely off the SQL server.

**Interviewer:** You decided to use a Read Replica instead of Redis for your high-read workload. The C# web application routes all `SELECT` queries to the Replica. Is the Read Replica safe from the "Cache Stampede" CPU spikes you mentioned earlier?
**Candidate:** No, the Read Replica is entirely vulnerable to the exact same CPU spikes. A Read Replica is just a SQL database; it still has to parse SQL, calculate Execution Plans, and traverse B-Tree indexes. If 1,000 concurrent users ask the Read Replica to perform a heavy `GROUP BY` and `ORDER BY` aggregation simultaneously, the Replica's CPU will hit 100% and crash. 
A Read Replica provides *compute isolation* (preventing reads from crashing the Master), but it does not provide the $O(1)$ read-time complexity of a Key-Value cache like Redis. For heavy, identical aggregations (like a Homepage), you must materialize the result into a Cache; you cannot simply throw a Read Replica at it.
