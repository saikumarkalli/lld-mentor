# Chapter 19 � Delegates, Func, Action & Predicate

> **⚡ Core Idea (30 seconds):** A delegate is a **type-safe function pointer** — it holds a reference to a method just like an object holds references to data. `Func`, `Action`, and `Predicate` are pre-built generic delegate types that cover 95% of use cases without defining your own delegate types.

**Domain:** `C#` **Level:** `Intermediate` **Tags:** `#delegates` `#functional` `#callbacks` `#events`

---

## 1. Core Idea

Think of a delegate like a **plug socket** — you define the shape (return type + params), and anyone can plug in a method that fits. The code calling the delegate doesn't need to know *which* specific method it calls—it just invokes "whatever is plugged in."

This is the foundation of: **events, callbacks, LINQ, strategy pattern in functional style, and async callbacks**.

---

## 2. Deep Explanation

### How Delegates Work Internally

A delegate is a **class** that extends `System.MulticastDelegate` (which extends `System.Delegate`). When you write:
```csharp
public delegate int Calculate(int a, int b);
```
The compiler generates a sealed class roughly like:
```csharp
public sealed class Calculate : MulticastDelegate
{
    public int Invoke(int a, int b) { ... } // Calls the underlying method
    public IAsyncResult BeginInvoke(int a, int b, AsyncCallback cb, object state) { ... }
    public int EndInvoke(IAsyncResult result) { ... }
}
```

Behind the scenes, a delegate holds:
- **Target** — the object instance (null for static methods)
- **Method** — a `MethodInfo`-like reference to the method

### Multicast Delegates

Delegates are **invocation lists** — you can chain multiple methods together with `+=/remove`:
```csharp
Calculate c = Add;
c += Multiply; // Both methods in the chain
c(5, 3);       // Calls Add(5,3) then Multiply(5,3)
                // Return value = result of LAST method only
```

### Built-in Generic Delegates

| Type | Signature | Use Case |
|------|-----------|----------|
| `Func<TResult>` | No params → returns TResult | Factory, getter |
| `Func<T, TResult>` | One param → returns TResult | Mapper, selector |
| `Func<T1, T2, TResult>` | Two params → returns TResult | Combiner |
| `Action` | No params, no return | Fire and forget |
| `Action<T>` | One param, no return | Consumer, logger |
| `Predicate<T>` | One param → bool | Filter, condition check |

`Predicate<T>` is equivalent to `Func<T, bool>` — they're *interchangeable* at the IL level but have semantic differences.

### Closures — The Hidden Complexity

When a lambda captures a local variable, the compiler generates a **closure class** to hold it:
```csharp
int multiplier = 3; // Stack variable
Func<int, int> triple = x => x * multiplier; // Captured!
```
The compiler promotes `multiplier` to a heap-allocated closure object. This means **value types get heap promoted** inside closures (covered in Value Types topic).

---

## 3. Code Examples

### Example 1 — Basic: Custom Delegate vs Func
```csharp
// Old way — custom delegate definition
public delegate int MathOperation(int a, int b);

MathOperation add = (a, b) => a + b;
MathOperation multiply = (a, b) => a * b;
Console.WriteLine(add(5, 3));       // 8
Console.WriteLine(multiply(5, 3));  // 15

// Modern way — use Func<> (same result, less boilerplate)
Func<int, int, int> add2 = (a, b) => a + b;
Console.WriteLine(add2(5, 3)); // 8

// Action — no return value
Action<string> log = msg => Console.WriteLine($"[LOG] {msg}");
log("Order processed");

// Predicate — filter condition
Predicate<int> isEven = n => n % 2 == 0;
Console.WriteLine(isEven(4)); // True
Console.WriteLine(isEven(7)); // False
```

### Example 2 — Real-World: Strategy Pattern with Delegates
```csharp
// Payment processing with pluggable strategy
public class PaymentProcessor
{
    private readonly Func<decimal, bool> _chargeStrategy;
    private readonly Action<string> _logger;

    public PaymentProcessor(Func<decimal, bool> chargeStrategy, Action<string> logger)
    {
        _chargeStrategy = chargeStrategy;
        _logger = logger;
    }

    public bool ProcessPayment(decimal amount)
    {
        _logger($"Processing payment of {amount:C}");
        bool success = _chargeStrategy(amount);
        _logger(success ? "Payment succeeded" : "Payment failed");
        return success;
    }
}

// Usage — plug in different strategies
var creditCardProcessor = new PaymentProcessor(
    chargeStrategy: amount => CreditCardGateway.Charge(amount),
    logger: msg => _auditService.Log(msg)
);

var mockProcessor = new PaymentProcessor(
    chargeStrategy: _ => true,  // Always succeeds in tests
    logger: Console.WriteLine
);
```

### Example 3 — Real-World: Pipeline / Middleware Chain
```csharp
// Functional middleware pipeline (like ASP.NET Core middleware)
public class Pipeline<T>
{
    private readonly List<Func<T, Func<T, T>, T>> _middlewares = new();

    public Pipeline<T> Use(Func<T, Func<T, T>, T> middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    public T Execute(T input)
    {
        // Build the chain from the end backwards
        Func<T, T> next = x => x;
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var current = _middlewares[i];
            var nextCopy = next;
            next = x => current(x, nextCopy);
        }
        return next(input);
    }
}

// Usage
var pipeline = new Pipeline<string>()
    .Use((input, next) => next(input.Trim()))
    .Use((input, next) => next(input.ToUpper()))
    .Use((input, next) => $"[{next(input)}]");

Console.WriteLine(pipeline.Execute("  hello  ")); // [HELLO]
```

---

## 4. Interview Questions

