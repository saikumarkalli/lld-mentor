# C# Language — Knowledge Hub

> Deep mastery of the C# language for backend engineers targeting product-based company interviews.

---

## 📁 Folder Structure

```
docs/csharp/
├── 01-beginner/      ← Value types, boxing, nullables, strings, access modifiers
├── oops/             ← OOP pillars: constructors, sealing, hiding (→ LLDMaster.OOP)
├── 02-intermediate/  ← Collections, LINQ, Delegates, Events, Iterators, Generics
├── 03-advanced/      ← async/await, Threading, Memory, Span<T>, Records, Dynamic
├── 04-expert/        ← GC/LOH, CLR, JIT, TPL, Stackalloc, Memory leaks
└── 05-serialization/ ← File I/O, JSON, XML, large file handling
```

---

## 01-beginner

> Stack vs heap, types, boxing, keywords — the foundation everything else builds on.

| Topic | File | Status |
|-------|------|--------|
| Value Types vs Reference Types (Stack vs Heap) | [value-vs-reference-types.md](./01-beginner/value-vs-reference-types.md) | ✅ |
| Boxing & Unboxing | covered in [value-vs-reference-types.md](./01-beginner/value-vs-reference-types.md) | ✅ |
| Default Values & Nullable Types | [nullable-types.md](./01-beginner/nullable-types.md) | ✅ |
| String vs StringBuilder | [string-vs-stringbuilder.md](./01-beginner/string-vs-stringbuilder.md) | ✅ |
| Access Modifiers (internal, protected internal) | [access-modifiers.md](./01-beginner/access-modifiers.md) | ⬜ |
| Collections — List, Dictionary, HashSet | [collections-overview.md](./01-beginner/collections-overview.md) | ⬜ |
| IEnumerable vs ICollection vs IList vs IReadOnlyCollection | [collection-interfaces.md](./01-beginner/collection-interfaces.md) | ⬜ |
| Control Flow Basics | [control-flow.md](./01-beginner/control-flow.md) | ⬜ |

---

## oops/ — OOP Pillars

> These docs explain the **why** and **internals** for each OOP concept. No code here — actual C# code lives in [`LLDMaster.OOP/`](../../LLDMaster.OOP/).

| Topic | File | Code Reference | Status |
|-------|------|----------------|--------|
| Encapsulation | [encapsulation.md](./oops/encapsulation.md) | [LLDMaster.OOP/1.Encapsulation/](../../LLDMaster.OOP/1.Encapsulation/) | ✅ |
| Abstraction | [abstraction.md](./oops/abstraction.md) | [LLDMaster.OOP/2.Abstraction/](../../LLDMaster.OOP/2.Abstraction/) | ⬜ |
| Inheritance | [inheritance.md](./oops/inheritance.md) | [LLDMaster.OOP/3.Inheritance/](../../LLDMaster.OOP/3.Inheritance/) | ⬜ |
| Polymorphism | [polymorphism.md](./oops/polymorphism.md) | [LLDMaster.OOP/4.Polymorphism/](../../LLDMaster.OOP/4.Polymorphism/) | ✅ |
| Method Hiding vs Overriding (`new` vs `override`) | covered in [polymorphism.md](./oops/polymorphism.md) | [LLDMaster.OOP/4.Polymorphism/](../../LLDMaster.OOP/4.Polymorphism/) | ✅ |
| Sealed Classes & Sealed Methods | [sealed-classes.md](./oops/sealed-classes.md) | — | ⬜ |
| Constructor Chaining (`this`, `base`) | [constructors.md](./oops/constructors.md) | — | ⬜ |

---

## 02-intermediate

> Collections, LINQ, delegates/events, generics, iterators — core interview territory.

| Topic | File | Status |
|-------|------|--------|
| **Collections** | | |
| List\<T\>, Dictionary\<K,V\>, HashSet\<T\> | [collections-overview.md](./01-beginner/collections-overview.md) | ⬜ |
| IEnumerable vs ICollection vs IList vs IReadOnlyCollection | [collection-interfaces.md](./01-beginner/collection-interfaces.md) | ⬜ |
| **LINQ** | | |
| Deferred Execution, IQueryable vs IEnumerable | [linq.md](./02-intermediate/linq.md) | ✅ |
| Projection, Filtering, Grouping (SelectMany, GroupBy) | covered in [linq.md](./02-intermediate/linq.md) | ✅ |
| Expression Trees (basic concept) | [expression-trees.md](./02-intermediate/expression-trees.md) | ⬜ |
| **Delegates & Events** | | |
| Delegates — Func, Action, Predicate, Multicast | [delegates.md](./02-intermediate/delegates.md) | ✅ |
| Events vs Delegates | [events.md](./02-intermediate/events.md) | ⬜ |
| **Generics** | | |
| Generics & Constraints | [generics.md](./02-intermediate/generics.md) | ⬜ |
| **Other** | | |
| Extension Methods | [extension-methods.md](./02-intermediate/extension-methods.md) | ⬜ |
| Iterators & `yield return` | [iterators.md](./02-intermediate/iterators.md) | ⬜ |
| Indexers | [indexers.md](./02-intermediate/indexers.md) | ⬜ |
| Exception Handling Internals | [exception-handling.md](./02-intermediate/exception-handling.md) | ⬜ |

---

## 03-advanced

> High-weight interview topics: async, threading, memory, and modern C# features.

