# Chapter 9 — Collection Interfaces

> **⚡ Core Idea (30 seconds):** .NET has a hierarchy of collection interfaces — `IEnumerable<T>` → `ICollection<T>` → `IList<T>`. Each adds more capability. The rule: **accept the most abstract interface** your method needs, **return the most specific type** you can. This gives callers maximum power while keeping your API flexible.

**Domain:** `C#` **Level:** `Beginner` **Tags:** `#collections` `#interfaces` `#ienumerable` `#ilist`

---

## 1. Core Idea

Think of it like **credit cards**. A store that accepts "any card" (IEnumerable) gets the most customers. A store that only accepts "Visa Platinum with rewards" (IList) limits who can shop there. As a method author, accept the cheapest card (most abstract interface) that still lets you do your job.

This is the foundation of: **LINQ (needs IEnumerable), API design, dependency inversion, and preventing unnecessary coupling to concrete collection types**.

---

## 2. Deep Explanation

### The Interface Hierarchy

```
IEnumerable<T>          → Can iterate (foreach)
  └── ICollection<T>    → Can count, add, remove
        └── IList<T>    → Can access by index [i]

IReadOnlyCollection<T>  → Can iterate + count (no mutation)
  └── IReadOnlyList<T>  → Can iterate + count + index (no mutation)
```

### What Each Interface Gives You

| Interface | foreach | Count | Add/Remove | Index [i] | Mutation |
|-----------|:-------:|:-----:|:----------:|:---------:|:--------:|
| `IEnumerable<T>` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `ICollection<T>` | ✅ | ✅ | ✅ | ❌ | ✅ |
| `IList<T>` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `IReadOnlyCollection<T>` | ✅ | ✅ | ❌ | ❌ | ❌ |
| `IReadOnlyList<T>` | ✅ | ✅ | ❌ | ✅ | ❌ |

### The Golden Rule

```csharp
// ❌ Bad: Demanding a specific type restricts callers
public void Process(List<Order> orders) { ... }

// ✅ Good: Accept the most abstract interface you need
public void Process(IEnumerable<Order> orders) { ... }  // If you only iterate
public void Process(IReadOnlyList<Order> orders) { ... } // If you need count + index
```

### IEnumerable<T> — The Universal Interface

Every collection implements `IEnumerable<T>`. This is the **only interface LINQ requires**. If your method just iterates with `foreach`, accept `IEnumerable<T>`.

**Danger:** `IEnumerable<T>` can be lazy (deferred). Calling `.Count()` on a deferred query triggers full evaluation. If you need the count, accept `IReadOnlyCollection<T>` instead.

### IReadOnlyCollection<T> and IReadOnlyList<T>

These are the **modern best practice** for return types when you don't want callers mutating the collection. `List<T>` implements both `IList<T>` and `IReadOnlyList<T>`.

```csharp
// ✅ Good: Return a read-only view — callers can't accidentally .Add() or .Remove()
public IReadOnlyList<Order> GetOrders() => _orders.AsReadOnly();
```

---

## 3. Code Examples

### Example 1 — Parameter Type Choice
```csharp
public class OrderService
{
    // ❌ Bad: Accepts only List<T> — cannot pass arrays, HashSets, or LINQ results
    public decimal CalculateTotal_Bad(List<Order> orders)
    {
        return orders.Sum(o => o.Amount);
    }

    // ✅ Good: Accepts any iterable — arrays, lists, HashSets, LINQ queries all work
    public decimal CalculateTotal_Good(IEnumerable<Order> orders)
    {
        return orders.Sum(o => o.Amount);
    }

    // ✅ Good: When you need Count without triggering evaluation
    public string GetSummary(IReadOnlyCollection<Order> orders)
    {
        return $"{orders.Count} orders totalling {orders.Sum(o => o.Amount):C}";
    }
}
```

### Example 2 — Return Type Choice
```csharp
public class OrderRepository
{
    private readonly List<Order> _orders = new();

    // ❌ Bad: Exposes internal list — callers can .Add() or .Clear() your data!
    public List<Order> GetOrders_Bad() => _orders;

    // ✅ Good: Returns read-only view — data is safe from mutation
    public IReadOnlyList<Order> GetOrders_Good() => _orders.AsReadOnly();
}
```

---

## 4. Interview Questions

1. **What is the difference between IEnumerable, ICollection, and IList?**
   *`IEnumerable<T>` provides only iteration via `foreach`. `ICollection<T>` adds `Count`, `Add`, `Remove`, and `Contains`. `IList<T>` adds indexed access with `[i]`, `Insert`, and `RemoveAt`. Each level adds more capability but restricts which types the caller can pass.*

2. **When should a method accept IEnumerable<T> vs IList<T>?**
   *Accept `IEnumerable<T>` if you only iterate. Accept `IReadOnlyList<T>` if you need indexed access or count. Accept `IList<T>` only if you need to mutate the collection. The more abstract your parameter type, the more flexible your API.*

3. **What is IReadOnlyCollection<T> and when do you use it?**
   *It provides `Count` and iteration but no mutation. I use it when I need to know the size without triggering deferred execution (unlike `IEnumerable<T>.Count()`) and I want to guarantee callers that the method won't modify their data.*

4. **Why shouldn't you return List<T> from a public API method?**
   *Returning `List<T>` exposes mutation methods (`Add`, `Clear`, `Remove`) to callers, who might accidentally corrupt your internal state. Return `IReadOnlyList<T>` or `IReadOnlyCollection<T>` instead to protect encapsulation.*

5. **What happens if you call .Count() on an IEnumerable<T> that is a deferred LINQ query?**
   *It forces full evaluation of the query — iterating every element to count them. If the source is a database query, it executes the SQL. Then if you iterate again (e.g., `foreach`), it evaluates a second time. This is the multiple-enumeration bug.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Accepting List<T> when IEnumerable<T> suffices
public void Print(List<string> items) { ... } // ❌ Can't pass arrays or LINQ results
public void Print(IEnumerable<string> items) { ... } // ✅

// MISTAKE 2: Returning List<T> exposing mutability
public List<User> GetUsers() => _users; // ❌ Caller can _users.Clear()!
public IReadOnlyList<User> GetUsers() => _users.AsReadOnly(); // ✅

// MISTAKE 3: Calling Count() on IEnumerable multiple times
public void Process(IEnumerable<Order> orders)
{
    Console.WriteLine(orders.Count()); // Evaluates query
    foreach (var o in orders) { ... }  // ❌ Evaluates AGAIN!
}
// FIX: Materialise once or accept IReadOnlyCollection<T>
```

---

## 🔗 Connected Topics

- [Collections Overview](./08-collections-overview.md) — Concrete types that implement these interfaces
- [LINQ](../03-intermediate/21-linq.md) — LINQ operates on `IEnumerable<T>` and `IQueryable<T>`
- [Generics](../03-intermediate/18-generics.md) — All collection interfaces are generic
- [Encapsulation](../02-oop/12-encapsulation.md) — Returning read-only interfaces protects internal state

---

*Created: May 2026 · Level: Beginner*
