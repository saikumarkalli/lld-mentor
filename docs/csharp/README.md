# C# Language — The Complete Learning Guide

> A structured, progressive learning path to master the C# language from first principles
> to CLR internals. Designed to be read like a book — each chapter builds on the previous one.

**Legend:** ✅ Complete · ⬜ Planned

---

## How to Use This Guide

```
Read in chapter order. Each topic assumes you understood the previous one.
Start at Chapter 1 and work your way down — do not skip ahead.

Every file follows the same structure:
  1. Core Idea (30 seconds)      ← Understand WHAT it is
  2. Deep Explanation             ← Understand WHY and HOW
  3. Code Examples                ← See it in real-world context (❌ Bad → ✅ Good)
  4. Interview Questions          ← Test yourself with model answers
  5. Follow-up Questions          ← Go deeper with edge cases
  6. Edge Cases / Common Mistakes ← Avoid production bugs
  7. Real-World Usage             ← Know where it matters
  8. Depth Levels                 ← Gauge your current understanding
```

---

## 📁 Folder Structure

```
docs/csharp/
├── 01-beginner/        Chapter 1–11    Types, memory, strings, keywords, collections
├── 02-oop/             Chapter 12–17   OOP pillars and class design
├── 03-intermediate/    Chapter 18–26   Generics, delegates, LINQ, iterators
├── 04-advanced/        Chapter 27–39   Async, threading, synchronization, modern C#
├── 05-expert/          Chapter 40–46   GC internals, CLR, JIT, TPL, performance
└── 06-serialization/   Chapter 47–50   File I/O, JSON, XML  (planned)
```

---

## Part I — Beginner: The Type System & Memory Model
#### 📂 `01-beginner/`

> Every line of C# code creates, moves, or destroys data. This part teaches you
> exactly where that data lives, how the runtime manages it, and the fundamental
> building blocks of the language.

