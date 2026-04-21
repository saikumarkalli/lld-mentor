# Collections in C#

> **⚡ Core Idea (30 seconds):** A collection is a container that holds multiple items. C# has many — `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `Queue<T>`, `Stack<T>`, and more. Picking the **wrong one** is one of the most common performance mistakes. Each exists for a specific job — know the job, pick the tool.

**Domain:** `C#` **Level:** `Beginner → Intermediate` **Tags:** `#collections` `#list` `#dictionary` `#hashset` `#queue` `#stack`

---

## 1. Core Idea

Think of collections like physical containers:
- **`List<T>`** → a numbered shelf (ordered, indexed, resizable)
- **`Dictionary<K,V>`** → a filing cabinet with labeled drawers (key → value)
- **`HashSet<T>`** → a bouncer list (unique items only)
- **`Queue<T>`** → a supermarket line (first in, first out)
- **`Stack<T>`** → a stack of plates (last in, first out)
- **`LinkedList<T>`** → a chain of rings (cheap insert anywhere, no index)

The collection you pick **determines the time complexity** of your operations. This is not optional knowledge — interviewers test this heavily.

---

## 2. The Collection Interface Hierarchy

Before the types, understand the interfaces. Every collection in .NET implements some subset of these:

```
IEnumerable<T>                   ← Can be iterated with foreach
  └── ICollection<T>             ← Has Count, Add, Remove, Contains
        ├── IList<T>             ← Has indexer [i], Insert, IndexOf
        │     └── List<T>
        └── IDictionary<K,V>     ← Has key-based access [key], Keys, Values
              └── Dictionary<K,V>
```

| Interface | What It Adds | Use When |
|-----------|-------------|---------|
| `IEnumerable<T>` | `foreach` only, read-only iteration | Passing data for reading only |
| `ICollection<T>` | `Count`, `Add`, `Remove`, `Contains` | Basic mutable collection |
| `IList<T>` | Index access `[i]`, `Insert`, `RemoveAt` | Need positional access |
| `IReadOnlyList<T>` | `Count` + `[i]` read-only | Expose list without allowing mutation |
| `IReadOnlyCollection<T>` | `Count` + read-only | Expose size without allowing mutation |
| `IDictionary<K,V>` | Key-based lookup | Key → value mapping |

**Always code to the narrowest interface your caller needs:**
```csharp
// BAD: Exposes more than needed — caller can Add(), Remove(), Insert()
public List<Order> GetOrders() => _orders;

// GOOD: Caller can only iterate
public IEnumerable<Order> GetOrders() => _orders;

// GOOD: Caller can read by index and check count, but not mutate
public IReadOnlyList<Order> GetOrders() => _orders;
```

---

## 3. Generic vs Non-Generic Collections

This is one of the most overlooked beginner topics in C# — and one interviewers love to probe.

### The Non-Generic Collections (Legacy — `System.Collections`)

Before C# 2.0 generics existed, .NET had collections that stored everything as `object`:

| Non-Generic | Generic Equivalent | Namespace |
|------------|-------------------|-----------|
| `ArrayList` | `List<T>` | `System.Collections` |
| `Hashtable` | `Dictionary<K,V>` | `System.Collections` |
| `Queue` | `Queue<T>` | `System.Collections` |
| `Stack` | `Stack<T>` | `System.Collections` |
| `SortedList` | `SortedDictionary<K,V>` | `System.Collections` |

```csharp
// NON-GENERIC: ArrayList — stores as object
ArrayList list = new ArrayList();
list.Add(1);          // int boxed → object (heap allocation!)
list.Add("hello");    // string — fine (already reference)
list.Add(new User()); // object

int value = (int)list[0]; // Must cast — ❌ no compile-time safety
// list[1] cast to int at runtime → InvalidCastException 💥
```

### The Problems with Non-Generic Collections

**Problem 1 — No type safety (runtime crashes)**
```csharp
ArrayList items = new ArrayList();
items.Add(42);
items.Add("oops"); // Compiles fine — but...

foreach (int item in items) // ❌ InvalidCastException at runtime on "oops"
    Console.WriteLine(item);
```

