# Chapter 25 � Exception Handling Internals

> **⚡ Core Idea (30 seconds):** Exceptions are **not control flow** — they are an expensive unwinding mechanism the CLR uses when something truly unexpected happens. Understanding how `try/catch/finally` works at the CLR level, when to throw, and what it costs helps you write resilient, performant code.

**Domain:** `C#` **Level:** `Intermediate` **Tags:** `#exceptions` `#error-handling` `#try-catch` `#performance`

---

## 1. Core Idea

Think of an exception like a **fire alarm in a building**. When it goes off, every floor evacuates (the call stack unwinds), the fire department arrives (the catch block), and then cleanup happens (the finally block). You don't pull the fire alarm because the coffee machine is out of beans — that's a normal business condition, not an emergency. Exceptions are for emergencies.

This is the foundation of: **error handling strategy, global exception middleware in ASP.NET Core, Result pattern, and production debugging**.

---

## 2. Deep Explanation

### How Exceptions Work at the CLR Level

When you `throw new Exception()`:

1. **CLR creates an exception object** on the managed heap (allocation cost).
2. **Stack walk begins** — the CLR walks UP the call stack frame by frame, looking for a matching `catch` filter. This is the expensive part.
3. **First-pass: filter search** — the CLR checks each `catch` clause and `when` filter without unwinding yet.
4. **Second-pass: unwind** — once a matching handler is found, the CLR executes `finally` blocks in every frame between the throw site and the catch site.
5. **Catch handler executes** — your `catch` block runs.
6. **If no handler found** — the process terminates.

### The Cost of Exceptions

Exceptions are **100x–1000x** slower than normal return values. The stack walk, filter evaluation, and finally execution are incredibly expensive operations. Benchmarks show:

| Operation | Time |
|-----------|------|
| Normal return | ~1 nanosecond |
| Throwing + catching exception | ~10,000–50,000 nanoseconds |

This is why using exceptions for business logic flow (e.g., "user not found") is an anti-pattern in high-throughput systems.

### Exception Hierarchy

```
System.Object
└── System.Exception
    ├── System.SystemException (CLR-generated: NullRef, StackOverflow, OutOfMemory)
    │   ├── NullReferenceException
    │   ├── InvalidOperationException
    │   ├── ArgumentException
    │   │   ├── ArgumentNullException
    │   │   └── ArgumentOutOfRangeException
    │   └── IndexOutOfRangeException
    └── System.ApplicationException (legacy — do NOT inherit from this)
```

### Exception Filters (`when`)

C# 6 introduced exception filters. They execute during the **first pass** (before unwinding), preserving the original stack trace:

```csharp
try { /* ... */ }
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    // Only catches 404s — other HTTP errors pass through
}
```

### throw vs throw ex

```csharp
// ❌ Bad — RESETS the stack trace (loses the original throw location)
catch (Exception ex) { throw ex; }

// ✅ Good — PRESERVES the original stack trace
catch (Exception ex) { throw; }

// ✅ Good — WRAPS the original as InnerException
catch (Exception ex) { throw new BusinessException("Context", ex); }
```

---

## 3. Code Examples

### Example 1 — Basic: Exception Filter for Conditional Catching
```csharp
public async Task<Order?> GetOrderSafelyAsync(int id)
{
    try
    {
        return await _httpClient.GetFromJsonAsync<Order>($"/api/orders/{id}");
    }
    // Only catch transient failures — let permanent failures propagate
    catch (HttpRequestException ex) when (ex.StatusCode is 
        HttpStatusCode.ServiceUnavailable or 
        HttpStatusCode.GatewayTimeout)
    {
        _logger.LogWarning(ex, "Transient failure for order {Id}, returning null", id);
        return null;
    }
    // 404 is NOT an exception — it's a valid business outcome
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return null;
    }
    // All other exceptions bubble up to the global handler
}
```

### Example 2 — Real-World: Custom Exception Hierarchy
```csharp
// Domain-specific base exception
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    protected DomainException(string errorCode, string message, Exception? inner = null) 
        : base(message, inner) => ErrorCode = errorCode;
}

public class OrderNotFoundException : DomainException
{
    public Guid OrderId { get; }
    public OrderNotFoundException(Guid orderId) 
        : base("ORDER_NOT_FOUND", $"Order {orderId} not found") 
        => OrderId = orderId;
}

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string sku, int requested, int available)
        : base("INSUFFICIENT_STOCK", 
               $"SKU {sku}: requested {requested}, available {available}") { }
}
```

### Example 3 — Real-World: Result Pattern (Exception-Free Error Handling)
```csharp
// For expected business failures, use a Result type instead of exceptions
public readonly struct Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result(T value) { Value = value; IsSuccess = true; Error = null; }
    private Result(string error) { Value = default; IsSuccess = false; Error = error; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}

// Usage — no exceptions thrown for expected business failures
public Result<Order> PlaceOrder(OrderRequest request)
{
    if (request.Amount <= 0)
        return Result<Order>.Failure("Amount must be positive"); // NOT an exception

    if (!_inventory.HasStock(request.Sku))
        return Result<Order>.Failure("Insufficient stock"); // NOT an exception

    var order = new Order(request);
    _db.Save(order);
    return Result<Order>.Success(order);
}
```

---

