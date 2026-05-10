# Chapter 28 � CancellationToken

> **⚡ Core Idea (30 seconds):** A `CancellationToken` is a **cooperative cancellation signal**. You cannot force-kill an async operation — instead, you pass a token that the operation periodically checks. If cancellation is requested, the operation gracefully exits by throwing `OperationCanceledException`.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#cancellation` `#async` `#timeout` `#graceful-shutdown`

---

## 1. Core Idea

Think of a `CancellationToken` like a **walkie-talkie**. The commander (CancellationTokenSource) holds the transmit button. The soldiers (async operations) carry receivers (CancellationToken). When the commander says "abort", every soldier hears it and stops what they're doing at the next safe checkpoint. No soldier is shot mid-stride — they stop voluntarily.

This is the foundation of: **HTTP request cancellation in ASP.NET Core, timeout management, graceful shutdown, and linked cancellation for complex workflows**.

---

## 2. Deep Explanation

### The Two Types

| Type | Role | Analogy |
|------|------|---------|
| `CancellationTokenSource` (CTS) | The **controller** — has `.Cancel()` and `.CancelAfter()` | Remote control |
| `CancellationToken` (CT) | The **signal** — read-only, passed to operations | Receiver |

### Why Cooperative?

In .NET Framework, `Thread.Abort()` existed and was catastrophically dangerous — it could interrupt a thread mid-instruction, leaving locks held, files half-written, and state corrupted. Microsoft removed it entirely in .NET Core. All cancellation is now cooperative — the running code must actively participate.

### Key Methods

```csharp
// Creating a source
using var cts = new CancellationTokenSource();
CancellationToken token = cts.Token;

// Checking cancellation
token.IsCancellationRequested  // Returns true/false (non-throwing)
token.ThrowIfCancellationRequested()  // Throws OperationCanceledException

// Triggering cancellation
cts.Cancel();                         // Immediate cancellation
cts.CancelAfter(TimeSpan.FromSeconds(5)); // Timeout-based

// Linking multiple sources (cancel if ANY fires)
using var linked = CancellationTokenSource.CreateLinkedTokenSource(token1, token2);
```

### ASP.NET Core Integration

Every ASP.NET Core controller action can accept a `CancellationToken` parameter. The framework automatically wires it to the client's HTTP connection. If the client disconnects or the browser navigates away, the token fires.

```csharp
[HttpGet("report")]
public async Task<IActionResult> GetReport(CancellationToken ct)
{
    // If the user closes the browser tab, 'ct' fires,
    // and the database query is cancelled — saving server resources.
    var data = await _db.Reports.ToListAsync(ct);
    return Ok(data);
}
```

---

## 3. Code Examples

### Example 1 — Basic: Timeout Pattern
```csharp
public async Task<string> FetchWithTimeoutAsync(string url)
{
    // ✅ Always dispose CancellationTokenSource
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

    try
    {
        return await _httpClient.GetStringAsync(url, cts.Token);
    }
    catch (OperationCanceledException) when (!cts.Token.IsCancellationRequested)
    {
        // Distinguish: timeout vs external cancellation
        throw new TimeoutException($"Request to {url} timed out after 10 seconds");
    }
}
```

### Example 2 — Real-World: Linked Cancellation (User Cancel + Timeout)
```csharp
public async Task ProcessMessageAsync(Message msg, CancellationToken userCt)
{
    // Combine user cancellation (app shutdown) + per-message timeout
    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(userCt);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

    try
    {
        var enriched = await _api.EnrichAsync(msg.Data, timeoutCts.Token);
        await _db.SaveAsync(enriched, timeoutCts.Token);
        await _queue.CompleteAsync(msg, timeoutCts.Token);
    }
    catch (OperationCanceledException) when (userCt.IsCancellationRequested)
    {
        _logger.LogInformation("Shutdown requested — requeuing message {Id}", msg.Id);
        await _queue.AbandonAsync(msg); // Let another instance pick it up
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Message {Id} timed out after 30s", msg.Id);
        await _queue.DeadLetterAsync(msg);
    }
}
```

### Example 3 — Real-World: CPU-Bound Loop with Periodic Checking
```csharp
public void ProcessLargeDataset(IReadOnlyList<Record> records, CancellationToken ct)
{
    for (int i = 0; i < records.Count; i++)
    {
        // ✅ Check every 100 iterations to avoid overhead on tight loops
        if (i % 100 == 0)
            ct.ThrowIfCancellationRequested();

        ProcessRecord(records[i]);
    }
}
```

### Example 4 — Real-World: Cancellation Callback Registration
```csharp
public async Task StreamDataAsync(CancellationToken ct)
{
    var connection = await OpenConnectionAsync();

    // Register a cleanup callback — runs when cancellation fires
    ct.Register(() =>
    {
        _logger.LogInformation("Cancellation detected, closing connection");
        connection.Dispose();
    });

    await foreach (var item in connection.ReadAllAsync(ct))
    {
        await ProcessAsync(item);
    }
}
```

---

## 4. Interview Questions

