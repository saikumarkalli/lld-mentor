# Chapter 30 � Synchronization Primitives

> **⚡ Core Idea (30 seconds):** When multiple threads access shared mutable state simultaneously, you get **race conditions** — data corruption that produces wrong results silently. Synchronization primitives ensure only one (or a controlled number of) threads can access critical code sections at a time.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#synchronization` `#lock` `#semaphore` `#thread-safety` `#concurrency`

---

## 1. Core Idea

Think of shared state like a **single-lane bridge**. If two cars (threads) enter from opposite sides simultaneously, they crash (race condition). A traffic light (`lock`) ensures only one car crosses at a time. A toll booth with 3 lanes (`SemaphoreSlim(3)`) allows up to 3 cars simultaneously.

This is the foundation of: **thread-safe caches, connection pooling, rate limiters, producer/consumer patterns, and concurrent data access**.

---

## 2. Deep Explanation

### The Race Condition Bug

```csharp
private int _count = 0;

// Two threads call this simultaneously
void Increment() { _count++; }
```

`_count++` is NOT atomic. It compiles to:
1. Read `_count` from memory → register (value: 0)
2. Add 1 in register (value: 1)
3. Write register → `_count` (value: 1)

If two threads interleave at step 1 (both read 0), both write 1. Expected: 2. Actual: 1.

### Synchronization Primitives

| Primitive | Scope | Async? | Use Case |
|-----------|-------|--------|----------|
| `lock` | In-process | ❌ No | Protect memory access in sync code |
| `Monitor` | In-process | ❌ No | `lock` + `Wait()`/`Pulse()` signaling |
| `SemaphoreSlim` | In-process | ✅ Yes | Throttle concurrency (async-friendly) |
| `Semaphore` | Cross-process | ❌ No | Named OS semaphore |
| `Mutex` | Cross-process | ❌ No | Single instance app, cross-process lock |
| `Interlocked` | In-process | N/A | Atomic operations on primitives |
| `ReaderWriterLockSlim` | In-process | ❌ No | Many readers, rare writers |

### lock — The Most Common Primitive

```csharp
private readonly object _lock = new(); // Always lock on a dedicated private object

lock (_lock)
{
    _count++; // Only one thread at a time can execute this
}
```

The compiler translates `lock` into:
```csharp
bool lockTaken = false;
try
{
    Monitor.Enter(_lock, ref lockTaken);
    _count++;
}
finally
{
    if (lockTaken) Monitor.Exit(_lock);
}
```

### SemaphoreSlim — The Async-Compatible Throttle

You **cannot** `await` inside a `lock` block (the thread that enters the lock may differ from the thread that exits). `SemaphoreSlim` solves this with `WaitAsync()`.

```csharp
private readonly SemaphoreSlim _semaphore = new(1, 1); // Acts as an async lock

async Task SafeUpdateAsync()
{
    await _semaphore.WaitAsync();
    try
    {
        await _db.UpdateAsync(); // Safe to await inside!
    }
    finally
    {
        _semaphore.Release();
    }
}
```

### Interlocked — Lock-Free Atomic Operations

For simple counter/flag operations, `Interlocked` is orders of magnitude faster than `lock`:

```csharp
private int _requestCount = 0;

void OnRequest()
{
    Interlocked.Increment(ref _requestCount); // Atomic, no lock
}
```

---

## 3. Code Examples

### Example 1 — Basic: Race Condition and Fix
```csharp
// ❌ Bug: Race condition on shared state
public class BrokenCounter
{
    private int _count = 0;
    public void Increment() => _count++; // Not thread-safe!
    public int Count => _count;
}

// ✅ Fix 1: Using lock
public class LockedCounter
{
    private readonly object _lock = new();
    private int _count = 0;
    public void Increment() { lock (_lock) { _count++; } }
    public int Count { get { lock (_lock) { return _count; } } }
}

// ✅ Fix 2: Using Interlocked (much faster for simple operations)
public class InterlockedCounter
{
    private int _count = 0;
    public void Increment() => Interlocked.Increment(ref _count);
    public int Count => Volatile.Read(ref _count);
}
```

### Example 2 — Real-World: SemaphoreSlim as API Rate Limiter
```csharp
public class RateLimitedApiClient
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _throttle = new(10, 10); // Max 10 concurrent calls

    public async Task<string> GetAsync(string url, CancellationToken ct)
    {
        await _throttle.WaitAsync(ct); // Waits if 10 calls are in-flight
        try
        {
            return await _http.GetStringAsync(url, ct);
        }
        finally
        {
            _throttle.Release(); // Free a slot for the next caller
        }
    }
}

// Usage: call 1000 URLs concurrently — only 10 run at a time
var tasks = urls.Select(u => client.GetAsync(u, ct));
var results = await Task.WhenAll(tasks);
```

### Example 3 — Real-World: ReaderWriterLockSlim for Config Cache
```csharp
public class ConfigCache
{
    private readonly ReaderWriterLockSlim _rwLock = new();
    private Dictionary<string, string> _config = new();

    // Multiple threads can read simultaneously
    public string? GetValue(string key)
    {
        _rwLock.EnterReadLock();
        try { return _config.GetValueOrDefault(key); }
        finally { _rwLock.ExitReadLock(); }
    }

    // Only one thread can write — blocks all readers during write
    public void Reload(Dictionary<string, string> newConfig)
    {
        _rwLock.EnterWriteLock();
        try { _config = newConfig; }
        finally { _rwLock.ExitWriteLock(); }
    }
}
```