**Problem 2 — Boxing/Unboxing (performance hit)**
```csharp
ArrayList nums = new ArrayList();
nums.Add(1); // int → boxed to object on HEAP (allocation!)
nums.Add(2); // another allocation

int x = (int)nums[0]; // Unboxing: extract value from heap object

// For 1 million integers: 1M heap allocations just for the boxing!
```

**Problem 3 — No IntelliSense / readable code**
```csharp
// What does this ArrayList contain? No idea without reading all callsites
ArrayList Process(ArrayList data) { return data; }
```

### Generic Collections — The Fix (C# 2.0+)

Generic collections are **type-safe, zero-boxing, and self-documenting**:

```csharp
// GENERIC: List<int> — stores real ints, no boxing
List<int> nums = new List<int>();
nums.Add(1);    // ✅ No boxing — stored as actual int in array
nums.Add(2);
// nums.Add("oops"); // ❌ Compile error — caught at build time, not runtime

int x = nums[0]; // ✅ No cast needed
```

### Side-by-Side Comparison

```csharp
// --- NON-GENERIC (old) ---
Hashtable cache = new Hashtable();
cache["user:1"] = new User { Name = "Alice" };
User u1 = (User)cache["user:1"]; // Manual cast every time
User u2 = (User)cache["user:2"]; // Null if missing — no KeyNotFoundException
                                  // But also: (User)null → no exception, silent fail

// --- GENERIC (modern) ---
Dictionary<string, User> cache2 = new();
cache2["user:1"] = new User { Name = "Alice" };
User u3 = cache2["user:1"];        // No cast, typed
cache2.TryGetValue("user:2", out var u4); // Safe and explicit
```

### Key Differences at a Glance

| Feature | Non-Generic (`ArrayList`, `Hashtable`) | Generic (`List<T>`, `Dictionary<K,V>`) |
|---------|:--------------------------------------:|:--------------------------------------:|
| **Type safety** | ❌ Runtime errors | ✅ Compile-time errors |
| **Boxing (value types)** | ❌ Always boxes | ✅ Zero boxing |
| **Casting** | ❌ Manual cast required | ✅ No casting |
| **IntelliSense / readability** | ❌ Opaque `object` | ✅ Strongly typed |
| **Performance** | ❌ Slower (boxing + cast) | ✅ Faster (direct access) |
| **Null safety** | ❌ Silent `null` on miss | ✅ `TryGetValue` pattern |
| **Use today?** | ❌ Avoid — legacy only | ✅ Always |

### When Do You Still See Non-Generic Collections?

- **Legacy codebases** — old .NET Framework or pre-C# 2.0 code
- **COM interop** / WinForms DataGrid bindings that expect `IList` (non-generic)
- **Reflection-driven code** that doesn't know the type at compile time

> **Rule:** Never write new code using `ArrayList`, `Hashtable`, or other non-generic collections. Always prefer `List<T>`, `Dictionary<K,V>`, etc. If you encounter them in an old codebase, migrate them.

### Migration Guide (Old → New)

```csharp
// ArrayList → List<T>
ArrayList old1 = new ArrayList { 1, 2, 3 };
List<int> new1 = old1.Cast<int>().ToList(); // Or just: new List<int> { 1, 2, 3 }

// Hashtable → Dictionary<K,V>
Hashtable old2 = new Hashtable { ["a"] = 1, ["b"] = 2 };
Dictionary<string, int> new2 = old2.Cast<DictionaryEntry>()
    .ToDictionary(e => (string)e.Key, e => (int)e.Value);

// Non-generic Queue → Queue<T>
Queue old3 = new Queue();
Queue<string> new3 = new Queue<string>();
while (old3.Count > 0) new3.Enqueue((string)old3.Dequeue());
```

---

## 4. Types of Collections


### 3.1 `List<T>` — The Workhorse

The most-used collection. A **resizable array** under the hood.

```csharp
var orders = new List<Order>();
orders.Add(new Order(1));
orders.Add(new Order(2));
orders.Insert(0, new Order(0)); // Insert at index 0

var first = orders[0];           // O(1) — index access
orders.Remove(orders[0]);        // O(n) — finds and removes first match
orders.RemoveAt(0);              // O(n) — shifts all elements left
bool has = orders.Contains(o);   // O(n) — linear scan

// Pre-allocate capacity to avoid resize copies
var big = new List<int>(expectedSize); // Avoids internal array doubling
```

