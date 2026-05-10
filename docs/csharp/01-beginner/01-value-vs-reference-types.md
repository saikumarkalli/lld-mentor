# Chapter 1 � Value Types vs Reference Types

> **⚡ Core Idea (30 seconds):** Value types live on the **stack** and hold data directly. Reference types live on the **heap** and hold a pointer to data. This distinction drives memory layout, performance, and copying semantics throughout C#.

**Domain:** `C#` **Level:** `Beginner → Intermediate` **Tags:** `#memory` `#types` `#stack` `#heap`

---

## 1. Core Idea

Think of value types like a **sticky note** — when you copy it, you get a brand new note with the same content. Think of reference types like a **shared Google Doc** — when you copy the link, both people edit the same document.

This is not just trivia. Getting this wrong leads to subtle bugs, unintended mutations, and poor performance.

---

## 2. Deep Explanation

### How It Works Internally

**Value Types** (`int`, `double`, `bool`, `char`, `struct`, `enum`):
- Stored directly on the **stack** (or inline in the containing object if a field)
- When assigned, the **entire value is copied**
- No garbage collection involvement
- Dereferencing cost: zero (direct memory access)

**Reference Types** (`class`, `string`, `interface`, `delegate`, `array`):
- The **object** lives on the **managed heap**
- The variable holds a **reference (pointer)** to that heap location
- When assigned, only the **reference is copied** — both variables point to the same object
- Subject to **Garbage Collection**

```
Stack                    Heap
┌──────────────┐         ┌───────────────────────┐
│ int x = 5    │  5      │ Person { Name="Alice" }│
│ int y = x    │  5      │ ↑                     │
│ Person p1 ──────────── │ (reference)           │
│ Person p2 ──────────── │ (same object!)        │
└──────────────┘         └───────────────────────┘
```

### Why It Exists

C# gives you **control over memory layout**. If everything was heap-allocated, small numeric operations would generate millions of GC-tracked objects. Value types allow high-performance scenarios (game loops, parsers, financial calculations) without GC pressure.

### When to Use Value Types (struct)
- Small, immutable data (< 16 bytes ideally)
- Data that is copied frequently and you *want* copy semantics
- Hot paths where heap allocation must be avoided
- Examples: `Vector3`, `DateTime`, `Guid`, `Point`

### When NOT to Use struct
- Large data — copying a 200-byte struct on every assignment kills performance
- When you need inheritance
- When null representation matters (use `Nullable<T>` carefully)

### Boxing & Unboxing — The Hidden Trap
When a value type is assigned to an `object` (or interface), it gets **boxed** — wrapped in a heap object. This is expensive.

```csharp
int x = 42;
object boxed = x;       // Boxing: allocates heap memory
int unboxed = (int)boxed; // Unboxing: copies value back out
```

---

## 3. Code Examples

### Example 1 — Basic: Copy Semantics
```csharp
// Value type — copy semantics
int a = 10;
int b = a;
b = 20;
Console.WriteLine(a); // 10 — unchanged, a has its own copy

// Reference type — shared reference
class Point { public int X; }

var p1 = new Point { X = 10 };
var p2 = p1;   // p2 points to the SAME object
p2.X = 20;
Console.WriteLine(p1.X); // 20 — both see the mutation!
```

### Example 2 — Real-World: Struct Performance in Hot Path
```csharp
// BAD: Class allocation in tight loop — GC pressure
public class Coordinate
{
    public double Lat, Lng;
}

for (int i = 0; i < 1_000_000; i++)
{
    var c = new Coordinate { Lat = i, Lng = i }; // 1M heap allocations!
    ProcessCoordinate(c);
}

// GOOD: Struct — zero heap allocations
public struct Coordinate
{
    public double Lat, Lng;
}

for (int i = 0; i < 1_000_000; i++)
{
    var c = new Coordinate { Lat = i, Lng = i }; // Stack / inline
    ProcessCoordinate(c);
}
```

### Example 3 — Boxing Trap with Collections
```csharp
// BAD: ArrayList boxes every int
ArrayList list = new ArrayList();
list.Add(5);  // Boxing!
int val = (int)list[0]; // Unboxing!

// GOOD: Generic List<T> avoids boxing entirely
List<int> ints = new List<int>();
ints.Add(5);  // No boxing
int val2 = ints[0]; // No unboxing
```

---

## 4. Interview Questions

