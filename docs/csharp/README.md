# C# Language — Knowledge Hub

> Deep mastery of the C# language for backend engineers targeting product-based company interviews.
>
> 📌 **How to use:** Follow the `#` priority numbers — lower number = learn first. Each topic builds on the previous ones.

---

## 📁 Folder Structure

```
docs/csharp/
├── 01-beginner/      ← Value types, boxing, nullables, strings, access modifiers, collections
├── oops/             ← OOP pillars: encapsulation, inheritance, polymorphism, abstraction
├── 02-intermediate/  ← Collections, LINQ, Delegates, Events, Iterators, Generics
├── 03-advanced/      ← async/await, Memory, Span<T>, Records, Pattern Matching, Reflection
├── 04-expert/        ← GC/LOH, CLR, JIT, TPL, Performance Tuning
└── 05-serialization/ ← File I/O, JSON, XML, large file handling
```

---

## 01-beginner

> Stack vs heap, types, boxing, keywords — the foundation everything else builds on.

| # | Topic | File | Status |
|---|-------|------|--------|
| 1 | Value Types vs Reference Types (Stack vs Heap) | [value-vs-reference-types.md](./01-beginner/value-vs-reference-types.md) | ✅ |
| 2 | Stack vs Heap Memory (deep dive) | [stack-vs-heap.md](./01-beginner/stack-vs-heap.md) | ✅ |
| 3 | Boxing & Unboxing | covered in [value-vs-reference-types.md](./01-beginner/value-vs-reference-types.md) | ✅ |
| 4 | Default Values & Nullable Types | [nullable-types.md](./01-beginner/nullable-types.md) | ✅ |
| 5 | String vs StringBuilder | [string-vs-stringbuilder.md](./01-beginner/string-vs-stringbuilder.md) | ✅ |
| 6 | Access Modifiers (public, private, internal, protected internal) | [access-modifiers.md](./01-beginner/access-modifiers.md) | ✅ |
| 7 | Types of Constructors (default, parameterised, static, private…) | [types-of-constructors.md](./01-beginner/types-of-constructors.md) | ✅ |
| 8 | Types of Classes (abstract, sealed, static, partial…) | [types-of-class.md](./01-beginner/types-of-class.md) | ✅ |
| 9 | Collections — List, Dictionary, HashSet | [collections-overview.md](./01-beginner/collections-overview.md) | ✅ |
| 10 | IEnumerable vs ICollection vs IList vs IReadOnlyCollection | [collection-interfaces.md](./01-beginner/collection-interfaces.md) | ⬜ |
| 11 | Control Flow Basics | [control-flow.md](./01-beginner/control-flow.md) | ⬜ |

---

## oops/ — OOP Pillars

> These docs explain the **why** and **internals** for each OOP concept. Actual C# code lives in [`LLDMaster.OOP/`](../../LLDMaster.OOP/).

| # | Topic | File | Code Reference | Status |
|---|-------|------|----------------|--------|
| 12 | Encapsulation | [encapsulation.md](./oops/encapsulation.md) | [LLDMaster.OOP/1.Encapsulation/](../../LLDMaster.OOP/1.Encapsulation/) | ✅ |
| 13 | Inheritance | [inheritance.md](./oops/inheritance.md) | [LLDMaster.OOP/3.Inheritance/](../../LLDMaster.OOP/3.Inheritance/) | ✅ |
| 14 | Polymorphism | [polymorphism.md](./oops/polymorphism.md) | [LLDMaster.OOP/4.Polymorphism/](../../LLDMaster.OOP/4.Polymorphism/) | ✅ |
| 15 | Abstraction | [abstraction.md](./oops/abstraction.md) | [LLDMaster.OOP/2.Abstraction/](../../LLDMaster.OOP/2.Abstraction/) | ✅ |
| — | Method Hiding vs Overriding (`new` vs `override`) | covered in [polymorphism.md](./oops/polymorphism.md) | — | ✅ |
| 16 | Sealed Classes & Sealed Methods | [sealed-classes.md](./oops/sealed-classes.md) | — | ⬜ |
| 17 | Constructor Chaining (`this`, `base`) | [constructors.md](./oops/constructors.md) | — | ⬜ |

---

## 02-intermediate

> Collections, LINQ, delegates/events, generics, iterators — core interview territory.

