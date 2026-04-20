# Iterators & yield return

> **⚡ Core Idea (30 seconds):** `yield return` lets you write a method that **produces values one at a time on demand**. Instead of building a full list and returning it, you produce each item lazily — the caller asks for the next item, you compute it, they pull the next, and so on. **This is how all deferred LINQ operators work internally.**

**Domain:** `C#` **Level:** `Intermediate` **Tags:** `#iterators` `#yield` `#ienumerable` `#lazy` `#deferred`

---

## 1. Core Idea

Imagine a vending machine vs a kitchen. A kitchen prepares all meals upfront (eager — `List<T>`). A vending machine dispenses one item at a time when you press a button (lazy — `yield return`).

For large data sets, streaming, or infinite sequences, `yield return` gives you the vending machine model. The caller controls the pace of consumption.

---

## 2. Deep Explanation

### What the Compiler Generates

When you use `yield return`, the compiler transforms your method into a **state machine class** that implements both `IEnumerable<T>` and `IEnumerator<T>`:

```csharp
// Your code:
public IEnumerable<int> GetEvens(int max)
{
    for (int i = 0; i <= max; i += 2)
        yield return i;
}

// Compiler generates roughly:
private sealed class <GetEvens>d__0 : IEnumerable<int>, IEnumerator<int>
{
    private int _state;
    private int _current;
    private int max;

    public bool MoveNext()
    {
        switch (_state)
        {
            case 0: _state = 1; i = 0; goto case 1;
            case 1:
                if (i <= max) { _current = i; i += 2; return true; }
                return false;
        }
        return false;
    }
    public int Current => _current;
}
```

Each `yield return` becomes a **resume point** in the state machine. The method body is paused at `yield return` and resumed by the next `MoveNext()` call.

### `yield break` — Early Exit

```csharp
public IEnumerable<Order> GetActiveOrders(IEnumerable<Order> all)
{
    foreach (var order in all)
    {
        if (order.IsDeleted) yield break; // Stop the sequence entirely
        if (!order.IsActive) continue;    // Skip this item
        yield return order;               // Emit this item
    }
}
```

### Lazy Evaluation — The Key Benefit

```csharp
// EAGER: Loads all 1M records into memory
var users = context.Users.ToList(); // 1M objects in RAM

// LAZY: Streams records one at a time — only one in memory at a time
public IEnumerable<User> StreamUsers()
{
    foreach (var user in context.Users) // EF Core iterates database cursor
        yield return user;              // One user at a time
}
```

### IAsyncEnumerable\<T\> — Async Streaming (C# 8+)

```csharp
public async IAsyncEnumerable<Product> GetProductsAsync(
    [EnumeratorCancellation] CancellationToken ct)
{
    await foreach (var batch in _api.GetBatchesAsync(ct))
        foreach (var product in batch)
            yield return product; // Stream results as they arrive
}

// Consume:
await foreach (var product in GetProductsAsync(ct))
    await ProcessAsync(product); // Process one at a time without buffering all
```

---

## 3. Code Examples

### Basic — Filtering with yield
```csharp
// Without yield — builds entire result list first
public List<int> GetPrimesUpTo(int max)
{
    var primes = new List<int>();
    for (int n = 2; n <= max; n++)
        if (IsPrime(n)) primes.Add(n);
    return primes; // All computed before caller sees any
}

// With yield — lazy streaming
public IEnumerable<int> GetPrimesUpTo(int max)
{
    for (int n = 2; n <= max; n++)
        if (IsPrime(n)) yield return n; // Caller gets each prime as computed
}

// Caller can stop early — avoids computing rest!
var first5Primes = GetPrimesUpTo(1_000_000).Take(5).ToList(); // Only computes until 5th prime found
```

