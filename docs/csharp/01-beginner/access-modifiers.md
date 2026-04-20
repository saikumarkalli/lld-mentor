# Access Modifiers

> **⚡ Core Idea (30 seconds):** Access modifiers control **who can see what**. They're not just a security feature — they define your API contract, enforce encapsulation, and prevent unintended coupling between modules. Choosing the wrong modifier is a design mistake, not just a style issue.

**Domain:** `C#` **Level:** `Beginner` **Tags:** `#access-modifiers` `#encapsulation` `#assembly`

---

## 1. Core Idea

Think of access modifiers as doors in a building:
- `private` = door only you have the key to
- `protected` = door you and your family have keys to
- `internal` = door everyone in the same building (assembly) can open
- `public` = door open to the whole world

Getting this right is the difference between a maintainable library and a mess where every class touches every other class.

---

## 2. Deep Explanation

### All Six Modifiers

| Modifier | Same Class | Derived Class (same assembly) | Derived Class (other assembly) | Same Assembly (non-derived) | Other Assemblies |
|----------|:---:|:---:|:---:|:---:|:---:|
| `private` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `private protected` | ✅ | ✅ | ❌ | ❌ | ❌ |
| `protected` | ✅ | ✅ | ✅ | ❌ | ❌ |
| `internal` | ✅ | ✅ | ❌ | ✅ | ❌ |
| `protected internal` | ✅ | ✅ | ✅ | ✅ | ❌ |
| `public` | ✅ | ✅ | ✅ | ✅ | ✅ |

### Default Modifiers (When you omit the keyword)

```csharp
class Foo { }             // internal (top-level types default to internal)
public class Bar
{
    int x;                // private (members default to private)
    void Method() { }    // private
}
```

### Assemblies — The `internal` Boundary

An **assembly** is a compiled `.dll` or `.exe`. `internal` gives access to everything in the same project, but nothing crosses that boundary. This is the key design tool for **library authors**:

```
MyLibrary.dll
├── public class ApiClient       ← Exposed to consumers
├── internal class HttpHelper    ← Library implementation detail, hidden from NuGet consumers
└── private class RequestState  ← Only ApiClient can see it
```

### `InternalsVisibleTo` — Testing Internal Classes

```csharp
// In AssemblyInfo.cs or top of any .cs file:
[assembly: InternalsVisibleTo("MyLibrary.Tests")]
// Now MyLibrary.Tests can access 'internal' members — needed for unit testing internals
```

---

## 3. Code Examples

### Example 1 — Basic: The Wrong vs Right Visibility
```csharp
// BAD: Everything public — no contract, no protection
public class OrderService
{
    public DbContext _context;           // Exposes implementation detail
    public void ValidateInternal() { }  // Internal detail made public
    public decimal CalculateTax() { }   // Should be private logic
}

// GOOD: Minimal surface area
public class OrderService
{
    private readonly IOrderRepository _repo;  // Hidden impl detail
    private readonly ITaxCalculator _tax;

    public OrderService(IOrderRepository repo, ITaxCalculator tax) // Public: DI entry point
    {
        _repo = repo;
        _tax = tax;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest req) { } // Public API
    private decimal CalculateTotal(IEnumerable<OrderLine> lines) { }     // Private logic
    protected virtual void OnOrderCreated(Order order) { }                // Hook for subclasses
}
```

### Example 2 — Real-World: Library Design with `internal`
```csharp
// Payments.dll

// PUBLIC: What consumers use
public interface IPaymentGateway { Task<PaymentResult> ChargeAsync(decimal amount, string token); }
public class StripeGateway : IPaymentGateway { /* ... */ }

// INTERNAL: Library plumbing — consumers never see this
internal class StripeHttpClient
{
    internal async Task<string> PostAsync(string endpoint, object body) { /* ... */ }
}

internal class StripeResponseParser
{
    internal PaymentResult Parse(string json) { /* ... */ }
}

// PRIVATE: Only StripeGateway touches this
// private readonly StripeHttpClient _client; -- inside StripeGateway
```

---

## 4. Interview Questions

1. **What is the difference between `protected` and `internal`?**
2. **When would you use `protected internal` vs `private protected`?**
3. **What is the default accessibility of a class member in C#?**
4. **What is `InternalsVisibleTo` and when is it used?**
5. **Why is making everything `public` bad design?**

---

## 5. Follow-up Questions

- Can a derived class in a different assembly access `protected internal` members?
  *(Yes — `protected internal` means protected **OR** internal. Either condition grants access.)*
- Can a derived class in a different assembly access `private protected` members?
  *(No — `private protected` means protected **AND** internal. Both conditions required.)*
- What modifier would you use for a base class method you want only leaf classes in your own library to override, but not external consumers?
  *(`private protected` — accessible only to derived classes within the same assembly)*
- Why do interfaces have all-public members by default? (pre C# 8)
- What is a **default interface method** (C# 8) and what modifier does it use?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Assuming protected = safe from external access
// A derived class in ANY assembly can access protected members — not "internal to your library"

// MISTAKE 2: Making test helpers public just to test them
public class HelperOnlyForTests { } // ❌ Pollutes your public API
// FIX: Use internal + [assembly: InternalsVisibleTo("Tests")]

// MISTAKE 3: Overusing internal — coupling everything in the assembly
// internal is still a contract within the assembly. Code it to an interface when possible.

// MISTAKE 4: Nested type visibility
public class Outer
{
    private class Inner { }  // Can only be used by Outer
    // Inner's members can be public, but Inner itself is private — access still blocked
}
```

---

## 7. Real-World Usage

| Scenario | Modifier Used |
|----------|--------------|
| Public API in NuGet package | `public` on facade; `internal` on implementation |
| Unit testing internals | `internal` + `InternalsVisibleTo` |
| Extension points for derived classes | `protected virtual` / `protected abstract` |
| Sealed implementation class | `internal sealed` — implementation detail, no subclassing |
| Utility helpers within a project | `internal static` class |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | public, private, protected meanings |
| **Level 2** | internal and assembly boundaries, default modifiers |
| **Level 3** | protected internal vs private protected, InternalsVisibleTo |
| **Level 4** | Library API design with minimal public surface, default interface methods |

## 🔗 Connected Topics
- [Encapsulation](../oops/encapsulation.md) — Access modifiers are encapsulation's mechanism
- [Abstraction](../oops/abstraction.md) — public interfaces, internal implementations
- [Inheritance](../oops/inheritance.md) — protected members and derived class access

*Created: April 2026 · Level: Beginner*
