# Memory Management & IDisposable

> **⚡ Core Idea (30 seconds):** The GC handles managed memory automatically, but **unmanaged resources** (file handles, DB connections, sockets, COM objects) must be explicitly released. `IDisposable` is the contract for deterministic cleanup. The `using` statement guarantees `Dispose()` is called even if exceptions occur.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#memory` `#idisposable` `#using` `#dispose` `#unmanaged`

---

## 1. Core Idea

Think of unmanaged resources like renting a car. The GC handles your garbage at home (managed memory). But the rental car (DB connection, file handle) — you must return it yourself. If you don't, nobody else can rent it. `IDisposable` + `using` is the return-the-car system.

---

## 2. Deep Explanation

### Managed vs Unmanaged Resources

| | Managed | Unmanaged |
|--|---------|-----------|
| **Examples** | objects, arrays, strings | File handles, sockets, DB connections, GDI handles |
| **Cleanup** | GC handles automatically | Must be explicitly released |
| **When cleaned** | Non-deterministic (GC schedule) | Deterministic via `Dispose()` |
| **Risk if not cleaned** | Memory pressure (but will eventually GC) | Resource exhaustion, file locks, connection pool starvation |

### The Standard Dispose Pattern

```csharp
public class ResourceHolder : IDisposable
{
    private FileStream? _stream;        // Managed wrapper (itself IDisposable)
    private IntPtr _unmanagedHandle;    // Raw unmanaged handle
    private bool _disposed;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this); // No need for GC to call finalizer — we already cleaned up
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _stream?.Dispose(); // Managed resource — only dispose during Dispose(), not finalizer
        }

        // Unmanaged cleanup — safe to do in both Dispose() and finalizer
        if (_unmanagedHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(_unmanagedHandle);
            _unmanagedHandle = IntPtr.Zero;
        }

        _disposed = true;
    }

    ~ResourceHolder() // Finalizer — safety net if Dispose() was not called
    {
        Dispose(disposing: false); // DON'T touch managed resources here — they may be GC'd already
    }
}
```

### `using` Statement — The Pattern Enforcer

```csharp
// Classic using — Dispose() called at } even if exception thrown
using (var stream = new FileStream("file.txt", FileMode.Open))
{
    // work with stream
} // Dispose() called here

// C# 8 using declaration — Dispose() called at end of enclosing scope
using var stream = new FileStream("file.txt", FileMode.Open);
// ... work ...
// Dispose() called at end of method
```

### Dispose vs Finalizer

