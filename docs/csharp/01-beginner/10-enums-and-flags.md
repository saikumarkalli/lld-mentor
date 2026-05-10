# Chapter 10 — Enums & Flags

> **⚡ Core Idea (30 seconds):** An `enum` is a named set of integer constants. Instead of using magic numbers like `status = 3`, you write `status = OrderStatus.Shipped`. The `[Flags]` attribute lets you combine enum values with bitwise operators for multi-select scenarios like file permissions.

**Domain:** `C#` **Level:** `Beginner` **Tags:** `#enum` `#flags` `#bitwise` `#constants`

---

## 1. Core Idea

Think of a regular enum like a **radio button group** — you pick exactly one option. Think of a `[Flags]` enum like **checkboxes** — you can select multiple options at once by combining them.

This is the foundation of: **state machines, permission systems, configuration options, API status codes, and domain modelling**.

---

## 2. Deep Explanation

### Basic Enum

```csharp
public enum OrderStatus
{
    Pending = 0,     // Default value
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
```

Under the hood, an enum is a **value type** backed by an `int` (default). You can change the underlying type:
```csharp
public enum StatusCode : byte  // Uses 1 byte instead of 4
{
    OK = 200,
    NotFound = 404  // ❌ Compile error! 404 > byte.MaxValue (255)
}
```

### Flags Enum (Bitwise Combinations)

```csharp
[Flags]
public enum FilePermissions
{
    None    = 0,         // 0000
    Read    = 1,         // 0001
    Write   = 1 << 1,    // 0010 (2)
    Execute = 1 << 2,    // 0100 (4)
    All     = Read | Write | Execute  // 0111 (7)
}

// Usage
var perms = FilePermissions.Read | FilePermissions.Write;  // 0011 (3)
bool canRead = perms.HasFlag(FilePermissions.Read);        // true
bool canExec = perms.HasFlag(FilePermissions.Execute);     // false
```

**Critical Rule:** `[Flags]` values must be powers of 2 (1, 2, 4, 8, 16...). If you use sequential values (1, 2, 3, 4), bitwise operations produce wrong results.

### Parsing and Conversion

```csharp
// String → Enum (safe parsing)
if (Enum.TryParse<OrderStatus>("Shipped", out var status))
    Console.WriteLine(status); // Shipped

// Int → Enum (dangerous! No validation)
var unknown = (OrderStatus)999; // Compiles! No runtime error!
bool isValid = Enum.IsDefined(typeof(OrderStatus), 999); // false
```

---

## 3. Code Examples

### Example 1 — Switch on Enum (Exhaustive Handling)
```csharp
// ✅ Good Practice: Switch expression ensures all values handled
public decimal CalculateShippingCost(OrderStatus status) => status switch
{
    OrderStatus.Pending => 0m,
    OrderStatus.Processing => 5.99m,
    OrderStatus.Shipped => 0m,
    OrderStatus.Delivered => 0m,
    OrderStatus.Cancelled => 0m,
    _ => throw new ArgumentOutOfRangeException(nameof(status))
};
```

### Example 2 — Flags for Role-Based Permissions
```csharp
[Flags]
public enum UserRole
{
    None       = 0,
    Viewer     = 1,
    Editor     = 1 << 1,   // 2
    Approver   = 1 << 2,   // 4
    Admin      = 1 << 3,   // 8
    SuperAdmin = Admin | Editor | Approver | Viewer  // 15
}

public class AuthService
{
    public bool HasPermission(UserRole userRoles, UserRole required)
    {
        return (userRoles & required) == required;
    }
}

// Usage
var user = UserRole.Viewer | UserRole.Editor;
var auth = new AuthService();
auth.HasPermission(user, UserRole.Viewer);   // true
auth.HasPermission(user, UserRole.Admin);    // false
```

---

## 4. Interview Questions

1. **What is an enum and what type does it use by default?**
   *An enum is a value type that defines a set of named integer constants. By default, the underlying type is `int` (4 bytes). You can change it to `byte`, `short`, `long`, etc. The default value of any enum variable is `0`, which is why you should always define a meaningful member with value `0` (like `None` or `Unknown`).*

2. **What does the [Flags] attribute do?**
   *`[Flags]` tells the runtime that this enum represents a bit field — multiple values can be combined with bitwise OR (`|`). It also changes `ToString()` behaviour: without `[Flags]`, value `3` prints `"3"`. With `[Flags]` and proper power-of-2 values, it prints `"Read, Write"`.*

3. **Can you cast any integer to an enum in C#?**
   *Yes, and this is dangerous. `(OrderStatus)999` compiles and runs without error, even though `999` isn't a defined value. Always validate with `Enum.IsDefined()` when accepting external input (API parameters, database values).*

4. **Why must [Flags] enum values be powers of 2?**
   *Because bitwise operations work on individual bits. If you use `Read = 1, Write = 2, ReadWrite = 3`, then `Read | Write` equals `3`, which is the same as `ReadWrite` — correct. But if you used `Read = 1, Write = 2, Execute = 3`, then `Read | Write` (1|2 = 3) would falsely match `Execute`.*

5. **How do you safely parse a string to an enum?**
   *Use `Enum.TryParse<T>(string, out T result)`. It returns `false` if the string doesn't match. Never use `Enum.Parse` without a try/catch, as it throws `ArgumentException` on invalid input. For flags enums, parsing `"Read, Write"` correctly returns the combined value.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Not defining a zero value
public enum Status { Active = 1, Inactive = 2 }
Status s = default; // s == 0, which is NOT any defined member!
// FIX: Always have a zero member
public enum Status { Unknown = 0, Active = 1, Inactive = 2 }

// MISTAKE 2: Sequential values in [Flags]
[Flags]
public enum Bad { A = 1, B = 2, C = 3 } // ❌ C should be 4, not 3
// A | B == 3 == C — bitwise operations break!

// MISTAKE 3: Trusting user input without validation
var status = (OrderStatus)int.Parse(userInput); // ❌ Could be any integer
// FIX:
if (Enum.TryParse<OrderStatus>(userInput, out var parsed) && Enum.IsDefined(typeof(OrderStatus), parsed))
    return parsed; // ✅ Safe
```

---

## 🔗 Connected Topics

- [Value Types vs Reference Types](./01-value-vs-reference-types.md) — Enums are value types (stored on the stack)
- [Pattern Matching](../04-advanced/35-pattern-matching.md) — Switch expressions on enums with exhaustiveness checking
- [Access Modifiers](./05-access-modifiers.md) — Enum visibility in APIs

---

*Created: May 2026 · Level: Beginner*