| # | Topic | File | Status |
|---|-------|------|--------|
| **Generics** | | | |
| 18 | Generics & Constraints (covariance, contravariance) | [generics.md](./02-intermediate/generics.md) | ✅ |
| **Delegates & Events** | | | |
| 19 | Delegates — Func, Action, Predicate, Multicast | [delegates.md](./02-intermediate/delegates.md) | ✅ |
| 20 | Events vs Delegates | [events.md](./02-intermediate/events.md) | ✅ |
| **LINQ** | | | |
| 21 | Deferred Execution, IQueryable vs IEnumerable | [linq.md](./02-intermediate/linq.md) | ✅ |
| — | Projection, Filtering, Grouping (SelectMany, GroupBy) | covered in [linq.md](./02-intermediate/linq.md) | ✅ |
| 22 | Expression Trees (basic concept) | [expression-trees.md](./02-intermediate/expression-trees.md) | ⬜ |
| **Other** | | | |
| 23 | Extension Methods | [extension-methods.md](./02-intermediate/extension-methods.md) | ✅ |
| 24 | Iterators & `yield return` | [iterators.md](./02-intermediate/iterators.md) | ✅ |
| 25 | Indexers | [indexers.md](./02-intermediate/indexers.md) | ⬜ |
| 26 | Exception Handling Internals | [exception-handling.md](./02-intermediate/exception-handling.md) | ⬜ |

---

## 03-advanced

> High-weight interview topics: async, threading, memory, and modern C# features.

| # | Topic | File | Status |
|---|-------|------|--------|
| **Async & Multithreading** | | | |
| 27 | async / await & Task Internals (state machine, ValueTask) | [async-await.md](./03-advanced/async-await.md) | ✅ |
| — | ConfigureAwait & CPU-bound vs I/O-bound | covered in [async-await.md](./03-advanced/async-await.md) | ✅ |
| 28 | CancellationToken | [cancellation-token.md](./03-advanced/cancellation-token.md) | ⬜ |
| 29 | Task vs Thread vs ThreadPool | [task-vs-thread.md](./03-advanced/task-vs-thread.md) | ⬜ |
| 30 | Synchronization — lock, Monitor, SemaphoreSlim, Mutex | [synchronization.md](./03-advanced/synchronization.md) | ⬜ |
| 31 | Deadlocks — how they happen & prevention | [deadlocks.md](./03-advanced/deadlocks.md) | ⬜ |
| 32 | Parallel.For, Parallel.ForEach, PLINQ | [parallel-programming.md](./03-advanced/parallel-programming.md) | ⬜ |
| **Memory** | | | |
| 33 | Memory Management, IDisposable & `using` | [memory-management.md](./03-advanced/memory-management.md) | ✅ |
| — | Finalizer vs Dispose — managed vs unmanaged | covered in [memory-management.md](./03-advanced/memory-management.md) | ✅ |
| 34 | Span\<T\> & Memory\<T\> | [span-memory.md](./03-advanced/span-memory.md) | ✅ |
| **Advanced C# Features** | | | |
| 35 | Pattern Matching — switch expressions, `is`, `when` | [pattern-matching.md](./03-advanced/pattern-matching.md) | ✅ |
| 36 | Records vs Classes & Immutability | [records-structs.md](./03-advanced/records-structs.md) | ✅ |
| 37 | Reflection & Attributes | [reflection.md](./03-advanced/reflection.md) | ✅ |
| 38 | Generics — Covariance & Contravariance (deep dive) | [variance.md](./03-advanced/variance.md) | ⬜ |
| 39 | `dynamic` keyword | [dynamic-keyword.md](./03-advanced/dynamic-keyword.md) | ⬜ |

---

## 04-expert

> CLR/GC internals, zero-allocation techniques, parallel library deep dives.

