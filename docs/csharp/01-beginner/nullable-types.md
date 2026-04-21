# Nullable Types & Null Safety

> **⚡ Core Idea (30 seconds):** Value types can't be null by default — `Nullable<T>` wraps them to add null capability. C# 8 extended this to reference types with nullable annotations (`?`) to eliminate `NullReferenceException` at compile time.

**Domain:** `C#` **Level:** `Beginner → Intermediate` **Tags:** `#null` `#nullable` `#safety` `#c#8`

---

## 1. Core Idea

`NullReferenceException` is called the "billion-dollar mistake" by its inventor (Tony Hoare). C# solves this in two ways:
1. **`Nullable<T>`** — lets value types represent "no value" (`int? x = null`)
2. **Nullable Reference Types (C# 8+)** — compiler warns you when you might dereference null

Think of nullable as putting a "maybe empty" wrapper around something that normally must have a value.

---

## 2. Deep Explanation

### `Nullable<T>` — How It Works Internally

`Nullable<T>` is a **generic struct** with two fields:
```csharp
public struct Nullable<T> where T : struct
{
    private readonly T _value;
    private readonly bool _hasValue;
}
```

When `_hasValue` is false, accessing `.Value` throws `InvalidOperationException`. The compiler sugar `int?` is just shorthand for `Nullable<int>`.

**Key behavior:**
- `int? x = null;` → `_hasValue = false`
- `x.HasValue` → false
- `x.Value` → throws if null
- `x.GetValueOrDefault()` → returns `0` (safe)
- `x ?? 0` → returns `0` if null (null-coalescing)

### Null-Conditional & Null-Coalescing Operators

```csharp
// Null-conditional: short-circuit on null
string? name = GetName(); // might be null
int? len = name?.Length;  // len is null if name is null — no NullReferenceException

// Null-coalescing: provide default
int length = name?.Length ?? 0;

// Null-coalescing assignment (C# 8)
name ??= "Anonymous"; // assign only if null
```

### Nullable Reference Types (C# 8+) — Compile-Time Safety

Enable in `.csproj`:
```xml
<Nullable>enable</Nullable>
```

Now the compiler tracks nullability:
```csharp
string name = null;    // ⚠️ Warning: cannot assign null to non-nullable
string? name2 = null;  // ✅ Fine — declared as nullable
Console.WriteLine(name2.Length); // ⚠️ Warning: possible null dereference
```

The compiler performs **flow analysis**:
```csharp
if (name2 != null)
{
    Console.WriteLine(name2.Length); // ✅ Safe — compiler knows it's not null here
}
```

### Null-forgiving operator `!`
```csharp
// Use ONLY when you know better than the compiler
string? config = GetConfig();
string definitelyNotNull = config!; // "Trust me, it's not null"
// Use sparingly — it defeats the purpose of null analysis
```

### When to Use
- **`int?`**: Database columns that can be null, optional settings, output parameters
- **`string?`**: Method parameters/returns that may legitimately have no value
- **Nullable enable**: On all new projects. Retrofitting old code needs care.

### When NOT to Use
- Don't use `Nullable<T?>` — it's not valid (double nullable)
- Don't suppress nullable warnings with `!` everywhere — that defeats the system
- Don't use `?` on value types just to get `null` when a sentinel value (like `-1`) would be semantically clearer

---

## 3. Code Examples

### Example 1 — Basic: Nullable Value Type in Real Scenario
```csharp
// Database-mapped entity — Age is optional in the DB
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Age { get; set; }  // NULL in DB = unknown age
    public DateTime? LastLogin { get; set; }
}

// Usage — safe pattern
User user = GetUser(id);
if (user.Age.HasValue)
{
    Console.WriteLine($"Age: {user.Age.Value}");
}
else
{
    Console.WriteLine("Age unknown");
}

// Or using null-coalescing
Console.WriteLine($"Age: {user.Age ?? -1}");

// Pattern matching (C# 9+) — cleanest approach
if (user.Age is int age)
{
    Console.WriteLine($"Age: {age}");
}
```

### Example 2 — Real-World: API Response Parsing with Null Safety
```csharp
#nullable enable

public class ProductService
{
    // Return null if not found — clearly communicated by ?
    public Product? GetById(int id)
    {
        return _repository.Find(id); // returns null if not found
    }

    public decimal GetDiscountedPrice(int productId, string? couponCode)
    {
        var product = GetById(productId);

        // Null-conditional chain — safe navigation
        decimal basePrice = product?.Price ?? throw new ArgumentException("Product not found");

        // couponCode might be null — handle it
        decimal discount = couponCode != null
            ? _couponService.GetDiscount(couponCode)
            : 0m;

        return basePrice - discount;
    }
}
```

---

## 4. Interview Questions

1. **What is `Nullable<T>` and how is it different from a regular nullable reference type?**
   *`Nullable<T>` (written as `int?`) is a **struct** that wraps a value type (`int`, `bool`, `DateTime`) to let it hold `null`. It has two internal fields: `_value` (the data) and `_hasValue` (a flag). A nullable reference type (`string?`) is different — it's purely a compile-time annotation. The reference itself can already be null at runtime; the `?` just tells the compiler to warn you about possible null dereferences.*

2. **What is the difference between `null` and `default` for a nullable value type?**
   *For `int? x`: `null` explicitly means "no value" (`_hasValue = false`). `default` also resolves to `null` for `int?` — both produce the same result. But for `int` (non-nullable), `default` is `0`. So for nullable types, `null` and `default` are equivalent; for non-nullable value types, `default` gives the zero value.*

3. **What does enabling `<Nullable>enable</Nullable>` actually do at runtime?**
   *Nothing at runtime — zero performance overhead. It is purely a **compile-time static analysis** feature. It adds attributes to your assembly metadata that tools can read, but the IL and runtime behaviour are unchanged. All null safety is enforced at build time via warnings and errors.*

4. **When would you use `??=` vs just `??`?**
   *`??` returns a default if the left side is null, but doesn't change the variable. `??=` **assigns** the default to the variable if it's null — it's a shorthand for `if (x == null) x = value`. Use `??=` when you want to initialise a nullable field lazily: `_cache ??= LoadCache();`*

5. **What is the null-forgiving operator `!` and when is it appropriate?**
   *The `!` operator (`name!`) tells the compiler "I know this might look nullable, but trust me — it won't be null here." It suppresses the nullable warning without any runtime effect. Use it **only** when you have external knowledge the compiler can't see — like after a framework-guaranteed initialisation (`[Required]` properties after model binding). Avoid using it to silence warnings you haven't actually fixed.*

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These separate developers who understand mechanics from those who memorize syntax.*

- `int? x = null; object o = x;` — Is `o` null or a boxed nullable? *(null — boxing a null Nullable<T> produces null)*
- Can you have `Nullable<Nullable<int>>`? Why not?
- Does enabling NRT (Nullable Reference Types) generate any IL/runtime difference?
- What is the difference between `T?` where `T : class` and `T?` where `T : struct`?
  *(Class: can be null reference. Struct: becomes `Nullable<T>`. Behavior differs!)*
- How do nullable annotations interact with generics?
  ```csharp
  T? GetFirst<T>(List<T> items); // What does T? mean here?
  ```
- In EF Core, how does nullable affect your schema generation?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Checking .HasValue instead of != null (both work but != null is idiomatic)
int? x = 5;
if (x.HasValue) { }  // verbose
if (x != null) { }   // cleaner, equivalent

// MISTAKE 2: Calling .Value without checking HasValue
int? age = null;
int years = age.Value; // InvalidOperationException! Use ?? or check first

// MISTAKE 3: Confusing NRT warnings with runtime null checks
// NRT is COMPILE-TIME analysis — you still need runtime guards for external data
string? input = Console.ReadLine(); // Might genuinely be null at runtime
Console.WriteLine(input!.Length);   // ! suppresses warning but STILL crashes if null

// MISTAKE 4: Forgetting that ? on reference types in non-nullable context
public string Name { get; set; } // Without NRT enabled: implicitly nullable
// Enable NRT and suddenly you have 200 warnings across your codebase!

// MISTAKE 5: Performance — Nullable<T> has overhead vs direct value type
// For extremely hot paths, null-checks via separate bool may be faster
```

---

## 7. Real-World Usage

| Scenario | How Nullability Applies |
|----------|------------------------|
| Entity Framework | Nullable columns map to `int?`, `DateTime?` etc. |
| API responses | `JsonSerializer` honors nullable annotations |
| Optional API params | `[FromQuery] int? pageSize = null` |
| Repository pattern | `GetById` returns `T?` — signaling not-found clearly |
| Configuration | `IConfiguration["key"]` returns `string?` — must handle null |
| gRPC generated code | Proto optional fields become nullable types in C# |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | `int?`, null check, `??` operator |
| **Level 2** | `?.`, `??=`, `Nullable<T>` struct internals, `HasValue` / `Value` |
| **Level 3** | NRT compile-time flow analysis, T? with generics, boxing behavior |
| **Level 4** | NRT + generics constraints, EF Core schema generation, serialization impact |

---

## 🔗 Connected Topics

- [Value vs Reference Types](./value-vs-reference-types.md) — Nullable<T> wraps value types
- [Pattern Matching](../02-intermediate/pattern-matching.md) — `if (x is int val)` is the modern null-safe check
- [Records & Structs](../03-advanced/records-structs.md) — `record struct` and nullable interactions

---

*Created: April 2026 · Level: Beginner → Intermediate*
