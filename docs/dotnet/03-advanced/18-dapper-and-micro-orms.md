# Dapper & Micro-ORMs — Complete Deep Dive

## Part 1 — High-Performance Raw SQL

### 1. Plain English Explanation
**WHAT:** Dapper is a "Micro-ORM" (Object-Relational Mapper) built by StackOverflow. Unlike Entity Framework Core, which automatically translates C# code into SQL and tracks changes, Dapper forces you to write the exact raw SQL query yourself. Dapper's only job is to take the result of that SQL query and incredibly quickly map the data into a C# class.

**WHY:** Full ORMs like EF Core have overhead (compiling LINQ to SQL, building expression trees, memory tracking). When you are building a system that requires maximum, bare-metal performance—like a stock trading platform or StackOverflow itself—that overhead is unacceptable. Dapper is essentially a thin wrapper around native ADO.NET that removes the pain of manually mapping `SqlDataReader` rows to objects, providing speed practically identical to raw database drivers.

### 2. Real-World Analogy
- **Entity Framework Core (The Smart Oven):** You put in raw ingredients (C# objects), press "Cook Dinner", and the oven figures out the temperature, the time, and the method. It's easy, but you lose fine control, and the computer takes time to calculate the cooking algorithm.
- **Dapper (The Professional Gas Stove):** You manually light the fire, set the exact temperature, and stir the pot yourself (writing raw SQL). It requires more skill and effort, but it heats up instantly and cooks exactly the way you demand.

### 3. C# .NET 8 Code Example

```csharp
using Dapper;
using Microsoft.Data.SqlClient;

public class Order
{
    public int Id { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
}

public class OrderRepository
{
    private readonly string _connectionString;
    public OrderRepository(string connectionString) => _connectionString = connectionString;

    // ✅ Dapper is used as an extension method directly on the SqlConnection
    public async Task<List<Order>> GetPendingOrdersAsync(decimal minimumAmount)
    {
        // 1. Manually open the connection
        using var connection = new SqlConnection(_connectionString);

        // 2. Write the exact raw SQL string. Use @ variables to prevent SQL Injection!
        string sql = "SELECT Id, TotalAmount, Status FROM Orders WHERE Status = 'Pending' AND TotalAmount > @Amount";

        // 3. Dapper executes the query and maps the result to the Order class
        var orders = await connection.QueryAsync<Order>(sql, new { Amount = minimumAmount });
        
        return orders.ToList();
    }
}
```

### 4. Under the Hood
Dapper works by using Reflection exactly *once* per query structure to analyze your C# class. It then uses **Reflection.Emit** (IL Generation) to generate a highly optimized, custom mapping method in memory specifically for that class. The next time you run the query, it bypasses reflection and runs the dynamically generated mapping code. This makes mapping SQL rows to C# objects insanely fast.

### 5. Production Relevance
In modern enterprise architectures, developers rarely choose *only* EF Core or *only* Dapper. They use **CQRS (Command Query Responsibility Segregation)**.
- **Commands (Writes):** Use Entity Framework Core. Writing complex object graphs, managing foreign keys, and handling transactions is much easier with EF Core's Change Tracker.
- **Queries (Reads):** Use Dapper. When retrieving a massive dashboard report joining 6 tables, writing the optimized SQL manually and using Dapper to map the read-only result provides massive performance gains and lower memory usage.

### 6. Architectural Trade-offs

| Feature | Dapper | EF Core |
| :--- | :--- | :--- |
| **Performance** | **Ultra-Fast.** Near native ADO.NET speed. | Fast, but has LINQ-to-SQL translation overhead. |
| **SQL Control** | Absolute. You write the exact SQL. | Delegated. EF Core generates the SQL. |
| **Refactoring** | **Dangerous.** Renaming a SQL column breaks strings at runtime. | **Safe.** Renaming a C# property is checked by the compiler. |
| **Multi-Database Support** | Hard. You must write specific SQL for SQL Server vs Postgres. | Easy. EF Core translates LINQ to the correct dialect automatically. |

### 7. Common Mistakes and Misconceptions
- **Mistake:** Using string interpolation for SQL queries (`$"SELECT * FROM Users WHERE Name = '{name}'"`). **This is a catastrophic security vulnerability (SQL Injection).** You must always use parameterized queries with anonymous objects (`new { Name = name }`), which Dapper securely maps to ADO.NET parameters.
- **Misconception:** "Dapper is always faster than EF Core, so I should rewrite everything."
  **Reality:** On a simple `SELECT * FROM Users WHERE Id = 1`, the performance difference between Dapper and EF Core compiled queries is often measured in microseconds. You will not notice a difference. Dapper shines on massive datasets or incredibly complex, database-specific SQL features (like SQL Server CTEs or Window Functions) that EF Core cannot easily generate.

### Mock Interview Block

**Interviewer (Junior):** What is Dapper and how is it different from Entity Framework?
**Candidate:** Dapper is a Micro-ORM. Instead of generating SQL for you and tracking object changes like Entity Framework does, Dapper requires you to write raw SQL queries. Its only job is to execute that query and map the database rows into C# objects extremely quickly.

**Interviewer (Mid):** If Dapper requires raw SQL, how do you prevent SQL Injection attacks?
**Candidate:** You prevent SQL injection by never concatenating strings. Dapper supports parameterized queries. You write your SQL with parameter markers like `@UserId`, and then pass an anonymous C# object like `new { UserId = id }` to Dapper. Dapper safely passes these to the underlying database driver as strict parameters, neutralizing any injection attempts.

**Interviewer (Senior):** You have a complex dashboard screen that needs to join 5 tables and aggregate data. Would you use EF Core or Dapper for this specific endpoint?
**Candidate:** I would absolutely use Dapper. Joining 5 tables with aggregations in EF Core often results in massive, inefficient SQL generation, or forces client-side evaluation. With Dapper, I can write the exact, highly optimized SQL query (utilizing database-specific features if needed) and map the flat result directly into a read-only Data Transfer Object (DTO) with near-zero overhead.

**Interviewer (Architect):** We are designing a multi-tenant system that must support both SQL Server (for enterprise clients) and PostgreSQL (for smaller clients). A senior developer wants to use Dapper exclusively for maximum performance. As the architect, what are the long-term maintenance risks of this approach, and what alternative architecture would you propose?
**Candidate:** The massive risk of using Dapper exclusively across multiple database engines is SQL Dialect fragmentation. SQL Server and PostgreSQL have different syntaxes for dates, string concatenation, and pagination (`OFFSET FETCH` vs `LIMIT`). If we use Dapper, we have to write and maintain two separate raw SQL queries for every single data access method in the app. 
Instead, I would propose using EF Core as the primary ORM because it abstracts the SQL dialects perfectly. For the top 5% of queries that represent performance bottlenecks (like heavy reporting), we can drop down to Dapper, maintaining those specific dual-dialect raw SQL queries only where the performance gain justifies the maintenance cost.
