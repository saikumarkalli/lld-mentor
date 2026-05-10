# Chapter 23 � Extension Methods

> **⚡ Core Idea (30 seconds):** Extension methods let you **add methods to existing types without modifying them** — not via inheritance, not via a wrapper. They're syntactic sugar: the compiler turns `myString.IsNullOrEmpty()` into `StringExtensions.IsNullOrEmpty(myString)`. All of LINQ is built this way.

**Domain:** `C#` **Level:** `Intermediate` **Tags:** `#extension-methods` `#linq` `#fluent` `#static`

---

## 1. Core Idea

You can't modify `string` — it's in `System`. But you can write `IsNullOrWhiteSpace()` once and call it like it's built into `string`. Extension methods are the feature that makes LINQ possible and fluent APIs clean.

The rule: define a `static` method in a `static` class with `this` as the first parameter type.

---

## 2. Deep Explanation

### How They Work (Compilation)

```csharp
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value)
        => string.IsNullOrEmpty(value);
}

// Call site — looks like an instance method:
"hello".IsNullOrEmpty(); // false

// What the compiler ACTUALLY generates:
StringExtensions.IsNullOrEmpty("hello"); // Identical IL
```

There is **zero runtime overhead**. It's purely a compiler transformation.

### Precedence Rules

When there's a conflict between a real instance method and an extension method:
- **Instance method always wins**
- Extension method is only called if no instance method with that name/signature exists

### Chapter 23 � Extension Methods on Interfaces — The Power Move

This is how LINQ works. `Where`, `Select`, `OrderBy` — all extension methods on `IEnumerable<T>`:

```csharp
// You can extend ANY interface — even ones you don't own
public static class EnumerableExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class
        => source.Where(x => x is not null)!;
}

// Now works on any IEnumerable<T?>:
var names = new List<string?> { "Alice", null, "Bob" };
var valid = names.WhereNotNull(); // ["Alice", "Bob"]
```

---

## 3. Code Examples

### Basic — Utility Extensions
```csharp
public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string? s) => string.IsNullOrWhiteSpace(s);
    public static string TruncateAt(this string s, int maxLength)
        => s.Length <= maxLength ? s : s[..maxLength] + "...";
    public static string ToSlug(this string s)
        => s.ToLower().Replace(' ', '-').Replace("--", "-");
}

// Usage
string title = "Hello World  Extra Spaces";
Console.WriteLine(title.TruncateAt(10)); // "Hello Worl..."
Console.WriteLine(title.ToSlug());       // "hello-world--extra-spaces"
```

### Real-World — Fluent Builder API
```csharp
public class QueryBuilder
{
    private string _table = "";
    private readonly List<string> _conditions = new();
    private int? _limit;

    public string Build()
    {
        var sql = $"SELECT * FROM {_table}";
        if (_conditions.Any()) sql += $" WHERE {string.Join(" AND ", _conditions)}";
        if (_limit.HasValue) sql += $" LIMIT {_limit}";
        return sql;
    }
    // Properties exposed to extensions
    internal string Table { set => _table = value; }
    internal List<string> Conditions => _conditions;
    internal int? Limit { set => _limit = value; }
}

// Extension methods make it fluent — caller sees clean API
public static class QueryBuilderExtensions
{
    public static QueryBuilder From(this QueryBuilder qb, string table)
    { qb.Table = table; return qb; }

    public static QueryBuilder Where(this QueryBuilder qb, string condition)
    { qb.Conditions.Add(condition); return qb; }

    public static QueryBuilder Take(this QueryBuilder qb, int n)
    { qb.Limit = n; return qb; }
}

// Now reads like SQL:
var sql = new QueryBuilder()
    .From("Orders")
    .Where("Status = 'Active'")
    .Where("Total > 100")
    .Take(50)
    .Build();
```

---

## 4. Interview Questions

1. **What is an extension method? What are the three requirements to write one?**
   *An extension method lets you add methods to existing types without modifying them. Three requirements: (1) the class must be `static`, (2) the method must be `static`, (3) the first parameter must use the `this` keyword followed by the type you're extending: `public static bool IsEmail(this string s)`. The compiler turns `myString.IsEmail()` into `StringExtensions.IsEmail(myString)` — no runtime overhead.*