---

## 4. Interview Questions

1. **What is a race condition and how do you prevent it?**
   *A race condition occurs when two threads read and modify shared state simultaneously, producing unpredictable results. Prevention: use `lock` for synchronous code sections, `SemaphoreSlim` for async sections, or `Interlocked` for simple atomic operations on primitives. The best strategy is to minimize shared mutable state entirely.*

2. **What is the difference between `lock`, `SemaphoreSlim`, and `Mutex`?**
   *`lock` is the simplest — mutual exclusion within a single process, synchronous only. `SemaphoreSlim` supports async (`WaitAsync`) and can allow N concurrent entries (not just 1). `Mutex` is an OS-level construct for cross-process synchronization — used to ensure only one instance of an application runs.*

3. **Why can't you use `await` inside a `lock` block?**
   *`lock` is based on `Monitor.Enter/Exit`, which requires the same thread to enter and exit. When you `await`, the current thread yields to the pool. A different thread may resume the method after the await. That different thread would try to call `Monitor.Exit` on a lock it never entered — throwing `SynchronizationLockException`. Use `SemaphoreSlim(1,1)` instead.*

4. **When would you use `Interlocked` over `lock`?**
   *`Interlocked` uses CPU-level atomic instructions (like `LOCK XADD`) that are dramatically faster than OS-level lock objects. I use it for simple operations: incrementing counters, swapping references (`Interlocked.Exchange`), or compare-and-swap (`CompareExchange`). If the critical section involves multiple statements, `lock` is necessary.*

5. **What is `ReaderWriterLockSlim` and when is it useful?**
   *It allows multiple threads to read simultaneously but blocks all readers when a single writer enters. It's useful for read-heavy, write-rare scenarios — like a configuration cache that's read thousands of times per second but reloaded once a minute.*

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These expose whether you understand the costs.*

- What happens if you forget to call `_semaphore.Release()` in a finally block?
  *(The semaphore count is permanently decremented. Eventually, all slots are consumed and every subsequent `WaitAsync` call hangs forever — a resource leak that looks like a deadlock.)*
- Can you lock on `this` or on a `string` literal?
  *(Technically yes, but NEVER do it. `this` is publicly accessible — external code can lock on the same object and deadlock you. String literals are interned — all code using the same string literal shares the same reference, causing unintended global locks.)*
- What is `Volatile.Read` / `Volatile.Write`?
  *(They prevent the CPU and JIT from reordering reads/writes across the barrier. Without it, one thread may not see writes made by another thread due to CPU cache coherence delays.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Locking on 'this'
lock (this) { /* ... */ } // ❌ External code can lock on your instance
// FIX:
private readonly object _lock = new();
lock (_lock) { /* ... */ } // ✅ Private, dedicated lock object

// MISTAKE 2: Locking on a string
lock ("myLock") { } // ❌ String interning means ALL code using "myLock" shares the lock

// MISTAKE 3: Missing Release in SemaphoreSlim
await _semaphore.WaitAsync();
await DoWork(); // If this throws, Release() never runs!
// FIX: Always use try/finally
await _semaphore.WaitAsync();
try { await DoWork(); }
finally { _semaphore.Release(); }

// MISTAKE 4: Over-granular locking — lock around EVERY property
// This serializes your entire object and is worse than no concurrency
// FIX: Use immutable data structures or lock at the operation level

// MISTAKE 5: Nested locks in different orders — classic deadlock setup
// Thread A: lock(X) → lock(Y)
// Thread B: lock(Y) → lock(X) // ❌ DEADLOCK!
// FIX: Always acquire locks in the same global order
```

---

## 7. Real-World Usage

| Scenario | Primitive |
|----------|-----------|
| **Thread-safe counter** | `Interlocked.Increment` |
| **In-memory cache updates** | `lock` or `ReaderWriterLockSlim` |
| **HTTP call throttling** | `SemaphoreSlim(maxConcurrent)` |
| **Async mutual exclusion** | `SemaphoreSlim(1, 1)` |
| **Single-instance application** | Named `Mutex` |
| **ConcurrentDictionary atomic ops** | Built-in (but watch `GetOrAdd` factory!) |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | What a race condition is, basic `lock` usage |
| **Level 2** | `SemaphoreSlim.WaitAsync()`, `Interlocked`, why you can't await in lock |
| **Level 3** | `ReaderWriterLockSlim`, `Volatile`, `CompareExchange` patterns |
| **Level 4** | `SpinLock`, `SpinWait`, memory barriers, lock-free data structures |

---

## 🔗 Connected Topics

- [Task vs Thread](./29-task-vs-thread.md) — Tasks run on shared ThreadPool → shared state needs synchronization
- [Deadlocks](./31-deadlocks.md) — Incorrect lock ordering causes deadlocks
- [Async/Await](./27-async-await.md) — `SemaphoreSlim` is the async-compatible lock
- [CancellationToken](./28-cancellation-token.md) — `SemaphoreSlim.WaitAsync(CancellationToken)` for cancelable waits

> 🎯 **Interviewer Mindset Note:** *"How do you make this class thread-safe?"* → Identify the shared mutable state → Choose minimal primitive → *"What if I need async?"* → SemaphoreSlim → *"What about performance?"* → Interlocked for counters, immutable types to eliminate locking entirely.

---

*Created: May 2026 · Level: Advanced*