1. **What is the difference between value types and reference types in C#?**
   *Value types (int, bool, struct) store data directly and are copied on assignment — each variable has its own independent copy. Reference types (class, string, array) store a reference (memory address) to the actual data on the heap — assignment copies the reference, so both variables point to the same object.*

2. **Where are value types stored? Is it always the stack?**
   *Not always. Local variable value types go on the stack. But if a value type is a **field inside a class**, it lives on the heap — embedded inside the class object. The rule is: value types live where their container lives.*

3. **What is boxing? Why is it a performance concern?**
   *Boxing is when a value type (e.g. `int`) is converted to `object`. The CLR wraps it in a heap-allocated object. Unboxing extracts it back. It's expensive because: (1) a heap allocation happens, (2) the value is copied, and (3) the GC must eventually clean it up. In a hot loop, this can cause thousands of unnecessary allocations.*

4. **Why does `string` behave like a value type even though it's a reference type?**
   *Because `string` is **immutable**. Every "change" creates a new string — the original never mutates. So even though two variables can point to the same string, you'll never see one variable's string change through another variable — making it appear to have copy semantics like a value type.*

5. **When would you choose `struct` over `class`?**
   *Use struct when: the object is small (ideally ≤ 16 bytes), has no identity (two structs with same data should be "equal"), is short-lived and created frequently (avoid GC), and doesn't need inheritance. Classic examples: `DateTime`, `Point`, `Vector3`, `Money` value objects.*

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These come after you give the standard answer. Don't get caught.*

- `int x = 5; object o = x;` — How many heap allocations happen here?
- If a `struct` has a `List<T>` field, where does the list live?
- What happens to value types captured in a **lambda** or **closure**?
  *(They get promoted to heap — the closure object holds them)*
- Explain why `Span<T>` is a `ref struct` and what that means for its storage.
- What is a **by-ref struct**? Why can't `ref struct` be boxed?
- Can a struct implement an interface? What happens if you call an interface method on a struct stored as the interface type?
  *(It gets boxed! The virtual dispatch requires a heap object)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Assuming struct fields are independent
struct Rect { public int Width; public int Height; }
Rect r1 = new Rect { Width = 10, Height = 5 };
Rect r2 = r1;
r2.Width = 20;
// r1.Width is still 10 — this is correct behaviour, but devs expect mutation

// MISTAKE 2: Mutable struct as readonly field — compiler warning + confusion
readonly struct Point { public int X; public int Y; } // Use readonly struct!

// MISTAKE 3: ValueType in Dictionary/HashSet without overriding GetHashCode
// Default GetHashCode for structs uses reflection — SLOW
public struct OrderKey
{
    public int OrderId;
    public int CustomerId;
    // MUST override GetHashCode() and Equals() for perf
    public override int GetHashCode() => HashCode.Combine(OrderId, CustomerId);
}

// MISTAKE 4: Forgetting null-check for reference types
string name = GetName(); // could return null
Console.WriteLine(name.Length); // NullReferenceException!
```

**Performance Issue:** Passing large structs by value into methods copies the entire struct on every call. Use `in` keyword for read-only large struct parameters:
```csharp
void Process(in LargeStruct data) { } // Passes by ref, read-only
```

---

## 7. Real-World Usage

| Scenario | How It Applies |
|----------|---------------|
| Game engines (Unity) | `Vector3`, `Quaternion` are structs — millions created per frame with zero GC pressure |
| Financial systems | `decimal` is a value type — prevents reference equality bugs on monetary values |
| High-performance parsers | `Span<T>`, `ReadOnlySpan<char>` avoid string allocations |
| EF Core | Entity classes are reference types — tracked by reference identity in the change tracker |
| ASP.NET Core | `HttpContext` is a class; `Guid` route params are value types |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | Stack vs heap, value copied vs reference copied |
| **Level 2** | Boxing/unboxing, struct vs class trade-offs |
| **Level 3** | How closures promote value types to heap, ref struct limitations |
| **Level 4** | `Span<T>` / `Memory<T>`, `in`/`ref`/`out` parameters, `readonly struct` for JIT optimizations |

---

## 🔗 Connected Topics

- [Nullable Types](./03-nullable-types.md) — `Nullable<T>` wraps value types for null representation
- [Collections Overview](./08-collections-overview.md) — `List<T>` vs `ArrayList` boxing implications
- [Span & Memory](../04-advanced/34-span-memory.md) — Zero-allocation slicing of value type arrays
- [Memory Management](../04-advanced/33-memory-management.md) — GC only manages reference types

---

*Created: April 2026 · Level: Beginner → Intermediate*
