# Chapter 18 � Generics & Constraints

> **⚡ Core Idea (30 seconds):** Generics let you write **type-safe, reusable code without boxing**. Instead of writing a `Stack` for every type, you write `Stack<T>` once and the compiler generates the correct version at compile time. Constraints tell the compiler what `T` is allowed to be.

**Domain:** `C#` **Level:** `Intermediate` **Tags:** `#generics` `#constraints` `#covariance` `#contravariance`

---

## 1. Core Idea

Before generics, you had `ArrayList` — it stored everything as `object`, required casting, and boxed value types. Generics replaced all of that. `List<int>` stores real `int`s, no boxing, no casting, full compile-time type safety.

Think of `T` as a **blank** that gets filled in when someone uses your class: `List<string>` fills in `string`, `List<Order>` fills in `Order`.

---

## 2. Deep Explanation

### How Generics Are Handled by the CLR

For **reference type** arguments (`List<string>`, `List<Order>`): The JIT generates **one** shared implementation. All reference types share the same native code (pointers are all the same size).

For **value type** arguments (`List<int>`, `List<double>`): The JIT generates a **separate** native code version per type. `List<int>` and `List<double>` have different machine code — this is what gives zero-boxing overhead.

### Generic Constraints

Constraints restrict what `T` can be, enabling you to call methods on it:

```csharp
// No constraint: T is unknown, can only call object methods on it
public T Process<T>(T input) => input; // Can't call input.Name, input.Start(), etc.

// With constraints:
public T Clone<T>(T item) where T : class, new()         // Must be class + have parameterless ctor
public T Max<T>(T a, T b) where T : IComparable<T>       // Can call .CompareTo()
public void Print<T>(T item) where T : struct             // Value types only
public void Save<T>(T entity) where T : IEntity, new()   // Interface + new()
```

| Constraint | Meaning |
|-----------|---------|
| `where T : class` | Reference type |
| `where T : struct` | Value type (non-nullable) |
| `where T : new()` | Has parameterless constructor |
| `where T : BaseClass` | Must inherit from BaseClass |
| `where T : IInterface` | Must implement interface |
| `where T : notnull` | Non-nullable (C# 8+) |
| `where T : unmanaged` | Unmanaged type (for unsafe/Span) |

### Covariance & Contravariance (with `out`/`in`)

```csharp
// Covariance (out): IEnumerable<Dog> is assignable to IEnumerable<Animal>
IEnumerable<Dog> dogs = new List<Dog>();
IEnumerable<Animal> animals = dogs; // Works! IEnumerable<out T>

// Contravariance (in): Action<Animal> is assignable to Action<Dog>
Action<Animal> feedAnimal = a => Console.WriteLine($"Feeding {a.Name}");
Action<Dog> feedDog = feedAnimal; // Works! Action<in T>
```

---

## 3. Code Examples

### Basic — Generic Repository
```csharp
// Without generics: must write per-type
public class UserRepository { public User GetById(int id) { } }
public class OrderRepository { public Order GetById(int id) { } }

// With generics: one implementation
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Remove(T entity);
}

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;
    public EfRepository(DbContext context) => _context = context;

    public T? GetById(int id) => _context.Set<T>().Find(id);
    public IEnumerable<T> GetAll() => _context.Set<T>().ToList();
    public void Add(T entity) => _context.Set<T>().Add(entity);
    public void Remove(T entity) => _context.Set<T>().Remove(entity);
}
```

### Real-World — Generic Result Type (no exceptions for flow control)
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error) { IsSuccess = false; Error = error; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(string error) => new(error);

    // Functor map — transform value if success
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Ok(mapper(Value!)) : Result<TOut>.Fail(Error!);
}

// Usage
public Result<Order> CreateOrder(CreateOrderRequest request)
{
    if (request.Items.Count == 0)
        return Result<Order>.Fail("Order must have at least one item");

    var order = new Order(request);
    return Result<Order>.Ok(order);
}

var result = CreateOrder(request);
if (result.IsSuccess)
    await _repo.SaveAsync(result.Value!);
