# Chapter 20 � Events & Event Patterns

> **⚡ Core Idea (30 seconds):** An event is a **delegate with restricted access** — only the declaring class can invoke it, but anyone can subscribe or unsubscribe. Events implement the Observer pattern natively in C# and are how the language decouples publishers from subscribers.

**Domain:** `C#` **Level:** `Intermediate` **Tags:** `#events` `#delegates` `#observer` `#publisher-subscriber`

---

## 1. Core Idea

A delegate is like giving someone your phone number — they can call you whenever they want. An event is like a subscription service — you can sign up (+= ) or cancel (−=), but only the publisher decides when to call (invoke). **Subscribers can't call each other or fire the event themselves**.

Events ARE delegates under the hood — but with `add`/`remove` accessor protection.

---

## 2. Deep Explanation

### How Events Differ from Plain Delegates

```csharp
// Plain delegate field — ANYONE can invoke it (dangerous)
public Action<string> OnMessage; // External code can call OnMessage("hacked!")

// Event — ONLY the declaring class invokes, anyone can subscribe
public event Action<string> OnMessage; // External code can only += or -=
```

The `event` keyword generates a private backing delegate field plus `add` and `remove` accessors — analogous to property getters/setters.

### EventHandler\<T\> Convention

The standard .NET pattern:
```csharp
// Custom event args
public class OrderCompletedEventArgs : EventArgs
{
    public Order Order { get; init; }
    public DateTime CompletedAt { get; init; }
}

// Declare using EventHandler<T> — standard, consistent with .NET ecosystem
public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;

// Raise (protected virtual to allow subclasses to raise too)
protected virtual void OnOrderCompleted(Order order)
{
    OrderCompleted?.Invoke(this, new OrderCompletedEventArgs
    {
        Order = order,
        CompletedAt = DateTime.UtcNow
    });
}
```

### Memory Leak — The Classic Event Trap

If a subscriber object is subscribed to an event on a long-lived publisher, **the publisher holds a reference to the subscriber** → preventing GC:

```
Long-lived Publisher ──→ Subscriber via delegate ──→ Subscriber never GC'd!
```

Fix: always unsubscribe when done.

---

## 3. Code Examples

### Basic — Publisher / Subscriber Pattern
```csharp
public class StockPriceMonitor
{
    private decimal _price;

    public event EventHandler<decimal>? PriceChanged;

    public decimal Price
    {
        get => _price;
        set
        {
            if (_price == value) return;
            _price = value;
            PriceChanged?.Invoke(this, value); // Raise — null-safe
        }
    }
}

// Subscriber
public class AlertService : IDisposable
{
    private readonly StockPriceMonitor _monitor;

    public AlertService(StockPriceMonitor monitor)
    {
        _monitor = monitor;
        _monitor.PriceChanged += OnPriceChanged; // Subscribe
    }

    private void OnPriceChanged(object? sender, decimal newPrice)
    {
        if (newPrice < 100) Console.WriteLine($"ALERT: Price dropped to {newPrice}");
    }

    public void Dispose()
    {
        _monitor.PriceChanged -= OnPriceChanged; // MUST unsubscribe to prevent memory leak
    }
}
```

### Real-World — Domain Events in DDD
```csharp
// Domain event — raised inside the aggregate
public class Order
{
    public event EventHandler<Order>? OrderSubmitted;

    public void Submit()
    {
        if (Status != OrderStatus.Draft) throw new InvalidOperationException();
        Status = OrderStatus.Submitted;
        OrderSubmitted?.Invoke(this, this); // Raise domain event
    }
}

// Application layer handles the event (sends email, updates audit log, etc.)
public class OrderApplicationService
{
    public void PlaceOrder(Order order)
    {
        order.OrderSubmitted += async (sender, completedOrder) =>
        {
            await _emailService.SendConfirmationAsync(completedOrder);
            await _auditLog.RecordAsync(completedOrder);
        };
        order.Submit();
    }
}
```

---

## 4. Interview Questions

1. **What is the difference between a delegate and an event?**
   *A delegate is a type-safe function pointer — any code holding the delegate can invoke it. An event wraps a delegate with `add`/`remove` accessors and makes the backing delegate `private`. This means: only `+=` and `-=` are accessible externally — nobody outside the declaring class can fire the event or reset it to null. Events enforce the publisher-subscriber contract.*

