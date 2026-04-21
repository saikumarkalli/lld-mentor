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
   *`protected`: accessible in the declaring class **and any derived class** — regardless of which assembly they are in. `internal`: accessible to **any code in the same assembly** — regardless of whether it's derived. They solve different problems: protected is about inheritance hierarchy; internal is about assembly (project) boundaries.*

2. **When would you use `protected internal` vs `private protected`?**
   *`protected internal` = protected **OR** internal — accessible to derived classes anywhere AND to all code in the same assembly. `private protected` = protected **AND** internal — accessible only to derived classes that are also in the same assembly. Use `private protected` when you want a hook for internal subclasses only, hidden from external consumers.*

3. **What is the default accessibility of a class member in C#?**
   *`private`. If you write a field, method, or property inside a class without any modifier, it is `private` by default. For top-level types (classes, interfaces at namespace level), the default is `internal`.*

4. **What is `InternalsVisibleTo` and when is it used?**
   *It's an assembly-level attribute that grants another named assembly access to `internal` members: `[assembly: InternalsVisibleTo("MyProject.Tests")]`. The main use case is unit testing — your test project can access and test internal classes/methods without making them `public` and polluting your public API.*

5. **Why is making everything `public` bad design?**
   *Public members are your API contract — once published, callers depend on them and you can't change them without breaking those callers. Making everything public means: (1) no encapsulation — implementation details leak out, (2) no contract clarity — callers don't know what they should actually use, (3) harder to refactor — any internal change might break external consumers.*

---

## 5. Follow-up Questions

- Can a derived class in a different assembly access `protected internal` members?
  *(Yes — `protected internal` means protected **OR** internal. Either condition grants access.)*
- Can a derived class in a different assembly access `private protected` members?
  *(No — `private protected` means protected **AND** internal. Both conditions required.)*
- What modifier would you use for a base class method you want only leaf classes in your own library to override, but not external consumers?
  *(`private protected` — accessible only to derived classes within the same assembly)*
- Why do interfaces have all-public members by default? (pre C# 8)
  *(Because an interface is a **contract meant to be consumed by anyone** — it defines what callers can do. Making interface members anything other than public would defeat the purpose: a private interface method would be invisible to implementors, and an internal one would break cross-assembly usage. Pre-C# 8, the only valid option was public, so the language just made it the implicit default.)*

- What is a **default interface method** (C# 8) and what modifier does it use?
  *(A **default interface method** is a method defined directly in an interface with a body — an implementation. It uses `public` visibility by default. Its main purpose is **backward compatibility**: you can add a new method to an existing interface without breaking all existing implementors — they inherit the default behaviour unless they choose to override it.)*

  ```csharp
  public interface ILogger
  {
      void Log(string message);

      // Default interface method — C# 8+
      // Existing ILogger implementors don't need to change — they get this for free
      void LogError(string message) => Log($"[ERROR] {message}");
  }

  // Old implementor: still compiles, gets LogError for free
  public class ConsoleLogger : ILogger
  {
      public void Log(string message) => Console.WriteLine(message);
      // LogError inherited from interface — no change needed
  }

  // New implementor: can override if needed
  public class FileLogger : ILogger
  {
      public void Log(string message) => File.AppendAllText("log.txt", message);
      public void LogError(string message) => Log($"[CRITICAL] {message}"); // Custom override
  }
  ```
  > ⚠️ Use sparingly — if you find yourself adding many default methods, the interface is growing into an abstract class. Prefer splitting into a new interface instead.

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