## 4. Interview Questions

1. **What happens at the CLR level when you throw an exception?**
   *The CLR creates the exception object on the heap, then performs a two-pass stack walk. In the first pass, it searches up the call stack for a matching `catch` filter without unwinding. Once found, the second pass unwinds the stack, executing `finally` blocks in every intermediate frame. Finally, the matched `catch` handler runs.*

2. **What is the difference between `throw;` and `throw ex;`?**
   *`throw;` re-throws the current exception preserving the original stack trace, so you can see exactly where the exception originated. `throw ex;` resets the stack trace to the current line, destroying the original call information and making debugging much harder.*

3. **Why shouldn't you use exceptions for normal control flow like "user not found"?**
   *Exceptions involve a stack walk that is 10,000x–50,000x slower than a normal return. In high-throughput APIs handling thousands of requests per second, using exceptions for expected outcomes like "entity not found" or "validation failed" creates massive performance degradation and floods structured logs with noise.*

4. **What is the Result pattern, and when would you use it instead of exceptions?**
   *The Result pattern uses a return type like `Result<T>` that contains either a success value or an error description. I use it for expected business failures — validation errors, "not found", insufficient permissions. I reserve exceptions for truly unexpected failures — database connection lost, null reference bugs, out-of-memory situations.*

5. **How do exception filters (`when`) differ from a regular `if` inside a catch block?**
   *Exception filters run during the CLR's first pass, before the stack unwinds. If the filter returns false, the CLR continues searching for another handler — the stack is still intact. A regular `if` inside a `catch` block runs AFTER the stack has already unwound, losing the original execution context. Filters also preserve the original stack trace for debugging tools.*

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These check if you know the cost and the edge cases.*

- What happens if a `finally` block itself throws an exception?
  *(The original exception is lost. The new exception from `finally` propagates instead. This is a very dangerous bug. Never throw in `finally`.)*
- Can you catch a `StackOverflowException`?
  *(No. By default in .NET, `StackOverflowException` terminates the process immediately. It cannot be caught because the stack is exhausted and there's no space to run the catch handler.)*
- What about `OutOfMemoryException`?
  *(Technically catchable, but practically useless — if the heap is full, your catch block likely can't allocate the objects it needs to recover.)*
- How does the global `UnhandledException` event work?
  *(It fires when an exception escapes all handlers. In .NET Core, the process still terminates after the event. It's for logging, not recovery.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: throw ex — resets the stack trace
try { SomeMethod(); }
catch (Exception ex)
{
    Log(ex);
    throw ex; // ❌ Stack trace now points HERE, not the original throw site
    throw;    // ✅ Preserves original stack trace
}

// MISTAKE 2: Catching Exception and swallowing
try { RiskyOperation(); }
catch (Exception) { } // ❌ Silent failure — bugs become invisible

// MISTAKE 3: Using exceptions for validation
public decimal Divide(decimal a, decimal b)
{
    try { return a / b; }
    catch (DivideByZeroException) { return 0; } // ❌ Exceptions as control flow
}
// FIX:
public decimal Divide(decimal a, decimal b)
{
    if (b == 0) return 0; // ✅ Guard clause — no exception thrown
    return a / b;
}

// MISTAKE 4: Throwing ApplicationException — legacy, never use it
throw new ApplicationException("Something failed"); // ❌ 
throw new InvalidOperationException("Order is already shipped"); // ✅

// MISTAKE 5: Losing async stack traces
catch (Exception ex)
{
    throw new BusinessException("Failed", ex); // ✅ Pass inner exception!
    throw new BusinessException("Failed");     // ❌ Original exception LOST
}
```

---

## 7. Real-World Usage

| Scenario | Exception Strategy |
|----------|-------------------|
| **ASP.NET Core API** | Global `UseExceptionHandler` middleware → ProblemDetails |
| **Domain Logic** | Result pattern for expected failures, exceptions for bugs |
| **External API Calls** | Catch transient `HttpRequestException`, retry with Polly |
| **Database Operations** | Catch `DbUpdateConcurrencyException` for optimistic concurrency |
| **Background Workers** | Catch per-message, log, move to dead-letter queue |
| **Validation** | FluentValidation returns errors, never throws |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | try/catch/finally, throw, custom exceptions |
| **Level 2** | `throw` vs `throw ex`, exception filters (`when`), hierarchy |
| **Level 3** | CLR two-pass mechanism, performance cost, Result pattern |
| **Level 4** | `ExceptionDispatchInfo.Capture()`, `Environment.FailFast()`, first-chance exception events |

---

## Connected Topics

- [Memory Management](../04-advanced/33-memory-management.md) — `using`/`IDisposable` relies on `finally` to guarantee cleanup
- [Async/Await](../04-advanced/27-async-await.md) — `AggregateException` unwrapping in async exception handling
- [Delegates](./19-delegates.md) — Multicast delegate exception propagation stops the chain
- [Pattern Matching](../04-advanced/35-pattern-matching.md) — `catch` filters use pattern matching syntax

> 🎯 **Interviewer Mindset Note:** *"When should you throw exceptions vs return error codes?"* is a classic architect-level question. The correct answer balances performance (Result pattern for hot paths) with developer experience (exceptions for unexpected failures).

---

*Created: May 2026 · Level: Intermediate*
