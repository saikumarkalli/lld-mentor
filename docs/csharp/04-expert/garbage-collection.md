# Garbage Collection (GC) Internals

> **⚡ Core Idea (30 seconds):** The .NET GC automatically manages heap memory by collecting objects that are no longer reachable. It uses a **generational model** — short-lived objects are collected often and cheaply, long-lived objects are promoted and collected rarely. Understanding GC generations, the LOH, and finalization lets you write code that doesn't cause GC pauses.

**Domain:** `C#` · **Level:** `Expert` · **Tags:** `#gc` `#memory` `#clr` `#performance` `#expert`

---

## 1. Core Idea

GC is like a janitor that runs at night to clean up — but sometimes the janitor interrupts everyone at random. If you litter constantly (allocate many short-lived objects), the janitor is always around. If you keep your space clean (reuse objects, pool resources), the janitor barely needed.

Senior developers write code that *works with* the GC, not against it.

---

## 2. Deep Explanation

### Generations: The Core Mechanism

The GC heap is split into **3 generations**:

```
Gen 0  [small, fast]  ← New objects allocated here (~256KB default)
Gen 1  [medium]       ← Survived one Gen 0 collection
Gen 2  [large, slow]  ← Long-lived objects (static data, cached objects)
LOH    [≥ 85KB]       ← Large Object Heap — allocated separately, Gen 2 collected
```

**Collection frequency:** Gen 0 >> Gen 1 >> Gen 2. Most objects should die in Gen 0.

**Generational hypothesis:** Most objects die young. A temporary request-scoped object shouldn't ever reach Gen 2.

### What Happens During a Collection

1. **Trigger**: Gen 0 budget exhausted OR explicit `GC.Collect()` call
2. **Suspension**: GC suspends application threads ("Stop-The-World") at safe points
3. **Mark**: GC traverses root references (stack, statics, handles) and marks all reachable objects
4. **Sweep/Compact**: Gen 0: Compact (move live objects, update references). LOH: Sweep only (no compaction by default — pinning risk).
5. **Promote**: Objects that survived are moved to the next generation
6. **Resume**: Application threads resume

### Server GC vs Workstation GC

```csharp
// Check in project file:
// <GarbageCollectionAdaptationMode>0</GarbageCollectionAdaptationMode>
// <ServerGarbageCollection>true</ServerGarbageCollection>
```

| | Workstation GC | Server GC |
|--|---------------|-----------|
| **Threads** | 1 GC thread | 1 GC thread per CPU core |
| **Heaps** | 1 heap | N heaps (N = CPU count) |
| **Use Case** | Desktop apps | ASP.NET Core, microservices |
| **Throughput** | Lower | Higher (parallelism) |

ASP.NET Core uses **Server GC by default** — more heap segments, more parallelism, higher throughput.

### Background GC

Gen 2 collections can run **concurrently** with application code (background GC). Gen 0 and Gen 1 are still STW. This reduces long pauses in server apps.

### Finalization — The Hidden Cost

When an object has a **finalizer** (`~MyClass()`), it's put on the **finalization queue** and cannot be collected in its current GC cycle. It needs TWO collections:
1. First GC: Detected as unreachable, moved to f-reachable queue
2. Finalizer thread runs the finalizer
3. Second GC: Actually collected

This means **finalizers promote objects to Gen 2** — very expensive. Use `IDisposable` + `Dispose()` instead.

### The IDisposable Pattern

```csharp
public class ResourceHolder : IDisposable
{
    private bool _disposed;
    private SafeHandle _handle; // Managed wrapper for unmanaged resource

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this); // Tell GC: no need to finalize, we already cleaned up
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _handle?.Dispose(); // Managed resource cleanup
        }
        // Unmanaged cleanup here (if any)
        _disposed = true;
    }

    ~ResourceHolder() // Finalizer — safety net only
    {
        Dispose(disposing: false);
    }
}
```

---

## 3. Code Examples

### Example 1 — Identifying and Avoiding GC Pressure
```csharp
// BAD: Allocating in a hot loop — Gen 0 hammered
public string ProcessRequest(HttpRequest request)
{
    var parts = request.Path.Split('/');       // string[] allocation
    var sb = new StringBuilder();              // StringBuilder allocation
    foreach (var part in parts)
        sb.Append(part.ToUpper());            // More string allocations per part
    return sb.ToString();
}

// BETTER: Use Span<char> to avoid intermediate allocations
public string ProcessRequest(HttpRequest request)
{
    var path = request.Path.Value.AsSpan();
    Span<char> buffer = stackalloc char[path.Length]; // Stack allocation!
    path.ToUpperInvariant(buffer);
    return new string(buffer);
}
```

