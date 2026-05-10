# Chapter 44 � Low-Level Performance Tuning

> **⚡ Core Idea (30 seconds):** Performance tuning in .NET means: measure first (BenchmarkDotNet), eliminate allocations (Span, ArrayPool, ObjectPool), reduce GC pressure, help the JIT optimize (sealed, inlining), and use the right data structure. **Never optimize without measuring.**

**Domain:** `C#` **Level:** `Expert` **Tags:** `#performance` `#benchmarking` `#allocation` `#optimization`

---

## 1. Core Idea

The golden rule: **"premature optimization is the root of all evil"** (Knuth). Profile first, optimize the 20% that causes 80% of the bottleneck. In .NET the usual culprits are: excessive allocations (GC pressure), boxing, synchronous I/O on async paths, inefficient algorithms, and lock contention.

---

## 2. Deep Explanation

### Performance Categories in .NET

| Category | Symptoms | Fix |
|----------|---------|-----|
| **Excessive allocations** | High Gen 0 GC rate | Span<T>, ArrayPool, ObjectPool, record struct |
| **Boxing** | Value types stored as object | Generics, avoid non-generic collections |
| **I/O blocking** | Thread pool starvation | async/await, ConfigureAwait(false) |
| **Virtual dispatch overhead** | Hot loop on interfaces | sealed classes, concrete types |
| **Lock contention** | High CPU, low throughput | ConcurrentDictionary, Channel<T>, lockless patterns |
| **Large LOH allocations** | LOH fragmentation | ArrayPool<byte>, Memory<T> |
| **Memory copies** | Redundant data movement | `ref` params, Span<T> slicing |

### Measuring — BenchmarkDotNet

Never guess. Measure:
```csharp
[MemoryDiagnoser]  // Shows allocations per operation
[RankColumn]       // Ranks by speed
public class StringBenchmark
{
    private string[] _items = Enumerable.Range(0, 100).Select(i => i.ToString()).ToArray();

    [Benchmark(Baseline = true)]
    public string Concat()
    {
        string result = "";
        foreach (var s in _items) result += s;
        return result;
    }

    [Benchmark]
    public string StringBuilder()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var s in _items) sb.Append(s);
        return sb.ToString();
    }

    [Benchmark]
    public string StringJoin() => string.Join("", _items);
}
// Run: dotnet run -c Release
```

### Zero-Allocation Patterns

```csharp
// Pattern 1: ArrayPool — reuse byte arrays
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try { await stream.ReadAsync(buffer.AsMemory(0, 4096), ct); }
finally { ArrayPool<byte>.Shared.Return(buffer); }

// Pattern 2: ObjectPool — reuse expensive objects
private readonly ObjectPool<MyExpensiveObject> _pool;
var obj = _pool.Get();
try { obj.Process(data); }
finally { _pool.Return(obj); }

// Pattern 3: Span for string operations — no intermediate strings
ReadOnlySpan<char> input = "2024-04-20".AsSpan();
int year = int.Parse(input[..4]);  // No "2024" string created
int month = int.Parse(input[5..7]);

// Pattern 4: stackalloc for small arrays — zero GC
Span<int> buffer2 = stackalloc int[16]; // Stack, no GC
FillBuffer(buffer2);
```

### Ref Returns and Ref Locals

```csharp
// Avoid copying large structs — return by reference
public ref Item GetItem(int index)
{
    return ref _items[index]; // Returns reference to array element — no copy!
}

// Caller modifies in-place
ref Item item = ref GetItem(5);
item.Price = 99.99m; // Modifies original in array directly
```

### Reducing Lock Contention

