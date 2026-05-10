# Chapter 31 � Deadlocks

> **⚡ Core Idea (30 seconds):** A deadlock occurs when two or more threads are **permanently waiting for each other** to release resources. Thread A holds Lock 1 and waits for Lock 2. Thread B holds Lock 2 and waits for Lock 1. Neither can proceed — the application hangs forever.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#deadlock` `#synchronization` `#async` `#debugging`

---

## 1. Core Idea

Think of a deadlock like **two cars on a narrow one-lane bridge approaching from opposite directions**. Neither can reverse, neither can go forward. Both are blocked indefinitely. The only resolution is external intervention (killing the process).

This is the foundation of: **understanding sync-over-async bugs, lock ordering discipline, timeout strategies, and production incident debugging**.

---

## 2. Deep Explanation

### The Four Conditions for Deadlock (Coffman Conditions)

ALL four must be true simultaneously for a deadlock to occur:

| Condition | Meaning | Example |
|-----------|---------|---------|
| **Mutual Exclusion** | At least one resource is held exclusively | `lock(_obj)` |
| **Hold and Wait** | A thread holds one lock while waiting for another | `lock(A) { lock(B) { } }` |
| **No Preemption** | Locks can only be released voluntarily | `Monitor.Exit` only by holder |
| **Circular Wait** | Thread A waits for B, B waits for A | A→B, B→A |

Break ANY ONE condition → no deadlock.

### The Three Most Common Deadlocks in .NET

#### 1. Classic Lock Ordering Deadlock

```csharp
// Thread 1                          // Thread 2
lock (lockA)                         lock (lockB)
{                                    {
    Thread.Sleep(100);                   Thread.Sleep(100);
    lock (lockB) { /* ... */ }           lock (lockA) { /* ... */ }
}                                    }
// ❌ DEADLOCK: Thread 1 holds A, waits for B. Thread 2 holds B, waits for A.
```

#### 2. Sync-Over-Async Deadlock (The Most Common in .NET)

```csharp
// In ASP.NET (classic, not Core) or WPF/WinForms:
public string GetData()
{
    // This blocks the UI/request thread
    return GetDataAsync().Result; // ❌ DEADLOCK
}

private async Task<string> GetDataAsync()
{
    var data = await _httpClient.GetStringAsync("/api/data");
    // await tries to resume on the ORIGINAL SynchronizationContext thread
    // But that thread is blocked by .Result — nobody can move!
    return data;
}
```

**Why this happens:**
1. `.Result` blocks the calling thread (e.g., ASP.NET request thread with `SynchronizationContext`).
2. The async method finishes and the continuation needs to marshal back to that original thread.
3. But the original thread is frozen waiting for `.Result`.
4. Deadlock — both are waiting for each other.

**Why ASP.NET Core is safer:** ASP.NET Core removed `SynchronizationContext`, so continuations run on any ThreadPool thread. But `.Result` still causes ThreadPool starvation.

#### 3. SemaphoreSlim Deadlock (Forgotten Release)

```csharp
await _semaphore.WaitAsync();
await DoWorkAsync(); // This throws!
_semaphore.Release(); // Never reached! Semaphore count permanently reduced.
// After N failures, all WaitAsync() calls hang forever — looks like a deadlock.
```

---

## 3. Code Examples

### Example 1 — Classic Lock Ordering Deadlock and Fix
```csharp
// ❌ Bad: Different lock ordering in different methods
public class TransferService
{
    private readonly object _lockA = new();
    private readonly object _lockB = new();

    public void TransferAToB()
    {
        lock (_lockA) { lock (_lockB) { /* transfer */ } }
    }

    public void TransferBToA()
    {
        lock (_lockB) { lock (_lockA) { /* transfer */ } } // ❌ Reversed order!
    }
}

// ✅ Fix: Always acquire locks in the SAME global order
public class TransferServiceFixed
{
    private readonly object _lockA = new(); // "A" is always first (alphabetical)
    private readonly object _lockB = new();

    public void TransferAToB()
    {
        lock (_lockA) { lock (_lockB) { /* transfer */ } }
    }

    public void TransferBToA()
    {
        lock (_lockA) { lock (_lockB) { /* transfer */ } } // ✅ Same order!
    }
}
```

### Example 2 — Sync-Over-Async Deadlock and Fix
```csharp
// ❌ Bad: Blocking on async in UI/legacy ASP.NET
public class LegacyController
{
    public ActionResult Index()
    {
        var data = GetDataAsync().Result; // DEADLOCK on SynchronizationContext
        return View(data);
    }
}

// ✅ Fix 1: Async all the way (preferred)
public class ModernController
{
    public async Task<ActionResult> Index()
    {
        var data = await GetDataAsync(); // ✅ No blocking
        return View(data);
    }
}

// ✅ Fix 2: If stuck in sync code, use ConfigureAwait(false) in the async method
private async Task<string> GetDataAsync()
{
    var data = await _httpClient.GetStringAsync("/api/data")
        .ConfigureAwait(false); // Don't capture SynchronizationContext
    return data;
}
```

### Example 3 — SemaphoreSlim with Proper try/finally
```csharp
// ❌ Bad: If DoWorkAsync throws, Release never runs
await _semaphore.WaitAsync(ct);
await DoWorkAsync();
_semaphore.Release();

// ✅ Fix: Always use try/finally
await _semaphore.WaitAsync(ct);
try
{
    await DoWorkAsync();
}
finally
{
    _semaphore.Release(); // Always runs, even on exception
}
```

