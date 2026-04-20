# Collections — List, Dictionary, HashSet

> **⚡ Core Idea (30 seconds):** `List<T>` is a resizable array, `Dictionary<K,V>` is a hash table for key lookups, `HashSet<T>` is a hash table for membership checks. Choosing the wrong collection is one of the most common performance mistakes in C# code.

**Domain:** `C#` **Level:** `Beginner → Intermediate` **Tags:** `#collections` `#list` `#dictionary` `#hashset` `#performance`

---

## 1. Core Idea

Think of:
- **`List<T>`** as a numbered shelf — you access things by position, insert anywhere, iterate in order.
- **`Dictionary<K,V>`** as a filing cabinet with labeled drawers — O(1) lookup by key.
- **`HashSet<T>`** as a filing cabinet where all you care about is "does this exist?" — uniqueness enforced automatically.

The collection you pick determines whether your code runs in O(1) or O(n).

---

## 2. Deep Explanation

### List\<T\> Internals

`List<T>` wraps a `T[]` array internally. When you `Add()` beyond capacity, it **doubles** the internal array (amortized O(1) add):

```
Initial: T[4]
After 5th Add: allocates T[8], copies 4 elements over
After 9th Add: allocates T[16], copies 8 elements over
```

- **Random access** (`list[i]`): O(1)
- **Add at end**: Amortized O(1)
- **Insert/RemoveAt middle**: O(n) — shifts all trailing elements
- **Contains**: O(n) — linear scan

### Dictionary\<K,V\> Internals

Uses a **hash table with chaining**. Process for key lookup:
1. Compute `key.GetHashCode()`
2. Map to bucket index: `bucket = hash % bucketCount`
3. Scan the chain at that bucket for equality

- **Get/Set/ContainsKey**: O(1) average, O(n) worst case (hash collision)
- **Requires**: Key must properly implement `GetHashCode()` and `Equals()`
- **Load factor**: When 72% full, the table is resized (doubled + rehashed)

### HashSet\<T\> Internals

`HashSet<T>` is a `Dictionary<T, bool>` internally (just the keys, no values). Same O(1) characteristics for `Contains`, `Add`, `Remove`. Use when:
- You need uniqueness enforcement
- Frequent membership tests
- Set operations: `UnionWith`, `IntersectWith`, `ExceptWith`

### Other Key Collections

| Collection | Best For | Key Characteristic |
|-----------|----------|-------------------|
| `Queue<T>` | First-in, first-out | `Enqueue`/`Dequeue` |
| `Stack<T>` | Last-in, first-out | `Push`/`Pop` |
| `LinkedList<T>` | Frequent middle insertions/deletions | O(1) insert at known node |
| `SortedDictionary<K,V>` | Ordered key iteration | O(log n) ops (Red-Black tree) |
| `ConcurrentDictionary<K,V>` | Thread-safe dictionary | Lock-free reads |
| `ImmutableList<T>` | Thread-safe, functional style | Structural sharing |

---

## 3. Code Examples

### Example 1 — Basic: Choosing the Right Collection
```csharp
// BAD: Using List for frequent lookups by ID — O(n) per lookup
var users = new List<User>();
users.Add(new User { Id = 1, Name = "Alice" });
var found = users.FirstOrDefault(u => u.Id == 1); // O(n) scan every time!

// GOOD: Dictionary for O(1) lookups by key
var userMap = new Dictionary<int, User>();
userMap[1] = new User { Id = 1, Name = "Alice" };
var found2 = userMap[1]; // O(1)

// BAD: List for uniqueness check
var tags = new List<string>();
if (!tags.Contains("urgent")) tags.Add("urgent"); // O(n) check + O(1) add

// GOOD: HashSet — O(1) uniqueness enforced automatically
var tagSet = new HashSet<string>();
tagSet.Add("urgent"); // Returns false if already present — no Contains needed
```

### Example 2 — Real-World: Order Processing Pipeline
```csharp
public class OrderProcessor
{
    // O(1) lookup: productId → Product
    private readonly Dictionary<int, Product> _productCache;

    // O(1) membership: already-processed order IDs
    private readonly HashSet<Guid> _processedOrders = new();

    // Ordered list for batch processing
    private readonly List<Order> _pendingOrders = new();

    public async Task<ProcessResult> ProcessAsync(Order order)
    {
        // Idempotency check — O(1)
        if (_processedOrders.Contains(order.Id))
            return ProcessResult.AlreadyProcessed;

        // Product lookup — O(1)
        if (!_productCache.TryGetValue(order.ProductId, out var product))
            return ProcessResult.ProductNotFound;

        // Process...
        _processedOrders.Add(order.Id); // O(1) add + uniqueness
        return ProcessResult.Success;
    }

    // Set operations: find orders for products we carry
    public HashSet<int> GetRelevantProductIds(IEnumerable<Order> orders)
    {
        var orderProductIds = orders.Select(o => o.ProductId).ToHashSet();
        orderProductIds.IntersectWith(_productCache.Keys); // Products we carry
        return orderProductIds;
    }
}
```

