# JIT Compilation & Tiered Compilation

> **⚡ Core Idea (30 seconds):** The JIT (Just-In-Time) compiler converts IL to native CPU instructions when a method is **first called**. Tiered compilation made this smarter — it compiles fast unoptimized code first (quick startup), then recompiles hot methods with full optimizations in the background. Knowing this helps you write code the JIT can optimize aggressively.

**Domain:** `C#` **Level:** `Expert` **Tags:** `#jit` `#optimization` `#inlining` `#devirtualization` `#tiered`

---

## 1. Core Idea

Before .NET, the JIT was a one-shot deal: compile IL → native, with full optimizations. This made startup slow. Tiered compilation introduced a two-phase approach: compile quickly first (startup wins), recompile hot paths later (throughput wins).

For you as a developer: writing **JIT-friendly code** means the JIT can apply inlining, devirtualization, and SIMD automatically — giving you near-C++ performance in safe managed code.

---

## 2. Deep Explanation

### Tiered Compilation Phases

```
First call to Method()
  → Tier 0: Fast codegen (minimal optimization, ~2-5ms compile)
  → Method is called many times (hot)
    → Background thread recompiles
  → Tier 1: Full optimization (inlining, loop unrolling, SIMD)
  → Future calls use Tier 1 code
```

### JIT Optimizations Applied at Tier 1

**Method Inlining** — Replaces a method call with the method body:
```csharp
// Inlined: no call overhead for tiny methods
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int Add(int a, int b) => a + b;
// JIT substitutes: result = a + b; directly — no stack frame push/pop
```

**Devirtualization** — Converting virtual calls to direct calls:
```csharp
// JIT can devirtualize if it proves only one type is used
List<int> list = new(); // JIT knows it's List<int>, not IList<int>
list.Add(1);            // JIT calls List<int>.Add directly — no vtable lookup!

// Sealed classes always devirtualize
sealed class Validator { public bool IsValid(int x) => x > 0; }
var v = new Validator();
v.IsValid(5); // JIT inlines + devirtualizes — essentially: `return 5 > 0;`
```

**Dead Code Elimination & Constant Folding**:
```csharp
const int X = 5;
int result = X * 2 + 3; // JIT reduces to: result = 13; at compile time
```

**SIMD Vectorization** — Auto-vectorizing loops using CPU SIMD instructions (SSE, AVX):
```csharp
// JIT can auto-vectorize this into SIMD instructions
for (int i = 0; i < array.Length; i++)
    array[i] *= 2; // Processes 4/8 elements per instruction with SIMD
```

### `MethodImpl` Attributes

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]   // Force inline — use for hot small methods
[MethodImpl(MethodImplOptions.NoInlining)]            // Prevent inline — for stack security or profiling
[MethodImpl(MethodImplOptions.AggressiveOptimization)] // Apply Tier 1 immediately (skip warmup)
```

---

## 3. Code Examples

### Basic — JIT-friendly vs JIT-unfriendly patterns
```csharp
// JIT-FRIENDLY: Short methods, no unnecessary allocations
public static bool IsValid(int value) => value is > 0 and < 1000;
// JIT inlines this everywhere it's called

// JIT-UNFRIENDLY: Large methods resist inlining
public void Process() // 200 lines of logic
{
    // JIT won't inline — too large
    // Can't devirtualize interfaces used here
    // Heap allocations pressure GC between Tier 0 and Tier 1
}

// JIT-FRIENDLY: sealed helps devirtualization
public sealed class FastList<T> : IList<T> { } // JIT knows exact type, devirtualizes all calls

// Use Span/ReadOnlySpan for sequential processing — JIT vectorizes naturally
public static int Sum(ReadOnlySpan<int> values)
{
    int sum = 0;
    foreach (var v in values) sum += v; // JIT may vectorize with SIMD
    return sum;
}
```

### Real-World — Benchmarking JIT behavior
```csharp
// Always benchmark with BenchmarkDotNet — it handles JIT warmup automatically
[MemoryDiagnoser]
public class JitBenchmark
{
    private readonly int[] _data = Enumerable.Range(0, 10_000).ToArray();