2. **Why can an event only be invoked from the class that declared it?**
   *The `event` keyword generates a private backing delegate field. External code only has access to the `add` (+=) and `remove` (-=) accessors. Those accessors can't invoke the delegate — only the internal field can. So the compiler physically prevents external invocation, not just by convention.*

3. **What is `EventHandler<T>` and why does it exist?**
   *`EventHandler<TEventArgs>` is a built-in delegate type with signature `void Handler(object? sender, TEventArgs e)`. It exists to standardise event signatures across the .NET ecosystem — every UI event, every framework event follows this pattern. `sender` tells you who raised the event; `TEventArgs` carries the data. Using it makes your events consistent with all .NET tooling, documentation, and developer expectations.*

4. **How do events cause memory leaks and how do you prevent them?**
   *When you subscribe (`+=`), the publisher's event delegate holds a reference to your subscriber object. If the publisher lives longer than the subscriber, the subscriber can never be GC'd — the publisher is rooting it. Fix: always unsubscribe (`-=`) when done, typically in `Dispose()`. Alternatively, use weak references (WPF's `WeakEventManager`) or an event aggregator that manages subscription lifetimes.*

5. **What does `?.Invoke()` do when raising an event?**
   *It's the **null-safe invocation pattern**. If no one is subscribed to the event, the backing delegate is `null` — calling it directly would throw `NullReferenceException`. `?.Invoke()` first checks if the delegate is null; if not, it invokes it. Critically, it also captures the delegate reference before the null check, making it **thread-safe** against a race where the last subscriber unsubscribes between the null check and the actual call.*

---

## 5. Follow-up Questions

- What happens if you invoke an event with no subscribers without a null check?
  *(NullReferenceException — always use `?.Invoke()` or check `!= null` first)*
- How does `event` prevent external invocation at the compiler level? *(Generates `add`/`remove` accessors. The backing delegate field is private.)*
- Can an event be `static`? What are the risks?
  *(Yes — but static events on long-lived classes are a very common and hard-to-find memory leak source)*
- What is the `WeakEventManager` pattern and when would you use it?
  *(Available in WPF — subscriber can be GC'd even while still subscribed; weak reference pattern)*
- Can you `await` an event? *(Not directly — use `TaskCompletionSource<T>` to bridge event to awaitable)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Not null-checking before invoking
OrderCompleted(this, args); // ❌ Crashes if no subscribers
OrderCompleted?.Invoke(this, args); // ✅ Safe

// MISTAKE 2: Memory leak from static event
public static class AppEvents
{
    public static event EventHandler? AppStarted;
}
// If any object subscribes and is "discarded," it's still in memory forever
// because the static event holds the reference.

// MISTAKE 3: Subscribing anonymous lambda — can never unsubscribe!
button.Click += (s, e) => SaveData(); // ❌ Can't -= this later

// FIX: Named method
button.Click += SaveButton_Click;
// ... later ...
button.Click -= SaveButton_Click;

// MISTAKE 4: Exception in one subscriber kills the entire chain
// Fix: wrap in try-catch per subscriber using GetInvocationList()
foreach (EventHandler handler in MyEvent.GetInvocationList())
{
    try { handler(this, args); }
    catch (Exception ex) { _logger.LogError(ex, "Handler failed"); }
}
```

---

## 7. Real-World Usage

| Scenario | Event Pattern |
|----------|--------------|
| UI frameworks (WPF, WinForms) | Button.Click, TextChanged |
| Domain events (DDD) | Order.Submitted → send email, update analytics |
| Progress reporting | `IProgress<T>` internally uses events |
| SignalR | Hub invocation raises client events |
| EF Core interceptors | SaveChanges raises interceptor events |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Event syntax, += / -=, null-safe invocation |
| **Level 2** | EventHandler<T> convention, memory leak prevention |
| **Level 3** | Generated add/remove accessors, GetInvocationList for error isolation |
| **Level 4** | WeakEventManager, TaskCompletionSource bridge, Reactive Extensions (Rx) |

## 🔗 Connected Topics
- [Delegates](./19-delegates.md) — Events ARE delegates with access restrictions
- [Memory Management](../04-advanced/33-memory-management.md) — Event subscription memory leaks

*Created: April 2026 · Level: Intermediate*
