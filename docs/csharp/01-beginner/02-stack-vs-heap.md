# Chapter 2 � Stack vs Heap Memory

> **⚡ Core Idea (30 seconds):** Every variable in C# lives in one of two memory areas. The **stack** is fast, small, and automatic — local variables live here. The **heap** is large and GC-managed — objects live here. **Value types** typically live on the stack; **reference types** always live on the heap. That distinction drives performance, memory layout, and assignment semantics.

**Domain:** `C#` **Level:** `Beginner` **Tags:** `#stack` `#heap` `#memory` `#value-types` `#reference-types`

---

## 1. Core Idea

Think of memory in two ways:
- **Stack** = a notepad on your desk. You jot things down, finish the task, and tear off the page. Fast, temporary, automatic.
- **Heap** = a warehouse. You store large, long-lived things here. A manager (the GC) periodically clears what you no longer need.

When you write `int x = 5`, the number `5` lives on your notepad (stack).
When you write `var o = new Order()`, you create an object in the warehouse (heap) and write the warehouse address on your notepad.

---

## 2. Deep Explanation

### The Stack

- A **LIFO** (Last In, First Out) block of memory per thread
- Stores: local variables, method parameters, return addresses
- Allocation/deallocation is a **single pointer move** — essentially free
- Fixed size (~1 MB by default per thread in .NET)
- Automatically cleaned up when a method returns — no GC involved
- **Thread-local** — each thread has its own stack

```
Method A calls Method B:
│ B: local int y = 10  │ ← Stack grows down
│ A: local int x = 5   │
│ A: return address     │
└───────────────────────┘
When B returns → y is instantly gone (pointer moves back up)
```

### The Heap

- A large shared pool of memory, managed by the **Garbage Collector**
- Stores: all **reference type** objects (`class` instances, arrays, strings, delegates)
- Allocation = finding a free block, with bookkeeping overhead
- Deallocation = GC runs, determines what's unreachable, frees memory
- **Shared** across threads — concurrent access requires care

### What Lives Where

| What | Where | Why |
|------|-------|-----|
| `int`, `double`, `bool`, `char`, `decimal` | Stack (local) | Value types — small, fixed size |
| `struct` (local variable) | Stack | Value type |
| `class` instance | Heap | Reference type |
| `string` | Heap | Reference type (despite behaving like a value) |
| Array `int[]` | Heap (array body); Stack (reference variable) | Arrays are reference types |
| Value type inside a class | Heap (embedded in the object) | Lives where the object lives |

### Value Types vs Reference Types — The Memory Picture

```csharp
// VALUE TYPE — copied on assignment
int a = 10;
int b = a;   // b gets its own copy of 10
b = 99;
Console.WriteLine(a); // 10 — unaffected. a and b are independent.

// REFERENCE TYPE — reference (address) copied on assignment
var list1 = new List<int> { 1, 2, 3 };
var list2 = list1;   // list2 gets a COPY of the address — points to same object
list2.Add(4);
Console.WriteLine(list1.Count); // 4 — same object modified!
```

**Memory diagram:**
```
VALUE TYPE (stack):
┌──────────────────────┐
│  a = 10              │  Two separate boxes
│  b = 99              │
└──────────────────────┘

REFERENCE TYPE:
┌──────────────────────┐       ┌──────────────────┐
│  list1 → 0x1A2B      │──────▶│  List object     │
│  list2 → 0x1A2B      │──────▶│  [1, 2, 3, 4]   │ (Heap)
└──────────────────────┘       └──────────────────┘
(Stack)                    Both point to same address!
```

---

### Boxing and Unboxing — The Silent Performance Killer

When a **value type** is treated as a **reference type** (`object` or an interface), it must be **boxed** — wrapped in a heap object:

```csharp
int x = 42;
object boxed = x;   // BOXING: allocates heap object, copies x into it
int unboxed = (int)boxed; // UNBOXING: extracts value from heap object

// Why it's expensive:
// 1. Heap allocation (GC pressure)
// 2. Memory copy
// 3. Cast verification

// Common boxing traps:
ArrayList list = new ArrayList();
list.Add(42);    // ❌ Boxing — int becomes object
int val = (int)list[0]; // Unboxing

// Fix: use generics
List<int> genericList = new List<int>();
genericList.Add(42); // ✅ No boxing — List<int> is strongly typed
```

---

### Stack Overflow

If the stack runs out of space (typically from infinite recursion), you get a `StackOverflowException`:

```csharp
public void Recurse()
{
    Recurse(); // No base case → stack fills up → StackOverflowException
}
```

---

### `ref` and `out` — Passing Stack Addresses

`ref` and `out` pass the **address** of a stack variable, allowing a method to modify the caller's variable directly:

```csharp
void Double(ref int value) => value *= 2;

int x = 5;
Double(ref x);
Console.WriteLine(x); // 10 — method modified the stack variable directly
```

---

## 3. Code Examples

### Basic — Seeing the Difference in Action
```csharp
// STRUCT (value type) — behaves like a copy
public struct Point { public int X; public int Y; }

Point p1 = new Point { X = 1, Y = 2 };
Point p2 = p1;       // Full copy of the struct
p2.X = 99;
Console.WriteLine(p1.X); // 1 — p1 is unchanged

// CLASS (reference type) — shares the address
public class PointClass { public int X; public int Y; }

PointClass c1 = new PointClass { X = 1, Y = 2 };
PointClass c2 = c1;  // Copies the REFERENCE, not the object
c2.X = 99;
Console.WriteLine(c1.X); // 99 — c1 was modified through c2!
```

