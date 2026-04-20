# async / await & Task Internals

> **⚡ Core Idea (30 seconds):** `async`/`await` lets you write asynchronous code that reads like synchronous code. The compiler transforms your method into a **state machine** that yields the thread when waiting for I/O, then resumes it when the operation completes — without ever blocking.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#async` `#await` `#task` `#threading` `#statemachine`

---

## 1. Core Idea

Without async, waiting for I/O blocks a thread. With async, the thread is *released back to the pool* while waiting. When I/O completes, a thread picks up exactly where you left off.

Analogy: Async is a waiter who takes other orders while your food cooks, then delivers it when ready. Sync is a waiter who stands watching the kitchen. Same result, far better throughput.

---

## 2. Deep Explanation

### The State Machine Transformation

For this method:
```csharp
public async Task<string> GetDataAsync()
{
    var response = await httpClient.GetAsync(url);
    var content = await response.Content.ReadAsStringAsync();
    return content;
}
```

The compiler generates a **struct implementing `IAsyncStateMachine`** with states:
- **State 0**: Start `GetAsync`, yield if not complete (thread released!)
- **State 1**: Resume, start `ReadAsStringAsync`, yield again
- **State 2**: Resume, set result, mark Task complete

Local variables (`response`, `content`) become **fields on the struct** so they survive across yields.

### SynchronizationContext

`await` captures the current `SynchronizationContext`. In UI apps (WPF/WinForms), the continuation resumes on the UI thread. In ASP.NET Core, there is **no** sync context (removed for performance).

This is why library code should always use `ConfigureAwait(false)`:
```csharp
var data = await GetDataAsync().ConfigureAwait(false);
// Continuation runs on thread pool, not captured context
```

### Thread Pool vs Thread Blocking

```
Sync:   Thread 1 ████▓▓▓▓▓▓████  (blocked waiting for I/O)
Async:  Thread 1 ████      ████  (released, resumed)
```

100 concurrent sync requests = 100 blocked threads.
100 concurrent async requests = ~10–20 active threads. That's the ASP.NET Core scaling story.

### ValueTask — Avoiding Allocation on Hot Paths

`Task<T>` always allocates a heap object. `ValueTask<T>` is a struct:
```csharp
public ValueTask<User> GetUserAsync(int id)
{
    if (_cache.TryGetValue(id, out var user))
        return ValueTask.FromResult(user); // Zero heap allocation!

    return new ValueTask<User>(FetchFromDbAsync(id));
}
```
Use `ValueTask` when the synchronous path (cache hit) is the common case.

---

## 3. Code Examples

### Example 1 — Basic: async I/O vs blocking
```csharp
// BAD: Blocks a thread pool thread
public string GetData() => httpClient.GetStringAsync(url).Result;

// GOOD: Thread returned to pool during I/O wait
public async Task<string> GetDataAsync()
    => await httpClient.GetStringAsync(url);
```

### Example 2 — Real-World: Concurrent async operations
```csharp
public async Task<OrderSummary> GetOrderSummaryAsync(int orderId)
{
    // Sequential: ~300ms total
    var order = await _orderRepo.GetAsync(orderId);
    var customer = await _customerRepo.GetAsync(order.CustomerId);
    var inventory = await _inventoryService.CheckAsync(orderId);

    // Concurrent: ~120ms (limited by slowest)
    var orderTask = _orderRepo.GetAsync(orderId);
    var customerTask = _customerRepo.GetAsync(orderId);
    var inventoryTask = _inventoryService.CheckAsync(orderId);
    await Task.WhenAll(orderTask, customerTask, inventoryTask);

    return new OrderSummary(orderTask.Result, customerTask.Result, inventoryTask.Result);
}

// Timeout + cancellation pattern
public async Task<Product?> GetWithTimeoutAsync(int id)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    try
    {
        return await _productRepo.GetAsync(id, cts.Token);
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Timeout on ID {Id}", id);
        return null;
    }
}
```

### Example 3 — Classic Deadlock Pattern
```csharp
// DEADLOCK (UI/pre-Core ASP.NET): UI thread blocks, waiting for continuation
// that needs the UI thread → circular wait
public void GetDataDeadlock()
{
    var result = GetDataAsync().Result; // Blocks UI thread
}
public async Task<string> GetDataAsync()
{
    await Task.Delay(1000); // Tries to resume on UI thread — but it's blocked!
    return "data";
}

// FIX: Always go async all the way up the call stack
public async Task GetDataFixed()
{
    var result = await GetDataAsync(); // Never block
}
```

