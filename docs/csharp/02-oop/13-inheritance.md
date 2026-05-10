# Chapter 13 � Inheritance

> **⚡ Core Idea (30 seconds):** Inheritance lets a class **reuse and extend** the behavior of a parent class. In C#, a class can inherit from only one base class. But inheritance is often overused — when in doubt, prefer **composition over inheritance**.

**Domain:** `C#` · **Level:** `Beginner → Intermediate` · **Tags:** `#oop` `#inheritance` `#base` `#composition`


---

## 1. Core Idea

Inheritance models an **"is-a" relationship**: a `Dog` IS an `Animal`. The child class gets everything from the parent and can add or override behavior.

But inheritance creates **tight coupling**. If `Animal` changes, `Dog` is affected. That's why the rule is: inherit when the "is-a" relationship is **genuinely permanent and strong** — otherwise use interfaces or composition.

---

## 2. Deep Explanation

### The C# Single Inheritance Model

C# allows **single class inheritance** but **multiple interface implementation**:

```csharp
// ONE base class, MANY interfaces
public class ElectricCar : Car, IDriveable, IChargeable, IConnected { }
```

### Constructor Chaining with `base`

When a derived class is constructed, the **base constructor runs first**:

```csharp
public class Animal
{
    protected string Name;
    public Animal(string name) { Name = name; }
}

public class Dog : Animal
{
    private string Breed;
    public Dog(string name, string breed) : base(name) // Calls Animal(name) first
    {
        Breed = breed;
    }
}
```

If you don't explicitly call `base()`, the compiler tries to call the **parameterless constructor** of the base. If there isn't one → **compile error**.

### `virtual` / `override` / `sealed`

```csharp
public class Shape
{
    public virtual double Area() => 0;           // Can be overridden
    public virtual string Describe() => "Shape"; // Can be overridden
}

public class Circle : Shape
{
    private double _r;
    public override double Area() => Math.PI * _r * _r;   // Overrides
    public sealed override string Describe() => "Circle";  // Sealed: no further override
}

// sealed class: nothing can inherit from it
public sealed class SqlConnection { } // System.Data example — implementation detail
```

### Composition vs Inheritance

| Inheritance | Composition |
|-------------|-------------|
| "Is-a" relationship | "Has-a" relationship |
| Tight coupling to base class | Loose coupling via interface |
| Cannot change base at runtime | Can swap implementations at runtime |
| Breaks if base class changes | Base changes don't affect composers |

```csharp
// Inheritance approach — Dog IS an Animal
public class Dog : Animal { }

// Composition approach — Logger HAS a formatter (preferred for non-"is-a")
public class Logger
{
    private readonly IFormatter _formatter; // Composed in, swappable
    public Logger(IFormatter formatter) => _formatter = formatter;
}
```

---

## 3. Code Examples

### Basic — Inheritance Hierarchy
```csharp
public abstract class Employee
{
    public string Name { get; init; }
    public decimal BaseSalary { get; protected set; }

    public Employee(string name, decimal baseSalary)
    {
        Name = name;
        BaseSalary = baseSalary;
    }

    public abstract decimal CalculateBonus(); // Each type has its own bonus logic

    public virtual string GetSummary() => $"{Name}: Base={BaseSalary:C}";
}

public class SalesEmployee : Employee
{
    private decimal _salesAmount;

    public SalesEmployee(string name, decimal baseSalary, decimal salesAmount)
        : base(name, baseSalary) // Chain to parent
    {
        _salesAmount = salesAmount;
    }

    public override decimal CalculateBonus() => _salesAmount * 0.05m; // 5% commission

    public override string GetSummary()
        => base.GetSummary() + $", Bonus={CalculateBonus():C}"; // Extend parent
}

public class Manager : Employee
{
    private int _teamSize;
    public Manager(string name, decimal base_, int teamSize) : base(name, base_)
        => _teamSize = teamSize;

    public override decimal CalculateBonus() => BaseSalary * 0.20m; // 20% flat
}
```

### Real-World — The Fragile Base Class Problem
```csharp
// Base class in a library (v1)
public class Collection<T>
{
    private int _count;
    public virtual void Add(T item) { _count++; /* ... */ }
    public virtual void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items) Add(item); // Delegates to Add
    }
}

// Derived class — tracks adds
public class TrackedCollection<T> : Collection<T>
{
    private int _addCount;
    public override void Add(T item) { _addCount++; base.Add(item); }
    // AddRange calls Add() — so _addCount is correct. Good.

    // BUT: if base class changes AddRange to not call Add() (an internal optimization)
    // _addCount will stop counting AddRange items — broken by base class change!
    // This is the "Fragile Base Class Problem"
}
// Lesson: Composition avoids this entirely
```

