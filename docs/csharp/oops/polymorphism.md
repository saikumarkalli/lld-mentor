# Polymorphism

> **⚡ Core Idea (30 seconds):** Polymorphism means **one interface, many implementations**. The same method call produces different behavior depending on the actual runtime type. It's what lets you write code against abstractions rather than concrete types.

**Domain:** `C#` · **Level:** `Beginner → Intermediate` · **Tags:** `#oop` `#polymorphism` `#virtual` `#override`

> 🔗 **Code Implementation:** [`LLDMaster.OOP/4.Polymorphism/`](../../LLDMaster.OOP/4.Polymorphism/)

---

## 1. Core Idea

Imagine a `Shape` base class with a `Draw()` method. A `Circle` draws circles. A `Rectangle` draws rectangles. The calling code just says `shape.Draw()` — it doesn't care which shape it is. That's polymorphism: **the correct behavior is resolved at runtime**.

---

## 2. Deep Explanation

### Two Types of Polymorphism in C#

**Compile-Time (Static)** — Method overloading:
```csharp
void Log(string message) { }
void Log(string message, LogLevel level) { }  // Same name, different signature
// Resolved at compile time based on argument types
```

**Runtime (Dynamic)** — Method overriding via `virtual`/`override`:
```csharp
// Base defines the contract; derived overrides behavior
public class Shape { public virtual double Area() => 0; }
public class Circle : Shape { public override double Area() => Math.PI * r * r; }
public class Square : Shape { public override double Area() => side * side; }
```

### How the CLR Dispatches Virtual Calls

Every class with virtual methods has a **vtable (virtual method table)** — a pointer table mapping each virtual method to its implementation. When you call a virtual method, the CLR:
1. Looks up the object's actual type
2. Reads its vtable
3. Jumps to the correct implementation

This is why virtual calls have slightly more overhead than direct calls — one extra indirection via the vtable pointer.

### `virtual` vs `abstract` vs `new`

```csharp
// virtual: has a default implementation, can be overridden
public virtual void Process() { /* default */ }

// abstract: no implementation, MUST be overridden
public abstract void Process();

// new: hides the base method — not polymorphic! Dangerous.
public new void Process() { } // Only called if reference type is the derived class
```

### Interface Polymorphism

```csharp
// Any type implementing IPaymentGateway can be used here
public class CheckoutService
{
    private readonly IPaymentGateway _gateway;

    public CheckoutService(IPaymentGateway gateway) // Injected
    {
        _gateway = gateway;
    }

    public async Task<bool> Checkout(Order order)
        => await _gateway.ChargeAsync(order.Total);
}
// Works with StripeGateway, PayPalGateway, MockGateway — same code
```

---

## 3. Code Examples

### Basic — Runtime Polymorphism
```csharp
public abstract class Notification
{
    public abstract void Send(string message);

    // Template method pattern — polymorphism within an algorithm
    public void SendWithLogging(string message)
    {
        Console.WriteLine("Sending...");
        Send(message); // Calls the derived implementation
        Console.WriteLine("Sent.");
    }
}

public class EmailNotification : Notification
{
    public override void Send(string message)
        => Console.WriteLine($"Email: {message}");
}

public class SmsNotification : Notification
{
    public override void Send(string message)
        => Console.WriteLine($"SMS: {message}");
}

// Polymorphic usage — caller doesn't know or care about exact type
List<Notification> channels = new() { new EmailNotification(), new SmsNotification() };
channels.ForEach(n => n.Send("Order shipped!")); // Both work
```

### Real-World — Strategy + Polymorphism
```csharp
// This pattern is implemented in LLDMaster.Patterns/03_Behavioral/12_Strategy/
public interface IDiscountStrategy
{
    decimal Apply(decimal price);
}

public class PercentageDiscount : IDiscountStrategy
{
    private readonly decimal _percent;
    public PercentageDiscount(decimal percent) => _percent = percent;
    public decimal Apply(decimal price) => price * (1 - _percent / 100);
}

public class FlatDiscount : IDiscountStrategy
{
    private readonly decimal _amount;
    public FlatDiscount(decimal amount) => _amount = amount;
    public decimal Apply(decimal price) => Math.Max(0, price - _amount);
}

public class PricingService
{
    public decimal Calculate(decimal price, IDiscountStrategy strategy)
        => strategy.Apply(price); // Polymorphic — which discount? Doesn't matter!
}
```

---

## 4. Interview Questions

1. **What is the difference between method overloading and method overriding?**
2. **What is the `virtual` keyword? What happens if you don't use it?**
3. **What is the difference between `override` and `new`?**
4. **How does the CLR dispatch virtual method calls?**
5. **Can a struct implement an interface and participate in polymorphism?**

---

## 5. Follow-up Questions

- What is the performance cost of virtual dispatch vs direct call?
  *(One vtable lookup — negligible in most cases, significant in tight hot loops)*
- If a base class method is `virtual` and a derived class overrides it, but the variable is declared as the base type — which version runs?
  *(The derived override — that's the point of polymorphism)*
- What is `sealed override` and why use it?
  *(Prevents further overriding — allows JIT to devirtualize the call = faster)*
- What is the difference between `is` and `as` when checking types in polymorphic scenarios?
- When would you use an abstract class over an interface?
- What is covariance in the context of polymorphism?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using 'new' thinking it's the same as 'override'
public class Animal { public virtual void Speak() => Console.WriteLine("..."); }
public class Dog : Animal { public new void Speak() => Console.WriteLine("Woof"); }

Animal dog = new Dog();
dog.Speak(); // Prints "..." — NOT "Woof"! 'new' hides, doesn't override.

// MISTAKE 2: Overriding without calling base when needed
public override void Initialize()
{
    // Forgot: base.Initialize(); — base setup skipped!
    SetupChildOnly();
}

// MISTAKE 3: Struct implementing interface — boxing on virtual dispatch
IComparable comp = someStruct; // Boxes the struct just to call the interface method!
```

---

## 7. Real-World Usage

| Scenario | Polymorphism Used |
|----------|------------------|
| ASP.NET Core Middleware | Each middleware has the same `InvokeAsync` signature |
| EF Core Providers | `DbContext` works regardless of SQL Server, PostgreSQL, SQLite |
| MediatR Handlers | `IRequestHandler<TRequest, TResponse>` — any handler works |
| Strategy Pattern | [`LLDMaster.Patterns/12_Strategy/`](../../LLDMaster.Patterns/03_Behavioral/12_Strategy/) |
| Template Method | [`LLDMaster.Patterns/15_TemplateMethod/`](../../LLDMaster.Patterns/03_Behavioral/15_TemplateMethod/) |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Override vs overload, virtual keyword |
| **Level 2** | abstract vs interface, new vs override |
| **Level 3** | vtable mechanics, JIT devirtualization, sealed override |
| **Level 4** | Covariance/contravariance, generic polymorphism |

## 🔗 Connected Topics
- [Encapsulation](./encapsulation.md) | [Abstraction](./abstraction.md) | [Inheritance](./inheritance.md)
- [Strategy Pattern](../../LLDMaster.Patterns/03_Behavioral/12_Strategy/) — polymorphism through composition
- [Delegates](../02-intermediate/delegates.md) — functional polymorphism

*Created: April 2026 · Code Reference: [`LLDMaster.OOP/4.Polymorphism/`](../../LLDMaster.OOP/4.Polymorphism/)*
