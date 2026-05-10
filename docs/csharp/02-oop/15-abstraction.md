# Chapter 15 � Abstraction

> **⚡ Core Idea (30 seconds):** Abstraction means **defining what something does without exposing how it does it**. In C#, you achieve this via `abstract` classes and `interface`s. The calling code depends on the *contract*, not the *implementation* — making your code swappable and testable.

**Domain:** `C#` · **Level:** `Beginner → Intermediate` · **Tags:** `#oop` `#abstraction` `#interface` `#abstract`


---

## 1. Core Idea

A TV remote is an abstraction — you press "volume up" without knowing whether it's using infrared, Bluetooth, or RF signals. The interface (button) is stable; the implementation can change.

In code: your `CheckoutService` should call `IPaymentGateway.ChargeAsync()` without caring if it talks to Stripe, PayPal, or a mock. **The interface is the remote. The implementation is the electronics inside.**

---

## 2. Deep Explanation

### Abstract Class vs Interface

| | `abstract class` | `interface` |
|--|-----------------|-------------|
| **Can have implementation** | ✅ Yes (concrete methods, fields) | ✅ C# 8+ (default methods only) |
| **Can have state/fields** | ✅ | ❌ (no instance fields) |
| **Multiple inheritance** | ❌ Single only | ✅ A class can implement many |
| **Constructor** | ✅ | ❌ |
| **Access modifiers on members** | ✅ | Public by default (before C# 8) |
| **Use when** | Shared base behavior + contract | Pure contract, no shared state |

### When to Use Abstract Class

- Shared behavior exists that all subclasses need (Template Method pattern)
- You need protected state or constructor dependencies
- "Is-a" relationship is strong (a `Dog` IS an `Animal`)

### When to Use Interface

- No shared state needed — just contract
- Multiple implementations possible that aren't related in hierarchy
- Dependency injection (DI containers work best with interfaces)
- You're a library author giving consumers an extension point

### C# 8 Default Interface Methods (DIM)

```csharp
public interface ILogger
{
    void Log(string message);

    // Default implementation — doesn't break existing implementors when added
    void LogWarning(string message) => Log($"[WARNING] {message}");
}
```

Use sparingly — overuse makes interfaces feel like abstract classes.

---

## 3. Code Examples

### Basic — Abstract Class as Template
```csharp
// Abstract class: shared pipeline, specific steps overridden
public abstract class ReportGenerator
{
    // Template Method — the algorithm skeleton (don't override this)
    public string Generate(ReportRequest request)
    {
        var data = FetchData(request);        // abstract — each subclass provides
        var processed = Transform(data);      // abstract
        return Format(processed);             // concrete default: returns JSON
    }

    protected abstract IEnumerable<object> FetchData(ReportRequest request);
    protected abstract IEnumerable<object> Transform(IEnumerable<object> raw);
    protected virtual string Format(IEnumerable<object> data) => JsonSerializer.Serialize(data);
}

public class SalesReportGenerator : ReportGenerator
{
    protected override IEnumerable<object> FetchData(ReportRequest req)
        => _salesRepo.GetSales(req.StartDate, req.EndDate);

    protected override IEnumerable<object> Transform(IEnumerable<object> raw)
        => raw.Cast<Sale>().GroupBy(s => s.Region).Select(g => new { g.Key, Total = g.Sum(s => s.Amount) });
}
```

### Real-World — Interface for DI & testability
```csharp
// Interface — the contract
public interface INotificationService
{
    Task SendAsync(string recipient, string message, CancellationToken ct = default);
}

// Real implementation
public class EmailNotificationService : INotificationService
{
    private readonly SmtpClient _smtp;
    public async Task SendAsync(string recipient, string message, CancellationToken ct)
        => await _smtp.SendMailAsync(/* ... */, ct);
}

// Test implementation — no actual emails sent
public class FakeNotificationService : INotificationService
{
    public List<(string Recipient, string Message)> Sent = new();
    public Task SendAsync(string recipient, string message, CancellationToken ct)
    {
        Sent.Add((recipient, message));
        return Task.CompletedTask;
    }
}

// Service only knows the interface — completely decoupled
public class OrderService
{
    private readonly INotificationService _notifier;
    public OrderService(INotificationService notifier) => _notifier = notifier;

    public async Task CompleteOrderAsync(Order order)
    {
        // ... complete order logic ...
        await _notifier.SendAsync(order.CustomerEmail, "Your order is confirmed!");
    }
}
```

---

## 4. Interview Questions

1. **What is the difference between an abstract class and an interface?**
   *Abstract class: can have concrete methods, fields, constructors, and state — it IS a class. Interface: is a pure contract — no instance fields, no constructors (pre-C# 8). A class can inherit only one abstract class but implement many interfaces. Use abstract class when you need shared implementation; use interface when you only need a contract.*

2. **When would you use an abstract class instead of an interface?**
   *Use abstract class when: (1) you want to share concrete code across all subclasses (Template Method pattern), (2) you need protected fields or a constructor with dependencies, (3) the "is-a" relationship is strong and you need default behaviour. Use interface when there's no shared state, multiple implementations are unrelated, or you need DI-friendly swappability.*

3. **Can an abstract class have a constructor? Can you instantiate it directly?**
   *Yes, abstract classes can have constructors — they are called by derived class constructors via `base(...)`. No, you cannot instantiate an abstract class directly (`new AbstractClass()` → compile error). Its constructor only runs as part of creating a concrete subclass.*

4. **What are default interface methods? What problem do they solve?**
   *Default interface methods (C# 8+) let you add a method with a body directly to an interface. They solve the **backward compatibility** problem: adding a new method to a widely-used interface would break all existing implementations. With a default method, existing implementors inherit the default behaviour automatically and only override if they need custom logic.*

5. **How does abstraction relate to the Dependency Inversion Principle?**
   *DIP states: high-level modules should not depend on low-level modules — both should depend on abstractions. Abstraction (interfaces/abstract classes) is the mechanism that makes DIP possible. Instead of `OrderService` depending on `SqlProductRepository`, it depends on `IProductRepository`. Now you can swap the implementation (SQL, In-memory, mock) without touching `OrderService`.*

---

## 5. Follow-up Questions

- Can an abstract class implement an interface?
  *(Yes — and it can leave interface methods unimplemented, forcing subclasses to implement them)*
- What happens if you add a method to an interface that has 50 implementors across a codebase?
  *(All 50 break — they all need to implement the new method. Default interface methods solve this.)*
- If `IAnimal` has `void Speak()`, and both `Dog` and `Cat` implement it, can you call `Speak()` on an `IAnimal` list?
  *(Yes — that's polymorphism through abstraction)*
- How does `abstract` differ from `virtual`?
  *(abstract = no implementation, MUST override. virtual = has default, MAY override.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Fat interface — interface doing too much (violates ISP)
public interface IUserService
{
    User GetById(int id);
    void UpdateProfile(UserProfile p);
    void SendEmail(string to, string body);  // ❌ Email has nothing to do with user management
    void ExportToExcel();                   // ❌ Definitely not a user concern
}
// Split into: IUserRepository, IEmailService, IReportExporter

// MISTAKE 2: Abstract class with too many concrete methods — becomes a God class
// Keep abstract classes focused on the extension points

// MISTAKE 3: Depending on concrete class instead of interface
public class OrderService
{
    private readonly EmailNotificationService _email; // ❌ Tightly coupled to email
    // FIX: private readonly INotificationService _notifier;
}
```

---

## 7. Real-World Usage

| Scenario | Abstraction Used |
|----------|-----------------|
| Repository pattern | `IProductRepository` → `SqlProductRepository` / `InMemoryProductRepository` |
| Payment processing | `IPaymentGateway` → Stripe, PayPal, Mock implementations |
| Logging | `ILogger<T>` (Microsoft.Extensions.Logging) |
| Email / SMS | `INotificationService` — swappable providers |
| ASP.NET Core middleware | `IMiddleware` interface |
| Template Method (reports, parsers) | `abstract` base class with override points |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | abstract keyword, interface syntax, can't instantiate abstract |
| **Level 2** | abstract class vs interface trade-offs, DI with interfaces |
| **Level 3** | Default interface methods, interface segregation, explicit implementation |
| **Level 4** | Covariant return types, static abstract members (C# 11), generic interfaces |

## 🔗 Connected Topics
- [Encapsulation](./12-encapsulation.md) | [Inheritance](./13-inheritance.md) | [Polymorphism](./14-polymorphism.md)
- [Delegates](../03-intermediate/19-delegates.md) — Functional abstraction alternative to interfaces

