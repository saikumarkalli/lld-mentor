# Chapter 16 — Sealed Classes & Sealed Methods

> **⚡ Core Idea (30 seconds):** The `sealed` keyword prevents a class from being inherited or a method from being further overridden. It isn't just about design intent — `sealed` enables the JIT compiler to **devirtualize** method calls, turning virtual dispatch into direct calls for a measurable performance gain.

**Domain:** `C#` **Level:** `OOP` **Tags:** `#sealed` `#inheritance` `#performance` `#jit`

---

## 1. Core Idea

Think of `sealed` like a **final will and testament** — once sealed, no one can change it. A sealed class says "this is the final version; no one inherits from me." A sealed method says "my override is the last one; no derived class changes this behaviour."

This is the foundation of: **Framework design guidelines, JIT devirtualization, preventing fragile base class problems, and communicating architectural intent**.

---

## 2. Deep Explanation

### Sealed Class

```csharp
public sealed class ConnectionStringValidator
{
    public bool IsValid(string connectionString) { /* ... */ }
}

// public class CustomValidator : ConnectionStringValidator { } // ❌ Compile error!
```

### Sealed Method Override

A method can only be sealed if it's an `override`. You can't seal a `virtual` method at the point of declaration — you seal it in a derived class to stop further overriding.

```csharp
public class Animal
{
    public virtual void Speak() => Console.WriteLine("...");
}

public class Dog : Animal
{
    public sealed override void Speak() => Console.WriteLine("Woof!");
    // No class inheriting Dog can override Speak()
}

public class Puppy : Dog
{
    // public override void Speak() { } // ❌ Compile error! Speak is sealed
}
```

### Performance: JIT Devirtualization

When the JIT sees a `virtual` method call, it must go through the virtual method table (vtable) — an indirect function pointer. For `sealed` classes/methods, the JIT knows there's no derived override, so it replaces the indirect call with a **direct call** (or even inlines the method body).

```
Virtual call:     [Load vtable → Find slot → Jump to address]  ~3 ns
Devirtualized:    [Direct jump to address]                      ~1 ns
Inlined:          [No jump at all — code copied in-place]       ~0 ns
```

This is why .NET's own internal types (like `String`) are sealed.

### When to Seal

| Seal | Don't Seal |
|------|------------|
| Utility/helper classes with no extension point | Base classes designed for inheritance |
| DTOs, configuration classes | Abstract classes |
| Classes where incorrect subclassing would break invariants | Classes with `virtual` methods intended for overriding |
| Hot-path classes where JIT devirtualization matters | Library types where users need extensibility |

---

## 3. Code Examples

### Example 1 — Preventing Broken Inheritance
```csharp
// ❌ Dangerous: Unsealed class with assumptions about internal state
public class OrderCalculator
{
    public virtual decimal GetDiscount(Order order) => order.Total > 100 ? 0.1m : 0m;

    public decimal CalculateFinal(Order order)
    {
        var discount = GetDiscount(order); // Calls virtual method
        return order.Total * (1 - discount);
    }
}

// A careless subclass breaks the contract
public class BuggyCalculator : OrderCalculator
{
    public override decimal GetDiscount(Order order) => 2.0m; // 200% discount!
}

// ✅ Fix: Seal the class if it's not designed for inheritance
public sealed class OrderCalculator { /* ... */ }
```

### Example 2 — Sealed for Performance
```csharp
// Hot path: called millions of times per second
public sealed class FastJsonParser
{
    // JIT can devirtualize and inline because the class is sealed
    public ReadOnlySpan<byte> ParseNext(ReadOnlySpan<byte> buffer) { /* ... */ }
}
```

---

## 4. Interview Questions

1. **What does the sealed keyword do in C#?**
   *On a class, `sealed` prevents inheritance — no class can derive from it. On a method, `sealed` prevents further overriding in derived classes. It can only be applied to an `override` method, not a `virtual` method at the declaration point.*

2. **Why does sealing a class improve performance?**
   *The JIT compiler knows that a sealed class has no derived types, so virtual method calls can be devirtualized — replaced with direct calls or even inlined. This eliminates the vtable lookup overhead, which matters on hot paths called millions of times.*

3. **Is `System.String` sealed? Why?**
   *Yes. If `String` were inheritable, someone could create a subclass that overrides methods like `GetHashCode()`, breaking every `Dictionary<string, T>` in the runtime. Sealing protects the invariants that the entire framework depends on.*

4. **Can you seal a method that isn't an override?**
   *No. `sealed` on a method only makes sense on an `override` — you are sealing the override chain. A `virtual` method at the base class level is meant to be overridden; the sealing happens downstream when a derived class decides "no more overrides below me."*

5. **When should you NOT seal a class?**
   *When it's designed as an extension point — abstract base classes, framework base types, classes with `virtual` methods intended for customization. Also, sealing makes mocking harder in unit tests — you can't create a subclass mock of a sealed class without a framework like NSubstitute with Castle.Core.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Sealing a class that needs to be mocked
public sealed class EmailService : IEmailService { /* ... */ }
// In tests, you can't subclass it — but you CAN mock IEmailService
// FIX: Always depend on the interface, not the sealed class

// MISTAKE 2: Forgetting that sealed applies to the METHOD, not the class
public class Base { public virtual void M() { } }
public class Mid : Base { public sealed override void M() { } }
public class Child : Mid { } // ✅ Compiles — the CLASS isn't sealed, just M()
```

---

## Connected Topics

- [Polymorphism](./14-polymorphism.md) — Sealed stops the virtual dispatch chain
- [Inheritance](./13-inheritance.md) — Sealed is the opposite of abstract in the inheritance spectrum
- [Types of Classes](../01-beginner/07-types-of-class.md) — Sealed is one of the class modifier types

---

*Created: May 2026 · Level: OOP*