1. **What is a delegate in C#? How is it different from an interface?**
   *A delegate is a **type-safe function pointer** — a variable that holds a reference to a method (or multiple methods). The compiler generates a class extending `MulticastDelegate` for it. An interface defines a contract for an entire object with multiple methods. Use a delegate when you need a single callable — a callback, a handler, a filter. Use an interface when you need a richer contract with multiple members.*

2. **What is the difference between `Func<T>` and `Action<T>`?**
   *`Action<T>` is a delegate that takes a parameter and returns **nothing** (`void`) — use it for fire-and-forget operations like logging or notifications. `Func<T, TResult>` takes a parameter and **returns a value** — use it for transformations, factories, and selectors. `Predicate<T>` is equivalent to `Func<T, bool>` — a delegate that returns a yes/no decision.*

3. **What is a multicast delegate and what happens with the return value?**
   *A multicast delegate holds an **invocation list** of multiple methods. When invoked, it calls each in order. If the delegate has a return type, **only the return value of the last method** in the list is returned — all earlier return values are discarded. If any subscriber throws, execution stops and remaining subscribers are skipped.*

4. **What is a closure? What happens to captured variables behind the scenes?**
   *A closure is a lambda that "captures" (references) a variable from the enclosing scope. The compiler promotes the captured variable from the stack into a **heap-allocated closure class** — so the lambda can still access it after the enclosing method returns. This means captured value types silently get heap-allocated, which can surprise people expecting stack semantics.*

5. **How do delegates relate to events in C#?**
   *An event IS a delegate — specifically a delegate with restricted access. The `event` keyword wraps the delegate field with `add` and `remove` accessors (analogous to get/set on a property), making the field `private` and limiting external callers to only `+=` and `-=`. External code cannot invoke the event directly — only the declaring class can call it.*

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These expose whether you understand the CLR representation.*

- `Predicate<T>` and `Func<T, bool>` are structurally identical — can you assign one to the other directly? Why not?
  ```csharp
  Func<int, bool> func = n => n > 0;
  Predicate<int> pred = func; // Compile error! Different delegate types despite same signature
  Predicate<int> pred2 = new Predicate<int>(func); // Works
  ```
- What happens if a multicast delegate throws an exception in the first subscriber — does the second run?
  *(No, by default. The exception propagates and remaining are skipped. Use `GetInvocationList()` to handle each independently.)*
- How does capturing a loop variable in a lambda cause a classic bug?
  ```csharp
  var actions = new List<Action>();
  for (int i = 0; i < 5; i++)
      actions.Add(() => Console.WriteLine(i)); // Captures reference, not value!
  actions.ForEach(a => a()); // Prints 5, 5, 5, 5, 5 — not 0, 1, 2, 3, 4
  ```
- What is the IL difference between a lambda that captures variables and one that doesn't?
  *(No capture = static cached delegate. Capture = new closure object per invocation.)*
- Can delegates be used across threads? Are they thread-safe?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Closure captures reference, not value — loop variable bug
for (int i = 0; i < 3; i++)
{
    Task.Run(() => Console.WriteLine(i)); // May print 3, 3, 3
}
// FIX: Copy the variable
for (int i = 0; i < 3; i++)
{
    int copy = i;
    Task.Run(() => Console.WriteLine(copy)); // Prints 0, 1, 2 correctly
}

// MISTAKE 2: Null delegate invocation
Action? myAction = null;
myAction(); // NullReferenceException!
myAction?.Invoke(); // Safe — null-conditional invocation

// MISTAKE 3: Memory leak from event subscriptions (not a delegate issue per se, but related)
// See Events topic for += without -= pattern

// MISTAKE 4: Using Func/Action when a named interface gives better discoverability
// If a callback is complex, an interface is clearer:
// Instead of Func<Order, decimal, bool, Task<Result>>, use IPaymentHandler

// PERFORMANCE NOTE: Closures create a new object per call site if captured vars change
// For hot paths, prefer static lambdas (C# 9):
Func<int, int> triple = static x => x * 3; // No closure class generated
```

---

## 7. Real-World Usage

| Scenario | Delegate Usage |
|----------|---------------|
| **LINQ** | `Where(Predicate<T>)`, `Select(Func<T, TResult>)` |
| **Events** | `EventHandler<TEventArgs>` is a delegate type |
| **Dependency Injection** | Factory functions: `Func<IService>` registered in DI container |
| **Strategy Pattern** | `Func<T, TResult>` injected as a processing strategy |
| **Timer/Threading** | `TimerCallback`, `ThreadStart` are delegates |
| **ASP.NET Core** | `RequestDelegate` is `Func<HttpContext, Task>` |
| **MediatR** | `RequestHandlerDelegate<TResponse>` in pipeline behaviors |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | What a delegate is, Func/Action/Predicate basic usage |
| **Level 2** | Multicast delegates, closures, capturing variables |
| **Level 3** | IL representation, closure class generation, `GetInvocationList()` |
| **Level 4** | Static lambdas, zero-alloc delegate caching, `delegate*` (function pointers in unsafe code) |

---

## 🔗 Connected Topics

- [Events](./20-events.md) — Events are delegates with `add`/`remove` access modifiers and publisher-only invocation
- [LINQ](./21-linq.md) — Entire LINQ is built on `Func<T, bool>`, `Func<T, TResult>` delegates
- [Async/Await](../04-advanced/27-async-await.md) — `Task.ContinueWith` uses `Func<Task, T>` continuation delegates

> 🎯 **Interviewer Mindset Note:** The classic progression is: *"Tell me about delegates" → "How do events use delegates?" → "What's the difference between event and plain delegate?" → "Can you have an event of type Func<>?"* Be ready for this chain.

---

*Created: April 2026 · Level: Intermediate*
