# Chapter 32 � Parallel Programming

> **⚡ Core Idea (30 seconds):** Parallel programming is about splitting **CPU-bound** work across multiple processor cores to run simultaneously. `Parallel.For/ForEach` partitions a collection and runs iterations on ThreadPool threads. PLINQ adds parallelism to LINQ queries. They are NOT for I/O work — use `async/await` for that.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#parallel` `#plinq` `#cpu-bound` `#concurrency`

---

## 1. Core Idea

Think of it like a **production line in a factory**. If you have 10,000 packages to label:
- **Sequential (`foreach`):** One worker labels them one by one. Takes 10,000 seconds.
- **Parallel (`Parallel.ForEach`):** 8 workers each grab a stack. Takes ~1,250 seconds.
- **Over-parallelised (1000 workers):** Workers bump into each other fighting for the labeling machine (context switching). Takes 15,000 seconds — SLOWER than sequential.

This is the foundation of: **batch processing, image/video processing, scientific computing, and data pipeline stages**.

---

## 2. Deep Explanation

### Parallel.For and Parallel.ForEach

```csharp
// Process a batch of images using all CPU cores
Parallel.ForEach(images, image => ResizeImage(image));
```

Internally, TPL uses a **Partitioner** to divide the collection into chunks. Each chunk is assigned to a ThreadPool thread. The `MaxDegreeOfParallelism` option controls the upper bound.

### ParallelOptions

```csharp
var options = new ParallelOptions
{
    MaxDegreeOfParallelism = Environment.ProcessorCount, // Match CPU cores
    CancellationToken = cancellationToken,
    TaskScheduler = TaskScheduler.Default
};
```

### Parallel.ForEachAsync (.NET 6+)

The game-changer for I/O-bound parallel work. Unlike `Parallel.ForEach` (which blocks threads), `ForEachAsync` allows `await` in the body, releasing threads during I/O:

```csharp
await Parallel.ForEachAsync(urls, options, async (url, ct) =>
{
    var data = await _httpClient.GetStringAsync(url, ct); // Thread released during I/O!
    await _db.SaveAsync(data, ct);
});
```

### PLINQ (Parallel LINQ)

```csharp
var results = data
    .AsParallel()
    .WithDegreeOfParallelism(4)
    .Where(x => x.IsValid)
    .Select(x => Transform(x))
    .ToList();
```

**Warning:** PLINQ does NOT preserve order by default. Use `.AsOrdered()` if order matters (at a performance cost).

---

## 3. Code Examples

### Example 1 — Basic: Sequential vs Parallel Comparison
```csharp
var records = Enumerable.Range(0, 10000).ToList();

// Sequential — uses 1 core
var sw1 = Stopwatch.StartNew();
foreach (var r in records) { CpuIntensiveWork(r); }
Console.WriteLine($"Sequential: {sw1.ElapsedMilliseconds}ms");

// Parallel — uses all cores
var sw2 = Stopwatch.StartNew();
Parallel.ForEach(records, new ParallelOptions 
    { MaxDegreeOfParallelism = Environment.ProcessorCount },
    r => CpuIntensiveWork(r));
Console.WriteLine($"Parallel: {sw2.ElapsedMilliseconds}ms");
// Expect ~4-8x speedup on an 8-core machine
```

### Example 2 — Real-World: Thread-Local State for Aggregation
```csharp
// ❌ Bad: Shared state with lock — serializes everything
long total = 0;
object lockObj = new();
Parallel.ForEach(records, record =>
{
    var value = Compute(record);
    lock (lockObj) { total += value; } // ❌ Lock contention kills parallelism
});

// ✅ Good: Thread-local accumulators — no locking during processing
long total = 0;
Parallel.ForEach(
    records,
    () => 0L,                          // Initialize per-thread subtotal
    (record, state, subtotal) =>        // Body — each thread accumulates locally
    {
        return subtotal + Compute(record); // No lock!
    },
    subtotal => Interlocked.Add(ref total, subtotal) // Merge at end — minimal contention
);
```

### Example 3 — Real-World: Parallel.ForEachAsync for HTTP Enrichment
```csharp
// Enrich 10,000 orders from an external API — but only 20 concurrent HTTP calls
var options = new ParallelOptions { MaxDegreeOfParallelism = 20 };

await Parallel.ForEachAsync(orders, options, async (order, ct) =>
{
    var pricing = await _pricingApi.GetAsync(order.Sku, ct);
    order.Price = pricing.Amount;
});
```

### Example 4 — PLINQ with Ordering
```csharp
// ❌ Bad: PLINQ reorders results by default
var results = data.AsParallel()
    .Select(x => ExpensiveTransform(x))
    .ToList(); // Order scrambled!

// ✅ Good: Preserve order when it matters
var results = data.AsParallel()
    .AsOrdered()  // Preserves input order (slight perf cost)
    .Select(x => ExpensiveTransform(x))
    .ToList();
```

---

## 4. Interview Questions