### Real-World — Performance Consideration
```csharp
// SCENARIO: Processing 1 million 2D points
// If stored as class — 1M heap objects → GC pressure
List<PointClass> classPoints = Enumerable.Range(0, 1_000_000)
    .Select(i => new PointClass { X = i, Y = i }) // 1M heap allocations!
    .ToList();

// If stored as struct — data is inline in the array (heap once, no per-object overhead)
List<Point> structPoints = Enumerable.Range(0, 1_000_000)
    .Select(i => new Point { X = i, Y = i }) // 1 heap allocation (the array)
    .ToList();

// The struct list is much friendlier to the GC — all points are packed inline in memory
// (CPU cache-friendly too — sequential memory access)
```

---

## 4. Interview Questions

1. **What is the difference between the stack and the heap in .NET?**
   *The **stack** is a fast, thread-local, LIFO memory region for local variables and method frames — allocation is just a pointer move and cleanup is automatic when a method returns. The **heap** is a large shared pool managed by the Garbage Collector — objects allocated here survive across method calls but require GC to reclaim.*

2. **Where does a local `int` variable live? Where does a `new Order()` live?**
   *A local `int` lives on the **stack** — it's a value type and local, so it goes in the current method frame and disappears when the method returns. `new Order()` lives on the **heap** — `Order` is a class (reference type), so the object is allocated on the GC-managed heap. The variable holding the reference to it sits on the stack.*

3. **What is boxing and unboxing? Why is it a performance concern?**
   *Boxing is wrapping a value type (e.g., `int`) in a heap-allocated `object` wrapper. Unboxing is extracting the value back out. Every box causes a heap allocation plus a copy — in a tight loop with millions of operations, this means millions of small GC-tracked objects. This floods Gen 0 and triggers frequent GC collections, killing throughput.*

4. **What happens when you assign one reference type variable to another?**
   *Only the **reference** (memory address) is copied — both variables point to the same heap object. Modifying the object through one variable is immediately visible through the other. To get an independent copy, you must explicitly clone or deep-copy the object.*

5. **What is a `StackOverflowException` and what causes it?**
   *It happens when the call stack exceeds its fixed size (~1MB per thread). The most common cause is **infinite recursion** — a method calling itself with no base case. It can also happen with very deep legitimate recursion on large input. In modern .NET, `StackOverflowException` cannot be caught — it terminates the process.*

---

## 5. Follow-up Questions

- If a `struct` is a field inside a `class`, where does it live?
  *(On the heap — it's embedded inline in the class object which lives on the heap. The "struct lives on stack" rule only applies to local variables.)*

- Is `string` a value type or reference type? Why does it behave like a value type?
  *(Reference type — lives on the heap. But it's **immutable** — every "modification" creates a new string. This makes it appear to behave like a value type in terms of not sharing mutations.)*

- What is the `readonly struct` keyword and why does it help performance?
  *(Guarantees the struct is never mutated. The JIT can pass it `in` parameters as a reference without copying — avoiding the copy overhead of large structs.)*

- What is `stackalloc` and when would you use it?
  ```csharp
  Span<byte> buffer = stackalloc byte[64]; // Allocates 64 bytes on the STACK — zero GC
  // Use for small, short-lived buffers in hot paths
  ```

- Does the GC ever collect stack memory?
  *(No — the stack is self-managed via the method call frame mechanism. The GC only manages the heap.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Passing large structs by value — every call copies the entire struct
public struct HugeStruct { public int A, B, C, D, E, F, G, H; /* 8 ints = 32 bytes */ }
void Process(HugeStruct s) { } // ❌ Copies 32 bytes every call
void Process(in HugeStruct s) { } // ✅ Passes by readonly reference — no copy

// MISTAKE 2: Expecting struct mutation via interface to affect original
public interface IMoveable { void Move(int dx, int dy); }
public struct Point : IMoveable
{
    public int X, Y;
    public void Move(int dx, int dy) { X += dx; Y += dy; }
}

Point p = new Point { X = 0, Y = 0 };
IMoveable m = p; // ❌ BOXING! m holds a BOXED COPY of p
m.Move(5, 5);    // Mutates the boxed copy — not p
Console.WriteLine(p.X); // 0 — p was never changed!

// MISTAKE 3: Believing "stack is always faster than heap"
// For cache-friendly sequential access of small structs in arrays: true
// For random access, large objects, or cross-method lifetime: heap is fine

// MISTAKE 4: Infinite recursion → StackOverflowException (cannot be caught in .NET 2+)
// The process terminates. Use iterative approaches for deep recursion.
```

---

## 7. Real-World Usage

| Scenario | Stack/Heap Consideration |
|----------|------------------------|
| High-frequency math (game loop, signals) | `struct` / `Span<T>` to stay on stack/avoid GC |
| Domain entities (Order, User, Product) | `class` on heap — lifetime is complex |
| Short-lived network buffers | `stackalloc` + `Span<byte>` for zero GC |
| Value objects (Money, Point, GUID) | `readonly struct` or `record struct` |
| Collections of primitives | `List<int>` — ints stored inline, only one heap object |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Stack = fast/local; Heap = GC-managed; value types vs reference types |
| **Level 2** | Boxing/unboxing, assignment semantics, `ref`/`out` |
| **Level 3** | struct in class lives on heap; `in` keyword; `readonly struct` |
| **Level 4** | `stackalloc`, GC generations interaction, struct layout padding, CPU cache line effects |

## Connected Topics
- [Value Types vs Reference Types](./01-value-vs-reference-types.md) — The "what" to this topic's "where"
- [Nullable Types](./03-nullable-types.md) — `Nullable<T>` wraps value types with heap semantics
- [Garbage Collection](../05-expert/40-garbage-collection.md) — The GC that manages the heap
- [Span\<T\> & Memory\<T\>](../04-advanced/34-span-memory.md) — Zero-allocation stack-based processing

*Created: April 2026 · Level: Beginner*
