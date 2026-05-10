# Chapter 22 � Expression Trees

> **⚡ Core Idea (30 seconds):** An Expression Tree is **code represented as data**. Instead of compiling a lambda into executable IL, the compiler builds a tree of objects describing the lambda's structure. This allows frameworks like Entity Framework to inspect, translate, and execute your C# code as SQL.

**Domain:** `C#` **Level:** `Intermediate` **Tags:** `#expression-trees` `#linq` `#ef-core` `#metaprogramming`

---

## 1. Core Idea

Think of it as the difference between **a recipe card** and **a cooked dish**. A regular `Func<int, bool>` is the cooked dish — it's executable but you can't inspect the ingredients. An `Expression<Func<int, bool>>` is the recipe card — you can read each step, translate it into another language (SQL), or modify it before cooking (compiling).

This is the foundation of: **EF Core LINQ-to-SQL, dynamic query builders, Specification Pattern, OData filters, and runtime code generation**.

---

## 2. Deep Explanation

### Func<T> vs Expression<Func<T>>

```csharp
// This IS compiled C# code — a delegate pointing to IL instructions
Func<int, bool> isEven = x => x % 2 == 0;

// This is a DATA STRUCTURE describing the lambda — NOT compiled code
Expression<Func<int, bool>> isEvenExpr = x => x % 2 == 0;
```

When the compiler sees `Expression<Func<...>>`, it does NOT emit a delegate. Instead, it generates code that builds a tree of `System.Linq.Expressions` objects at runtime:

```
BinaryExpression (Equal)
├── BinaryExpression (Modulo)
│   ├── ParameterExpression ("x")
│   └── ConstantExpression (2)
└── ConstantExpression (0)
```

### Why EF Core Needs Expression Trees

When you write:
```csharp
dbContext.Users.Where(u => u.Age > 18 && u.IsActive);
```

EF Core receives an `Expression<Func<User, bool>>`. It walks the tree node by node:
- `u.Age > 18` → `WHERE Age > 18`
- `&&` → `AND`
- `u.IsActive` → `IsActive = 1`

It cannot do this with a compiled `Func<User, bool>` because compiled IL is opaque machine instructions.

### Key Expression Types

| Node Type | Example | SQL Equivalent |
|-----------|---------|----------------|
| `BinaryExpression` | `x + y`, `a && b` | `+`, `AND` |
| `MemberExpression` | `u.Name` | Column reference |
| `ConstantExpression` | `42`, `"hello"` | Literal value |
| `MethodCallExpression` | `s.Contains("test")` | `LIKE '%test%'` |
| `ParameterExpression` | `u =>` | Table alias |
| `UnaryExpression` | `!x` | `NOT` |
| `ConditionalExpression` | `x > 0 ? "a" : "b"` | `CASE WHEN` |

### Compiling an Expression Tree

You can convert an expression tree back into executable code:
```csharp
Expression<Func<int, int>> expr = x => x * 2;
Func<int, int> compiled = expr.Compile(); // Now it's a real delegate
int result = compiled(5); // 10
```

---

## 3. Code Examples

### Example 1 — Basic: Inspecting an Expression Tree
```csharp
Expression<Func<int, bool>> expr = x => x > 10;

// Walk the tree
var body = (BinaryExpression)expr.Body;
Console.WriteLine($"Left:     {body.Left}");       // x
Console.WriteLine($"Operator: {body.NodeType}");    // GreaterThan
Console.WriteLine($"Right:    {body.Right}");       // 10
Console.WriteLine($"Type:     {body.Type}");        // System.Boolean
```

### Example 2 — Real-World: Dynamic Query Builder (Specification Pattern)
```csharp
// Building WHERE clauses dynamically based on user filters
public static class PredicateBuilder
{
    // Start with a predicate that always returns true
    public static Expression<Func<T, bool>> True<T>() =>
        Expression.Lambda<Func<T, bool>>(Expression.Constant(true),
            Expression.Parameter(typeof(T), "x"));

    // Combine two expressions with AND
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));

        // Replace both expressions' parameters with a shared one
        var combined = Expression.AndAlso(
            Expression.Invoke(left, parameter),
            Expression.Invoke(right, parameter));

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}

// Usage in a search API
public IQueryable<Order> SearchOrders(OrderFilter filter)
{
    var predicate = PredicateBuilder.True<Order>();

    if (filter.MinAmount.HasValue)
        predicate = predicate.And(o => o.Amount >= filter.MinAmount.Value);

    if (!string.IsNullOrEmpty(filter.CustomerName))
        predicate = predicate.And(o => o.CustomerName.Contains(filter.CustomerName));

    return _dbContext.Orders.Where(predicate); // Translates to SQL!
}
```

### Example 3 — Real-World: Sorting by Dynamic Property Name
```csharp
// User clicks "sort by Name" in the UI — property name comes as a string
public static IQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string propertyName)
{
    var parameter = Expression.Parameter(typeof(T), "x");
    var property = Expression.Property(parameter, propertyName);
    var lambda = Expression.Lambda(property, parameter);

    var methodCall = Expression.Call(
        typeof(Queryable), "OrderBy",
        new[] { typeof(T), property.Type },
        source.Expression, Expression.Quote(lambda));

    return source.Provider.CreateQuery<T>(methodCall);
}

// Usage
var sorted = dbContext.Products.OrderByProperty("Price"); // WHERE clause is built at runtime
```

---

## 4. Interview Questions