**Internals:** Wraps a `T[]`. When full, doubles its capacity and copies everything. First add is O(1) amortised; mid-list insert/remove is O(n).

**Use when:** Ordered collection, frequent iteration, occasional index access.  
**Avoid when:** Frequent lookup by key, frequent mid-list insert/delete.

| Operation | Time |
|-----------|------|
| `Add` (end) | O(1) amortised |
| `Insert` / `RemoveAt` | O(n) |
| `[i]` read/write | O(1) |
| `Contains` / `Remove` | O(n) |

---

### 3.2 `Dictionary<TKey, TValue>` — The Lookup Table

A **hash table**: maps keys to values. O(1) average for get/set/lookup.

```csharp
var userById = new Dictionary<int, User>();
userById[1] = new User("Alice");
userById[2] = new User("Bob");

// ALWAYS use TryGetValue — not the indexer — to avoid KeyNotFoundException
if (userById.TryGetValue(3, out var user))
    Console.WriteLine(user.Name);
else
    Console.WriteLine("Not found");

// Iterate
foreach (var (id, u) in userById)
    Console.WriteLine($"{id}: {u.Name}");

// Useful methods
userById.ContainsKey(1);           // O(1)
userById.ContainsValue(someUser);  // O(n) — linear scan over values!
userById.Keys;                     // All keys
userById.Values;                   // All values
```

**Internals:** Keys are hashed via `GetHashCode()`, mapped to a bucket. Collisions are handled by chaining. Resizes (rehashes) when ~72% full. Requires key to correctly implement `GetHashCode()` and `Equals()`.

**Use when:** Lookup by a known key, caching, grouping by category.  
**Avoid when:** You need keys in sorted order → use `SortedDictionary<K,V>`.

| Operation | Time |
|-----------|------|
| `[key]` / `TryGetValue` | O(1) avg |
| `Add` / `Remove` | O(1) avg |
| `ContainsKey` | O(1) avg |
| `ContainsValue` | O(n) |

---

### 3.3 `HashSet<T>` — The Uniqueness Enforcer

Like a `Dictionary` with only keys — **no duplicates, O(1) membership test**.

```csharp
var processedIds = new HashSet<Guid>();

// Add returns false if already present — no separate Contains needed
bool isNew = processedIds.Add(someGuid); // True = new; False = already existed

if (processedIds.Contains(otherId)) { } // O(1)

// Set operations — very powerful
var setA = new HashSet<int> { 1, 2, 3, 4 };
var setB = new HashSet<int> { 3, 4, 5, 6 };
setA.IntersectWith(setB);  // setA = { 3, 4 }
setA.UnionWith(setB);      // setA = { 1, 2, 3, 4, 5, 6 }
setA.ExceptWith(setB);     // setA = { 1, 2 } — items in A but NOT in B
```

**Use when:** Idempotency tracking (seen this ID?), uniqueness enforcement, fast membership tests, set maths (intersection, union).

| Operation | Time |
|-----------|------|
| `Add` / `Remove` / `Contains` | O(1) avg |
| Set operations | O(n) |

---

### 3.4 `Queue<T>` — First In, First Out (FIFO)

Items are added to the back and removed from the front.

```csharp
var printJobs = new Queue<PrintJob>();
printJobs.Enqueue(job1); // Add to back
printJobs.Enqueue(job2);

var next = printJobs.Dequeue();  // Remove from front — O(1)
var peek = printJobs.Peek();     // Look at front without removing — O(1)
int count = printJobs.Count;
```

**Use when:** Processing in arrival order — messaging queues, job schedulers, BFS graph traversal.

| Operation | Time |
|-----------|------|
| `Enqueue` / `Dequeue` / `Peek` | O(1) |

---

### 3.5 `Stack<T>` — Last In, First Out (LIFO)

Items are added and removed from the same end (top).

```csharp
var history = new Stack<string>();
history.Push("Page A");
history.Push("Page B");
history.Push("Page C");

var last = history.Pop();   // "Page C" — removes top
var top  = history.Peek();  // "Page B" — peeks without removing
```

