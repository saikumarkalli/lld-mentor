# Chapter 26 — Tuples & Deconstruction

> **⚡ Core Idea (30 seconds):** A tuple is a lightweight, unnamed data structure that groups multiple values together without creating a class. `ValueTuple` (C# 7+) lives on the stack (no heap allocation), supports named elements, and can be deconstructed into individual variables with `var (x, y) = ...`.

**Domain:** `C#` **Level:** `Intermediate` **Tags:** `#tuples` `#valuetuple` `#deconstruction` `#pattern-matching`

---

## 1. Core Idea

Think of a tuple like a **quick sticky note** — you scribble two pieces of information together ("John", 42) and pass it along. You don't need a formal letter (a full class) just to pass a name and an age between two methods.

This is the foundation of: **multiple return values, LINQ projections, pattern matching positional patterns, and eliminating trivial DTO classes**.

---

## 2. Deep Explanation

### Old Tuple (System.Tuple) vs New Tuple (System.ValueTuple)

| Feature | `Tuple<T1, T2>` (.NET 4.0) | `ValueTuple` (C# 7+) |
|---------|---------------------------|----------------------|
| Type | Reference type (heap) | Value type (stack) |
| Elements | `Item1`, `Item2` (unnamed) | Named elements |
| Syntax | `Tuple.Create(1, "a")` | `(1, "a")` |
| Mutability | Immutable | Mutable (fields, not properties) |
| Performance | Heap allocation + GC pressure | Zero allocation |

### Named Tuple Elements

```csharp
// Unnamed — Item1, Item2 are meaningless
(string, int) person = ("Sai", 30);
Console.WriteLine(person.Item1); // "Sai" — unclear what Item1 means

// Named — self-documenting
(string Name, int Age) person = ("Sai", 30);
Console.WriteLine(person.Name); // "Sai" — clear intent
```

### Deconstruction

```csharp
// Deconstructing a tuple into variables
var (name, age) = GetPerson();

// Works in foreach
foreach (var (key, value) in dictionary) { ... }

// Works with discards
var (_, age) = GetPerson(); // Don't need the name
```

### Custom Deconstruct Method

Any class can support deconstruction by adding a `Deconstruct` method:

```csharp
public class Address
{
    public string City { get; set; }
    public string Country { get; set; }

    public void Deconstruct(out string city, out string country)
    {
        city = City;
        country = Country;
    }
}

// Now this works:
var (city, country) = new Address { City = "Hyderabad", Country = "India" };
```

### Tuples as Return Types

```csharp
// ❌ Old way: Create a class just to return two values
public class DivisionResult { public int Quotient; public int Remainder; }

// ✅ Modern: Named tuple return
public (int Quotient, int Remainder) Divide(int a, int b)
    => (a / b, a % b);

var result = Divide(17, 5);
Console.WriteLine($"{result.Quotient} R {result.Remainder}"); // 3 R 2
```

---

## 3. Code Examples

### Example 1 — Multiple Return Values
```csharp
public class UserService
{
    // ❌ Bad: out parameters are clunky
    public bool TryGetUser_Bad(int id, out string name, out string email)
    {
        name = "Sai"; email = "sai@example.com"; return true;
    }

    // ✅ Good: Named tuple return
    public (bool Found, string Name, string Email) TryGetUser(int id)
    {
        var user = _db.Find(id);
        return user is not null
            ? (true, user.Name, user.Email)
            : (false, "", "");
    }
}

// Usage with deconstruction
var (found, name, _) = service.TryGetUser(42);
if (found) Console.WriteLine(name);
```

### Example 2 — LINQ Projections with Tuples
```csharp
// Instead of creating an anonymous type or a DTO
var summary = orders
    .GroupBy(o => o.CustomerId)
    .Select(g => (CustomerId: g.Key, Total: g.Sum(o => o.Amount), Count: g.Count()))
    .OrderByDescending(x => x.Total)
    .ToList();

foreach (var (customerId, total, count) in summary)
    Console.WriteLine($"Customer {customerId}: {count} orders, {total:C}");
```

### Example 3 — Pattern Matching with Positional Patterns
```csharp
public record Point(int X, int Y);

string Classify(Point p) => p switch
{
    (0, 0) => "Origin",
    (0, _) => "Y-axis",
    (_, 0) => "X-axis",
    (> 0, > 0) => "Quadrant I",
    (< 0, > 0) => "Quadrant II",
    _ => "Other"
};
```

---

## 4. Interview Questions

1. **What is the difference between Tuple and ValueTuple?**
   *`Tuple<T1, T2>` is a reference type from .NET 4.0 — it allocates on the heap, uses unnamed `Item1`/`Item2` properties, and creates GC pressure. `ValueTuple` (C# 7+) is a value type on the stack, supports named elements, and has zero allocation overhead. Always prefer `ValueTuple`.*

2. **When should you use a tuple vs a class/record?**
   *Tuples are ideal for private, short-lived groupings — internal method returns, LINQ projections, local calculations. For public API boundaries (controller responses, service interfaces), use a named record or class for readability and maintainability.*

3. **What is deconstruction and how do you enable it for custom types?**
   *Deconstruction is unpacking a value into individual variables with `var (a, b) = value`. Tuples and records support it automatically. For regular classes, add a `public void Deconstruct(out T1 x, out T2 y)` method.*

4. **Are ValueTuples mutable?**
   *Yes — `ValueTuple` fields are mutable, which means you can write `person.Age = 31`. This is unusual for value types and can be confusing. If you need immutability, use a `readonly record struct` instead.*

5. **How do named tuple elements work at the IL level?**
   *Named elements are a compiler fiction. At the IL level, they are still `Item1`, `Item2`, etc. The names are preserved via `TupleElementNames` attributes for tooling and reflection, but the runtime doesn't enforce them.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using tuples in public APIs
public (string, int, bool) GetUser(int id) { ... } // ❌ Callers see Item1, Item2, Item3
// FIX: Use a record or class for public contracts

// MISTAKE 2: Tuple equality is structural
var a = (Name: "Sai", Age: 30);
var b = (Name: "Sai", Age: 30);
Console.WriteLine(a == b); // true! Element names don't affect equality

// MISTAKE 3: Mutating a tuple stored in a variable
var point = (X: 0, Y: 0);
point.X = 5; // ✅ This works — ValueTuple fields are mutable
// But if stored in a readonly field, mutation is blocked

// GOTCHA: Tuple names are erased across assemblies
// If library returns (string Name, int Age) and consumer expects (string N, int A)
// — it still works because names are compile-time only
```

---

## Connected Topics

- [Value vs Reference Types](../01-beginner/01-value-vs-reference-types.md) — ValueTuple is a value type (stack)
- [Records](../04-advanced/36-records-structs.md) — Records are the named, immutable alternative to tuples
- [Pattern Matching](../04-advanced/35-pattern-matching.md) — Positional patterns use Deconstruct
- [LINQ](./21-linq.md) — Tuples in Select projections

---

*Created: May 2026 · Level: Intermediate*
