# Types of Classes in C#

> **⚡ Core Idea (30 seconds):** C# has several types of classes — each keyword (`abstract`, `sealed`, `static`, `partial`) restricts or enables specific behaviours. Knowing *why* each type exists (not just the syntax) is what separates a senior from a junior in interviews.

**Domain:** `C#` **Level:** `Beginner → Intermediate` **Tags:** `#class` `#abstract` `#sealed` `#static` `#partial`

---

## 1. Core Idea

A `class` is just the default. Every modifier you add to it is a deliberate design restriction:
- `abstract` → "you must specialise me before using me"
- `sealed` → "you cannot specialise me further"
- `static` → "I have no instances, only shared utilities"
- `partial` → "my definition is split across multiple files"

Each one communicates **intent** to the compiler and to other developers.

---

## 2. Deep Explanation

### 1. Concrete Class (Default)
The regular class — can be instantiated directly, can be inherited.

```csharp
public class Order { }          // Instantiate: new Order()
public class PriorityOrder : Order { } // Can be inherited
```

---

### 2. Abstract Class
**Cannot be instantiated directly.** Designed to be a base. Forces subclasses to implement abstract members.

```csharp
public abstract class Shape
{
    public abstract double Area();           // No body — MUST override
    public virtual string Describe() => "I am a shape"; // Can optionally override
}

// var s = new Shape(); ❌ Compile error — cannot instantiate abstract class
var c = new Circle(5); // ✅ Concrete subclass
```

**When to use:** Shared base behaviour + mandatory contract for subclasses (Template Method pattern).

---

### 3. Sealed Class
**Cannot be inherited.** Marks the class as the final implementation in a hierarchy.

```csharp
public sealed class SqlConnection { }
// class MyConnection : SqlConnection { } ❌ Compile error

// On a method — prevents that specific virtual method from being overridden further
public class Animal { public virtual void Speak() { } }
public class Dog : Animal { public sealed override void Speak() { } } // No subclass of Dog can override Speak
```

**Why sealed matters for performance:** The JIT compiler can **devirtualize** calls to sealed types — it knows exactly which method to call at compile time, skipping the vtable lookup. This is a measurable win on hot paths.

**When to use:** Implementation-detail classes that should never be extended (e.g., `string`, `DateTime`).

---

### 4. Static Class
**Cannot be instantiated or inherited.** All members must be `static`. Used for pure utility/helper collections.

```csharp
public static class MathHelper
{
    public static double Square(double x) => x * x;
    public static double CircleArea(double r) => Math.PI * r * r;
}

// MathHelper.Square(5); ✅
// new MathHelper(); ❌ Compile error
// class Ext : MathHelper { } ❌ Cannot inherit
```

**Extension methods must live in a static class:**
```csharp
public static class StringExtensions
{
    public static bool IsEmail(this string s) => s.Contains('@');
}
```

**When to use:** Stateless utilities (validators, converters, extension methods). **Avoid** if the class needs configuration or state — use a service class with DI instead.

---

### 5. Partial Class
**Definition split across multiple files.** The compiler merges them into one type at compile time.

```csharp
// File: Order.cs
public partial class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
}

// File: Order.Validation.cs
public partial class Order
{
    public bool IsValid() => Id > 0 && !string.IsNullOrEmpty(CustomerName);
}

// Compiles as if it were one class
var o = new Order { Id = 1, CustomerName = "Alice" };
o.IsValid(); // ✅
```

**Primary use cases:**
- Auto-generated code (EF Core model, WinForms designer) lives in one file; your hand-written code in another
- Large classes split by concern for readability

---

### 6. Generic Class
Parameterised by type — becomes a blueprint for multiple typed variants.

```csharp
public class Repository<T> where T : class
{
    private readonly List<T> _store = new();
    public void Add(T item) => _store.Add(item);
    public T? GetById(int index) => _store.ElementAtOrDefault(index);
}

var userRepo = new Repository<User>();
var orderRepo = new Repository<Order>();
```

---

### 7. Nested Class
A class defined inside another class. Can access private members of the outer class.

```csharp
public class Outer
{
    private int _secret = 42;

    public class PublicNested { }          // Accessible from outside as Outer.PublicNested
    private class PrivateNested            // Only Outer can use this
    {
        public void ShowSecret(Outer o) => Console.WriteLine(o._secret); // Can access private!
    }
}
```