| | `Dispose()` | Finalizer (`~T()`) |
|--|-------------|-------------------|
| **Triggered by** | Developer explicitly / `using` | GC (nondeterministic) |
| **Timing** | Immediate | Unpredictable |
| **Cost** | Cheap — called once | Expensive — adds extra GC round-trip |
| **Access to managed objects** | ✅ Safe | ❌ Unsafe (may be GC'd) |
| **Should call** | `GC.SuppressFinalize(this)` | Nothing — just clean up unmanaged |

---

## 3. Code Examples

### Basic — Correct Resource Handling
```csharp
// BAD: Resource leaked if exception thrown
var conn = new SqlConnection(connStr);
conn.Open();
var cmd = new SqlCommand("SELECT ...", conn);
var reader = cmd.ExecuteReader();
// If exception here, conn/reader never disposed!

// GOOD: using ensures cleanup
await using var conn = new SqlConnection(connStr);
await conn.OpenAsync();
await using var cmd = new SqlCommand("SELECT ...", conn);
await using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync()) { /* ... */ }
// All disposed in reverse order automatically
```

### Real-World — Custom IDisposable Service
```csharp
public class ReportExporter : IDisposable
{
    private readonly ExcelPackage _excel; // EPPlus — wraps COM/unmanaged
    private readonly MemoryStream _stream;
    private bool _disposed;

    public ReportExporter()
    {
        _stream = new MemoryStream();
        _excel = new ExcelPackage(_stream);
    }

    public byte[] Export(IEnumerable<Order> orders)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); // Guard against use-after-dispose

        var sheet = _excel.Workbook.Worksheets.Add("Orders");
        int row = 1;
        foreach (var o in orders)
        {
            sheet.Cells[row, 1].Value = o.Id;
            sheet.Cells[row, 2].Value = o.Total;
            row++;
        }
        _excel.Save();
        return _stream.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _excel.Dispose();
        _stream.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

// Usage
using var exporter = new ReportExporter();
var bytes = exporter.Export(orders);
await File.WriteAllBytesAsync("report.xlsx", bytes);
// exporter.Dispose() called automatically
```

---

## 4. Interview Questions

1. **What is `IDisposable` and why does it exist?**
   *`IDisposable` is a contract for **deterministic cleanup** of resources the GC doesn't manage — file handles, DB connections, sockets, GDI objects. The GC cleans managed memory automatically but doesn't know about OS-level resources. `IDisposable.Dispose()` lets you release them immediately and predictably, not at some unknown future GC time.*

2. **What is the difference between `Dispose()` and a finalizer?**
   *`Dispose()`: called explicitly (or by `using`) — runs immediately, full access to managed resources. Finalizer (`~T()`): called by the GC at some unpredictable future time — it's a safety net if `Dispose()` was never called. Finalizers run on a dedicated finalizer thread, can't safely access managed objects (they may already be GC'd), and add a full extra GC round-trip cost. Always prefer `Dispose()` + `GC.SuppressFinalize` over relying on finalizers.*

3. **What does `GC.SuppressFinalize(this)` do and why do you call it in `Dispose()`?**
   *It tells the GC: "Don't bother running the finalizer for this object — we already cleaned up in `Dispose()`." Without this call, objects with finalizers survive an extra GC collection cycle (they're moved to the finalization queue). Calling it in `Dispose()` removes the object from the queue entirely, preventing the overhead and the double-cleanup.*

4. **What does the `using` statement guarantee?**
   *The compiler transforms `using (var x = new T()) { ... }` into a `try/finally` block where `Dispose()` is called in the `finally`. This guarantees `Dispose()` is called **even if an exception is thrown** inside the block. The C# 8 `using var` declaration does the same at the end of the enclosing scope.*

5. **What is the difference between managed and unmanaged resources?**
   *Managed resources: .NET objects on the heap — arrays, strings, other class instances. The GC tracks and collects these automatically. Unmanaged resources: OS-level handles — file descriptors, socket handles, Windows GDI objects, database connections. The GC has no knowledge of these — if you don't explicitly close them via `Dispose()` or equivalent, they leak until the process exits.*

---

## 5. Follow-up Questions

- If `Dispose()` is called twice, what should happen? *(Nothing bad — guard with `if (_disposed) return;` — idempotent)*
- In the standard dispose pattern, why is it unsafe to access managed resources in the finalizer? *(The GC may have already collected them — finalizers run in unpredictable order)*
- What is `IAsyncDisposable` and when would you use it instead of `IDisposable`? *(When cleanup itself is async — e.g., flushing a network buffer: `await using var conn = ...`)*
- What happens if you throw an exception inside a `using` block? *(Dispose is still called — guaranteed by the try/finally the compiler generates)*
- What is `SafeHandle` and why is it preferred over raw `IntPtr` for unmanaged handles?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using object after Dispose
var reader = new StreamReader("file.txt");
reader.Dispose();
var line = reader.ReadLine(); // ❌ ObjectDisposedException

// MISTAKE 2: Not disposing in error paths (forgetting using)
var client = new HttpClient(); // ❌ Never disposed — socket leak
var response = await client.GetAsync(url); // If exception, client leaks

// MISTAKE 3: Calling Dispose() manually AND having 'using' — double dispose
using var stream = new FileStream("x", FileMode.Open);
stream.Dispose(); // Disposing twice — should be idempotent but avoid this

// MISTAKE 4: Accessing managed resources in finalizer
~MyClass()
{
    _managedObject.DoSomething(); // ❌ _managedObject may already be finalized!
}
```

---

## 7. Real-World Usage

| Scenario | Dispose Pattern |
|----------|----------------|
| `HttpClient` | Should be reused (not disposed per-request) — use `IHttpClientFactory` |
| `DbContext` | Disposed per-request in DI (scoped lifetime) |
| File streams | `using var stream = File.OpenRead(path)` |
| `SqlConnection` | `await using var conn = new SqlConnection(...)` |
| `MemoryStream` | Technically doesn't need disposing (no unmanaged), but good practice |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | IDisposable, using statement, basic cleanup |
| **Level 2** | Full dispose pattern, GC.SuppressFinalize, managed vs unmanaged |
| **Level 3** | IAsyncDisposable, double-dispose safety, SafeHandle |
| **Level 4** | Finalizer threading, finalization queue, GC generations impact of finalizers |

## 🔗 Connected Topics
- [Garbage Collection](../04-expert/garbage-collection.md) — Finalizers interact with GC generations
- [Memory Leaks](../04-expert/memory-leaks.md) — Not disposing causes resource leaks
- [async/await](./async-await.md) — `IAsyncDisposable` and `await using`

*Created: April 2026 · Level: Advanced*
