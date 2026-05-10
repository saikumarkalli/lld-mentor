# Chapter 43 � Task Parallel Library (TPL)

> **⚡ Core Idea (30 seconds):** The TPL is the layer above raw threads — it provides `Task`, `Parallel.For`, `PLINQ`, `Dataflow`, and `Channel<T>` for efficient parallel and concurrent programming. It manages work on the **thread pool** so you don't spin up OS threads manually.

**Domain:** `C#` **Level:** `Expert` **Tags:** `#tpl` `#parallel` `#plinq` `#channel` `#dataflow` `#concurrent`

---

## 1. Core Idea

A thread is a hammer. The TPL is a powered nail gun — it uses the same tool (threads) but manages them efficiently, limiting context switches, load-balancing across CPUs, and integrating with `async/await`. Use TPL for **CPU-bound** parallel work the same way you use `async/await` for I/O-bound work.

---

## 2. Deep Explanation

### Task vs Thread

| | `Thread` | `Task` |
|--|---------|--------|
| **Creates OS thread?** | ✅ Always (1MB stack) | ❌ Uses thread pool |
| **Lightweight?** | ❌ Expensive | ✅ Very cheap |
| **Supports async?** | ❌ | ✅ (`async/await`) |
| **Return value?** | ❌ | ✅ (`Task<T>`) |
| **Cancellation?** | Manual | `CancellationToken` |
| **Use for** | Never (in modern .NET) | Always |

### Parallel.For / Parallel.ForEach — CPU-Bound Parallelism

```csharp
// Single-threaded: 1 CPU core used
for (int i = 0; i < items.Length; i++)
    Process(items[i]);

// Parallel: all CPU cores used — good for CPU-bound work
Parallel.For(0, items.Length, i => Process(items[i]));

// With options — limit degree of parallelism
var options = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct };
Parallel.ForEach(items, options, item => Process(item));

// .NET 6+: Async parallel processing
await Parallel.ForEachAsync(items, async (item, ct) =>
{
    await ProcessAsync(item, ct); // Can await inside!
});
```

**Use Parallel when:** CPU-bound work takes meaningful time (> 1ms per item). Avoid for I/O-bound or very fast operations (overhead outweighs gain).

### PLINQ — Parallel LINQ

```csharp
// Sequential
var results = data.Where(x => IsCpuIntensive(x)).Select(Transform).ToList();

// Parallel — uses thread pool partitioning
var results = data
    .AsParallel()
    .WithDegreeOfParallelism(4)
    .WithCancellation(ct)
    .Where(x => IsCpuIntensive(x))
    .Select(Transform)
    .ToList();

// Preserve order (has performance cost):
.AsParallel().AsOrdered()
```

### Channel\<T\> — Producer/Consumer Pipeline

`Channel<T>` is a thread-safe, async-aware queue — the modern way to implement producer/consumer:

```csharp
// Create bounded channel (backpressure support)
var channel = Channel.CreateBounded<Order>(new BoundedChannelOptions(capacity: 100)
{
    FullMode = BoundedChannelFullMode.Wait // Producer waits when full
});

// Producer — runs on one or more threads
async Task ProduceAsync(ChannelWriter<Order> writer, CancellationToken ct)
{
    await foreach (var order in GetOrderStreamAsync(ct))
    {
        await writer.WriteAsync(order, ct); // Awaits if channel is full
    }
    writer.Complete(); // Signal no more items
}

// Consumer — runs on one or more threads
async Task ConsumeAsync(ChannelReader<Order> reader, CancellationToken ct)
{
    await foreach (var order in reader.ReadAllAsync(ct))
    {
        await ProcessOrderAsync(order);
    }
}

// Wire up
await Task.WhenAll(
    ProduceAsync(channel.Writer, ct),
    ConsumeAsync(channel.Reader, ct),
    ConsumeAsync(channel.Reader, ct)); // Multiple consumers!
```

### Dataflow (TPL Dataflow NuGet)

For complex pipeline topologies — blocks that can be linked, buffered, and throttled:

```csharp
// Pipeline: Download → Parse → Save
var downloader = new TransformBlock<Uri, string>(
    async uri => await httpClient.GetStringAsync(uri),
    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });

var parser = new TransformBlock<string, Document>(
    html => ParseHtml(html));

var saver = new ActionBlock<Document>(
    async doc => await _repo.SaveAsync(doc));

// Link the pipeline
downloader.LinkTo(parser, new DataflowLinkOptions { PropagateCompletion = true });
parser.LinkTo(saver, new DataflowLinkOptions { PropagateCompletion = true });

// Feed it
foreach (var uri in uris) await downloader.SendAsync(uri);
downloader.Complete();
await saver.Completion;
```

---

## 3. Interview Questions

1. **What is the difference between `Thread` and `Task` in .NET?**
   *`Thread` creates a full OS thread — 1MB stack, expensive to create and destroy, no return value, no built-in cancellation. `Task` is a lightweight unit of work that runs on the **thread pool** — cheap to create, supports `CancellationToken`, returns `Task<T>`, composes with `async/await` and `Task.WhenAll`. In modern .NET you almost never use `Thread` directly — always prefer `Task`.*

