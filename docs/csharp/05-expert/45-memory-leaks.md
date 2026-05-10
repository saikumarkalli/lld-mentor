# Chapter 45 — Memory Leaks in Managed Code

> **⚡ Core Idea (30 seconds):** C# has a Garbage Collector, but you can still leak memory. A "managed memory leak" happens when you hold onto object references longer than needed, preventing the GC from reclaiming them. The most common culprits: static collections, event handlers, and captured closures in long-lived delegates.

**Domain:** `C#` **Level:** `Expert` **Tags:** `#memory-leak` `#gc` `#diagnostics` `#events`

---

## 1. Core Idea

Think of the GC like a **cleaning crew**. They will throw away anything in the trash can. But if you leave a useless object on your desk (by keeping a reference to it), the cleaning crew assumes you still need it and won't touch it. Over time, your desk fills up until the program crashes with an `OutOfMemoryException`.

---

## 2. Deep Explanation

### 1. The "Lapsed Listener" (Event Handler Leaks)

The most common leak in .NET UI apps (WPF, WinForms, MAUI). When object A subscribes to object B's event, object B holds a reference to object A. If object B lives longer than object A, object A cannot be garbage collected.

```csharp
public class Publisher { public event EventHandler DataChanged; }

public class Subscriber
{
    public Subscriber(Publisher p)
    {
        // p now holds a strong reference to THIS Subscriber instance!
        p.DataChanged += OnDataChanged;
    }
    private void OnDataChanged(object sender, EventArgs e) { }
}
```
If `Subscriber` is a temporary window, and `Publisher` is a singleton service, every time you open and close the window, you leak a `Subscriber` because the singleton `Publisher` is still holding its event handler.

### 2. Static Collections

Static fields live for the lifetime of the AppDomain (usually the process). If you add items to a static `Dictionary` or `List` and never remove them, they will never be garbage collected.

```csharp
public static class Cache
{
    // If we only Add and never Remove, this grows infinitely
    private static readonly Dictionary<int, UserData> _users = new();
}
```

### 3. Captured Closures in Long-Lived Delegates

When you use a lambda expression that references a local variable or class member, the compiler creates a hidden class (a closure) to hold that state. If the delegate lives a long time (e.g., passed to a background thread or a singleton), the captured objects leak.

```csharp
public class Worker
{
    private byte[] _largeBuffer = new byte[10 * 1024 * 1024]; // 10MB

    public Action GetAction()
    {
        // The lambda captures 'this' to access _largeBuffer
        // Whoever holds the Action now holds the entire 10MB Worker!
        return () => Console.WriteLine(_largeBuffer.Length);
    }
}
```

### 4. CancellationTokenSource Timers

`new CancellationTokenSource(TimeSpan)` creates an internal timer. If you don't dispose the CTS, the timer queue holds a reference to it until the timeout expires. If the timeout is very long (or infinite), you leak the CTS and everything it captures.

---

## 3. Code Examples

### Example 1 — Fixing the Event Leak
```csharp
// ❌ Bad: Leaks if publisher outlives subscriber
public void Subscribe(Publisher p) => p.DataChanged += Handle;

// ✅ Good: Implement IDisposable and unsubscribe
public class Subscriber : IDisposable
{
    private readonly Publisher _p;
    public Subscriber(Publisher p) { _p = p; _p.DataChanged += Handle; }
    private void Handle(object sender, EventArgs e) { }

    public void Dispose()
    {
        _p.DataChanged -= Handle; // Break the strong reference!
    }
}

// ✅ Good (Alternative): Weak Event Pattern
// The publisher holds a WeakReference to the subscriber, allowing GC to collect it.
```

### Example 2 — Fixing Static Caches
```csharp
// ❌ Bad: Infinite growth static cache
public static class BadCache { public static Dictionary<string, object> Data = new(); }

// ✅ Good: Use MemoryCache with expiration policies
public class GoodCache
{
    private readonly IMemoryCache _cache;
    public GoodCache(IMemoryCache cache) => _cache = cache;

    public void Add(string key, object value)
    {
        _cache.Set(key, value, TimeSpan.FromMinutes(10)); // Evicts automatically
    }
}
```

---

## 4. Interview Questions

1. **How can memory leak in C# if it has a Garbage Collector?**
   *The GC only collects unreachable objects. A managed leak occurs when objects remain reachable via strong references long after they are logically no longer needed. The GC sees the reference and correctly keeps the object alive, eventually exhausting memory.*

2. **What is the most common cause of memory leaks in .NET?**
   *Event handlers. When a short-lived object subscribes to an event on a long-lived object (like a singleton service or static class), the long-lived object holds a delegate pointing to the short-lived object, preventing its collection. You must explicitly unsubscribe (`-=`) when done.*

3. **How do captured variables in lambdas cause memory leaks?**
   *To pass local variables into a lambda, the compiler creates a hidden closure class. If the lambda captures an instance member, it captures `this` (the entire object). If that lambda is passed to a long-lived service, the entire original object and all its fields are kept alive.*

4. **How do you diagnose a memory leak in a production .NET app?**
   *1. Capture a process dump using `dotnet-dump collect`. 2. Open it in Visual Studio, WinDbg, or use `dotnet-dump analyze`. 3. Run `dumpheap -stat` to find objects consuming the most memory. 4. Run `gcroot <address>` on a leaked object to see the chain of strong references keeping it alive.*

5. **Why must you dispose a `CancellationTokenSource` created with a timeout?**
   *A timeout CTS registers a timer with the system timer queue. The queue holds a root reference to the CTS. If you don't dispose the CTS, it remains rooted until the timer fires. If you create many of these (e.g., per web request), you will exhaust memory quickly.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Relying on Finalizers to unsubscribe events
~Subscriber()
{
    _publisher.DataChanged -= Handle; // ❌ Too late!
    // The GC will NEVER call the finalizer because _publisher is keeping this object alive!
}

// MISTAKE 2: Capturing 'this' unintentionally in logging callbacks
_logger.RegisterCallback(() => Console.WriteLine(this.Id));
// ❌ The logger now keeps 'this' alive forever.

// MISTAKE 3: Unbounded ConcurrentDictionary caches
private static readonly ConcurrentDictionary<string, User> _cache = new();
// FIX: Use Microsoft.Extensions.Caching.Memory.IMemoryCache with size limits
```

---

## Connected Topics

- [Garbage Collection](./40-garbage-collection.md) — How the GC determines what is reachable via GC Roots
- [Events](../03-intermediate/20-events.md) — Event handler implementation details
- [IDisposable](../04-advanced/33-memory-management.md) — Unsubscribing in `Dispose()`
- [Delegates](../03-intermediate/19-delegates.md) — Closures and variable capture mechanics

---

*Created: May 2026 · Level: Expert*