| Ch | Topic | File | What You Will Learn | Status |
|:--:|-------|------|---------------------|:------:|
| 1 | Value Types vs Reference Types | [value-vs-reference-types.md](./01-beginner/01-value-vs-reference-types.md) | The fundamental split in the .NET type system — `int` lives on the stack, `object` lives on the heap. Boxing/unboxing overhead and when it silently kills performance. | ✅ |
| 2 | Stack vs Heap Memory | [stack-vs-heap.md](./01-beginner/02-stack-vs-heap.md) | How the CLR manages the call stack (LIFO, per-thread, 1MB) vs the managed heap (GC-controlled). Why understanding this changes how you write high-performance code. | ✅ |
| 3 | Nullable Types & Default Values | [nullable-types.md](./01-beginner/03-nullable-types.md) | `Nullable<T>`, the `?` syntax, null-coalescing (`??`), null-conditional (`?.`), and C# 8+ nullable reference types (`#nullable enable`). | ✅ |
| 4 | String vs StringBuilder | [string-vs-stringbuilder.md](./01-beginner/04-string-vs-stringbuilder.md) | Why `string` is immutable (every concatenation creates a new heap object), when `StringBuilder` saves thousands of allocations, and string interning. | ✅ |
| 5 | Access Modifiers | [access-modifiers.md](./01-beginner/05-access-modifiers.md) | `public`, `private`, `protected`, `internal`, `protected internal`, `private protected` — the visibility rules that enforce encapsulation. | ✅ |
| 6 | Types of Constructors | [types-of-constructors.md](./01-beginner/06-types-of-constructors.md) | Default, parameterised, static, private, and copy constructors. Constructor chaining with `this()` and `base()`. Static constructor thread-safety guarantees. | ✅ |
| 7 | Types of Classes | [types-of-class.md](./01-beginner/07-types-of-class.md) | `abstract`, `sealed`, `static`, `partial` classes — when to use each, what the compiler enforces, and how they affect inheritance hierarchies. | ✅ |
| 8 | Collections Overview | [collections-overview.md](./01-beginner/08-collections-overview.md) | `List<T>`, `Dictionary<TKey,TValue>`, `HashSet<T>`, `Queue<T>`, `Stack<T>` — internal data structures, time complexity (O(1) vs O(n)), and choosing the right collection. | ✅ |
| 9 | Collection Interfaces | [collection-interfaces.md](./01-beginner/09-collection-interfaces.md) | `IEnumerable` → `ICollection` → `IList` → `IReadOnlyCollection` hierarchy. Why you should accept the most abstract interface and return the most specific type. | ✅ |
| 10 | Enums & Flags | [enums-and-flags.md](./01-beginner/10-enums-and-flags.md) | Enum underlying types, `[Flags]` attribute for bitwise combinations, parsing with `Enum.TryParse`, `ToString()` internals, and enum-based state machines. | ✅ |
| 11 | Interfaces Deep Dive | [interfaces.md](./01-beginner/11-interfaces.md) | Explicit vs implicit implementation, default interface methods (C# 8+), interface segregation, `IComparable<T>`, `IEquatable<T>`, and `ICloneable` pitfalls. | ✅ |

---

## Part II — Object-Oriented Programming
#### 📂 `02-oop/`

> OOP isn't just theory — it's the backbone of every .NET framework, every design pattern,
> and every enterprise codebase. These four pillars appear in every senior-level interview.

| Ch | Topic | File | What You Will Learn | Status |
|:--:|-------|------|---------------------|:------:|
| 12 | Encapsulation | [encapsulation.md](./02-oop/12-encapsulation.md) | Data hiding with access modifiers. Properties vs fields. Auto-properties, computed properties. Why direct field access is an anti-pattern. Encapsulation's role in SOLID. | ✅ |
| 13 | Inheritance | [inheritance.md](./02-oop/13-inheritance.md) | `base` keyword, constructor chaining, method overriding (`virtual`/`override`), `sealed` override. Why C# forbids multiple class inheritance and uses interfaces instead. | ✅ |
| 14 | Polymorphism | [polymorphism.md](./02-oop/14-polymorphism.md) | Compile-time (overloading) vs runtime (overriding) polymorphism. `new` keyword hiding vs `override`. How the CLR uses the Virtual Method Table (vtable) for dispatch. | ✅ |
| 15 | Abstraction | [abstraction.md](./02-oop/15-abstraction.md) | Abstract classes vs interfaces. When to use each. Default interface methods (C# 8+). The template method pattern as abstraction in action. | ✅ |
| 16 | Sealed Classes & Methods | [sealed-classes.md](./02-oop/16-sealed-classes.md) | Why `sealed` improves JIT performance (devirtualization). Sealed overrides. When to seal and when not to. Framework design guidelines. | ✅ |
| 17 | Constructor Chaining | [constructors.md](./02-oop/17-constructors.md) | `this()` and `base()` chaining. Execution order in inheritance hierarchies. Mandatory vs optional parameter patterns. Object initializer syntax vs constructor arguments. | ✅ |

---

## Part III — Intermediate: Functional C# & Data Querying
#### 📂 `03-intermediate/`

> This is where C# transforms from a simple OOP language into a powerful, expressive tool.
> Delegates enable functional patterns, LINQ replaces verbose loops, and generics eliminate
> code duplication. These topics dominate technical interviews.

| Ch | Topic | File | What You Will Learn | Status |
|:--:|-------|------|---------------------|:------:|
| 18 | Generics & Constraints | [generics.md](./03-intermediate/18-generics.md) | Generic classes, methods, and interfaces. Constraints (`where T : class`, `struct`, `new()`, `IComparable`). How .NET reification differs from Java type erasure — `List<int>` and `List<string>` are physically distinct types at runtime. | ✅ |
| 19 | Delegates — Func, Action, Predicate | [delegates.md](./03-intermediate/19-delegates.md) | Type-safe function pointers. `MulticastDelegate` internals (invocation list). Built-in `Func<T>`, `Action<T>`, `Predicate<T>`. Closure traps — how captured variables get heap-promoted. The classic loop-variable bug. | ✅ |
| 20 | Events | [events.md](./03-intermediate/20-events.md) | How `event` restricts delegate access (add/remove accessors). `EventHandler<T>` pattern. Memory leaks from forgotten subscriptions. Thread-safe event invocation with `?.Invoke()`. | ✅ |
| 21 | LINQ | [linq.md](./03-intermediate/21-linq.md) | Deferred execution — queries don't run until enumerated. `IQueryable<T>` vs `IEnumerable<T>` — server-side SQL vs client-side C#. All standard operators: `Where`, `Select`, `SelectMany`, `GroupBy`, `Join`, `Aggregate`. The multiple-enumeration trap. | ✅ |
| 22 | Expression Trees | [expression-trees.md](./03-intermediate/22-expression-trees.md) | `Func<T>` is compiled code; `Expression<Func<T>>` is code-as-data. How EF Core reads expression nodes to generate SQL. Dynamic query builders (PredicateBuilder/Specification pattern). Why arbitrary C# methods fail inside `IQueryable`. | ✅ |
| 23 | Extension Methods | [extension-methods.md](./03-intermediate/23-extension-methods.md) | Adding methods to types you don't own. `this` parameter syntax. How LINQ is built entirely on extension methods. Fluent API design. Compile-time resolution rules. | ✅ |
| 24 | Iterators & `yield return` | [iterators.md](./03-intermediate/24-iterators.md) | Lazy sequence generation. How the compiler generates a state machine `IEnumerator<T>` class. `yield return` vs `yield break`. Why iterators enable deferred LINQ pipelines without materialising collections. | ✅ |
| 25 | Exception Handling Internals | [exception-handling.md](./03-intermediate/25-exception-handling.md) | CLR two-pass stack walk (filter → unwind). `throw` vs `throw ex` — preserving stack traces. Exception cost (10,000x slower than return). Result pattern for business errors. Exception filters with `when`. | ✅ |
| 26 | Tuples & Deconstruction | [tuples-deconstruction.md](./03-intermediate/26-tuples-deconstruction.md) | `ValueTuple` (stack) vs `Tuple` (heap). Named elements. Deconstruction with `var (x, y) = ...`. Custom `Deconstruct()` methods. Using tuples as lightweight return types without creating a class. | ✅ |

---

## Part IV — Advanced: Async, Threading & Modern C#
#### 📂 `04-advanced/`

> This is the senior engineer's territory. Async programming, thread synchronisation, and
> modern C# features (records, pattern matching, Span) separate mid-level developers from
> architects. Every topic here is a common source of production bugs.

#### Async & Multithreading

| Ch | Topic | File | What You Will Learn | Status |
|:--:|-------|------|---------------------|:------:|
| 27 | async / await & Task Internals | [async-await.md](./04-advanced/27-async-await.md) | Compiler-generated state machines. `Task` vs `ValueTask`. `ConfigureAwait(false)`. CPU-bound vs I/O-bound — why I/O-bound tasks use zero threads while waiting. The `SynchronizationContext` trap. | ✅ |
| 28 | CancellationToken | [cancellation-token.md](./04-advanced/28-cancellation-token.md) | Cooperative cancellation model — why `Thread.Abort()` was removed. `CancellationTokenSource` vs `CancellationToken`. Timeout patterns, linked tokens, ASP.NET Core auto-wiring. The disposal requirement. | ✅ |
| 29 | Task vs Thread vs ThreadPool | [task-vs-thread.md](./04-advanced/29-task-vs-thread.md) | Raw threads (1MB stack), ThreadPool (reusable workers), Tasks (high-level promises). I/O completion ports. `Task.Run` vs `Task.Factory.StartNew`. `LongRunning` flag. ThreadPool starvation diagnosis. | ✅ |
| 30 | Synchronization Primitives | [synchronization.md](./04-advanced/30-synchronization.md) | Race conditions and why `count++` isn't atomic. `lock` (Monitor internals), `SemaphoreSlim` (async-compatible throttle), `Interlocked` (atomic CPU instructions), `ReaderWriterLockSlim`. Why you can't `await` inside `lock`. | ✅ |
| 31 | Deadlocks | [deadlocks.md](./04-advanced/31-deadlocks.md) | Coffman's four conditions. The sync-over-async deadlock (`.Result` on SynchronizationContext). Lock ordering discipline. `Monitor.TryEnter` timeouts. Diagnosing deadlocks with `dotnet-dump`. | ✅ |
| 32 | Parallel Programming | [parallel-programming.md](./04-advanced/32-parallel-programming.md) | `Parallel.For/ForEach` for CPU-bound work. `Parallel.ForEachAsync` for I/O-bound. PLINQ (`AsParallel`, `AsOrdered`). Thread-local accumulators to avoid lock contention. `MaxDegreeOfParallelism`. | ✅ |

#### Memory Management

| Ch | Topic | File | What You Will Learn | Status |
|:--:|-------|------|---------------------|:------:|
| 33 | IDisposable & Memory Management | [memory-management.md](./04-advanced/33-memory-management.md) | Managed vs unmanaged resources. The Dispose pattern (full implementation). `using` statement and `using` declaration (C# 8+). Finalizer cost and suppression with `GC.SuppressFinalize`. | ✅ |
| 34 | Span\<T\> & Memory\<T\> | [span-memory.md](./04-advanced/34-span-memory.md) | Zero-allocation slicing of arrays, strings, and native memory. `ref struct` limitations. `stackalloc` + `Span<T>` for high-perf parsing. `ReadOnlySpan<T>` for safe substring operations without heap allocation. | ✅ |

#### Modern C# Features

| Ch | Topic | File | What You Will Learn | Status |
|:--:|-------|------|---------------------|:------:|
| 35 | Pattern Matching | [pattern-matching.md](./04-advanced/35-pattern-matching.md) | Type patterns, property patterns, relational patterns (`is > 0 and <= 100`), list patterns. Switch expressions with exhaustiveness checking. Replacing visitor pattern with switch expressions. | ✅ |
| 36 | Records & Immutability | [records-structs.md](./04-advanced/36-records-structs.md) | `record class` vs `record struct` vs `readonly record struct`. Value-based equality. `with` expressions for non-destructive mutation. Decision framework: when to use record vs class vs struct. | ✅ |
| 37 | Reflection & Attributes | [reflection.md](./04-advanced/37-reflection.md) | Runtime type inspection. `typeof`, `GetType()`, `Type.GetMethods()`. Custom attributes. Performance cost of reflection. Source generators as a compile-time alternative. | ✅ |
| 38 | Covariance & Contravariance | [variance.md](./04-advanced/38-variance.md) | Generic type variance — `out T` (covariant, safe for output) vs `in T` (contravariant, safe for input). Why variance only works on interfaces/delegates. `IEnumerable<out T>`, `Action<in T>`. | ✅ |
| 39 | `dynamic` Keyword | [dynamic-keyword.md](./04-advanced/39-dynamic-keyword.md) | Dynamic dispatch via the DLR. `ExpandoObject`. When dynamic is justified (COM interop, JSON deserialization) and when it's an anti-pattern (losing compile-time safety). | ✅ |

---

## Part V — Expert: Runtime Internals & Performance
#### 📂 `05-expert/`

> Under the hood. Understanding these topics means you can diagnose production incidents,
> optimise hot paths, and make architectural decisions that prevent performance problems
> before they happen.

| Ch | Topic | File | What You Will Learn | Status |
|:--:|-------|------|---------------------|:------:|
| 40 | Garbage Collection Internals | [garbage-collection.md](./05-expert/40-garbage-collection.md) | Generational GC (Gen 0/1/2). Roots, mark phase, compact phase. Large Object Heap (LOH) fragmentation. Server GC vs Workstation GC. `GC.Collect()` — why you should almost never call it. Pinning and its impact. | ✅ |
| 41 | CLR Internals | [clr-internals.md](./05-expert/41-clr-internals.md) | Intermediate Language (IL/MSIL). Managed execution pipeline: C# → IL → JIT → Native. `AssemblyLoadContext` for plugin isolation. AppDomain removal in .NET Core. Type metadata and the Method Table. | ✅ |
| 42 | JIT Compilation & Tiered Compilation | [jit-compilation.md](./05-expert/42-jit-compilation.md) | Just-In-Time vs Ahead-Of-Time (AOT). Tiered compilation: Tier 0 (quick, unoptimised) → Tier 1 (optimised at runtime). ReadyToRun (R2R) images. Profile-Guided Optimisation (PGO). | ✅ |
| 43 | Task Parallel Library (TPL) | [task-parallel-library.md](./05-expert/43-task-parallel-library.md) | TPL Dataflow (BufferBlock, TransformBlock, ActionBlock). `System.Threading.Channels` for async producer/consumer. Pipeline patterns. Backpressure with bounded channels. | ✅ |
| 44 | Low-Level Performance Tuning | [performance-tuning.md](./05-expert/44-performance-tuning.md) | BenchmarkDotNet methodology. Reducing allocations (struct, `Span<T>`, `ArrayPool<T>`). Object pooling. `dotnet-counters`, `dotnet-trace`, `dotnet-dump` diagnostic tools. Allocation-free logging with `LoggerMessage.Define`. | ✅ |
| 45 | Memory Leaks in Managed Code | [memory-leaks.md](./05-expert/45-memory-leaks.md) | Event handler leaks, static collection growth, `CancellationTokenSource` timer leaks, captured closures in long-lived delegates. Diagnosing with `dotnet-dump` heap analysis. | ✅ |
| 46 | `stackalloc` & Unsafe Memory | [stackalloc.md](./05-expert/46-stackalloc.md) | Stack-allocated buffers for zero-GC-pressure hot paths. `unsafe` keyword, pointer arithmetic, `fixed` statement. Interop with native C/C++ libraries via P/Invoke. | ✅ |

---

## Part VI — Serialization & I/O
#### 📂 `06-serialization/`

> Real-world applications read and write data constantly. Understanding stream semantics,
> serialization performance, and large file handling is critical for production systems.

| Ch | Topic | File | What You Will Learn | Status |
|:--:|-------|------|---------------------|:------:|
| 47 | File I/O & Streams | [file-io.md](./06-serialization/47-file-io.md) | `Stream`, `FileStream`, `StreamReader/Writer`, `BinaryReader/Writer`. Buffering strategies. Async file I/O. `IAsyncEnumerable` for line-by-line processing of massive files. | ✅ |
| 48 | JSON Serialization | [json-serialization.md](./06-serialization/48-json-serialization.md) | `System.Text.Json` vs `Newtonsoft.Json`. Source generators for AOT-compatible serialization. `JsonSerializerOptions`, custom converters, polymorphic serialization. Performance benchmarks. | ✅ |
| 49 | XML Basics | [xml-basics.md](./06-serialization/49-xml-basics.md) | `XDocument`, `XmlSerializer`, `XmlReader/Writer`. LINQ to XML. When XML is still required (SOAP, config files, legacy integrations). | ✅ |
| 50 | Serialization Performance | [serialization-performance.md](./06-serialization/50-serialization-performance.md) | Large file handling (streaming vs buffering). `Utf8JsonReader/Writer` for zero-alloc JSON parsing. `System.IO.Pipelines` for high-throughput I/O. Memory-mapped files. | ✅ |

---

## 🗺️ Learning Roadmap

```
PART I — THE TYPE SYSTEM (Ch 1–11)
Read this first. Everything in C# is built on the type system.
  Ch 1  Value vs Reference Types   →  Ch 2  Stack vs Heap
  Ch 3  Nullable Types             →  Ch 4  String vs StringBuilder
  Ch 5  Access Modifiers           →  Ch 6  Constructors  →  Ch 7  Class Types
  Ch 8  Collections                →  Ch 9  Collection Interfaces
  Ch 10 Enums & Flags              →  Ch 11 Interfaces

PART II — OOP (Ch 12–17)
The four pillars plus class design. Read in order.
  Ch 12 Encapsulation  →  Ch 13 Inheritance  →  Ch 14 Polymorphism  →  Ch 15 Abstraction
  Ch 16 Sealed Classes →  Ch 17 Constructor Chaining

PART III — FUNCTIONAL C# (Ch 18–26)
This unlocks LINQ, events, and modern C# patterns.
  Ch 18 Generics       →  Ch 19 Delegates      →  Ch 20 Events
  Ch 21 LINQ           →  Ch 22 Expression Trees
  Ch 23 Extension Methods  →  Ch 24 Iterators   →  Ch 25 Exception Handling
  Ch 26 Tuples & Deconstruction

PART IV — ASYNC & MODERN C# (Ch 27–39)
The senior-level topics. Read the async chain in order.
  Ch 27 async/await     →  Ch 28 CancellationToken  →  Ch 29 Task vs Thread
  Ch 30 Synchronization →  Ch 31 Deadlocks          →  Ch 32 Parallel Programming
  Ch 33 IDisposable     →  Ch 34 Span<T>
  Ch 35 Pattern Matching →  Ch 36 Records  →  Ch 37 Reflection
  Ch 38 Variance         →  Ch 39 dynamic

PART V — INTERNALS (Ch 40–46)
For architects. Read after mastering Parts I–IV.
  Ch 40 Garbage Collection  →  Ch 41 CLR Internals  →  Ch 42 JIT Compilation
  Ch 43 TPL Dataflow        →  Ch 44 Performance Tuning
  Ch 45 Memory Leaks        →  Ch 46 stackalloc & Unsafe

PART VI — I/O (Ch 47–50)
Practical production skills.
  Ch 47 File I/O  →  Ch 48 JSON  →  Ch 49 XML  →  Ch 50 Serialization Performance
```

---

## 🔗 Interview Question Chains

These are the most common interview progressions. Follow the arrows:

| Chain | Flow |
|-------|------|
| **Types** | Value vs Reference `→` Boxing cost `→` Stack vs Heap `→` `struct` performance |
| **OOP** | Polymorphism `→` vtable dispatch `→` `new` vs `override` `→` Sealed class perf benefit |
| **Delegates** | Delegate `→` Events `→` Closure loop bug `→` `async void` danger |
| **LINQ** | Deferred execution `→` IQueryable `→` Expression Trees `→` EF N+1 |
| **Async** | State machine `→` ConfigureAwait `→` Deadlock `→` CancellationToken |
| **Memory** | GC Generations `→` LOH `→` IDisposable `→` Finalizer cost `→` Memory leaks |
| **Threading** | Task vs Thread `→` ThreadPool starvation `→` Synchronization `→` Deadlocks |

---

## 🔗 Related Code Projects

| Code Project | What's Inside |
|-------------|---------------|

---

## 📊 Progress

| Part | Chapters | Complete | Remaining |
|------|:--------:|:--------:|:---------:|
| I — Beginner | 11 | 11 | 0 |
| II — OOP | 6 | 6 | 0 |
| III — Intermediate | 9 | 9 | 0 |
| IV — Advanced | 13 | 13 | 0 |
| V — Expert | 7 | 7 | 0 |
| VI — Serialization | 4 | 4 | 0 |
| **Total** | **50** | **50** | **0** |

> 📅 Last updated: May 2026