```csharp
// BAD: Global lock — all threads serialize
private readonly object _lock = new();
private readonly Dictionary<int, Product> _cache = new();
public Product GetOrAdd(int id) { lock(_lock) { return _cache.GetOrAdd(id, LoadProduct); } }

// GOOD: ConcurrentDictionary — lock-free reads, striped locks for writes
private readonly ConcurrentDictionary<int, Product> _cache = new();
public Product GetOrAdd(int id) => _cache.GetOrAdd(id, LoadProduct); // Lock-free reads!

// BEST for hot reads: ImmutableDictionary published atomically
private volatile ImmutableDictionary<int, Product> _cache = ImmutableDictionary<int, Product>.Empty;
public Product? Get(int id) => _cache.TryGetValue(id, out var p) ? p : null; // No lock at all
public void Add(Product p) => _cache = _cache.Add(p.Id, p); // Atomic swap
```

---

## 3. Code Examples

### Real-World — High-performance HTTP payload processing
```csharp
// BEFORE: 3 allocations per request, string parsing
public Order ParseRequest(string json)
{
    var dto = JsonSerializer.Deserialize<OrderDto>(json); // Deserializes from string
    return MapToOrder(dto!);
}

// AFTER: Zero-allocation JSON parsing from raw bytes
public Order ParseRequest(ReadOnlySpan<byte> utf8Json)
{
    var dto = JsonSerializer.Deserialize<OrderDto>(utf8Json); // Reads from Span<byte>
    return MapToOrder(dto!);
}
// ASP.NET Core passes ReadOnlySpan<byte> from request body — chain the Span through
```

### CPU-Bound: SIMD with System.Numerics
```csharp
// Scalar: processes 1 float per instruction
public float SumScalar(float[] values)
{
    float sum = 0;
    foreach (var v in values) sum += v;
    return sum;
}

// SIMD: processes 4 or 8 floats per instruction (or let JIT vectorize)
public float SumSimd(ReadOnlySpan<float> values)
{
    var sum = Vector<float>.Zero;
    int i = 0;
    int vectorSize = Vector<float>.Count; // 4 on SSE2, 8 on AVX2

    for (; i <= values.Length - vectorSize; i += vectorSize)
        sum += new Vector<float>(values.Slice(i, vectorSize));

    float result = 0;
    for (int j = 0; j < vectorSize; j++) result += sum[j];
    for (; i < values.Length; i++) result += values[i]; // Remainder
    return result;
}
```

---

## 4. Interview Questions

1. **How do you identify performance bottlenecks in a .NET application?**
   *Start with measurement, not guessing. Use: (1) **BenchmarkDotNet** for micro-benchmarks — measures allocations and throughput with JIT warmup handled correctly, (2) **`dotnet-trace`** with CPU sampling or allocation events for production-like profiling, (3) **`dotnet-counters monitor`** to watch GC rates, CPU%, thread pool queue length in real time, (4) **Application Insights** or Prometheus for distributed tracing and request latency. The bottleneck is usually excessive allocations, I/O blocking, or lock contention — not the algorithmic code you'd guess.*

2. **What is GC pressure and how do you reduce it?**
   *GC pressure is when the Gen 0 heap fills up frequently, causing high-frequency collections that pause the application. It's caused by too many short-lived allocations in hot paths. Common fixes: (1) `ArrayPool<byte>.Shared.Rent()` instead of `new byte[]` for large buffers, (2) `ObjectPool<T>` for expensive reusable objects, (3) `Span<T>` and `stackalloc` to avoid heap allocation for temporary data, (4) `struct`/`record struct` for small frequently-created values, (5) `StringBuilder` instead of string concatenation in loops.*

3. **What tools do you use to benchmark .NET code?**
   *`BenchmarkDotNet` is the standard — it handles JIT warmup, multiple iterations, statistical analysis, and reports both time and memory allocations per operation. For allocations and GC events at the process level, `dotnet-trace` with `--clreventlevel` exposes allocation events. `dotnet-dump` captures heap snapshots for memory leak analysis. `dotnet-gcdump` is faster for just GC/heap state. Visual Studio Profiler and JetBrains dotTrace/dotMemory are excellent GUI alternatives.*

4. **What is `ArrayPool<T>` and why is it used?**
   *`ArrayPool<T>` is a shared pool of reusable arrays — you rent an array, use it, return it. `ArrayPool<byte>.Shared.Rent(4096)` gives you an existing array without allocating a new one on the heap. This is critical for I/O workloads where every request might otherwise allocate a 4KB or 64KB buffer. Without pooling, each request's buffers flood Gen 0, triggering rapid GC collections. With ArrayPool, those arrays are recycled and the GC is not involved.*