2. **When should you use `Parallel.ForEach` vs `await foreach`?**
   *`Parallel.ForEach`: for **CPU-bound** work — it partitions the input and uses all CPU cores to process items simultaneously. `await foreach`: for consuming `IAsyncEnumerable<T>` — it processes items one at a time as they arrive from an async source (DB, API, file). Use `Parallel.ForEachAsync` when you need both: parallel processing of async operations (e.g., HTTP requests in parallel). Never use `Parallel.ForEach` with blocking I/O — it wastes thread pool threads.*

3. **What is PLINQ and what is `AsOrdered()` for?**
   *PLINQ (Parallel LINQ) adds `.AsParallel()` to LINQ — it partitions the source collection and processes each partition on a different CPU thread, then merges results. By default results arrive in **undefined order** (faster). `.AsOrdered()` forces the output to maintain the same order as the input, which has a performance cost (requires reordering the merged results). Use `AsOrdered()` only when order matters to the caller.*

4. **What is `Channel<T>` and what problem does it solve over `ConcurrentQueue<T>`?**
   *`Channel<T>` is an async-aware, bounded or unbounded FIFO queue for producer/consumer scenarios. Unlike `ConcurrentQueue<T>`, it supports: (1) **async backpressure** — `WriteAsync` awaits if the channel is full rather than discarding or throwing, (2) **async reading** — `ReadAllAsync()` returns `IAsyncEnumerable<T>` so consumers don't need to spin/poll, (3) **completion signaling** — `writer.Complete()` tells consumers no more items are coming.*

5. **What is `MaxDegreeOfParallelism` and why shouldn't you set it too high?**
   *`MaxDegreeOfParallelism` limits how many threads the Parallel loop or PLINQ can use simultaneously. Setting it to CPU count (or Environment.ProcessorCount) is ideal for **CPU-bound** work — one thread per core avoids context-switch overhead. Setting it too high floods the thread pool, causing excessive context switching and thread pool starvation for other work in the same process. For I/O-bound parallel work, a higher value is acceptable since threads spend most time waiting, not consuming CPU.*

---

## 4. Follow-up Questions

- `Parallel.For` vs `Task.WhenAll` on a batch of tasks — what's the difference?
  *(Parallel.For: thread pool work-stealing partitioner — best for pure CPU work. Task.WhenAll: runs all tasks concurrently — best for I/O tasks that already are async.)*
- What happens if an exception is thrown inside `Parallel.ForEach`?
  *(Wrapped in `AggregateException` — contains all exceptions from all parallel bodies)*
- When would you choose `Channel<T>` over `BlockingCollection<T>`?
  *(Channel<T>: async-first, no thread blocking, modern. BlockingCollection<T>: legacy, blocks the thread)*
- What is TPL Dataflow vs building a pipeline with `Channel<T>`?
  *(Dataflow: more advanced features — branching, joining, retry. Channel: simpler, sufficient for linear pipelines)*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using Parallel for I/O-bound work (blocks thread pool threads!)
Parallel.ForEach(urls, url => httpClient.GetStringAsync(url).Result); // ❌ Blocks threads!
// FIX:
await Parallel.ForEachAsync(urls, async (url, ct) => await httpClient.GetStringAsync(url));

// MISTAKE 2: MaxDegreeOfParallelism = Environment.ProcessorCount for I/O = wrong
// For CPU-bound: ProcessorCount is right
// For I/O-bound: can be much higher (100, 200) since threads don't block CPU

// MISTAKE 3: Not propagating cancellation into Parallel
Parallel.ForEach(items, item => Process(item, ct)); // ❌ ct not wired to Parallel itself
Parallel.ForEach(items, new ParallelOptions { CancellationToken = ct }, item => Process(item, ct)); // ✅

// MISTAKE 4: ConcurrentQueue without backpressure in producer/consumer
// Producer can flood the queue — memory unbounded
// Use Channel<T> with BoundedChannelOptions instead
```

---

## 6. Real-World Usage

| Scenario | TPL Tool |
|----------|---------|
| Parallel image/file processing | `Parallel.ForEachAsync` |
| Parallel HTTP scraping | `Parallel.ForEachAsync` with `HttpClient` |
| Report generation from CPU computation | `Parallel.For` with work-stealing |
| Event processing pipeline | `Channel<T>` producer/consumer |
| ETL pipeline with branching | TPL Dataflow |
| Background queue (job processing) | `Channel<T>` + hosted service |

---

## 7. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Task vs Thread, basic Parallel.For |
| **Level 2** | PLINQ, MaxDegreeOfParallelism, Parallel.ForEachAsync |
| **Level 3** | Channel<T>, producer/consumer, backpressure |
| **Level 4** | TPL Dataflow pipeline topology, work-stealing scheduler internals |

## Connected Topics
- [async/await](../04-advanced/27-async-await.md) — TPL is the foundation for async
- [GC](./40-garbage-collection.md) — Parallel work increases allocation rate → GC pressure
- [Deadlocks](../04-advanced/31-deadlocks.md) — Parallel work can deadlock if locking incorrectly

*Created: April 2026 · Level: Expert*