# Chapter 34 � Span<T> & Memory<T>

> **⚡ Core Idea (30 seconds):** `Span<T>` is a **stack-only window into contiguous memory** — array, stack memory, or unmanaged memory — without copying. It lets you slice and process data with **zero heap allocations**. `Memory<T>` is the heap-safe version for async scenarios.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#span` `#memory` `#stackalloc` `#performance` `#zero-alloc`

---

## 1. Core Idea

Before Span, slicing a string created a new string object. `string.Substring("Hello World", 6, 5)` → allocates "World". With Span, you get a **view** into the original memory — no allocation, no copy.

Span is like a window in a building. You look through it and see a portion of what's outside — but you didn't build a new building just to look at part of it.

---

## 2. Deep Explanation

### What Span\<T\> Is

`Span<T>` is a `ref struct` containing:
- A **pointer** to the start of the memory
- A **length** (how many elements in the window)

Because it's a `ref struct`:
- It can **only live on the stack** — cannot be a field in a class or heap object
- Cannot be used in `async` methods (stack frames are managed differently there)
- Cannot be boxed
- Cannot implement interfaces

```csharp
int[] array = { 1, 2, 3, 4, 5 };
Span<int> span = array;              // Window over entire array
Span<int> middle = array.AsSpan(1, 3); // [2, 3, 4] — no copy, same memory
middle[0] = 99;
Console.WriteLine(array[1]);         // 99 — modified the original!
```

### Memory Sources for Span

```csharp
// 1. Heap array
Span<byte> fromArray = new byte[100];

// 2. Stack allocation (stackalloc) — truly zero heap allocation
Span<byte> fromStack = stackalloc byte[64]; // 64 bytes on the stack

// 3. Unmanaged memory (unsafe)
unsafe { Span<byte> fromPtr = new Span<byte>(ptr, length); }

// 4. String (ReadOnlySpan<char>)
ReadOnlySpan<char> chars = "Hello World".AsSpan(6); // "World" — no string allocation
```

### Memory\<T\> — The Async-Compatible Cousin

`Memory<T>` wraps the same concept but is a **regular struct** (not ref struct), so it can:
- Be stored in classes
- Be used in `async` methods
- Be passed across `await` boundaries

```csharp
// Async method — must use Memory<T>, not Span<T>
public async Task ProcessAsync(Memory<byte> buffer, CancellationToken ct)
{
    await _stream.ReadAsync(buffer, ct);
    var span = buffer.Span; // Get Span<T> for synchronous processing
}
```

### ReadOnlySpan\<T\> and ReadOnlyMemory\<T\>

For read-only scenarios (string slicing, reading from buffers):
```csharp
ReadOnlySpan<char> name = "John Doe".AsSpan();
ReadOnlySpan<char> first = name[..4]; // "John" — no allocation
```

---

## 3. Code Examples

### Basic — Zero-allocation string parsing
```csharp
// Parse "2024-04-20" without any string allocations
public static bool TryParseDate(ReadOnlySpan<char> input, out DateTime result)
{
    result = default;
    if (input.Length != 10) return false;

    if (!int.TryParse(input[..4], out int year)) return false;   // "2024"
    if (!int.TryParse(input[5..7], out int month)) return false; // "04"
    if (!int.TryParse(input[8..], out int day)) return false;    // "20"

    result = new DateTime(year, month, day);
    return true;
}

// Call with no allocations:
TryParseDate("2024-04-20".AsSpan(), out var date);
```

### Real-World — High-Performance Buffer Processing
```csharp
// Processing binary protocol frames without copying
public class FrameParser
{
    private readonly byte[] _buffer = new byte[65536]; // Reusable buffer

    public async Task<Frame> ReadFrameAsync(Stream stream, CancellationToken ct)
    {
        // Read header into buffer without allocation
        Memory<byte> headerMemory = _buffer.AsMemory(0, 4);
        await stream.ReadExactlyAsync(headerMemory, ct);

        // Parse header fields using stack Span
        Span<byte> header = headerMemory.Span;
        int length = BinaryPrimitives.ReadInt32BigEndian(header); // Zero-copy read

        // Read payload into buffer (offset by 4)
        Memory<byte> payloadMemory = _buffer.AsMemory(4, length);
        await stream.ReadExactlyAsync(payloadMemory, ct);

        return new Frame(header.ToArray(), payloadMemory.ToArray()); // Only allocate final result
    }
}

// String tokenizer — split "a,b,c" without creating substrings
public static void TokenizeWithSpan(ReadOnlySpan<char> input, char delimiter)
{
    while (!input.IsEmpty)
    {
        int idx = input.IndexOf(delimiter);
        ReadOnlySpan<char> token = idx < 0 ? input : input[..idx];
        Console.WriteLine(token.ToString()); // Only allocate when printing
        input = idx < 0 ? default : input[(idx + 1)..];
    }
}
```

---

## 4. Interview Questions

1. **What is `Span<T>` and what problem does it solve?**
   *`Span<T>` is a **stack-only view** into a contiguous block of memory — an array, stack memory, or unmanaged memory — without copying it. Before Span, slicing a string (`Substring`) allocated a new string. Slicing an array gave you a new array. `Span<T>` lets you describe "elements 5 to 10" as a view — zero allocation, zero copy. It's essential for high-performance parsing and buffer processing.*