    [Benchmark(Baseline = true)]
    public int SumLoop()
    {
        int sum = 0;
        for (int i = 0; i < _data.Length; i++) sum += _data[i];
        return sum;
    }

    [Benchmark]
    public int SumSpan()
    {
        int sum = 0;
        foreach (var v in _data.AsSpan()) sum += v;
        return sum;
    }

    [Benchmark]
    public int SumLinq() => _data.Sum(); // LINQ — virtual calls resist JIT optimization
}
// Typical result: SumSpan ≈ SumLoop >> SumLinq (2-5x slower due to delegate and virtual overhead)
```

---

## 4. Interview Questions

1. **What is JIT compilation and when does it occur?**
2. **What is tiered compilation? What problem does it solve?**
3. **What is method inlining and when does the JIT apply it?**
4. **What is devirtualization? How can you help the JIT devirtualize?**
5. **What does `[MethodImpl(MethodImplOptions.AggressiveInlining)]` do?**

---

## 5. Follow-up Questions

- What is PGO (Profile-Guided Optimization) in .NET 6+?
  *(The JIT uses runtime profiling data to make better optimization decisions — even more aggressive than Tier 1)*
- Can the JIT inline a method that throws exceptions? *(Yes, but only if the exception path is not the common path — JIT uses branch prediction heuristics)*
- What is `ReadyToRun` (R2R) and how does it differ from NativeAOT?
  *(R2R: pre-JIT compilation embedded in DLL — fast startup + JIT can still optimize at runtime. NativeAOT: fully ahead-of-time — no JIT at all.)*
- Why does `virtual` call prevent JIT devirtualization on an interface but not on a sealed class?
  *(Sealed class: only one implementation possible → devirtualize safely. Interface: unlimited implementations → must use vtable.)*
- How does `dynamic` keyword interact with JIT? *(Dynamic calls bypass JIT optimization — they go through the DLR which adds significant overhead)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Benchmarking without JIT warmup = measuring Tier 0
// Always use BenchmarkDotNet — it does multiple rounds to reach Tier 1

// MISTAKE 2: AggressiveInlining on large methods — no benefit, may hurt I-cache
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void LargeMethod() { /* 100+ lines */ } // ❌ JIT may still not inline; hints aren't guarantees

// MISTAKE 3: Using interfaces everywhere and losing devirtualization on hot paths
// HIGH FREQUENCY code should prefer concrete types or sealed classes for JIT optimization
// BOUNDARY code (DI injection) can use interfaces freely

// MISTAKE 4: Dynamic dispatch with 'dynamic' in hot loops
dynamic obj = GetValue();
for (int i = 0; i < 1_000_000; i++)
    obj.Process(); // ❌ Each call resolved via DLR — NOT JIT-optimized
```

---

## 7. Real-World Usage

| Scenario | JIT Consideration |
|----------|------------------|
| Startup optimization | ReadyToRun or NativeAOT for fast cold start |
| Numeric hot paths | `sealed` types, `Span<T>`, `AggressiveInlining` |
| Plugin hot paths | `sealed` implementations, concrete type usage |
| HTTP request processing | Tiered JIT means first request is slow — warmup endpoints matter |
| BenchmarkDotNet | Isolates Tier 1 JIT — always use for perf measurements |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | JIT converts IL to native on first call |
| **Level 2** | Tiered compilation, inlining, devirtualization |
| **Level 3** | sealed for devirtualization, MethodImpl attributes, SIMD vectorization |
| **Level 4** | PGO, R2R, NativeAOT, custom JIT hints, dynamic PGO (.NET 8+) |

## 🔗 Connected Topics
- [CLR Internals](./clr-internals.md) — JIT is a CLR subsystem
- [Performance Tuning](./performance-tuning.md) — JIT-friendly patterns drive perf
- [Garbage Collection](./garbage-collection.md) — GC pauses interrupt JIT-compiled code

*Created: April 2026 · Level: Expert*
