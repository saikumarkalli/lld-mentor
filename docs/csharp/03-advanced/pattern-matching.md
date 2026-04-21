# Pattern Matching

> **⚡ Core Idea (30 seconds):** Pattern matching lets you **test a value against a shape and extract it in one step**. C# has evolved from simple `is` checks to powerful switch expressions with positional, property, and relational patterns. It makes type-branching code concise, exhaustive, and readable.

**Domain:** `C#` **Level:** `Intermediate → Advanced` **Tags:** `#pattern-matching` `#switch` `#is` `#deconstruct` `#c#9` `#c#10`

---

## 1. Core Idea

Pattern matching combines **type checking + extraction + condition** into a single expression. Instead of:
```csharp
if (shape is Circle) { var c = (Circle)shape; return Math.PI * c.Radius * c.Radius; }
```
You write:
```csharp
if (shape is Circle { Radius: var r }) return Math.PI * r * r;
```

---

## 2. Deep Explanation

### Pattern Types by C# Version

**C# 7 — Type patterns and `when` guards:**
```csharp
object obj = GetValue();
switch (obj)
{
    case int n when n > 0: Console.WriteLine($"Positive int: {n}"); break;
    case string s: Console.WriteLine($"String: {s}"); break;
    case null: Console.WriteLine("Null"); break;
    default: Console.WriteLine("Other"); break;
}
```

**C# 8 — Switch expressions (no `break`, exhaustive):**
```csharp
string Describe(object obj) => obj switch
{
    int n when n > 0 => $"Positive: {n}",
    int n            => $"Non-positive: {n}",
    string s         => $"String: {s}",
    null             => "Null",
    _                => "Unknown"  // Discard pattern = default
};
```

**C# 9 — Relational, logical patterns:**
```csharp
string Category(int score) => score switch
{
    >= 90         => "A",
    >= 80 and < 90 => "B",
    >= 70 and < 80 => "C",
    < 70          => "F"
};
```

**C# 9-10 — Property patterns & combinators:**
```csharp
// Property pattern: check nested properties
bool IsEligible(Order order) => order is
{
    Status: OrderStatus.Active,
    Customer.Age: >= 18,
    Total: > 0
};

// Negation
bool IsNotNull(object? obj) => obj is not null;
```

**C# 10 — Extended property patterns:**
```csharp
// Deep property access without nesting
bool IsVip(Order order) => order is { Customer: { Tier: CustomerTier.Gold or CustomerTier.Platinum } };
```

**C# 11 — List patterns:**
```csharp
int[] arr = { 1, 2, 3 };
bool isFirstTwo = arr is [1, 2, ..]; // Starts with 1, 2 — slice pattern with ..
bool hasThree = arr is [_, _, _];    // Exactly 3 elements
```

---

## 3. Code Examples

### Basic — Replacing if/else chains
```csharp
// Old way — verbose
string GetShippingLabel(object item)
{
    if (item is PhysicalItem pi)
        return $"Ship to: {pi.Address}";
    else if (item is DigitalItem di && di.IsDownloadable)
        return $"Download link: {di.DownloadUrl}";
    else if (item is GiftCard gc)
        return $"Gift card code: {gc.Code}";
    else throw new InvalidOperationException("Unknown item type");
}

// Pattern matching — exhaustive switch expression
string GetShippingLabel(object item) => item switch
{
    PhysicalItem { Address: var addr }          => $"Ship to: {addr}",
    DigitalItem { IsDownloadable: true, DownloadUrl: var url } => $"Download: {url}",
    GiftCard { Code: var code }                 => $"Gift code: {code}",
    _ => throw new InvalidOperationException("Unknown item")
};
```

### Real-World — Discriminated Union / Result handling
```csharp
// Modeling an HTTP result as a union
public abstract record ApiResult<T>;
public record Success<T>(T Value) : ApiResult<T>;
public record NotFound<T>(string Message) : ApiResult<T>;
public record ValidationError<T>(IEnumerable<string> Errors) : ApiResult<T>;
public record ServiceError<T>(Exception Ex) : ApiResult<T>;

// Exhaustive pattern matching on the result
public IActionResult ToActionResult<T>(ApiResult<T> result) => result switch
{
    Success<T> { Value: var v }              => Ok(v),
    NotFound<T> { Message: var msg }         => NotFound(msg),
    ValidationError<T> { Errors: var errs }  => BadRequest(new { errors = errs }),
    ServiceError<T> { Ex: var ex }           => StatusCode(500, ex.Message),
    _ => throw new UnreachableException() // Compiler warns if not all cases handled (records)
};
```

---

## 4. Interview Questions