**When to use:** Implementation details that are tightly coupled to the outer class (e.g., enumerator classes, builder sub-types).

---

## 3. Quick Comparison Table

| Type | Instantiate? | Inherit? | Members | Primary Purpose |
|------|:---:|:---:|---------|----------------|
| Concrete | ✅ | ✅ | Mix | General-purpose |
| `abstract` | ❌ | ✅ | Mix (can have abstract) | Base type with enforced contract |
| `sealed` | ✅ | ❌ | All concrete | Final, no extension allowed |
| `static` | ❌ | ❌ | All `static` | Stateless utilities |
| `partial` | ✅ | ✅ | Mix | Split definition across files |
| Generic | ✅ | ✅ | Mix | Type-parameterised reuse |
| Nested | ✅/❌ | ✅/❌ | Mix | Tightly coupled inner type |

---

## 4. Interview Questions

1. **What is the difference between an `abstract` class and a `sealed` class?**
2. **Can a `static` class implement an interface?**
   *(No — it cannot be instantiated, so implementing an interface makes no sense.)*
3. **Can a `sealed` class implement an interface or inherit from a class?**
   *(Yes — `sealed` only prevents others from inheriting from it, not from it inheriting upward.)*
4. **What are partial classes primarily used for in real projects?**
5. **Why does `sealed` help JIT performance?**

---

## 5. Follow-up Questions

- What happens if you mark a class as both `abstract` and `sealed`?
  *(Compile error — they're contradictory. `abstract` requires subclassing; `sealed` prevents it.)*
- Can an `abstract` class have a constructor?
  *(Yes — it's called by derived class constructors via `base()`. The abstract class itself cannot be `new`'d but it can initialise its own fields.)*
- Can `static` and `partial` be combined?
  *(Yes — `public static partial class MathHelper { }` is valid. Common in source generators.)*
- Can you override a `sealed` method in a subclass of the sealing class?
  *(No — that's the entire point of `sealed` on a method: no further overriding in any derived type.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using static class when you need configuration → can't inject
public static class EmailSender
{
    private static string _smtpHost = "localhost"; // Hard-coded — untestable
    public static void Send(string to, string body) { }
}
// FIX: Use a regular class with IEmailSender interface for DI

// MISTAKE 2: Making every utility class static — kills testability
// Static classes can't be mocked. Prefer instance classes with interfaces.

// MISTAKE 3: Forgetting sealed on records — allows equality confusion
public record Point(int X, int Y);
public record Point3D(int X, int Y, int Z) : Point(X, Y); // Inherits!
// Point p = new Point3D(1,2,3); p == new Point(1,2) → surprising result
// FIX: public sealed record Point(...) — if extension is not intended

// MISTAKE 4: Partial class across different assemblies
// Partial classes MUST be in the same assembly and namespace
```

---

## 7. Real-World Usage

| Class Type | Where You See It |
|-----------|-----------------|
| `abstract` | `ControllerBase` in ASP.NET Core; `Stream`; Template Method base classes |
| `sealed` | `string`, `DateTime`, `StringBuilder`; internal implementation classes |
| `static` | `File`, `Path`, `Math`, `Enumerable`; all extension method hosts |
| `partial` | EF Core scaffolded models; WinForms designer files; source-generated code |
| Generic | `List<T>`, `Repository<T>`, `Result<T>` |
| Nested | `IEnumerator` implementations; Builder sub-types; DDD inner value objects |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Concrete, abstract, sealed, static — definitions and syntax |
| **Level 2** | When to use each, partial class use cases |
| **Level 3** | sealed + JIT devirtualization, static vs instance service class design |
| **Level 4** | Generic class constraints, nested classes accessing outer private state |

## 🔗 Connected Topics
- [Abstraction](../oops/abstraction.md) — `abstract` class is the mechanism of abstraction
- [Inheritance](../oops/inheritance.md) — `sealed` controls the inheritance chain
- [Access Modifiers](./access-modifiers.md) — controls member visibility within class types
- [Generics](../02-intermediate/generics.md) — generic class deep dive

*Created: April 2026 · Level: Beginner → Intermediate*
