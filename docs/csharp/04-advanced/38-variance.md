# Chapter 38 — Covariance & Contravariance

> **⚡ Core Idea (30 seconds):** Variance is about whether a generic type `G<Derived>` can be used where `G<Base>` is expected. **Covariance** (`out T`) allows `IEnumerable<Dog>` as `IEnumerable<Animal>` — safe because you only READ. **Contravariance** (`in T`) allows `Action<Animal>` as `Action<Dog>` — safe because you only WRITE.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#variance` `#covariance` `#contravariance` `#generics`

---

## 1. Core Idea

Think of variance like **adapters**:
- **Covariance (out):** A basket of apples (`IEnumerable<Apple>`) can serve as a basket of fruit (`IEnumerable<Fruit>`) — you're only taking fruit OUT, so apples are fine.
- **Contravariance (in):** A fruit peeler (`Action<Fruit>`) can serve as an apple peeler (`Action<Apple>`) — it takes apples IN, and since it can handle any fruit, apples are fine.

---

## 2. Deep Explanation

### The Problem Without Variance

```csharp
// Without variance, this would NOT compile:
IEnumerable<Dog> dogs = GetDogs();
IEnumerable<Animal> animals = dogs; // ❌ Would fail without 'out' on T

// Because: what if IEnumerable had an Add method?
// animals.Add(new Cat()); // Adding a Cat to a list of Dogs — type violation!
```

### Covariance — `out T`

`out T` means T only appears as a **return type** (output position). Since you only ever receive T from the interface, substituting a more derived type is safe.

```csharp
public interface IEnumerable<out T>  // Covariant
{
    T GetNext();        // ✅ T in output position — allowed
    // void Add(T item); // ❌ Would be illegal — T in input position
}

IEnumerable<Dog> dogs = GetDogs();
IEnumerable<Animal> animals = dogs; // ✅ Safe — you only read Animals out
```

**Covariant types in .NET:** `IEnumerable<out T>`, `IReadOnlyList<out T>`, `IReadOnlyCollection<out T>`, `Func<out TResult>`, `Task<out TResult>`

### Contravariance — `in T`

`in T` means T only appears as a **parameter type** (input position). Since you only ever pass T into the interface, substituting a less derived type is safe.

```csharp
public interface IComparer<in T>  // Contravariant
{
    int Compare(T x, T y);  // ✅ T in input position — allowed
    // T GetDefault();       // ❌ Would be illegal — T in output position
}

IComparer<Animal> animalComparer = new AnimalComparer();
IComparer<Dog> dogComparer = animalComparer; // ✅ Safe — if it can compare any Animal, it can compare Dogs
```

**Contravariant types in .NET:** `Action<in T>`, `IComparer<in T>`, `IEqualityComparer<in T>`, `Predicate<in T>`

### Why Only Interfaces and Delegates?

Concrete classes like `List<T>` have BOTH input and output uses of T:
```csharp
public class List<T>
{
    T this[int index] { get; }  // Output (covariant position)
    void Add(T item);            // Input (contravariant position)
}
```
Since T appears in both positions, it cannot be either covariant or contravariant. Only interfaces/delegates that restrict T to one position can be variant.

---

## 3. Code Examples

### Example 1 — Covariant Event Handler System
```csharp
public interface IEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent domainEvent);
}

public class DomainEvent { public DateTime OccurredAt { get; set; } }
public class OrderPlaced : DomainEvent { public Guid OrderId { get; set; } }

// A handler that handles ANY DomainEvent
public class AuditLogger : IEventHandler<DomainEvent>
{
    public Task HandleAsync(DomainEvent e)
    {
        Console.WriteLine($"Event at {e.OccurredAt}");
        return Task.CompletedTask;
    }
}

// Contravariance allows using IEventHandler<DomainEvent> where IEventHandler<OrderPlaced> is expected
IEventHandler<OrderPlaced> handler = new AuditLogger(); // ✅ Contravariant
await handler.HandleAsync(new OrderPlaced { OrderId = Guid.NewGuid() });
```

### Example 2 — Covariant Repository Return
```csharp
public interface IReadRepository<out T> where T : Entity
{
    T GetById(Guid id);
    IReadOnlyList<T> GetAll();
}

public class ProductRepository : IReadRepository<Product> { /* ... */ }

// Covariance: can assign IReadRepository<Product> to IReadRepository<Entity>
IReadRepository<Entity> entityRepo = new ProductRepository(); // ✅
```

---

## 4. Interview Questions

1. **What is covariance and contravariance in C# generics?**
   *Covariance (`out T`) allows a generic type with a derived type parameter to be used where a base type parameter is expected — `IEnumerable<Dog>` as `IEnumerable<Animal>`. Contravariance (`in T`) is the reverse — `Action<Animal>` as `Action<Dog>`. Covariance is safe for output positions, contravariance for input positions.*

2. **Why can `IEnumerable<Dog>` be assigned to `IEnumerable<Animal>` but `List<Dog>` cannot be assigned to `List<Animal>`?**
   *`IEnumerable<out T>` is covariant because T only appears in output positions (you can only read from it). `List<T>` has both `Add(T)` (input) and `T this[i]` (output), making T invariant. If `List<Dog>` were assignable to `List<Animal>`, you could add a `Cat` to a list of `Dog`s — a type violation.*

3. **Can a type parameter be both covariant and contravariant?**
   *No. A type parameter can only be `in` or `out`, not both. If it appears in both input and output positions, it must be invariant (no keyword). This is why concrete classes are always invariant.*

4. **Give a real-world example of contravariance.**
   *`IComparer<in T>`. If you have a comparer that can compare any `Animal` by weight, you can safely use it to compare `Dog`s — because dogs are animals. The comparer takes T as input (`Compare(T x, T y)`), so the `in` keyword makes this substitution legal.*

5. **What happens if you try to use `out T` in an input position?**
   *The compiler refuses to compile. If you declare `interface IFoo<out T> { void Process(T item); }`, you get error CS1961: "Invalid variance: The type parameter 'T' must be contravariantly valid." The compiler enforces the safety guarantee at compile time.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Assuming List<T> is covariant
List<Dog> dogs = new();
// List<Animal> animals = dogs; // ❌ Compile error! List<T> is invariant

// FIX: Use the covariant interface
IEnumerable<Animal> animals = dogs; // ✅ IEnumerable<out T>
IReadOnlyList<Animal> readOnly = dogs; // ✅ IReadOnlyList<out T>

// MISTAKE 2: Trying variance on a class
// public class Box<out T> { } // ❌ Compile error! Only interfaces and delegates

// GOTCHA: Arrays are covariant (but UNSAFELY!)
Animal[] animals = new Dog[5]; // ✅ Compiles
animals[0] = new Cat();        // ❌ Runtime ArrayTypeMismatchException!
// This is a known design flaw from C# 1.0 — interfaces don't have this problem
```

---

## 🔗 Connected Topics

- [Generics](../03-intermediate/18-generics.md) — Variance is an advanced generic feature
- [Delegates](../03-intermediate/19-delegates.md) — `Func<out T>` and `Action<in T>` use variance
- [Collection Interfaces](../01-beginner/09-collection-interfaces.md) — `IReadOnlyList<out T>` is covariant

---

*Created: May 2026 · Level: Advanced*