| # | Topic | File | Status |
|---|-------|------|--------|
| 40 | Garbage Collection — Generations, Gen 0/1/2, Server GC | [garbage-collection.md](./04-expert/garbage-collection.md) | ✅ |
| — | Large Object Heap (LOH) & Fragmentation | covered in [garbage-collection.md](./04-expert/garbage-collection.md) | ✅ |
| 41 | CLR Internals (IL, JIT pipeline, AssemblyLoadContext) | [clr-internals.md](./04-expert/clr-internals.md) | ✅ |
| 42 | JIT Compilation & Tiered Compilation | [jit-compilation.md](./04-expert/jit-compilation.md) | ✅ |
| 43 | Task Parallel Library (TPL) — Dataflow, Channels | [task-parallel-library.md](./04-expert/task-parallel-library.md) | ✅ |
| 44 | Low-Level Performance Tuning | [performance-tuning.md](./04-expert/performance-tuning.md) | ✅ |
| 45 | Memory Leaks in Managed Code | [memory-leaks.md](./04-expert/memory-leaks.md) | ⬜ |
| 46 | `stackalloc` & unsafe memory | [stackalloc.md](./04-expert/stackalloc.md) | ⬜ |

---

## 05-serialization

> File I/O, JSON, XML — often underestimated in interviews; real-world critical.

| # | Topic | File | Status |
|---|-------|------|--------|
| 47 | File I/O — Stream, File, FileStream, StreamReader | [file-io.md](./05-serialization/file-io.md) | ⬜ |
| 48 | JSON Serialization — System.Text.Json vs Newtonsoft | [json-serialization.md](./05-serialization/json-serialization.md) | ⬜ |
| 49 | XML Basics — XDocument, XmlSerializer | [xml-basics.md](./05-serialization/xml-basics.md) | ⬜ |
| 50 | Serialization Performance & Large File Handling | [serialization-performance.md](./05-serialization/serialization-performance.md) | ⬜ |

---

## 🗺️ Learning Roadmap (Priority Order)

```
FOUNDATION (Topics #1–9)
  #1 Value Types vs Reference Types
  #2 Stack vs Heap (deep dive)
  #3 Boxing / Unboxing
  #4 Nullable Types
  #5 String vs StringBuilder
  #6 Access Modifiers
  #7 Types of Constructors
  #8 Types of Classes
  #9 Collections (List, Dictionary, HashSet)

OOP (Topics #10–17)
  #10 Collection Interfaces (IEnumerable → IList)
  #12 Encapsulation  →  #13 Inheritance
  #14 Polymorphism   →  #15 Abstraction
  #16 Sealed Classes →  #17 Constructor Chaining

INTERMEDIATE (Topics #18–26)
  #18 Generics & Constraints
  #19 Delegates → #20 Events
  #21 LINQ (deferred exec, IQueryable) → #22 Expression Trees
  #23 Extension Methods → #24 Iterators / yield

ADVANCED (Topics #27–39)
  #27 async/await (state machine, ValueTask)
  #28 CancellationToken → #29 Task vs Thread
  #30 Synchronization → #31 Deadlocks → #32 Parallel/PLINQ
  #33 Memory Management / IDisposable
  #34 Span<T> & Memory<T>
  #35 Pattern Matching → #36 Records → #37 Reflection

EXPERT (Topics #40–50)
  #40 Garbage Collection (Gen0/1/2, LOH, STW)
  #41 CLR Internals → #42 JIT / Tiered Compilation
  #43 Task Parallel Library → #44 Performance Tuning
  #47–50 Serialization (File I/O, JSON, XML)
```

**Key interview chains:**
- *Delegates* `#19` → Events `#20` → Closure loop bug → `async void`
- *LINQ* `#21` → Deferred execution → IQueryable → EF N+1
- *async/await* `#27` → State machine → ConfigureAwait → Deadlock `#31` → CancellationToken `#28`
- *GC* `#40` → Generations → LOH → IDisposable `#33` → Finalizer cost → Memory leaks

---

## 🔗 Links to Implementation Code

| Code Project | What's Inside |
|-------------|---------------|
| [`LLDMaster.OOP/`](../../LLDMaster.OOP/) | All 4 OOP pillars |
| [`LLDMaster.SOLID/`](../../LLDMaster.SOLID/) | 5 SOLID principles |
| [`LLDMaster.Patterns/`](../../LLDMaster.Patterns/) | 15 GoF design patterns |

---

**Legend:** ✅ Complete (answers + code examples) · ⬜ Planned

> 📅 Last updated: April 2026 · Commit: `d3e8407` — Added interview Q&A answers across all levels (Beginner / OOP / Intermediate / Advanced / Expert — 23 files, 253 insertions)
