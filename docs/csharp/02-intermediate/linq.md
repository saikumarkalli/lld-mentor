# LINQ — Deferred vs Immediate Execution

> **⚡ Core Idea (30 seconds):** LINQ is a query language embedded in C# using delegates and extension methods. The critical thing to understand is **when** the query executes: deferred queries build a pipeline and execute **only when iterated**. Getting this wrong causes N+1 bugs, multiple enumeration, and unexpected database hits.

**Domain:** `C#` · **Level:** `Intermediate` · **Tags:** `#linq` `#deferred` `#ienumerable` `#iqueryable`

---

## 1. Core Idea

LINQ is just **extension methods on `IEnumerable<T>` that accept delegates**. When you write `collection.Where(x => x.Age > 18)` you're not filtering yet — you're building a description of *how* to filter. The actual work happens when something iterates the result.

Think of it as writing a recipe (query) vs cooking the meal (execution). You can hand the recipe to different kitchens (`IEnumerable<T>` = in-memory, `IQueryable<T>` = database).

---

## 2. Deep Explanation

### Deferred (Lazy) Execution

Methods that return `IEnumerable<T>` are **deferred** — they use `yield return` internally:

| Deferred | Immediate |
|----------|----------|
| `Where`, `Select`, `OrderBy` | `ToList()`, `ToArray()`, `Count()` |
| `Skip`, `Take`, `GroupBy` | `First()`, `Single()`, `Sum()`, `Max()` |
| `SelectMany`, `Distinct` | `ToDictionary()`, `ToHashSet()` |

```csharp
var query = numbers.Where(n => {
    Console.WriteLine($"Checking {n}");
    return n > 5;
}); // Nothing printed yet — no execution

var result = query.ToList(); // NOW it prints — executes here
```

### Multiple Enumeration — A Real Bug

```csharp
IEnumerable<Order> orders = GetOrders(); // Deferred / DB query
var count = orders.Count();    // Executes query — hits DB
var first = orders.First();    // Executes query AGAIN — hits DB twice!
var list = orders.ToList();    // Makes it concrete — enumerate once

// Always materialize if you need to use the result multiple times
var materializedOrders = GetOrders().ToList(); // One DB hit
```

### `IEnumerable<T>` vs `IQueryable<T>`

The crucial difference for EF Core / database work:

```
IEnumerable<T>     → LINQ to Objects → C# delegates → runs IN MEMORY
IQueryable<T>      → LINQ to Entities → Expression trees → translates to SQL
```

```csharp
// IQueryable: WHERE is translated to SQL — efficient
var query = context.Products
    .Where(p => p.Price > 100)   // Becomes: WHERE Price > 100 in SQL
    .OrderBy(p => p.Name)         // Becomes: ORDER BY Name in SQL
    .ToList();                     // Executes one SQL query

// MISTAKE: Calling AsEnumerable() too early
var mistake = context.Products
    .AsEnumerable()               // Loads ALL products into memory!
    .Where(p => p.Price > 100);   // Filters in C# — N rows loaded unnecessarily
```

### Expression Trees vs Delegates

When LINQ method is called on `IQueryable<T>`, the lambda is treated as an `Expression<Func<T, bool>>` — a data structure representing the code, not compiled code. The EF Core provider translates this expression tree to SQL.

---

## 3. Code Examples

### Example 1 — Basic: LINQ transformation pipeline
```csharp
var orders = new List<Order>
{
    new(1, "Alice", 500m),
    new(2, "Bob", 150m),
    new(3, "Alice", 300m),
};

// Build pipeline — not executed yet
var query = orders
    .Where(o => o.Total > 200)
    .GroupBy(o => o.CustomerName)
    .Select(g => new
    {
        Customer = g.Key,
        OrderCount = g.Count(),
        TotalSpent = g.Sum(o => o.Total)
    })
    .OrderByDescending(x => x.TotalSpent);

// Execute now
foreach (var result in query) // ← Execution happens here
    Console.WriteLine($"{result.Customer}: {result.TotalSpent:C}");
```