1. **What is an Expression Tree in C#?**
   *An Expression Tree is a data structure that represents code as a tree of objects. Instead of compiling a lambda into IL, the C# compiler generates a tree of `Expression` nodes that describe the lambda's structure — its parameters, operators, and values. This allows frameworks like EF Core to inspect the C# code and translate it into SQL at runtime.*

2. **What is the difference between `Func<T, bool>` and `Expression<Func<T, bool>>`?**
   *`Func<T, bool>` is a compiled delegate — it's executable IL code. `Expression<Func<T, bool>>` is an object tree that describes the logic. You can inspect its nodes, translate it to SQL, or compile it back into a delegate with `.Compile()`. EF Core requires `Expression` to generate SQL; if you pass a `Func`, it downloads the entire table and filters in memory (client evaluation).*

3. **Why can't you use arbitrary C# methods inside an EF Core LINQ query?**
   *EF Core walks the Expression Tree node by node and translates each node into a SQL equivalent. If it encounters a `MethodCallExpression` for a custom C# method like `MyHelper.Format(x)`, it has no idea what SQL to generate. It throws a "could not be translated" exception. Only methods EF Core recognizes (like `string.Contains`, `DateTime.AddDays`) have registered SQL translations.*

4. **How would you build a dynamic search filter using Expression Trees?**
   *I would use a PredicateBuilder pattern. Start with a base `Expression<Func<T, bool>>` that returns true. For each filter condition the user provides, dynamically compose an `AndAlso` binary expression combining the existing predicate with the new condition. The result is a single expression tree that EF Core translates into one optimized SQL WHERE clause.*

5. **Can you modify an Expression Tree after it's created?**
   *Expression Trees are immutable. Once an `Expression` node is created, you cannot change it. To "modify" one, you must build a new tree, typically using an `ExpressionVisitor` that walks the original tree and replaces specific nodes while reconstructing the rest.*

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These test whether you understand the EF Core translation boundary.*

- What does `AsEnumerable()` do in the middle of an EF Core LINQ chain, and why is it dangerous?
  *(It switches from `IQueryable` (Expression Tree, server-side) to `IEnumerable` (compiled delegate, client-side). Everything after it runs in C# memory, pulling the entire dataset from the database first.)*
- Can you use `Expression<Func<>>` with `async` lambdas?
  *(No. Async lambdas cannot be converted to expression trees because the state machine generated by async is not representable as expression nodes.)*
- How does `ExpressionVisitor` work?
  *(It's a Visitor pattern implementation. You override methods like `VisitBinary` or `VisitMember`. The visitor walks every node in the tree, calling your overrides, and reconstructs a new tree with any modifications you make.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Passing Func<> to IQueryable.Where() — triggers CLIENT EVALUATION
IQueryable<User> users = dbContext.Users;
Func<User, bool> filter = u => u.IsActive; // Compiled delegate!
var active = users.Where(filter).ToList(); 
// ⚠️ This downloads ALL users and filters in C# memory!
// FIX: Use Expression<Func<User, bool>> filter = u => u.IsActive;

// MISTAKE 2: Calling C# methods inside IQueryable chains
var result = dbContext.Orders
    .Where(o => FormatCurrency(o.Amount) == "$100.00") // ❌ Cannot translate!
    .ToList();
// FIX: Filter on the raw value, format AFTER materialisation
var result = dbContext.Orders
    .Where(o => o.Amount == 100.00m)
    .ToList()
    .Select(o => FormatCurrency(o.Amount)); // ✅ Runs in C#

// MISTAKE 3: Closure capture in Expression Trees
string name = "Sai";
Expression<Func<User, bool>> expr = u => u.Name == name;
// The compiler creates a MemberExpression pointing to a closure field, NOT a ConstantExpression.
// EF Core handles this by parameterising it (SELECT ... WHERE Name = @p0).
// This is actually GOOD — it prevents SQL injection and enables query plan caching.
```

---

## 7. Real-World Usage

| Scenario | Expression Tree Usage |
|----------|----------------------|
| **EF Core** | Every `.Where()`, `.Select()`, `.OrderBy()` on `DbSet<T>` |
| **OData** | Translates `$filter=Price gt 100` into Expression Trees |
| **Specification Pattern** | Composable business rules as expressions |
| **AutoMapper** | `ProjectTo<T>()` builds projection expressions |
| **Dynamic Sorting** | Runtime `OrderBy("columnName")` using reflection + expressions |
| **GraphQL** | HotChocolate translates GraphQL filters into EF expressions |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | `Func` vs `Expression<Func>`, why EF Core needs expressions |
| **Level 2** | Tree node types, `.Compile()`, the client evaluation trap |
| **Level 3** | `ExpressionVisitor`, PredicateBuilder, dynamic query construction |
| **Level 4** | Custom `IMethodCallTranslator` for EF Core, AOT implications |

---

## 🔗 Connected Topics

- [LINQ](./21-linq.md) — LINQ operators on `IQueryable<T>` use expression trees internally
- [Delegates](./19-delegates.md) — `Func<T>` is the compiled form; `Expression<Func<T>>` is the data form
- [Generics](./18-generics.md) — Expression Trees are heavily generic (`Expression<TDelegate>`)
- [Reflection](../04-advanced/37-reflection.md) — Expressions use `PropertyInfo`, `MethodInfo` references

> 🎯 **Interviewer Mindset Note:** The classic chain is: *"What's IQueryable vs IEnumerable?" → "How does EF Core translate LINQ to SQL?" → "What is an Expression Tree?" → "Build me a dynamic filter."* Be ready.

---

*Created: May 2026 · Level: Intermediate*