---

## 4. Interview Questions

1. **What is the time complexity of `List<T>.Contains()` vs `HashSet<T>.Contains()`?**
2. **When would you choose `Dictionary<K,V>` over `List<T>`?**
3. **What happens internally when a `List<T>` exceeds its capacity?**
4. **What requirements does a type have to meet to be used as a Dictionary key?**
5. **What is the difference between `Dictionary` and `ConcurrentDictionary`?**

---

## 5. Follow-up Questions

- What happens if two different keys produce the same `GetHashCode()`?
  *(Hash collision — they land in the same bucket. Equality check then disambiguates. Performance degrades toward O(n) in extreme cases.)*
- Why should you never use a mutable object as a dictionary key?
  *(If the object mutates, its hash changes but its bucket position doesn't → you can never find it again)*
- What is the default initial capacity of `List<T>`? How would you optimize for known size?
  ```csharp
  var list = new List<int>(expectedCount); // Pre-allocate — avoids resizing copies
  ```
- `Dictionary<K,V>` vs `SortedDictionary<K,V>` — what's the trade-off?
  *(Dictionary: O(1) get, unordered. SortedDictionary: O(log n) get, ordered. Use sorted when you need keys in order.)*
- How does `ConcurrentDictionary<K,V>` achieve thread safety without locking reads?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: KeyNotFoundException on Dictionary access
var dict = new Dictionary<int, string>();
var val = dict[99]; // ❌ KeyNotFoundException if key doesn't exist
// ALWAYS use TryGetValue:
if (dict.TryGetValue(99, out var result)) { }

// MISTAKE 2: Modifying a collection while iterating it
foreach (var item in list)
    list.Remove(item); // ❌ InvalidOperationException

// FIX: Collect to remove first, or iterate backwards
var toRemove = list.Where(x => x.IsExpired).ToList();
foreach (var item in toRemove) list.Remove(item);

// MISTAKE 3: Using List<T> as a lookup — O(n) per call in loops
foreach (var order in orders) // O(n)
    if (validIds.Contains(order.Id)) // O(n) — making this O(n²) total!
       Process(order);
// FIX: Convert to HashSet first
var validSet = validIds.ToHashSet(); // O(n) once
foreach (var order in orders)
    if (validSet.Contains(order.Id)) // O(1) per lookup
       Process(order);

// MISTAKE 4: Using struct as Dictionary key without overriding GetHashCode
// Default struct GetHashCode uses reflection — very slow
public struct OrderKey { public int Id; public int Version; }
// MUST add: public override int GetHashCode() => HashCode.Combine(Id, Version);
```

---

## 7. Real-World Usage

| Scenario | Collection |
|----------|-----------|
| Cache / lookup table | `Dictionary<TKey, TValue>` |
| Idempotency tracking | `HashSet<Guid>` |
| FIFO message queue | `Queue<T>` |
| Undo/Redo stack | `Stack<T>` |
| Ordered results | `List<T>` (or `SortedList`) |
| Thread-safe shared state | `ConcurrentDictionary<K,V>` |
| Read-heavy, immutable data | `ImmutableDictionary<K,V>` |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | List, Dictionary, HashSet basics; when to use each |
| **Level 2** | Time complexity matrix, TryGetValue, capacity pre-allocation |
| **Level 3** | Hash collision internals, load factor, GetHashCode contract |
| **Level 4** | ConcurrentDictionary lock-free reads, ImmutableCollections, custom comparers |

---

## 🔗 Connected Topics
- [Collection Interfaces](./collection-interfaces.md) — IEnumerable vs ICollection vs IList vs IReadOnlyCollection
- [Generics](../02-intermediate/generics.md) — All collections are generic
- [LINQ](../02-intermediate/linq.md) — LINQ operates over `IEnumerable<T>`
- [Value vs Reference Types](./value-vs-reference-types.md) — GetHashCode behavior differs

*Created: April 2026 · Level: Beginner → Intermediate*