---

## 4. Interview Questions

1. **What is the difference between inheritance and composition? When would you choose each?**
   *Inheritance = "is-a" — a `Dog` IS an `Animal`, so `Dog` extends `Animal`. Composition = "has-a" — a `Car` HAS an `Engine`. Prefer inheritance when the relationship is genuinely permanent and strong, and there's shared behaviour. Prefer composition when you want looser coupling, runtime swappability, or when the relationship is not truly "is-a" — inheritance just for code reuse is a red flag.*

2. **Can a C# class inherit from multiple classes? Why not?**
   *No — C# only allows single class inheritance. This avoids the **diamond problem**: if class D inherits from B and C, both of which inherit from A, and all override the same method, it's ambiguous which version D gets. C# solves this by allowing only one base class per type. Multiple interface implementation is allowed because interfaces don't carry conflicting implementations (pre-C# 8).*

3. **What happens if a base class doesn't have a parameterless constructor?**
   *If the base class only has parameterised constructors and the derived class doesn't explicitly call `base(...)`, you get a **compile error** — the compiler can't auto-generate a valid call. You must always explicitly chain to a matching base constructor with `: base(args)`.*

4. **What does `sealed` do when applied to a class vs a method?**
   *On a class: prevents any class from inheriting from it — it's the final implementation. On a method: prevents further overriding of that specific virtual method in any subclass. A sealed method must itself be an `override`. It lets you stop the override chain at a specific level without sealing the whole class.*

5. **What is the Fragile Base Class problem?**
   *When a base class changes its internal implementation (e.g., refactoring `AddRange` to stop calling `Add`), derived classes that relied on that internal behaviour silently break — even though no public API changed. It's "fragile" because the base class holds invisible assumptions that derived classes depend on. The fix: prefer composition, or design base classes for extension with clear documented extension points.*

---

## 5. Follow-up Questions

- What is `base.Method()` vs calling `this.Method()` inside an override?
  *(base.Method() explicitly calls the parent version. this.Method() calls the most-derived version → polymorphic dispatch)*
- If a base class method is `virtual` and a derived class method uses `new` instead of `override`, what prints?
  ```csharp
  Animal a = new Dog();
  a.Speak(); // Prints Animal's version! 'new' hides, doesn't override
  ```
- Explain why `sealed` on a method allows JIT optimizations.
  *(JIT can devirtualize — it knows exactly which method to call, skipping vtable lookup)*
- What is constructor order when you have A → B → C inheritance chain?
  *(A constructor first, then B, then C — base before derived, always)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Calling virtual method from constructor
public class Base
{
    public Base() { Initialize(); } // Virtual call in constructor — dangerous!
    public virtual void Initialize() { Console.WriteLine("Base.Initialize"); }
}
public class Derived : Base
{
    private int _value = 10;
    public override void Initialize() { Console.WriteLine(_value); } // Prints 0!
    // _value not yet assigned when Base constructor runs Initialize()
}

// MISTAKE 2: Inheritance for code reuse only (not "is-a")
public class EmailLogger : List<string> { } // ❌ EmailLogger IS NOT a List!
// Use composition: class EmailLogger { private List<string> _history = new(); }

// MISTAKE 3: Deep inheritance chains (3+ levels)
// A → B → C → D → E — impossible to reason about, fragile
// Prefer: flat hierarchy + interfaces + composition
```

---

## 7. Real-World Usage

| Scenario | Inheritance Applied |
|----------|-------------------|
| Exception hierarchy | `Exception` → `ApplicationException` → `OrderException` |
| EF Core entities | Base `AuditableEntity` with `CreatedAt`, `UpdatedAt` fields |
| ASP.NET Core controllers | `ControllerBase` → your `OrdersController` |
| Domain entities (DDD) | Abstract `Entity<TId>` base with identity |
| Test base classes | `IntegrationTestBase` with shared setup/teardown |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | `: BaseClass`, `base()` constructor chaining |
| **Level 2** | virtual/override/sealed, constructor order, protected members |
| **Level 3** | Fragile base class, composition vs inheritance, multiple interface implementation |
| **Level 4** | Covariant return types (C# 9), devirtualization by JIT via sealed |

## Connected Topics
- [Abstraction](./15-abstraction.md) | [Encapsulation](./12-encapsulation.md) | [Polymorphism](./14-polymorphism.md)
- [Sealed Classes](./16-sealed-classes.md) — controlling inheritance