5. **What is the difference between CPU-bound and memory-bound performance issues?**
   *CPU-bound: the bottleneck is raw computation — algorithms doing too much work. Fix with parallelism (`Parallel.For`, PLINQ), better algorithms (O(n log n) vs O(n²)), or JIT-friendly patterns (inlining, SIMD). Memory-bound: the bottleneck is data access patterns — cache misses, excessive allocations, GC pauses. Fix with data locality (process arrays sequentially, not random access), smaller data structures (`struct` over `class`), and allocation reduction (Span, ArrayPool). Profiling tells you which category you're in.*

---

## 5. Follow-up Questions

- What is the `gen0/gen1/gen2 collection rate` metric and how do you read it in production?
  *(Available via `dotnet-counters monitor` or Application Insights. High Gen 0 = many short-lived allocations. Gen 2 = long-lived objects or LOH pressure.)*
- What's the performance difference between `Dictionary.TryGetValue` and `Dictionary[key]`?
  *(Both O(1). TryGetValue avoids exception on miss — also slightly faster due to single hash lookup without potential throw path)*
- How does `Volatile.Read` differ from a regular read?
  *(Inserts a memory barrier — guarantees you read the latest value, not a CPU-cached stale value. Needed for lockless patterns.)*
- When would PLINQ be SLOWER than sequential LINQ?
  *(When partition overhead > work per element — use only when work per item is significant)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Optimizing without measuring — worst mistake possible
// "This must be slow" is not a measurement. Run BenchmarkDotNet first.

// MISTAKE 2: Over-using Span in non-perf-critical code — reduces readability
// Span is for hot paths. Normal CRUD APIs don't need it.

// MISTAKE 3: Thread pool starvation from sync-over-async
Task.Run(() => asyncMethod().Result).Wait(); // Blocks thread pool threads → starvation

// MISTAKE 4: Excessive lock granularity
lock (globalLock) { /* whole method */ } // ❌ Serializes everything
// FIX: Reduce lock scope, use Interlocked for simple increments
Interlocked.Increment(ref _counter); // Atomic, no lock needed

// MISTAKE 5: Creating HttpClient per request
public async Task<string> Get(string url)
{
    using var client = new HttpClient(); // ❌ Port exhaustion!
    return await client.GetStringAsync(url);
}
// FIX: Inject via IHttpClientFactory or use static/singleton HttpClient
```

---

## 7. Real-World Usage

| Scenario | Optimization Applied |
|----------|---------------------|
| JSON parsing at high RPS | `ReadOnlySpan<byte>`, `System.Text.Json` from bytes |
| Image/file processing | `ArrayPool<byte>`, `Parallel.ForEachAsync` |
| Metrics/counters | `Interlocked.Increment` — lockless atomic |
| Cached lookups | `ImmutableDictionary` or `ConcurrentDictionary` |
| String-heavy report generation | `StringBuilder` + pre-allocated capacity |
| Binary protocol parsing | `Span<T>`, `BinaryPrimitives`, `SequenceReader<T>` |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Profile before optimizing, algorithmic complexity |
| **Level 2** | Reduce allocations (ArrayPool, Span), avoid boxing |
| **Level 3** | Lock-free patterns, Volatile, Interlocked, BenchmarkDotNet deep results |
| **Level 4** | SIMD (System.Numerics), unsafe, stackalloc, custom memory allocators |

## 🔗 Connected Topics
- [Garbage Collection](./40-garbage-collection.md) — Allocation reduction → GC pressure reduction
- [Span & Memory](../04-advanced/34-span-memory.md) — Zero-alloc memory operations
- [JIT Compilation](./42-jit-compilation.md) — JIT-friendly code is inherently faster
- [Task Parallel Library](./43-task-parallel-library.md) — CPU parallelism for throughput

*Created: April 2026 · Level: Expert*