**Use when:** Undo/redo, back navigation, expression parsing, DFS graph traversal, call stack simulation.

| Operation | Time |
|-----------|------|
| `Push` / `Pop` / `Peek` | O(1) |

---

### 3.6 `LinkedList<T>` — Cheap Insert/Delete Anywhere

A doubly-linked list. Each node has a `Next` and `Previous` pointer. **No index access, no copying on insert.**

```csharp
var playlist = new LinkedList<string>();
playlist.AddLast("Song A");
playlist.AddLast("Song B");
var nodeA = playlist.Find("Song A")!;
playlist.AddAfter(nodeA, "Song A.5"); // O(1) insert if you have the node!

// No indexer: playlist[0] ❌ — must traverse
foreach (var song in playlist)        // O(n) iteration
    Console.WriteLine(song);
```

**Use when:** Frequent insert/delete at known positions; implementing LRU cache (with `Dictionary` for O(1) lookup + `LinkedList` for O(1) eviction).

| Operation | Time |
|-----------|------|
| `AddFirst` / `AddLast` | O(1) |
| `AddBefore` / `AddAfter` (given node) | O(1) |
| `Find` (by value) | O(n) |
| Index access | ❌ No indexer |

---

### 3.7 `SortedDictionary<TKey, TValue>` & `SortedList<TKey, TValue>`

Both keep keys in **sorted order**, but with different trade-offs:

| | `SortedDictionary<K,V>` | `SortedList<K,V>` |
|--|------------------------|-------------------|
| **Internal structure** | Red-Black Tree | Two sorted arrays |
| **Get/Set** | O(log n) | O(log n) binary search |
| **Insert / Delete** | O(log n) | O(n) — array shift |
| **Memory** | Higher (node overhead) | Lower (arrays) |
| **Best for** | Frequent insert/delete | Mostly reads, rare writes |

```csharp
var scores = new SortedDictionary<string, int>(); // Keys alphabetically sorted
scores["Charlie"] = 88;
scores["Alice"] = 95;
scores["Bob"] = 72;
foreach (var (name, score) in scores) // Iterates: Alice, Bob, Charlie
    Console.WriteLine($"{name}: {score}");
```

**Use when:** You need key-ordered iteration — leaderboards, range queries.

---

### 3.8 `SortedSet<T>`

A sorted `HashSet<T>`. Unique elements kept in sorted order.

```csharp
var sorted = new SortedSet<int> { 5, 3, 1, 4, 1 }; // { 1, 3, 4, 5 } — unique + sorted
var range = sorted.GetViewBetween(2, 4); // { 3, 4 } — range query!
```

**Use when:** Unique sorted values + range queries (e.g., event timestamps, price levels).

---

### 3.9 Concurrent Collections (Thread-Safe)

When multiple threads access the same collection:

| Type | Thread-Safe Version | Notes |
|------|-------------------|-------|
| `Dictionary<K,V>` | `ConcurrentDictionary<K,V>` | Lock-free reads, striped locks for writes |
| `Queue<T>` | `ConcurrentQueue<T>` | Lock-free FIFO |
| `Stack<T>` | `ConcurrentStack<T>` | Lock-free LIFO |
| `Bag` (unordered) | `ConcurrentBag<T>` | Thread-local storage, fast for producer=consumer |
| Bounded queue | `BlockingCollection<T>` | Blocks producer when full, consumer when empty |

```csharp
// ConcurrentDictionary — safe multi-threaded cache
var cache = new ConcurrentDictionary<int, User>();
var user = cache.GetOrAdd(id, id => FetchUser(id)); // Atomic get-or-create

// GetOrAdd is NOT atomic for the factory side — factory may run multiple times
// Use AddOrUpdate for true atomic update logic
```

---

### 3.10 Immutable Collections (`System.Collections.Immutable`)

Collections that **cannot be modified** after creation. All operations return a new instance.

```csharp
// Install: dotnet add package System.Collections.Immutable
var original = ImmutableList<int>.Empty.Add(1).Add(2).Add(3);
var added = original.Add(4);     // Returns NEW list — original unchanged
var removed = original.Remove(2); // Returns NEW list

// Publish atomically for lock-free read scenarios
private volatile ImmutableDictionary<int, Config> _settings = ImmutableDictionary<int, Config>.Empty;
public void UpdateSetting(int key, Config config)
    => _settings = _settings.SetItem(key, config); // Atomic swap
```

