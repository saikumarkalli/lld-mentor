# Chapter 46 — `stackalloc` & Unsafe Memory

> **⚡ Core Idea (30 seconds):** `stackalloc` allows you to allocate a block of memory directly on the **call stack** instead of the managed heap. This creates zero Garbage Collection (GC) pressure. It's used for ultra-high-performance hot paths, but it's dangerous: stack space is small (usually 1MB per thread), and allocating too much causes an immediate `StackOverflowException`.

**Domain:** `C#` **Level:** `Expert` **Tags:** `#stackalloc` `#unsafe` `#memory` `#performance`

---

## 1. Core Idea

Think of the Heap as a **warehouse** — huge, but you need a forklift (the GC) to manage inventory. Think of the Stack as your **pockets** — instantly accessible, zero cleanup required when you leave the room, but you can only fit so much inside before your pants rip. `stackalloc` bypasses the warehouse entirely.

---

## 2. Deep Explanation

### Why use `stackalloc`?

Whenever you create a new array (`new byte[256]`), you allocate on the managed heap. If you do this 100,000 times a second in a web server parsing JSON, the GC must work constantly to clean up those arrays, causing CPU spikes and latency pauses. 

`stackalloc` allocates memory on the stack. When the method returns, the stack frame pops, and the memory is instantly gone. **Zero GC involvement.**

### The Old Way: `unsafe`

Before C# 7.2, `stackalloc` required the `unsafe` keyword and raw pointers (`*`). This required compiling with the `/unsafe` flag and bypassed CLR memory bounds checking, leading to buffer overflow vulnerabilities.

```csharp
// The old, dangerous way (still valid)
unsafe void ProcessData()
{
    byte* buffer = stackalloc byte[1024];
    buffer[0] = 42; // No bounds checking! buffer[2000] would corrupt memory.
}
```

### The Modern Way: `Span<T>`

Modern C# wraps `stackalloc` in `Span<T>`. This provides **safe, bounds-checked** stack memory without the `unsafe` keyword.

```csharp
// The modern, safe way
void ProcessData()
{
    Span<byte> buffer = stackalloc byte[1024];
    buffer[0] = 42; // Safe!
    // buffer[2000] = 42; // Throws IndexOutOfRangeException safely
}
```

### Stack Size Limits

The default stack size for a thread in Windows is **1MB** (Linux/macOS varies, often 1.5MB - 8MB). A `StackOverflowException` cannot be caught by a `try-catch` block — it instantly kills the entire application process.
**Golden Rule:** Never `stackalloc` more than a few kilobytes (e.g., `< 4KB`). For larger buffers, use `ArrayPool<T>`.

---

## 3. Code Examples

### Example 1 — Zero-Allocation String Formatting
```csharp
// ❌ Bad: Allocates a char array and a new string on the heap
public string GetGuidString_Bad()
{
    var guid = Guid.NewGuid();
    return guid.ToString("N"); // Heap allocation
}

// ✅ Good: High-performance, zero-allocation formatting
public string GetGuidString_Good()
{
    var guid = Guid.NewGuid();
    // 32 chars = 64 bytes on the stack. Perfectly safe.
    Span<char> buffer = stackalloc char[32]; 
    guid.TryFormat(buffer, out int charsWritten, "N");
    return new string(buffer); // Only allocates the final string
}
```

### Example 2 — The Fallback Pattern (Stack vs Heap)
If the required size is dynamic, you must check it before using `stackalloc` to prevent a stack overflow.

```csharp
public void ProcessData(int requiredLength)
{
    // Threshold: 1024 bytes (1KB)
    const int StackLimit = 1024;
    
    // Allocate on stack if small, otherwise rent from the ArrayPool
    byte[]? rentedArray = null;
    
    Span<byte> buffer = requiredLength <= StackLimit 
        ? stackalloc byte[requiredLength] 
        : (rentedArray = ArrayPool<byte>.Shared.Rent(requiredLength));

    try
    {
        // Use 'buffer' (Span abstracts away whether it's stack or heap!)
        FillBuffer(buffer);
    }
    finally
    {
        // Return rented array to pool if we used it
        if (rentedArray != null)
            ArrayPool<byte>.Shared.Return(rentedArray);
    }
}
```

---

## 4. Interview Questions

1. **What is `stackalloc` and when would you use it?**
   *`stackalloc` allocates a block of memory on the thread's call stack rather than the managed heap. I use it in high-performance hot paths (like parsing, formatting, or serialization) to eliminate GC pressure by creating zero-allocation buffers.*

2. **Why is `stackalloc` wrapped in `Span<T>` in modern C#?**
   *Historically, `stackalloc` required the `unsafe` keyword and raw pointers, which lacked bounds checking and could cause memory corruption. Wrapping it in `Span<T>` makes it completely memory-safe and bounds-checked while still avoiding heap allocations, eliminating the need for `unsafe`.*

3. **What happens if you `stackalloc` too much memory?**
   *You will cause a `StackOverflowException`. Thread stacks are very small (typically 1MB). Unlike `OutOfMemoryException`, a stack overflow instantly terminates the entire process and cannot be caught by a `try-catch` block.*

4. **How do you handle variable-length allocations safely?**
   *You should define a safe threshold (e.g., 1KB or 2KB). If the requested size is below the threshold, use `stackalloc`. If it exceeds the threshold, rent a buffer from `ArrayPool<T>.Shared`. `Span<T>` elegantly abstracts over both.*

5. **Can you return a `stackalloc` Span from a method?**
   *No. The compiler will prevent you with error CS8352. Stack memory is tied to the current method's stack frame. When the method returns, the stack frame is popped and the memory is overwritten by the next method call. `Span<T>` is a `ref struct` which enforces these lifetime rules at compile time.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: stackalloc inside a loop
void ProcessItems(int count)
{
    for (int i = 0; i < count; i++)
    {
        // ❌ DEADLY: Memory accumulates on the stack until the method exits!
        // 10,000 iterations = guaranteed StackOverflowException
        Span<byte> buffer = stackalloc byte[1024]; 
    }
}
// FIX: Move stackalloc outside the loop
Span<byte> buffer = stackalloc byte[1024];
for (int i = 0; i < count; i++) { /* reuse buffer */ }

// MISTAKE 2: Dynamic sizes without limits
void Process(int size)
{
    // ❌ DANGEROUS: What if an attacker passes size = 1,000,000? 
    Span<byte> buffer = stackalloc byte[size]; 
}

// MISTAKE 3: Returning stack memory
Span<byte> GetBuffer()
{
    Span<byte> buffer = stackalloc byte[100];
    return buffer; // ❌ Compile error: "Cannot use local variable 'buffer' in this context"
}
```

---

## 🔗 Connected Topics

- [Span\<T\> & Memory\<T\>](../04-advanced/34-span-memory.md) — The safe container for stackalloc memory
- [Stack vs Heap](../01-beginner/02-stack-vs-heap.md) — The architectural difference between the two memory areas
- [Performance Tuning](./44-performance-tuning.md) — ArrayPool vs stackalloc comparisons
- [Garbage Collection](./40-garbage-collection.md) — How stackalloc avoids Gen 0 allocations entirely

---

*Created: May 2026 · Level: Expert*