### Real-World — Recursive Tree Flattening
```csharp
public class Category
{
    public string Name { get; set; } = "";
    public List<Category> Children { get; set; } = new();
}

// Yield makes recursive traversal elegant — no intermediate list
public IEnumerable<Category> Flatten(Category root)
{
    yield return root; // Emit the current node
    foreach (var child in root.Children)
        foreach (var descendant in Flatten(child)) // Recursive
            yield return descendant;
}

// Or with LINQ:
public IEnumerable<Category> FlattenLinq(Category root)
    => Enumerable.Repeat(root, 1).Concat(root.Children.SelectMany(FlattenLinq));

// Caller:
var allCategories = Flatten(rootCategory).ToList();
var electronics = Flatten(rootCategory).FirstOrDefault(c => c.Name == "Electronics");
// If found early, rest of tree is NOT traversed!
```

---

## 4. Interview Questions

1. **What does `yield return` do? How is it different from `return`?**
2. **What type must a method return to use `yield return`?** (`IEnumerable<T>`, `IEnumerator<T>`, `IAsyncEnumerable<T>`)
3. **What is the difference between `yield return` and `yield break`?**
4. **What class does the compiler generate when you use `yield return`?**
5. **When would you prefer `yield return` over building and returning a `List<T>`?**

---

## 5. Follow-up Questions

- Can you use `yield return` inside a `try/catch` block?
  *(Yes — but NOT inside a `try` with a `finally` that calls `yield return`. `yield break` in `finally` is allowed.)*
- Does `yield return` hold resources open? (important for DB connections)
  *(Yes — the enumerator keeps the method frame alive. EF Core queries hold the DB connection open until fully consumed or disposed.)*
- Can you have multiple `yield return` statements in one method?
  *(Yes — each one is a separate resume point)*
- What is `IAsyncEnumerable<T>` and how does `await foreach` interact with it?
- If you call a method that uses `yield return`, when does the code before the first `yield` execute?
  *(Not until the first `MoveNext()` is called — i.e., the first iteration)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Forgetting deferred execution — no exception until iteration
public IEnumerable<int> Divide(int[] numbers, int divisor)
{
    if (divisor == 0) throw new ArgumentException("Divisor is 0"); // ❌ Not thrown on call!
    foreach (var n in numbers) yield return n / divisor;
}
var result = Divide(numbers, 0); // No exception here!
var list = result.ToList();       // Exception thrown HERE

// FIX: Eagerly validate, lazily produce
public IEnumerable<int> Divide(int[] numbers, int divisor)
{
    if (divisor == 0) throw new ArgumentException(".."); // Eager guard
    return DivideInternal(numbers, divisor); // Deferred inner method
}
private IEnumerable<int> DivideInternal(int[] numbers, int divisor)
{
    foreach (var n in numbers) yield return n / divisor;
}

// MISTAKE 2: Multiple enumeration of a generator — it re-runs each time
var evens = GetEvens(100); // Creates iterator, nothing runs yet
var count = evens.Count(); // Runs generator once
var list = evens.ToList(); // Runs generator AGAIN from start!
// FIX: evens = evens.ToList(); to materialize once
```

---

## 7. Real-World Usage

| Scenario | Iterator Pattern |
|----------|----------------|
| All LINQ deferred operators | `Where`, `Select` internally use `yield return` |
| Streaming large files | Read line-by-line with `yield return` |
| Pagination iteration | Yield each page result |
| Recursive tree traversal | Depth-first or breadth-first with yield |
| Infinite sequences | `public IEnumerable<int> NaturalNumbers() { int i=0; while(true) yield return i++; }` |
| EF Core result streaming | `IAsyncEnumerable<T>` for large dataset query results |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | yield return syntax, IEnumerable<T> return type |
| **Level 2** | Deferred execution implications, yield break, lazy vs eager |
| **Level 3** | State machine generated code, multiple enumeration gotcha, exception timing |
| **Level 4** | IAsyncEnumerable<T>, CancellationToken with EnumeratorCancellation, Channel<T> |

## 🔗 Connected Topics
- [LINQ](./linq.md) — Deferred LINQ operators use yield internally
- [async/await](../03-advanced/async-await.md) — IAsyncEnumerable + await foreach
- [Delegates](./delegates.md) — Iterator state machines are similar to async state machines

*Created: April 2026 · Level: Intermediate*