2. **Can you write an extension method on `null`? What happens if the first argument is null?**
   *Yes — extension methods can be called on `null` because they're just static method calls. `string? s = null; s.IsNullOrEmpty()` compiles and runs — the `this` parameter just receives `null`. Inside the method, you must null-check manually. This is actually useful: `string.IsNullOrEmpty(s)` can be written as `s.IsNullOrEmpty()` and both handle null gracefully.*

3. **What happens if an extension method has the same name as an existing instance method?**
   *The **instance method always wins**. The extension method is only considered if there's no instance method with a matching name and signature. This is by design — it prevents extension methods from accidentally overriding built-in behaviour. You'd need to call it as a static method directly to use the extension in such a conflict.*

4. **How does all of LINQ use extension methods?**
   *Every LINQ operator (`Where`, `Select`, `OrderBy`, `GroupBy`, etc.) is a static method in the `System.Linq.Enumerable` (for `IEnumerable<T>`) and `System.Linq.Queryable` (for `IQueryable<T>`) classes, with `this IEnumerable<T> source` as the first parameter. Import `using System.Linq` and all collections gain these methods. LINQ simply wouldn't exist without extension methods.*

5. **Can you write extension methods on sealed classes like `string`?**
   *Yes — that's one of the main use cases. `string` is sealed (can't inherit from it) and is owned by the framework (can't modify it), but you can freely add extension methods to it. All of `System.String` methods like `string.IsNullOrEmpty()` inspired the pattern of writing `myString.IsNullOrEmpty()` as an extension.*

---

## 5. Follow-up Questions

- Can you call an extension method on `null`?
  ```csharp
  string? s = null;
  s.IsNullOrEmpty(); // Works! 'this' parameter just receives null
  // Be careful: inside the method 's' is null — no NullReferenceException from the call site
  ```
- Can you write extension methods on generic types with constraints?
  ```csharp
  public static decimal Sum<T>(this IEnumerable<T> source, Func<T, decimal> selector)
      where T : class => source.Select(selector).Sum(); // Yes!
  ```
- Do extension methods participate in virtual dispatch?
  *(No — they're resolved at compile time by the declared type of the variable, not the runtime type)*
- What namespace must be imported to use extension methods?
  *(The namespace containing the static class defining them)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Putting ALL extensions in one giant static class
public static class Extensions { /* 500 methods */ }
// FIX: Split by domain
public static class StringExtensions { }
public static class DateTimeExtensions { }
public static class EnumerableExtensions { }

// MISTAKE 2: Mutating state in extensions on value types
public static void Clear(this List<int> list) { list.Clear(); } // OK — reference type
// For structs, mutation through extensions doesn't work as expected

// MISTAKE 3: Shadowing LINQ methods accidentally — very hard to debug
public static IEnumerable<T> Where<T>(this IEnumerable<T> src, bool always)
    => always ? src : Enumerable.Empty<T>(); // Compiles! But collides with LINQ's Where
// This won't override LINQ's Where(Func<T,bool>) — different signature — but still confusing
```

---

## 7. Real-World Usage

| Scenario | Extension Methods |
|----------|-----------------|
| All of LINQ | `Where`, `Select`, `OrderBy` on `IEnumerable<T>` |
| FluentValidation | `.NotEmpty()`, `.MaxLength(50)` on rule builder |
| ASP.NET Core DI | `services.AddSingleton<T>()`, `app.UseRouting()` |
| EF Core | `modelBuilder.Entity<T>()`, `.HasQueryFilter()` |
| AutoMapper | `mapper.Map<TDestination>()` |
| String utilities | Shared `StringExtensions` in project |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Syntax, static class, `this` keyword |
| **Level 2** | Extensions on interfaces (like LINQ), fluent patterns |
| **Level 3** | Null handling, precedence over instance methods, namespace resolution |
| **Level 4** | Extension methods vs default interface methods, generic constraints on extensions |

## 🔗 Connected Topics
- [LINQ](./21-linq.md) — entirely built on extension methods over `IEnumerable<T>`
- [Delegates](./19-delegates.md) — LINQ extensions accept `Func<T, bool>` delegates
- [Generics](./18-generics.md) — most extension methods are generic

*Created: April 2026 · Level: Intermediate*
