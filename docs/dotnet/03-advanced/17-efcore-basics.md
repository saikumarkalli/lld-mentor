# Entity Framework Core Basics — Complete Deep Dive

## Part 1 — The ORM Concept & DbContext

### 1. Plain English Explanation
**WHAT:** Entity Framework Core (EF Core) is an Object-Relational Mapper (ORM). It sits between your C# application and your SQL database. Instead of writing raw SQL strings (`SELECT * FROM Users`), you write C# LINQ queries (`_dbContext.Users.ToList()`). EF Core automatically translates that C# code into optimized SQL, executes it, and returns the data as C# objects.

**WHY:** Writing raw SQL inside C# is error-prone, hard to refactor, and susceptible to SQL Injection attacks. EF Core provides strong typing (compiler checks) and handles the tedious mapping of database rows to C# class properties.

### 2. Real-World Analogy
Imagine you speak only English (C#), and a massive warehouse worker speaks only Russian (SQL Database).
- **Without EF Core:** You have to learn Russian, write down a Russian request on a piece of paper, hand it to the worker, and when he gives you a box of parts, you have to manually assemble them into a product.
- **With EF Core:** You hire a translator (DbContext). You speak English to the translator ("Get me all the active users"). The translator speaks Russian to the worker, gets the parts, perfectly assembles the product for you, and hands you the finished item.

### 3. C# .NET 8 Code Example

```csharp
// 1. The Entity (C# Class mapped to a Database Table)
public class User
{
    public int Id { get; set; } // Automatically becomes the Primary Key
    public string Name { get; set; }
    public bool IsActive { get; set; }
}

// 2. The Translator (DbContext)
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // This property represents the actual 'Users' table in SQL
    public DbSet<User> Users { get; set; } 
}

// 3. Usage in a Service
public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    public async Task AddUserAsync(string name)
    {
        // 1. Create the C# object
        var newUser = new User { Name = name, IsActive = true };

        // 2. Tell EF to track this new object
        _db.Users.Add(newUser);

        // 3. Translate to SQL (INSERT INTO Users...) and execute
        await _db.SaveChangesAsync();
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        // Translates to: SELECT * FROM Users WHERE IsActive = 1
        return await _db.Users.Where(u => u.IsActive).ToListAsync();
    }
}
```

### 4. Under the Hood: Change Tracking
The true power of EF Core is the **Change Tracker**. When you query data from the database (e.g., `_db.Users.First()`), EF Core takes a "snapshot" of the original data and attaches the C# object to its internal tracker.
If you change a property (`user.Name = "Bob"`), EF Core notices the difference between the current object and the snapshot. When you call `SaveChangesAsync()`, EF Core generates a highly specific `UPDATE Users SET Name = 'Bob' WHERE Id = 1` query. You do not need to write an update query yourself.

### 5. Production Relevance: Migrations
In production, database schemas change (adding columns, dropping tables). EF Core handles this via **Migrations**. 
When you add a new property to your `User` class (like `public DateTime CreatedAt { get; set; }`), you run the CLI command `dotnet ef migrations add AddCreatedAt`. EF Core compares your C# class to the existing database schema and generates a C# script describing how to update the database via an `ALTER TABLE` command. 

### 6. Architectural Trade-offs

| Feature | EF Core | Raw SQL (Dapper) |
| :--- | :--- | :--- |
| **Developer Speed** | High. Generates queries automatically. | Low. Must write and map SQL manually. |
| **Execution Speed** | Good. Very fast, but has translation overhead. | **Maximum.** No translation overhead. |
| **Change Tracking** | Yes. Automatic `UPDATE` generation. | No. Manual state management required. |
| **Complexity** | High. Steep learning curve for advanced queries (N+1). | Low. If you know SQL, you know Dapper. |

### 7. Common Mistakes and Misconceptions
- **Mistake:** Calling `SaveChanges()` multiple times in a single method. `SaveChanges()` opens a transaction, hits the database, and closes the transaction. If you modify 100 users in a loop, doing it 100 times is incredibly slow. Modify all 100 objects in memory, and call `SaveChanges()` exactly **once** at the end of the method. EF Core will batch the 100 updates into a single round-trip to the database.
- **Misconception:** "EF Core is too slow for enterprise applications." 
  **Reality:** EF Core 8 is insanely fast. 95% of performance issues are caused by developers writing bad LINQ queries (like fetching 1,000,000 rows into memory to filter them in C#, instead of filtering them in SQL).

### Mock Interview Block

**Interviewer (Junior):** What is the purpose of the `DbContext` class in Entity Framework?
**Candidate:** `DbContext` acts as the bridge between the C# application and the database. It holds the `DbSet<T>` properties which represent the tables, manages the database connection, and tracks changes made to objects so it can save them back to the database.

**Interviewer (Mid):** Explain how EF Core's Change Tracking works when updating a record.
**Candidate:** When you retrieve an entity from the database, EF Core stores a snapshot of its original state. When you modify properties on that C# object, EF Core's Change Tracker detects the differences. When you call `SaveChangesAsync()`, EF Core automatically generates a SQL `UPDATE` statement targeting only the columns that actually changed, and executes it.

**Interviewer (Senior):** A junior developer complains that their read-only report query, which returns 50,000 rows, is consuming gigabytes of RAM and crashing the server. You look at the code: `_db.Orders.ToList()`. How do you fix this using EF Core features?
**Candidate:** The issue is Change Tracking overhead. Because they just used `.ToList()`, EF Core is allocating memory to track the state of all 50,000 objects in case they want to update them. Since it's a read-only report, they should add `.AsNoTracking()`. `_db.Orders.AsNoTracking().ToList()` tells EF Core to skip the change tracker entirely, which massively reduces memory allocation and speeds up the query execution.

**Interviewer (Architect):** You are tasked with designing a multi-tenant SaaS application where 100 different companies share the same SQL database. Every table has a `TenantId` column. How do you ensure that a developer never accidentally writes a query that leaks Company A's data to Company B, without forcing them to manually write `.Where(x => x.TenantId == currentTenant)` on every single query in the application?
**Candidate:** I would implement **Global Query Filters** inside the `DbContext`. In the `OnModelCreating` method, I would apply a filter to all entities: `modelBuilder.Entity<Order>().HasQueryFilter(x => x.TenantId == _tenantResolver.CurrentTenantId)`. The `DbContext` injects a scoped tenant resolver to know who the current user is. With this in place, whenever a developer writes `_db.Orders.ToList()`, EF Core intercepts the query and automatically silently appends `WHERE TenantId = 5` to the SQL. The developers can't accidentally leak data because the filter is enforced at the DbContext level.