2. **Why is `Span<T>` a `ref struct`? What limitations does that impose?**
   *`ref struct` means the struct can only live on the **stack** — never on the heap. This restriction exists because `Span<T>` holds a pointer to memory that could be stack memory (from `stackalloc`), which the GC doesn't track. If a `Span` could live on the heap, it could outlive the stack frame its memory came from. Limitations: cannot be a class field, cannot be used in `async` methods (stack frames don't survive `await`), cannot implement interfaces, cannot be boxed.*

3. **What is the difference between `Span<T>` and `Memory<T>`?**
   *Both are windows into contiguous memory, but `Span<T>` is a `ref struct` (stack-only, fast) and `Memory<T>` is a regular struct (heap-safe, usable as class field and in async methods). Use `Span` for synchronous code. Convert to `Memory` when you need to pass the buffer across `await` boundaries or store it in a class. Call `memory.Span` inside synchronous sections to get back a `Span` for processing.*

4. **What is `stackalloc` and why is it used with `Span<T>`?**
   *`stackalloc` allocates memory on the current thread's **stack** rather than the heap — zero GC involvement. It's used for small temporary buffers (< ~1KB). Combined with `Span<T>`, the result is completely allocation-free: `Span<byte> buf = stackalloc byte[64]`. This is ideal for parsing or formatting small data. Use `Memory<T>` if the buffer needs to survive beyond the current stack frame.*

5. **When would you use `ReadOnlySpan<char>` instead of `string`?**
   *When processing or parsing a string without needing to allocate substrings. For example, tokenizing a CSV line: `ReadOnlySpan<char> line = input.AsSpan(); int comma = line.IndexOf(','); var first = line[..comma];` — no string allocation. Use `ReadOnlySpan<char>` in APIs that accept input to parse; they can then be called with either a string (`.AsSpan()`) or a span from a buffer with zero extra allocation.*

---

## 5. Follow-up Questions

- Why can't `Span<T>` be used in `async` methods? *(Stack frames are moved/managed across await points — Span can't survive that)*
- Can you store a `Span<T>` in a class field? *(No — ref struct restriction means it can't be heap-allocated)*
- What is `MemoryMarshal.Cast<TFrom, TTo>` useful for? *(Reinterpreting raw bytes as structured types — zero-copy deserialization)*
- How does `Span<T>` interact with garbage collection? *(When wrapping an array, the GC still manages the array. When wrapping stackalloc memory, there's no GC involvement at all — that memory is freed when the stack frame pops.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using Span<T> in async method — compile error
public async Task ProcessAsync()
{
    Span<byte> buffer = stackalloc byte[64]; // ❌ CS4012: Span can't be used here
    await DoWorkAsync();
}
// FIX: Use Memory<T> for async
public async Task ProcessAsync()
{
    using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(64);
    Memory<byte> buffer = owner.Memory[..64];
    await DoWorkAsync(buffer); // ✅
}

// MISTAKE 2: stackalloc too large — stack overflow
Span<byte> huge = stackalloc byte[1_000_000]; // ❌ Stack is ~1MB. This crashes!
// Rule of thumb: stackalloc should be < 1KB for safety

// MISTAKE 3: Forgetting Span modifications affect original array
int[] arr = { 1, 2, 3 };
var span = arr.AsSpan();
span[0] = 99;           // Modifies arr[0]!
Console.WriteLine(arr[0]); // 99 — unexpected mutation

// MISTAKE 4: Holding onto Span after the stack frame it points to is gone
Span<int> Dangerous()
{
    int x = 42;
    return new Span<int>(ref x); // ❌ Points to stack memory that will be freed!
}
```

---

## 7. Real-World Usage

| Scenario | Span/Memory Usage |
|----------|-----------------|
| JSON parsers (`System.Text.Json`) | Parses from `ReadOnlySpan<byte>` — zero allocation |
| ASP.NET Core pipeline | Request body read as `ReadOnlySequence<byte>` via `Memory<T>` |
| String parsing utilities | `AsSpan()`, `TryParse()` with ReadOnlySpan<char> |
| Binary protocol readers | `BinaryPrimitives` with `Span<byte>` |
| gRPC serialization | `Memory<byte>` for streaming payloads |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Span as array window, AsSpan(), slicing |
| **Level 2** | ref struct limitation, Memory<T> for async, stackalloc |
| **Level 3** | ReadOnlySpan, BinaryPrimitives, MemoryPool<T> |
| **Level 4** | MemoryMarshal, Sequence, custom IBufferWriter<T>, unsafe pinning |

## Connected Topics
- [Value vs Reference Types](../01-beginner/01-value-vs-reference-types.md) — Span is a value type (ref struct)
- [Garbage Collection](../05-expert/40-garbage-collection.md) — Span avoids heap allocations entirely
- [Memory Management](./33-memory-management.md) — MemoryPool<T>, IMemoryOwner<T>
- [async/await](./27-async-await.md) — Memory<T> crosses await boundaries; Span cannot

*Created: April 2026 · Level: Advanced*