**Use when:** Configuration snapshots, DDD domain events, thread-safe read-heavy scenarios.

---

## 4. Time Complexity Cheat Sheet

| Collection | Add | Remove | Lookup/Contains | Access by Index |
|-----------|:---:|:------:|:---------------:|:---------------:|
| `List<T>` (end) | O(1)* | O(n) | O(n) | O(1) |
| `List<T>` (middle) | O(n) | O(n) | O(n) | O(1) |
| `Dictionary<K,V>` | O(1)* | O(1)* | O(1)* | ❌ |
| `HashSet<T>` | O(1)* | O(1)* | O(1)* | ❌ |
| `Queue<T>` | O(1) | O(1) | O(n) | ❌ |
| `Stack<T>` | O(1) | O(1) | O(n) | ❌ |
| `LinkedList<T>` | O(1)† | O(1)† | O(n) | ❌ |
| `SortedDictionary<K,V>` | O(log n) | O(log n) | O(log n) | ❌ |
| `SortedSet<T>` | O(log n) | O(log n) | O(log n) | ❌ |

*\* Amortised / average (O(n) worst case on resize or hash collision)*  
*† O(1) only when you already hold the `LinkedListNode<T>` reference*

---

## 5. Which Collection Should I Use?

```
I need to...
│
├── Store ordered items and access by position → List<T>
├── Look up values by a key → Dictionary<K,V>
├── Track unique items / membership test → HashSet<T>
├── Process in arrival order (FIFO) → Queue<T>
├── Process in reverse order / undo (LIFO) → Stack<T>
├── Insert/delete frequently at known positions → LinkedList<T>
├── Iterate keys in sorted order → SortedDictionary<K,V>
├── Unique items in sorted order + range queries → SortedSet<T>
├── Thread-safe access → Concurrent* collections
└── Immutable / published-once state → ImmutableList / ImmutableDictionary
```

---

## 6. Code Examples

### Basic — Choosing the Right Tool
```csharp
// WRONG: Using List for ID-based lookup — O(n) per call
var users = new List<User>();
users.Add(new User { Id = 1, Name = "Alice" });
var found = users.FirstOrDefault(u => u.Id == 1); // O(n) scan every time!

// CORRECT: Dictionary for O(1) lookup
var userMap = new Dictionary<int, User> { [1] = new User { Id = 1, Name = "Alice" } };
var found2 = userMap[1]; // O(1)

// WRONG: List + Contains for uniqueness — O(n²)
var seenIds = new List<int>();
foreach (var order in orders)
{
    if (!seenIds.Contains(order.Id)) // O(n) check inside O(n) loop = O(n²)!
        seenIds.Add(order.Id);
}

// CORRECT: HashSet — O(1) per check
var seenIds2 = new HashSet<int>();
foreach (var order in orders)
    seenIds2.Add(order.Id); // Returns false if duplicate — no Contains needed
```

### Real-World — Order Processing Cache
```csharp
public class OrderProcessor
{
    // O(1) lookup: productId → Product info
    private readonly Dictionary<int, Product> _productCache;

    // O(1) membership: orders already processed (idempotency)
    private readonly HashSet<Guid> _processedOrders = new();

    // FIFO: orders waiting to be processed
    private readonly Queue<Order> _pendingOrders = new();

    // Undo log: stack of recent actions
    private readonly Stack<OrderAction> _undoStack = new();

    public async Task<ProcessResult> ProcessNextAsync()
    {
        if (!_pendingOrders.TryDequeue(out var order))
            return ProcessResult.NothingPending;

        if (!_processedOrders.Add(order.Id)) // Add returns false = duplicate
            return ProcessResult.AlreadyProcessed;

        if (!_productCache.TryGetValue(order.ProductId, out var product))
            return ProcessResult.ProductNotFound;

        // Process...
        _undoStack.Push(new OrderAction(order)); // Record for undo
        return ProcessResult.Success;
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var action = _undoStack.Pop(); // Most recent action, reversed
        // Roll back...
    }
}
```