### Example 2 — Real-World: Repository with IQueryable
```csharp
public class ProductRepository
{
    private readonly AppDbContext _context;

    // Returns IQueryable — caller can compose further before executing
    public IQueryable<Product> GetActive()
        => _context.Products.Where(p => p.IsActive);
}

// Service layer composes the query — ONE SQL call total
public async Task<List<ProductDto>> GetExpensiveProductsAsync(decimal minPrice)
{
    return await _repo.GetActive()           // IQueryable<Product>
        .Where(p => p.Price > minPrice)      // Adds to SQL WHERE
        .OrderBy(p => p.Name)               // Adds ORDER BY
        .Select(p => new ProductDto          // SELECT only needed columns
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price
        })
        .ToListAsync();                      // Execute — single SQL query
}
```

---

## 4. Interview Questions

1. **What is deferred execution in LINQ? Name three operators that are deferred and three that are immediate.**
2. **What is the difference between `IEnumerable<T>` and `IQueryable<T>`?**
3. **What is the N+1 problem and how does LINQ contribute to it?**
4. **What is an expression tree? How is it different from a delegate?**
5. **What does `.AsEnumerable()` do and why is it dangerous mid-query?**

---

## 5. Follow-up Questions

- What happens if you modify the source collection while iterating a LINQ query on it?
  *(Throws `InvalidOperationException: Collection was modified`)*  
- `First()` vs `FirstOrDefault()` — which one is safer and when? What's the SQL difference?
- `Count()` vs `Any()` — which is faster for "does it have any items?"?
  *(Any() — stops on first match; Count() enumerates all)*
- Can EF Core translate any C# expression to SQL? What happens when it can't?
  *(Throws at runtime with client-evaluation warning/error — use raw SQL or restructure query)*
- What is `IAsyncEnumerable<T>` in the context of EF Core and when would you stream results?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Multiple enumeration
IEnumerable<Order> orders = GetOrdersQuery(); // IQueryable or deferred
if (orders.Any())                 // DB hit #1
    Process(orders.First());      // DB hit #2!
// FIX: orders = orders.ToList(); first

// MISTAKE 2: Lazy execution in using block
IEnumerable<Product> products;
using (var context = new AppDbContext())
{
    products = context.Products.Where(p => p.IsActive); // Query not executed
}
var list = products.ToList(); // ❌ ObjectDisposedException — context is gone!
// FIX: .ToList() inside the using block

// MISTAKE 3: Selecting too much data
var products = context.Products.ToList() // Loads EVERYTHING
    .Where(p => p.Price > 100);          // Filters in memory

// FIX: Filter in SQL
var products2 = context.Products.Where(p => p.Price > 100).ToList();

// MISTAKE 4: Using string methods that EF can't translate
var results = context.Users
    .Where(u => u.Name.CustomExtensionMethod()) // ❌ Can't translate to SQL
    .ToList();
```

---

## 7. Real-World Usage

| Scenario | Pattern |
|----------|---------|
| Repository filtering | Return `IQueryable<T>` — compose in service layer |
| Reporting / aggregation | `.GroupBy().Select()` projections before `.ToList()` |
| Pagination | `.Skip(page * size).Take(size).ToListAsync()` |
| Projection to DTOs | `.Select(e => new Dto {...}).ToListAsync()` — avoids loading navigation properties |
| Streaming large datasets | `IAsyncEnumerable<T>` with `await foreach` |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Basic LINQ operators, method vs query syntax |
| **Level 2** | Deferred vs immediate, IQueryable vs IEnumerable |
| **Level 3** | Expression trees, EF Core translation, multiple enumeration |
| **Level 4** | Custom LINQ providers, `IAsyncEnumerable<T>`, query plan analysis |

---

## 🔗 Connected Topics

- [Delegates](./delegates.md) — LINQ is built entirely on `Func<T, bool>` and `Func<T, TResult>` delegates
- [Iterators & yield return](./iterators.md) — How deferred LINQ operators are implemented
- [Generics](./generics.md) — `IEnumerable<T>`, `IQueryable<T>` are generic interfaces
- [async/await](../03-advanced/async-await.md) — `.ToListAsync()`, `IAsyncEnumerable<T>`

> 🎯 **Interviewer Mindset:** "Explain deferred execution" is a staple. The killer follow-up is "what are the consequences of multiple enumeration with EF Core?" Always demonstrate you know the DB hit implications, not just the in-memory mechanics.

*Created: April 2026 · Level: Intermediate*
