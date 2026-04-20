# Records, Structs & Immutability

> **⚡ Core Idea (30 seconds):** `record` types give you **value-based equality, immutability, and `with` expressions** with minimal boilerplate. They solve the "this DTO needs proper equality" problem. Know when to use `record class`, `record struct`, `class`, and `struct` — interviewers love this comparison.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#records` `#immutability` `#value-equality` `#struct` `#c#9`

---

## 1. Core Idea

`class`: reference equality (`obj1 == obj2` checks if same object), mutable by default.
`record`: value equality (`obj1 == obj2` checks if same *data*), immutable by default.
`struct`: value type, copied on assignment, value equality.
`record struct` (C# 10): value type + value equality + `with` expressions.

---

## 2. Deep Explanation

### What the Compiler Generates for `record`

A `record class` is a class with auto-generated:
- **`Equals()`** — compares all properties by value
- **`GetHashCode()`** — based on all properties
- **`==` / `!=` operators** — delegates to `Equals()`
- **`ToString()`** — readable: `Order { Id = 1, Total = 99.99 }`
- **`with` expression support** — positional or property-based copy
- **Deconstructor** — for positional records

```csharp
// Positional record — concise, immutable
public record Order(int Id, string CustomerName, decimal Total);

// Behind the scenes, compiler generates:
// - public int Id { get; init; }
// - Equals, GetHashCode, == , !=
// - ToString: "Order { Id = 1, CustomerName = Alice, Total = 99.99 }"
// - public void Deconstruct(out int Id, out string CustomerName, out decimal Total)
```

### `init` Accessor — Immutable After Construction

```csharp
public class Product
{
    public int Id { get; init; }   // Set in constructor or object initializer only
    public string Name { get; init; } = "";
}

var p = new Product { Id = 1, Name = "Widget" };
p.Name = "Other"; // ❌ Compile error — init-only
```

### `with` Expression — Non-Destructive Mutation

```csharp
var original = new Order(1, "Alice", 99.99m);
var updated = original with { Total = 149.99m }; // New instance with modified Total
// original is unchanged — immutable

// Works on classes, records, and record structs
```

### When to Use Each

| Type | Value Equality | Mutable | Heap | Use When |
|------|:---:|:---:|:---:|---------|
| `class` | ❌ (ref) | ✅ | ✅ | Domain entities, services, large objects |
| `record class` | ✅ | ⚠️ (init) | ✅ | DTOs, Events, Value Objects |
| `struct` | ✅ (if implemented) | ✅ | ❌ | Small, perf-critical value types |
| `record struct` | ✅ | ✅/❌ | ❌ | Small immutable value types (C# 10) |
| `readonly struct` | ❌ (unless override) | ❌ | ❌ | Small, immutable, high-perf types |

---

## 3. Code Examples

### Basic — Record vs Class Equality
```csharp
// Class: reference equality
public class OrderClass { public int Id { get; set; } }
var o1 = new OrderClass { Id = 1 };
var o2 = new OrderClass { Id = 1 };
Console.WriteLine(o1 == o2); // False — different objects!

// Record: value equality
public record OrderRecord(int Id, string Name);
var r1 = new OrderRecord(1, "Alice");
var r2 = new OrderRecord(1, "Alice");
Console.WriteLine(r1 == r2); // True — same data!

// with expression
var r3 = r1 with { Name = "Bob" };
Console.WriteLine(r1); // OrderRecord { Id = 1, Name = Alice }
Console.WriteLine(r3); // OrderRecord { Id = 1, Name = Bob }
```

### Real-World — DTOs, Domain Events & Value Objects
```csharp
// DTOs — records are perfect
public record CreateOrderRequest(
    string CustomerName,
    IReadOnlyList<OrderLineDto> Lines);

public record OrderLineDto(int ProductId, int Quantity);

// Domain events — immutable, value equality useful in tests
public record OrderPlacedEvent(
    Guid OrderId,
    string CustomerEmail,
    decimal Total,
    DateTime OccurredAt = default) // Default value in positional record
{
    public DateTime OccurredAt { get; init; } = OccurredAt == default
        ? DateTime.UtcNow : OccurredAt;
}

// Value Object in DDD — Money (struct appropriate here, or record struct)
public readonly record struct Money(decimal Amount, string Currency)
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency) throw new InvalidOperationException("Currency mismatch");
        return this with { Amount = Amount + other.Amount };
    }

    public static Money Zero(string currency) => new(0, currency);
}

// Usage
var price = new Money(10.00m, "USD");
var tax = new Money(1.50m, "USD");
var total = price.Add(tax); // Money { Amount = 11.50, Currency = USD }
Console.WriteLine(price == tax);  // False — value equality
```

---

## 4. Interview Questions

1. **What is a `record` in C# and how does it differ from a `class`?**
2. **What is the `with` expression and when is it useful?**
3. **What is the `init` accessor? How is it different from `set`?**
4. **When would you use a `record struct` vs a `record class`?**
5. **Can a `record` be mutable? How?**

---

## 5. Follow-up Questions

- If a `record` contains a `List<T>`, does equality check the list contents or the reference?
  *(The reference — records use default equality for each property. `List<T>` equality is reference-based unless you override.)*
- What does `sealed` on a record do?
  *(Prevents inheritance — also makes equality more predictable, no inheritance chain confusion)*
- Can records inherit from other records?
  *(Yes — `record` can inherit from `record`. But `record` CANNOT inherit from a non-record `class`. A `class` can only inherit from a `class`.)*
- What is the performance difference between `record class` and `record struct`?
  *(record class = heap allocation + GC. record struct = stack/inline, no GC.)*
- Can a `readonly record struct` have mutable methods? *(No — all methods are implicitly readonly)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Expecting deep equality on reference-type properties
public record Cart(List<Item> Items);
var c1 = new Cart(new List<Item> { new Item(1) });
var c2 = new Cart(new List<Item> { new Item(1) });
Console.WriteLine(c1 == c2); // ❌ False! Lists compared by reference

// FIX: Use ImmutableList<T> or override Equals manually

// MISTAKE 2: Mutating a "positional" record through the back door
public record Config(string Host, int Port);
// Records are immutable via init properties — but you CAN add mutable properties:
public record Config(string Host, int Port)
{
    public string? Tag { get; set; } // Mutable! Mixed — confusing
}

// MISTAKE 3: Record struct boxing — ref struct concerns
record struct Point(int X, int Y); // No ref struct restriction, but can be boxed
// For hot-path usage, readonly record struct is preferable

// MISTAKE 4: Using class for DTOs and getting false equality failures in tests
var expected = new ResponseDto { Id = 1 }; // class
var actual = new ResponseDto { Id = 1 };
Assert.Equal(expected, actual); // ❌ Fails without overriding Equals
// Use record — built-in value equality fixes this
```

---

## 7. Real-World Usage

| Scenario | Type |
|----------|------|
| API request/response DTOs | `record class` |
| Domain events | `record class` (immutable, value equality) |
| Value Objects (Money, Address) | `readonly record struct` |
| Database entities | `class` (mutable, EF Core tracked by reference) |
| Small math types (Point, Color) | `record struct` or `readonly struct` |
| Configuration snapshots | `record class` with `init` properties |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | record syntax, value equality, with expression |
| **Level 2** | init accessor, record vs class, when to use each |
| **Level 3** | record struct, equality on reference-type members, record inheritance |
| **Level 4** | Compiler-generated IL, positional deconstruct, ImmutableCollections with records |

## 🔗 Connected Topics
- [Pattern Matching](./pattern-matching.md) — records + switch expressions = discriminated unions
- [Value vs Reference Types](../01-beginner/value-vs-reference-types.md) — record struct vs record class
- [Garbage Collection](../04-expert/garbage-collection.md) — record struct avoids GC

*Created: April 2026 · Level: Advanced*