1. **What is a CancellationToken and why is cancellation cooperative in .NET?**
   *A CancellationToken is a lightweight struct that carries a cancellation signal. Cancellation is cooperative because force-aborting threads is unsafe — it can leave locks held and data corrupted. Instead, the running code periodically checks the token and throws `OperationCanceledException` at a safe point to unwind gracefully.*

2. **What is the difference between CancellationTokenSource and CancellationToken?**
   *CancellationTokenSource owns the ability to trigger cancellation — it has `Cancel()` and `CancelAfter()`. CancellationToken is a read-only struct derived from the source that you pass into async methods. This separation ensures that only the code that created the source can trigger cancellation.*

3. **How do you implement a timeout on an async HTTP call?**
   *Create a `CancellationTokenSource` with a `TimeSpan` in the constructor: `new CancellationTokenSource(TimeSpan.FromSeconds(10))`. Pass its token to `HttpClient.GetAsync()`. If the HTTP call doesn't complete within 10 seconds, the token fires and the call throws `TaskCanceledException`.*

4. **How does linked cancellation work?**
   *`CancellationTokenSource.CreateLinkedTokenSource(token1, token2)` creates a new source that fires if ANY of the input tokens fire. This is essential for combining user-initiated cancellation (app shutdown) with operation-specific timeouts into a single token.*

5. **Why must you dispose CancellationTokenSource?**
   *CancellationTokenSource allocates internal timers (when using `CancelAfter`) and registration callbacks. If not disposed, these resources leak. Always wrap it in a `using` statement. This is especially important in long-running services that create thousands of token sources.*

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These expose whether you handle edge cases.*

- What's the difference between `OperationCanceledException` and `TaskCanceledException`?
  *(`TaskCanceledException` inherits from `OperationCanceledException`. HttpClient throws `TaskCanceledException` on timeout, but `OperationCanceledException` on user cancellation. Catching `OperationCanceledException` handles both.)*
- Should you catch `OperationCanceledException` or let it propagate?
  *(In ASP.NET Core, let it propagate — the framework automatically returns a 499/disconnected response. In background services, catch it at the top level to log and clean up gracefully.)*
- What happens if you pass `CancellationToken.None` to an async method?
  *(The operation runs without cancellation support. It will complete or fail on its own terms. This is fine for fire-and-forget scenarios but dangerous for HTTP handlers where the client might disconnect.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Accepting a token but never passing it down
public async Task DoWorkAsync(CancellationToken ct)
{
    var data = await _httpClient.GetStringAsync("/api/data"); // ❌ Token not passed!
    await _db.SaveAsync(data); // ❌ Token not passed!
}
// FIX:
public async Task DoWorkAsync(CancellationToken ct)
{
    var data = await _httpClient.GetStringAsync("/api/data", ct); // ✅
    await _db.SaveAsync(data, ct); // ✅
}

// MISTAKE 2: Not disposing CancellationTokenSource
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
// ... use cts.Token ...
// ❌ Timer leaks! 
// FIX:
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // ✅

// MISTAKE 3: Catching OperationCanceledException and swallowing it
catch (OperationCanceledException) { } // ❌ Silently hides cancellation

// MISTAKE 4: Using CancellationToken.None when a real token is available
public async Task<IActionResult> GetData(CancellationToken ct)
{
    var result = await _service.QueryAsync(CancellationToken.None); // ❌ Ignores user disconnect
    var result = await _service.QueryAsync(ct); // ✅
}
```

---

## 7. Real-World Usage

| Scenario | Pattern |
|----------|---------|
| **ASP.NET Core API** | Accept `CancellationToken ct` in every action → pass to EF Core |
| **HttpClient Timeout** | `new CancellationTokenSource(TimeSpan)` per request |
| **Background Service** | `ExecuteAsync(CancellationToken stoppingToken)` for graceful shutdown |
| **Parallel Operations** | Pass token to `Parallel.ForEachAsync` to cancel batch on failure |
| **gRPC Streaming** | Token fires when client disconnects from server stream |
| **Polly Retry** | Token passed through retry policies to cancel retries on shutdown |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | What CancellationToken is, basic timeout pattern |
| **Level 2** | Linked tokens, ASP.NET Core integration, `ThrowIfCancellationRequested` |
| **Level 3** | `token.Register()` callbacks, distinguishing timeout vs user cancel |
| **Level 4** | Custom `ICancelableAsync` interfaces, `CancellationTokenSource` pooling for high-throughput |

---

## Connected Topics

- [Async/Await](./27-async-await.md) — Every async method should accept and forward a CancellationToken
- [Task vs Thread](./29-task-vs-thread.md) — Cancellation replaces the dangerous `Thread.Abort()`
- [Deadlocks](./31-deadlocks.md) — Timeouts via CancellationToken prevent indefinite blocking
- [Memory Management](./33-memory-management.md) — CTS must be disposed to avoid timer leaks

> 🎯 **Interviewer Mindset Note:** *"How does your API handle user disconnects?"* → `CancellationToken` in every controller action → *"What if the token is ignored?"* → wasted server resources → *"How do you add a hard timeout?"* → Linked token source.

---

*Created: May 2026 · Level: Advanced*