---

## 4. Interview Questions

1. **Does `async`/`await` create new threads?**
   *(No — it efficiently reuses thread pool threads. No new threads created.)*
2. **What is `ConfigureAwait(false)` and when should you use it?**
3. **What is the difference between `.Result`/`.Wait()` and `await`?**
4. **When should you use `ValueTask<T>` over `Task<T>`?**
5. **What happens if you call an async method without awaiting it?**

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These separate seniors from mid-levels.*

- What thread resumes after an `await`? *(Thread pool thread, unless SynchronizationContext captured it)*
- Explain what the compiler-generated state machine looks like.
- Can you `await` any type? What makes a type awaitable?
  *(Needs `GetAwaiter()` returning something with `IsCompleted`, `GetResult()`, and `OnCompleted()`)*
- What is `async void`? When is it acceptable?
  *(Only for event handlers. Exceptions crash the process. Never in library code.)*
- How does `Task.WhenAll` behave when one task throws?
  *(Waits for all, then throws `AggregateException` containing all errors)*
- What is `IAsyncEnumerable<T>` and how does it differ from `Task<IEnumerable<T>>`?
  *(Streams items one at a time; `Task<IEnumerable<T>>` waits for all before returning any)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: async void — unhandled exceptions crash the process
async void LoadData() { await LoadAsync(); } // ❌

// MISTAKE 2: .Result causing deadlock in non-Core contexts
var result = GetDataAsync().Result; // ❌ Potential deadlock

// MISTAKE 3: Not propagating CancellationToken
public async Task ProcessAsync(CancellationToken ct)
{
    await Step1Async();    // ❌ Ignores cancellation
    await Step2Async(ct);  // ✅
}

// MISTAKE 4: Unnecessary async wrapper adding state machine overhead
public async Task<string> GetNameAsync()
    => await _repo.GetNameAsync(); // ❌ Creates state machine for no reason

public Task<string> GetNameAsync()
    => _repo.GetNameAsync();       // ✅ Direct pass-through

// MISTAKE 5: Fire-and-forget swallowing exceptions
_ = SendEmailAsync(); // ❌ Exceptions silently lost

// BETTER:
_ = SendEmailAsync().ContinueWith(
    t => _logger.LogError(t.Exception, "Email failed"),
    TaskContinuationOptions.OnlyOnFaulted);
```

---

## 7. Real-World Usage

| Scenario | Pattern |
|----------|---------|
| ASP.NET Core controllers | `async Task<IActionResult>` throughout |
| EF Core | `await context.SaveChangesAsync(ct)` |
| HttpClient | `await client.GetStringAsync(url)` |
| Background Services | `IHostedService.ExecuteAsync(ct)` |
| Streaming large data | `IAsyncEnumerable<T>` |
| Concurrent I/O | `Task.WhenAll(tasks)` |
| Resilience | `await policy.ExecuteAsync(async () => ...)` |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | async/await syntax, Task<T>, basic I/O patterns |
| **Level 2** | Task.WhenAll, CancellationToken, ConfigureAwait, avoiding .Result |
| **Level 3** | State machine internals, SynchronizationContext, ValueTask<T> |
| **Level 4** | Custom awaitables, IAsyncEnumerable<T>, Channel<T>, TPL, thread pool tuning |

---

## 🔗 Connected Topics

- [Delegates](../02-intermediate/delegates.md) — `Task.ContinueWith` accepts `Func<Task, T>` delegates
- [Memory Management](./memory-management.md) — async state machines allocate; ValueTask reduces this
- [Garbage Collection](../04-expert/garbage-collection.md) — Long-lived tasks affect GC pressure
- [Task Parallel Library](../04-expert/task-parallel-library.md) — Parallel.ForEachAsync, Dataflow

> 🎯 **Interviewer Mindset:** The classic trap — "does async create threads?" Answer: No. It multiplexes existing thread pool threads. 10,000 async requests might use 20 threads; sync would require 10,000.

---

*Created: April 2026 · Level: Advanced*
