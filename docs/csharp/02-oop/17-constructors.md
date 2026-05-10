# Chapter 17 — Constructor Chaining

> **⚡ Core Idea (30 seconds):** Constructor chaining lets one constructor call another using `this()` (same class) or `base()` (parent class). This eliminates duplicate initialisation logic and ensures every object is created through a single, validated code path.

**Domain:** `C#` **Level:** `OOP` **Tags:** `#constructors` `#this` `#base` `#initialisation`

---

## 1. Core Idea

Think of constructor chaining like a **relay race**. Each runner (constructor) passes the baton to the next. The most specific constructor delegates to a more general one that does the actual work. This ensures all objects end up fully initialised, regardless of which constructor the caller used.

---

## 2. Deep Explanation

### `this()` — Chaining Within the Same Class

```csharp
public class Order
{
    public Guid Id { get; }
    public string Customer { get; }
    public DateTime CreatedAt { get; }

    // Primary constructor — does ALL the work
    public Order(Guid id, string customer, DateTime createdAt)
    {
        Id = id;
        Customer = customer ?? throw new ArgumentNullException(nameof(customer));
        CreatedAt = createdAt;
    }

    // Convenience: auto-generates ID and timestamp
    public Order(string customer) : this(Guid.NewGuid(), customer, DateTime.UtcNow)
    {
        // Body runs AFTER the chained constructor
    }
}
```

### `base()` — Chaining to Parent Class

```csharp
public class Entity
{
    public Guid Id { get; }
    protected Entity(Guid id) => Id = id;
}

public class Product : Entity
{
    public string Name { get; }

    public Product(Guid id, string name) : base(id) // Calls Entity(id)
    {
        Name = name;
    }

    public Product(string name) : this(Guid.NewGuid(), name) // Chains to Product(Guid, string)
    {
    }
}
```

### Execution Order

```
1. base class static constructor (once per type)
2. derived class static constructor (once per type)
3. base class instance field initialisers
4. base class constructor body
5. derived class instance field initialisers
6. derived class constructor body
```

### Primary Constructors (C# 12)

```csharp
// C# 12: Primary constructor parameters are available throughout the class
public class UserService(ILogger<UserService> logger, IUserRepository repo)
{
    public User GetUser(int id)
    {
        logger.LogInformation("Getting user {Id}", id);
        return repo.GetById(id);
    }
}
```

---

## 3. Code Examples

### Example 1 — Eliminating Duplicate Validation
```csharp
// ❌ Bad: Validation duplicated in every constructor
public class Connection
{
    public Connection(string host) { Validate(host); Host = host; Port = 5432; }
    public Connection(string host, int port) { Validate(host); Host = host; Port = port; }
    private void Validate(string host) { if (string.IsNullOrEmpty(host)) throw new ArgumentException(); }
}

// ✅ Good: Chain to the master constructor
public class Connection
{
    public string Host { get; }
    public int Port { get; }

    public Connection(string host, int port)
    {
        Host = !string.IsNullOrEmpty(host) ? host : throw new ArgumentException(nameof(host));
        Port = port > 0 ? port : throw new ArgumentOutOfRangeException(nameof(port));
    }

    public Connection(string host) : this(host, 5432) { } // Defaults handled cleanly
}
```

### Example 2 — Object Initialiser vs Constructor
```csharp
// Constructor: enforces required values at compile time
var order = new Order("Sai");              // ✅ Can't forget customer name

// Object initialiser: no enforcement — all properties optional
var order2 = new Order { Customer = "Sai" }; // ❌ Could forget to set Customer
```

---

## 4. Interview Questions

1. **What is constructor chaining and why is it useful?**
   *Constructor chaining is when one constructor calls another with `this()` or `base()`. It centralises initialisation logic in a single master constructor, eliminating code duplication and ensuring validation runs regardless of which constructor the caller uses.*

2. **What is the execution order when a derived class is instantiated?**
   *First, base class field initialisers run. Then, the base class constructor body executes. Then, derived class field initialisers run. Finally, the derived class constructor body executes. Static constructors run once before any instance is created.*

3. **What are primary constructors in C# 12?**
   *Primary constructors allow you to declare constructor parameters directly on the class/struct declaration. The parameters are available as captured variables throughout the class body, eliminating the need for private fields and manual assignment.*

4. **Can you call both `this()` and `base()` in the same constructor?**
   *No. A constructor can chain to either `this()` or `base()`, not both. If you chain to `this()`, that target constructor will eventually chain to `base()` (implicitly or explicitly), so the base constructor always runs.*

5. **When should you use a constructor vs an object initialiser?**
   *Use constructors for required, validated parameters that the object cannot exist without. Use object initialisers for optional properties. Constructors enforce correctness at compile time; initialisers defer validation to runtime.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Calling virtual methods in constructors
public class Base
{
    public Base() { Initialize(); } // Calls virtual method!
    public virtual void Initialize() { }
}
public class Derived : Base
{
    private readonly string _name;
    public Derived(string name) : base() { _name = name; }
    public override void Initialize()
    {
        Console.WriteLine(_name.Length); // ❌ NullReferenceException!
        // _name hasn't been set yet — Base constructor runs BEFORE Derived constructor
    }
}

// MISTAKE 2: Forgetting that base() is called implicitly
public class Animal { public Animal() { Console.Write("A"); } }
public class Dog : Animal { public Dog() { Console.Write("D"); } }
new Dog(); // Prints "AD" — base constructor runs first!
```

---

## 🔗 Connected Topics

- [Types of Constructors](../01-beginner/06-types-of-constructors.md) — Default, static, private constructors
- [Inheritance](./13-inheritance.md) — `base()` chaining in inheritance hierarchies
- [Encapsulation](./12-encapsulation.md) — Constructors enforce object invariants

---

*Created: May 2026 · Level: OOP*