---

## 7. Interview Questions

1. **What is the difference between `List<T>` and `LinkedList<T>`?**
   *(List: O(1) index access, O(n) mid-insert. LinkedList: O(1) insert/delete at known node, no index access.)*
2. **Why is `Dictionary<K,V>` O(1) for lookup and not O(n)?**
   *(Hash function maps key to bucket → direct memory access, no scanning.)*
3. **When would you use `HashSet<T>` instead of `List<T>`?**
   *(When you need uniqueness or O(1) membership checks, not indexed access.)*
4. **What is the difference between `Dictionary` and `SortedDictionary`?**
   *(Dictionary: O(1), unordered. SortedDictionary: O(log n), keeps keys sorted.)*
5. **What happens when two keys produce the same hash code in a Dictionary?**
   *(Hash collision — both land in the same bucket. The Dictionary chains/scans by equality to find the right key. Performance degrades toward O(n) if many collisions.)*

---

## 8. Follow-up Questions

- Can you use a `List<T>` as a `Dictionary<K,V>` key? As a `HashSet<T>` element?
  *(Technically yes, but `List<T>` uses reference equality for `GetHashCode()`/`Equals()` — two lists with identical contents are NOT equal. Very surprising and a common bug.)*
- What is the difference between `ConcurrentDictionary.GetOrAdd` and `AddOrUpdate`?
  *(GetOrAdd: if key absent, add. If present, return existing. AddOrUpdate: atomic add-or-update with separate add/update delegates.)*
- Why does `Dictionary<K,V>` require keys to be immutable?
  *(Mutable keys change their hash after insertion → the key is now in the wrong bucket → you can never find it.)*
- What is `EqualityComparer<T>` and when do you pass one to a collection?
  *(Custom equality/hash logic — e.g., case-insensitive string dictionary: `new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)`)*

---

## 9. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using indexer on Dictionary — throws on missing key
var val = dict[99]; // ❌ KeyNotFoundException!
dict.TryGetValue(99, out var v); // ✅ Safe

// MISTAKE 2: Modifying a collection during foreach
foreach (var item in list)
    list.Remove(item); // ❌ InvalidOperationException — collection modified
// FIX: Collect removals first
var toRemove = list.Where(ShouldRemove).ToList();
foreach (var item in toRemove) list.Remove(item);

// MISTAKE 3: O(n²) — List.Contains inside a loop
foreach (var order in orders)
    if (blacklistIds.Contains(order.Id)) // ❌ O(n) per iteration if blacklistIds is a List
        Skip(order);
// FIX: Convert to HashSet once
var blacklistSet = blacklistIds.ToHashSet(); // O(n) once
foreach (var order in orders)
    if (blacklistSet.Contains(order.Id)) // O(1) per check
        Skip(order);

// MISTAKE 4: Using non-thread-safe List in multiple threads
var sharedList = new List<int>();
Parallel.For(0, 1000, i => sharedList.Add(i)); // ❌ Race condition — data corruption!
// FIX: ConcurrentBag<T> or lock or channel

// MISTAKE 5: Using mutable object as Dictionary key
public class Category { public string Name { get; set; } = ""; }
var dict = new Dictionary<Category, List<Product>>();
var cat = new Category { Name = "Electronics" };
dict[cat] = new List<Product>();
cat.Name = "Changed"; // HashCode may change → dict[cat] now fails to find it!
```

---

## 10. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | List, Dictionary, HashSet — syntax and basic use |
| **Level 2** | Time complexity, choosing the right collection, TryGetValue |
| **Level 3** | Hash internals, collision, SortedDictionary, ConcurrentDictionary |
| **Level 4** | ImmutableCollections, custom IEqualityComparer, LinkedList LRU cache, channel-based queues |

---

## 🔗 Connected Topics
- [Collection Interfaces](./collection-interfaces.md) — IEnumerable vs ICollection vs IList
- [Generics](../02-intermediate/generics.md) — All collections are generic
- [LINQ](../02-intermediate/linq.md) — Operates over `IEnumerable<T>` — all collections
- [Value vs Reference Types](./value-vs-reference-types.md) — Struct collections (no boxing)

*Created: April 2026 · Level: Beginner → Intermediate*