1. **When should you use Parallel.ForEach instead of a regular foreach?**
   *When processing a large collection where each item requires significant CPU-bound computation (like image resizing, hashing, or mathematical calculations), and each item can be processed independently without shared mutable state. For small collections or I/O-bound work, the parallelization overhead outweighs the benefit.*

2. **What happens if you don't set MaxDegreeOfParallelism?**
   *The default is `-1` (unlimited). The TPL will use as many ThreadPool threads as it deems optimal. For CPU-bound work, this is usually fine (it converges to the core count). For I/O-bound work with sync blocking, it can exhaust the ThreadPool. Always explicitly set it for I/O operations.*

3. **What is the difference between Parallel.ForEach and Parallel.ForEachAsync?**
   *`Parallel.ForEach` expects a synchronous body. If you call `.Result` on an async operation inside it, you block ThreadPool threads. `Parallel.ForEachAsync` (.NET 6+) accepts an async body, releasing threads during `await`. Use `ForEach` for CPU-bound, `ForEachAsync` for I/O-bound parallel work.*

4. **Does PLINQ preserve the order of results?**
   *No, by default PLINQ reorders results for maximum throughput. The item that finishes processing first appears first. To preserve original order, call `.AsOrdered()`, which adds a merge step with a performance cost.*

5. **You have a shared counter inside a Parallel.ForEach body. What happens?**
   *Without synchronization, you get a race condition — the counter will have an incorrect final value. Using `lock` serializes the counter update, killing parallelism. The correct approach is thread-local accumulators (the `Parallel.ForEach` overload with `localInit`, `body`, `localFinally`) or `Interlocked.Add`.*

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These check if you know when NOT to parallelize.*

- When is parallel processing SLOWER than sequential?
  *(Small collections where partitioning overhead exceeds computation time. Work with heavy shared-state contention. I/O-bound operations in `Parallel.ForEach` — threads block waiting for network.)*
- Can you cancel a Parallel.ForEach mid-execution?
  *(Yes. Pass a `CancellationToken` in `ParallelOptions`. The TPL checks the token between iterations. Already-running iterations complete, but no new ones start.)*
- What is the `Partitioner` and why might you customize it?
  *(It divides the collection into chunks for distribution. Default range partitioning works well for arrays. For `IEnumerable` sources, it creates small batches. Customize with `Partitioner.Create()` for specific chunk sizes.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using Parallel.ForEach for I/O-bound work
Parallel.ForEach(urls, url =>
{
    var data = _httpClient.GetStringAsync(url).Result; // ❌ Blocks threads!
});
// FIX: Use Parallel.ForEachAsync
await Parallel.ForEachAsync(urls, async (url, ct) =>
{
    var data = await _httpClient.GetStringAsync(url, ct); // ✅
});

// MISTAKE 2: Modifying a shared collection inside Parallel.ForEach
var results = new List<string>(); // ❌ List<T> is NOT thread-safe
Parallel.ForEach(items, item =>
{
    results.Add(Process(item)); // Race condition → index corruption
});
// FIX: Use ConcurrentBag or pre-allocate and use index
var results = new ConcurrentBag<string>(); // ✅

// MISTAKE 3: AsParallel() on a tiny collection
var result = smallList.AsParallel().Select(x => x * 2).ToList();
// ❌ Overhead of partitioning/merging exceeds the work

// MISTAKE 4: PLINQ with side effects
data.AsParallel().ForAll(item => Console.WriteLine(item));
// ❌ Console.WriteLine has a lock — serializes everything
```

---

## 7. Real-World Usage

| Scenario | Tool |
|----------|------|
| **Batch image/PDF processing** | `Parallel.ForEach` (CPU-bound) |
| **Parallel HTTP enrichment** | `Parallel.ForEachAsync` (I/O-bound) |
| **Large LINQ aggregations** | PLINQ `.AsParallel()` |
| **Monte Carlo simulations** | `Parallel.For` with thread-local state |
| **ETL data transformation** | `Parallel.ForEach` + `ConcurrentBag` |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | `Parallel.ForEach` basic usage, MaxDegreeOfParallelism |
| **Level 2** | Thread-local state, `ForEachAsync`, PLINQ ordering |
| **Level 3** | Custom partitioners, `ParallelLoopState.Break/Stop`, cancellation |
| **Level 4** | PLINQ query plans, merge options, `ForAll` vs `ToList` trade-offs |

---

## 🔗 Connected Topics

- [Task vs Thread](./29-task-vs-thread.md) — Parallel APIs use the ThreadPool internally
- [Synchronization](./30-synchronization.md) — Shared state in parallel loops needs protection
- [Async/Await](./27-async-await.md) — `ForEachAsync` bridges parallel + async worlds
- [CancellationToken](./28-cancellation-token.md) — Pass tokens to stop parallel operations early

> 🎯 **Interviewer Mindset Note:** *"When would you NOT use Parallel.ForEach?"* → Small collections, I/O-bound work, shared mutable state → *"What about Parallel.ForEachAsync?"* → That fixes the I/O problem → *"How do you aggregate results safely?"* → Thread-local accumulators + Interlocked merge.

---

*Created: May 2026 · Level: Advanced*