| Topic | File | Status |
|-------|------|--------|
| **Async & Multithreading** | | |
| async / await & Task Internals (state machine, ValueTask) | [async-await.md](./03-advanced/async-await.md) | ✅ |
| ConfigureAwait & CPU-bound vs I/O-bound | covered in [async-await.md](./03-advanced/async-await.md) | ✅ |
| CancellationToken | [cancellation-token.md](./03-advanced/cancellation-token.md) | ⬜ |
| Task vs Thread vs ThreadPool | [task-vs-thread.md](./03-advanced/task-vs-thread.md) | ⬜ |
| Synchronization — lock, Monitor, SemaphoreSlim, Mutex | [synchronization.md](./03-advanced/synchronization.md) | ⬜ |
| Deadlocks — how they happen & prevention | [deadlocks.md](./03-advanced/deadlocks.md) | ⬜ |
| Parallel.For, Parallel.ForEach, PLINQ | [parallel-programming.md](./03-advanced/parallel-programming.md) | ⬜ |
| **Memory** | | |
| Memory Management, IDisposable & `using` | [memory-management.md](./03-advanced/memory-management.md) | ⬜ |
| Finalizer vs Dispose — managed vs unmanaged | covered in [memory-management.md](./03-advanced/memory-management.md) | ⬜ |
| Span\<T\> & Memory\<T\> | [span-memory.md](./03-advanced/span-memory.md) | ⬜ |
| **Advanced C# Features** | | |
| Generics — Covariance & Contravariance | [variance.md](./03-advanced/variance.md) | ⬜ |
| Reflection & Attributes | [reflection.md](./03-advanced/reflection.md) | ⬜ |
| Records vs Classes & Immutability | [records-structs.md](./03-advanced/records-structs.md) | ⬜ |
| Pattern Matching — switch expressions, `is`, `when` | [pattern-matching.md](./03-advanced/pattern-matching.md) | ⬜ |
| `dynamic` keyword | [dynamic-keyword.md](./03-advanced/dynamic-keyword.md) | ⬜ |

---

## 04-expert

> CLR/GC internals, zero-allocation techniques, parallel library deep dives.

| Topic | File | Status |
|-------|------|--------|
| Garbage Collection — Generations, Gen 0/1/2, Server GC | [garbage-collection.md](./04-expert/garbage-collection.md) | ✅ |
| Large Object Heap (LOH) & Fragmentation | covered in [garbage-collection.md](./04-expert/garbage-collection.md) | ✅ |
| Memory Leaks in Managed Code | [memory-leaks.md](./04-expert/memory-leaks.md) | ⬜ |
| `stackalloc` & unsafe memory | [stackalloc.md](./04-expert/stackalloc.md) | ⬜ |
| CLR Internals | [clr-internals.md](./04-expert/clr-internals.md) | ⬜ |
| JIT Compilation & Tiered Compilation | [jit-compilation.md](./04-expert/jit-compilation.md) | ⬜ |
| Task Parallel Library (TPL) — Dataflow, Channels | [task-parallel-library.md](./04-expert/task-parallel-library.md) | ⬜ |
| Low-Level Performance Tuning | [performance-tuning.md](./04-expert/performance-tuning.md) | ⬜ |

---

## 05-serialization

> File I/O, JSON, XML — often underestimated in interviews; real-world critical.

| Topic | File | Status |
|-------|------|--------|
| File I/O — Stream, File, FileStream, StreamReader | [file-io.md](./05-serialization/file-io.md) | ⬜ |
| JSON Serialization — System.Text.Json vs Newtonsoft | [json-serialization.md](./05-serialization/json-serialization.md) | ⬜ |
| XML Basics — XDocument, XmlSerializer | [xml-basics.md](./05-serialization/xml-basics.md) | ⬜ |
| Serialization Performance & Large File Handling | [serialization-performance.md](./05-serialization/serialization-performance.md) | ⬜ |

---

## 🔗 Topic Connection Map

```
Value Types ──→ Boxing ──→ Collections ──→ IEnumerable/ICollection
     │                            │               │
     └──→ Nullable Types          │           Generics ──→ Covariance
                                  │
Delegates ──→ Events ──→ LINQ (Deferred) ──→ Expression Trees
    │                        │
    └──→ Func/Action ─→ async/await ──→ Task vs Thread
                                │
                    CancellationToken ──→ Deadlocks ──→ Synchronization
                                │
                        Parallel.For / PLINQ / TPL

Memory: IDisposable ──→ Finalizer ──→ GC (Gen0/1/2/LOH)
    Span<T> ──→ stackalloc ──→ ArrayPool ──→ Zero-alloc patterns

OOP: Encapsulation → Inheritance → Polymorphism → Method Hiding/Sealing
    └──→ Constructors → Constructor Chaining

Serialization: File I/O → Stream → JSON (STJ/Newtonsoft) → XML
```

**Interview chains to master:**
- *Delegates* → Events → Multicast → Closure loop bug → `async void`
- *LINQ* → Deferred execution → Multiple enumeration → IQueryable → EF N+1
- *async/await* → State machine → ConfigureAwait → Deadlock → CancellationToken
- *GC* → Generations → LOH → IDisposable → Finalizer cost → Memory leaks

---

## 🔗 Links to Implementation Code

| Code Project | What's Inside |
|-------------|---------------|
| [`LLDMaster.OOP/`](../../LLDMaster.OOP/) | All 4 OOP pillars |
| [`LLDMaster.SOLID/`](../../LLDMaster.SOLID/) | 5 SOLID principles |
| [`LLDMaster.Patterns/`](../../LLDMaster.Patterns/) | 15 GoF design patterns |

**Legend:** ✅ Complete · ⬜ Planned