1. **What is pattern matching and how does it improve over `if`/`else` chains?**
   *Pattern matching combines type checking, extraction, and condition check into a single expression. Instead of `if (x is Foo) { var f = (Foo)x; ... }` you write `if (x is Foo { Name: var n })`. Switch expressions make type-branching **exhaustive** (compiler warns on unhandled cases), **concise** (no `break`, expression result), and **safe** (no cast exceptions). It eliminates the verbose cast-then-check boilerplate.*

2. **What is the difference between the `is` pattern and a switch expression?**
   *`is` is for a **single test** — check and optionally extract one value: `if (obj is Order { Total: > 100 } order)`. A switch expression handles **multiple mutually exclusive cases** on one input value: `shape switch { Circle c => ..., Rectangle r => ..., _ => ... }`. Switch expressions produce a value and can be exhaustive-checked by the compiler.*

3. **What is the discard pattern `_`?**
   *`_` is the **default/catch-all** pattern — it matches anything without binding it to a name. In a switch expression, `_ => value` is the "else" branch. In an `is` check, `_ => x` means "any remaining type I don't care about." It signals intent: I'm handling this case but don't need the value.*

4. **What are property patterns and how do they simplify property checks?**
   *A property pattern checks properties of an object inline: `if (order is { Status: OrderStatus.Active, Total: > 0 })`. Without it, you'd write two separate `&&` checks after a type cast. Property patterns can nest: `{ Customer: { Tier: CustomerTier.Gold } }` checks a nested property in one readable expression.*

5. **What does the compiler check for exhaustiveness in switch expressions?**
   *For **sealed class hierarchies** and **enums**, the compiler can statically enumerate all possible cases. If you don't handle all enum values or all subtypes of a sealed base, it emits a warning (CS8509). For open types like `object` or `string`, the compiler can't know all values, so it only warns if there's no default `_` arm. Adding `_ => throw new UnreachableException()` on open types is good defensive practice.*

---

## 5. Follow-up Questions

- When does the compiler warn you that a switch expression is not exhaustive?
  *(When the switch type is an enum or closed hierarchy of sealed/abstract types — not for open types like `string` or `object`)*
- What is the `when` guard and at what point is it evaluated relative to the pattern?
  *(Pattern must match first, then the `when` guard is evaluated — it's a secondary filter)*
- Can you use pattern matching in LINQ?
  ```csharp
  var circles = shapes.OfType<Circle>(); // Or:
  var circles = shapes.Where(s => s is Circle).Cast<Circle>();
  ```
- What is a **positional pattern** and what does it need to work?
  ```csharp
  var (x, y) = point; // Requires Deconstruct(out int x, out int y) method
  if (point is (0, 0)) // Positional pattern — origin check
  ```

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Non-exhaustive switch — runtime exception
int x = 42;
string result = x switch
{
    1 => "One",
    2 => "Two"
    // ❌ CS8509 warning: not exhaustive! All other values throw at runtime
};
// Always add: _ => "Other"

// MISTAKE 2: Order matters in switch expressions — first match wins
object obj = 5; // int
string r = obj switch
{
    int n      => "Int",
    int n when n > 0 => "Positive int" // ❌ Never reached! "int n" matches first
};
// FIX: Put more specific patterns first
string r2 = obj switch
{
    int n when n > 0 => "Positive int", // More specific first
    int n            => "Int",
    _                => "Other"
};

// MISTAKE 3: Property pattern doesn't call methods
if (order is { GetTotal(): > 100 }) { } // ❌ Can't call methods in property pattern
if (order is { Total: > 100 }) { }      // ✅ Properties only
```

---

## 7. Real-World Usage

| Scenario | Pattern Used |
|----------|-------------|
| Discriminated union (Result types) | Switch expression with record types |
| HTTP response handling | Switch on status code ranges |
| Parsing / tokenizing | Switch on token type and value |
| Domain event dispatch | Switch expression on event type |
| Null-safe navigation | `obj is not null` / `obj is SomeType { Prop: not null }` |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | `is` type check, simple `switch` patterns |
| **Level 2** | Switch expressions, property patterns, `when` guards |
| **Level 3** | Relational, logical, list patterns; exhaustiveness rules |
| **Level 4** | Positional patterns with `Deconstruct`, pattern matching + records for discriminated unions |

## 🔗 Connected Topics
- [Records & Structs](../03-advanced/records-structs.md) — Records work naturally with switch expressions
- [Nullable Types](../01-beginner/nullable-types.md) — `obj is not null` is modern null check
- [Generics](./generics.md) — `OfType<T>()` is LINQ's type pattern equivalent

*Created: April 2026 · Level: Intermediate → Advanced*