### Example 2 — Object Pooling to Reduce GC
```csharp
// Microsoft.Extensions.ObjectPool — prevents repeated allocations
public class ReportService
{
    private readonly ObjectPool<StringBuilder> _sbPool;

    public ReportService(ObjectPoolProvider poolProvider)
    {
        _sbPool = poolProvider.CreateStringBuilderPool();
    }

    public string GenerateReport(IEnumerable<Order> orders)
    {
        var sb = _sbPool.Get(); // Rent from pool — no allocation
        try
        {
            foreach (var order in orders)
                sb.AppendLine($"{order.Id}: {order.Total:C}");
            return sb.ToString();
        }
        finally
        {
            _sbPool.Return(sb); // Return to pool — no GC
        }
    }
}

// ArrayPool<T> — same concept for arrays
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    int bytesRead = stream.Read(buffer, 0, buffer.Length);
    // process...
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer); // No GC collection needed
}
```

---

## 4. Interview Questions

1. **How does the .NET GC generational model work? Why are there 3 generations?**
2. **What is the Large Object Heap and why doesn't it compact by default?**
3. **What is the difference between `Dispose()` and a finalizer?**
4. **What is `GC.SuppressFinalize()` and why do you call it in `Dispose()`?**
5. **What is a Stop-the-World pause? When does it happen?**

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These separate engineers who understand system performance from those who just use `using` blocks.*

- If I call `GC.Collect()` manually, what generation does it collect?
  *(All by default — Gen 0, 1, and 2. You can specify: `GC.Collect(0)` for Gen 0 only.)*
- What happens to an object's generation when it survives a collection?
  *(Promoted to the next generation — Gen 0 → Gen 1 → Gen 2)*
- What is a memory leak in a GC-managed language? How can objects stay in memory forever?
  *(Static references, event subscriptions without removal, long-lived caches)*
- What is `GC.KeepAlive()` and when would you use it?
- How does Server GC differ from Workstation GC in terms of heap structure?
- What monitoring tools would you use to diagnose GC pressure in production?
  *(dotnet-counters, dotnet-trace, Application Insights live metrics, EventSource)*
- What is `GCSettings.LargeObjectHeapCompactionMode` and when would you set it?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Event subscription memory leak
public class EventLeaker
{
    public EventLeaker(EventPublisher publisher)
    {
        publisher.OnEvent += HandleEvent; // ❌ Never unsubscribed!
        // publisher holds reference to this → this never GC'd
    }

    // FIX: Implement IDisposable and -= in Dispose()
    public void Dispose() => _publisher.OnEvent -= HandleEvent;
}

// MISTAKE 2: Caching too aggressively — objects promoted to Gen 2
private static readonly Dictionary<int, LargeReport> _cache = new();
// If LargeReport is 10MB and you have 100 entries, Gen 2 has 1GB!
// Use IMemoryCache with expiration, or WeakReference<T>

// MISTAKE 3: LOH fragmentation — allocating many large arrays
for (int i = 0; i < 1000; i++)
{
    var buffer = new byte[100_000]; // ≥85KB → goes to LOH, never compacted
    ProcessBuffer(buffer);
}
// FIX: Use ArrayPool<byte>.Shared.Rent(100_000)

// MISTAKE 4: Calling GC.Collect() in production
GC.Collect(); // ❌ Forces STW pause on Gen 2 — terrible for latency
```

---

## 7. Real-World Usage

| Scenario | GC Knowledge Applied |
|----------|---------------------|
| High-throughput APIs | Object pooling (`ArrayPool`, `ObjectPool`) to reduce Gen 0 pressure |
| Memory diagnostics | `dotnet-counters monitor` to watch GC gen0/gen1/gen2 collection rates |
| Large file processing | `ArrayPool<byte>` instead of `new byte[]` per request |
| Caching | `WeakReference<T>` for non-critical caches — GC can collect when needed |
| Unmanaged interop | Finalizer + IDisposable pattern for `SafeHandle` |
| LOH optimization | `GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce` |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | GC collects unused objects, IDisposable for cleanup |
| **Level 2** | Generations, LOH, finalizer cost, Dispose pattern |
| **Level 3** | STW pauses, Server vs Workstation GC, background GC, memory leaks |
| **Level 4** | GC monitoring, object pooling, pinning, low-latency GC mode (`GCLatencyMode.SustainedLowLatency`) |

---

## 🔗 Connected Topics

- [Memory Management](../03-advanced/memory-management.md) — Stack vs heap, IDisposable in practice
- [Span & Memory](../03-advanced/span-memory.md) — Stack allocations that avoid GC entirely
- [async/await](../03-advanced/async-await.md) — async state machines allocate; ValueTask reduces GC load
- [CLR Internals](./clr-internals.md) — GC is a subsystem of the CLR
- [Performance Tuning](./performance-tuning.md) — GC tuning is a core part of .NET perf work

> 🎯 **Interviewer Mindset:** "Have you ever dealt with memory pressure in production?" is the gateway question. Be ready to explain Gen 0 collection frequency, object pooling, and how you'd use `dotnet-counters` to diagnose the issue. Nobody expects you to memorize GC source code — they expect you to know how to reason about it and fix it.

---

*Created: April 2026 · Level: Expert*