```

---

## 4. Interview Questions

1. **What problem do generics solve? Why are they better than using `object`?**
   *Without generics, you'd use `object` — losing type safety (runtime `InvalidCastException` instead of compile-time errors), boxing every value type (heap allocations), and requiring manual casting everywhere. Generics give you **compile-time type safety** (the compiler rejects the wrong type), **zero boxing for value types** (the JIT generates specialised code), and **self-documenting code** (`IRepository<User>` tells you this deals with `User` objects).*

2. **What is a generic constraint? Give three examples.**
   *Constraints restrict what types can be used as `T`, enabling the compiler to know what operations are safe to call. Examples: (1) `where T : class` — T must be a reference type, (2) `where T : IComparable<T>` — T must implement that interface, allowing `.CompareTo()`, (3) `where T : new()` — T must have a parameterless constructor, allowing `new T()` inside the method.*

3. **What is the difference between covariance and contravariance?**
   *Covariance (`out T`): you can use a more-derived type. `IEnumerable<Dog>` can be assigned to `IEnumerable<Animal>` because you only read from it — widening is safe. Contravariance (`in T`): you can use a more-general type. `Action<Animal>` can be assigned to `Action<Dog>` because if it handles any Animal, it certainly handles a Dog — narrowing the input is safe.*

4. **How does the CLR handle generics for value types vs reference types?**
   *For **reference types** (like `List<string>` and `List<Order>`): the JIT generates **one shared** native implementation because all reference type pointers are the same size. For **value types** (like `List<int>` and `List<double>`): the JIT generates a **separate** native implementation per concrete type — this is what enables zero-boxing for value types in generic collections.*

5. **Can you instantiate a generic type directly: `new T()`? What constraint is needed?**
   *Yes, but only with the `where T : new()` constraint. Without it, the compiler doesn't know if `T` has a parameterless constructor and gives a compile error. With the constraint: `public T Create<T>() where T : new() => new T();` works. Note: `new()` must be the last constraint in the `where` clause.*

---

## 5. Follow-up Questions

- Why does `List<Dog>` not implement `IList<Animal>` even if `Dog : Animal`?
  *(Because `IList<T>` is invariant — you could `Add(new Cat())` which breaks type safety)*
- `IEnumerable<T>` is covariant — what does that mean and why is it safe?
  *(You can only read from it, never write, so widening to a parent type is safe)*
- Can a non-generic class have generic methods?
  ```csharp
  public class Converter { public T Convert<T>(string input) { ... } } // Yes!
  ```
- What is `default(T)` and when would you use it?
  *(Returns null for reference types, 0/false/empty for value types — useful when T is unknown)*
- What is `typeof(T)` vs `T.GetType()` at runtime?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using object instead of generics — loses type safety and boxes
public object Process(object input) { return input; } // ❌
public T Process<T>(T input) { return input; }         // ✅

// MISTAKE 2: Forgetting constraints and getting compile error
public T CreateNew<T>() => new T(); // ❌ T might not have parameterless ctor
public T CreateNew<T>() where T : new() => new T(); // ✅

// MISTAKE 3: Variance confusion — trying to assign List<Dog> to List<Animal>
List<Dog> dogs = new();
List<Animal> animals = dogs; // ❌ Compile error — List<T> is invariant (mutable)
IEnumerable<Animal> readable = dogs; // ✅ IEnumerable<T> is covariant (read-only)

// MISTAKE 4: Generic cache — forgetting each T gets its own static
public static class PerTypeCache<T>
{
    public static readonly Dictionary<int, T> Store = new();
}
// PerTypeCache<User>.Store and PerTypeCache<Order>.Store are DIFFERENT dictionaries
```

---

## 7. Real-World Usage

| Scenario | Pattern |
|----------|---------|
| Repository pattern | `IRepository<T>` with EF Core `DbSet<T>` |
| Result/Option type | `Result<T>`, `Maybe<T>` functional patterns |
| Generic validators | `IValidator<T>` (FluentValidation) |
| Event handlers | `INotificationHandler<TEvent>` (MediatR) |
| Factory | `IFactory<T>` where `T : IEntity, new()` |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Generic class/method syntax, type safety benefit |
| **Level 2** | Constraints, default(T), generic interfaces |
| **Level 3** | Covariance/Contravariance (out/in), CLR code generation per value type |
| **Level 4** | Static abstract members in interfaces (C# 11), unmanaged constraint, generic math |

## 🔗 Connected Topics
- [Collections](../01-beginner/08-collections-overview.md) — All built-in collections are generic
- [LINQ](./21-linq.md) — `IEnumerable<T>`, `IQueryable<T>` are generic covariant interfaces
- [Delegates](./19-delegates.md) — `Func<T,TResult>` is a generic delegate

*Created: April 2026 · Level: Intermediate*