### Example 4 — Timeout-Based Lock to Prevent Indefinite Waiting
```csharp
// ✅ Good: Monitor.TryEnter with timeout — prevents indefinite blocking
bool lockTaken = false;
try
{
    Monitor.TryEnter(_lock, TimeSpan.FromSeconds(5), ref lockTaken);
    if (lockTaken)
    {
        // Critical section
    }
    else
    {
        _logger.LogWarning("Failed to acquire lock within 5 seconds — possible deadlock");
        throw new TimeoutException("Could not acquire lock");
    }
}
finally
{
    if (lockTaken) Monitor.Exit(_lock);
}
```

---

## 4. Interview Questions

1. **What is a deadlock? What are the four conditions required?**
   *A deadlock is when two or more threads are permanently blocked, each waiting for a resource held by the other. The four Coffman conditions are: Mutual Exclusion (resource held exclusively), Hold and Wait (holding one lock, waiting for another), No Preemption (can't forcibly release), and Circular Wait (A waits for B, B waits for A). Preventing any one condition prevents deadlocks.*

2. **Explain the sync-over-async deadlock pattern.**
   *When code calls `.Result` or `.Wait()` on an async method in a context with a `SynchronizationContext` (like WPF or legacy ASP.NET), it blocks the calling thread. The async method's continuation needs to resume on that same thread (because of the captured context), but the thread is frozen. Neither can proceed — deadlock.*

3. **Why doesn't the sync-over-async deadlock typically occur in ASP.NET Core?**
   *ASP.NET Core deliberately removed `SynchronizationContext`. Async continuations resume on any available ThreadPool thread, not the original request thread. So `.Result` doesn't deadlock — but it still blocks a ThreadPool thread, causing starvation under load.*

4. **How do you prevent deadlocks from lock ordering?**
   *Establish a global ordering for all locks (e.g., always acquire Lock A before Lock B, regardless of the calling method). If all threads acquire locks in the same order, circular wait — the critical fourth condition — is impossible.*

5. **How would you diagnose a deadlock in production?**
   *First, capture a process dump using `dotnet-dump collect`. Then analyze with `dotnet-dump analyze` → `clrstack` to see what each thread is waiting on. Alternatively, use the Visual Studio Parallel Stacks window during debugging to visualize which threads are blocked on which locks.*

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These test production debugging skills.*

- What's the difference between a deadlock and a livelock?
  *(A livelock is when threads are not blocked but actively changing state in a way that prevents progress — like two people in a hallway who keep stepping aside in the same direction.)*
- Can `Task.WhenAll` deadlock?
  *(Not by itself. But if one of the tasks calls `.Result` on another that's waiting for the SynchronizationContext, yes. Always use `await` inside tasks.)*
- What tools can you use to detect potential deadlocks before production?
  *(Visual Studio Concurrency Visualizer, static analysis tools, stress testing with many concurrent requests, and code review for lock ordering discipline.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Nested lock acquisition in different orders
// Method A: lock(X) → lock(Y)
// Method B: lock(Y) → lock(X)
// FIX: Enforce a global lock ordering convention

// MISTAKE 2: .Result / .Wait() in ASP.NET
var result = SomeAsync().Result; // ❌
var result = await SomeAsync(); // ✅

// MISTAKE 3: Locking inside async methods
async Task DoWork()
{
    lock (_obj) { await Task.Delay(100); } // ❌ Compile error! Can't await in lock
    // FIX: Use SemaphoreSlim
}

// MISTAKE 4: ReaderWriterLockSlim without try/finally
_rwLock.EnterReadLock();
var data = ReadData(); // If this throws, lock is never released → eventual deadlock
// FIX: Always use try/finally for every Enter* call

// MISTAKE 5: Deadlock from blocking in a DI factory
services.AddSingleton(sp => sp.GetRequiredService<IAsyncInit>().InitAsync().Result);
// ❌ If InitAsync depends on another service being resolved → deadlock
```

---

## 7. Real-World Usage

| Scenario | Prevention Strategy |
|----------|-------------------|
| **ASP.NET Core APIs** | Never use `.Result` / `.Wait()` — async all the way |
| **Multi-lock operations** | Strict lock ordering convention (alphabetical/hierarchical) |
| **External API calls** | Timeout via `CancellationToken` + `Monitor.TryEnter` |
| **Background workers** | `SemaphoreSlim` with try/finally for every `WaitAsync` |
| **Legacy interop** | `.ConfigureAwait(false)` in library code |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | What a deadlock is, `lock` basics |
| **Level 2** | Sync-over-async deadlock, `ConfigureAwait(false)` |
| **Level 3** | Coffman conditions, `Monitor.TryEnter` timeout, lock ordering |
| **Level 4** | `dotnet-dump` analysis, Concurrency Visualizer, lock-free alternatives |

---

## Connected Topics

- [Synchronization](./30-synchronization.md) — Lock primitives that can cause deadlocks if misused
- [Task vs Thread](./29-task-vs-thread.md) — ThreadPool starvation is often mistaken for deadlocks
- [Async/Await](./27-async-await.md) — `ConfigureAwait(false)` prevents SynchronizationContext deadlocks
- [CancellationToken](./28-cancellation-token.md) — Timeouts break the "indefinite wait" condition

> 🎯 **Interviewer Mindset Note:** *"Have you ever dealt with a deadlock in production?"* → Describe the sync-over-async pattern → How you diagnosed it (thread dump) → How you fixed it (async all the way) → What you added to prevent recurrence (code review rules, analyzers).

---

*Created: May 2026 · Level: Advanced*