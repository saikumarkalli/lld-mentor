# Types of Constructors in C#

> **⚡ Core Idea (30 seconds):** A constructor is a special method that runs when an object is created. C# has 5 types: **default**, **parameterised**, **copy**, **static**, and **private**. Each solves a different object-creation scenario. Knowing which to use — and why — is a common interview topic.

**Domain:** `C#` **Level:** `Beginner` **Tags:** `#constructor` `#oop` `#object-creation` `#static` `#singleton`

---

## 1. Core Idea

Every time you write `new Order()`, a constructor runs. It's your one chance to ensure an object enters a valid state. A constructor with no guardrails means objects can be created in broken states — null fields, invalid IDs, impossible combinations.

**Rule of thumb:** If an object can't be valid without a value, that value should be a constructor parameter — not an optional setter.

---

## 2. Deep Explanation

### 1. Default Constructor (Parameterless)

A constructor with **no parameters**. If you write no constructor at all, the C# compiler generates one automatically — setting all fields to their default values (`0`, `null`, `false`).

```csharp
public class Logger
{
    public string Name;
    // Compiler auto-generates: public Logger() { } if you write nothing
}

var log = new Logger(); // Works — Name is null by default
```

> ⚠️ The auto-generated default constructor disappears the moment you define ANY other constructor. You must add it back explicitly if needed.

```csharp
public class Order
{
    public Order(int id) { Id = id; } // You added a ctor...
    // new Order(); ❌ No longer works — compiler-generated default is gone!
}

// Fix: explicitly add it back
public class Order
{
    public Order() { }  // Explicit default
    public Order(int id) { Id = id; }
}
```

---

### 2. Parameterised Constructor

Takes arguments to ensure the object is in a valid state from the moment of creation.

```csharp
public class BankAccount
{
    public int AccountNumber { get; }
    public string Owner { get; }
    private decimal _balance;

    // Parameterised: you MUST provide account number and owner
    public BankAccount(int accountNumber, string owner, decimal initialBalance = 0)
    {
        if (accountNumber <= 0) throw new ArgumentException("Invalid account number");
        if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Owner required");
        if (initialBalance < 0) throw new ArgumentException("Balance cannot be negative");

        AccountNumber = accountNumber;
        Owner = owner;
        _balance = initialBalance;
    }
}

var acc = new BankAccount(1001, "Alice", 500m); // ✅
// new BankAccount(); ❌ — no default ctor, must provide required fields
```

**This is encapsulation at work:** the object enforces its own invariants at construction time.

---

### 3. Copy Constructor

Creates a **new object as a copy** of an existing one. C# doesn't generate this automatically (unlike C++) — you write it manually when needed.

```csharp
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }

    public Address(string street, string city)
    {
        Street = street;
        City = city;
    }

    // Copy constructor — takes an instance of the same type
    public Address(Address other)
    {
        Street = other.Street;
        City = other.City;
    }
}

var original = new Address("10 Main St", "London");
var copy = new Address(original); // Independent copy
copy.City = "Manchester"; // Doesn't affect original

Console.WriteLine(original.City); // "London" — not affected
```

> **Modern alternative:** Use `record` types — they have built-in `with` expressions for non-destructive copies:
```csharp
public record Address(string Street, string City);
var original = new Address("10 Main St", "London");
var copy = original with { City = "Manchester" }; // Record copy — no manual ctor needed
```

---

### 4. Static Constructor

Runs **once** — automatically, before the first use of the class, whether that's calling a static method or creating the first instance. You cannot call it manually. It has **no access modifiers or parameters**.

```csharp
public class Configuration
{
    public static readonly string ConnectionString;
    public static readonly string ApiBaseUrl;

    // Static constructor — called once, automatically, before first use
    static Configuration()
    {
        ConnectionString = Environment.GetEnvironmentVariable("DB_CONN") ?? "localhost";
        ApiBaseUrl = Environment.GetEnvironmentVariable("API_URL") ?? "https://api.example.com";
        Console.WriteLine("Configuration loaded once.");
    }
}

// First access triggers the static constructor:
Console.WriteLine(Configuration.ConnectionString); // "Configuration loaded once." printed here
Console.WriteLine(Configuration.ConnectionString); // Nothing printed — already ran
```

**Guarantee:** The CLR guarantees the static constructor runs exactly once and is thread-safe — no locking needed.

**Use for:** One-time expensive initialisation — loading config, connecting to a resource, initialising static caches.

---

### 5. Private Constructor

The constructor is `private` — **external code cannot instantiate the class**. Used in two patterns:

**Pattern A — Singleton:** Only one instance ever exists.
```csharp
public class AppSettings
{
    private static AppSettings? _instance;
    public static AppSettings Instance => _instance ??= new AppSettings();

    public string Theme { get; private set; } = "Dark";

    private AppSettings() // Nobody outside can call new AppSettings()
    {
        Theme = File.ReadAllText("settings.json"); // Expensive load, done once
    }
}

var s1 = AppSettings.Instance;
var s2 = AppSettings.Instance;
Console.WriteLine(ReferenceEquals(s1, s2)); // true — same object
```

**Pattern B — Factory Method:** Control how objects are created with named constructors.
```csharp
public class Temperature
{
    private readonly double _celsius;

    private Temperature(double celsius) => _celsius = celsius; // Private

    // Named factory methods — clear intent
    public static Temperature FromCelsius(double c) => new Temperature(c);
    public static Temperature FromFahrenheit(double f) => new Temperature((f - 32) * 5 / 9);
    public static Temperature FromKelvin(double k) => new Temperature(k - 273.15);

    public override string ToString() => $"{_celsius:F1}°C";
}

var t1 = Temperature.FromCelsius(100);
var t2 = Temperature.FromFahrenheit(212);
// new Temperature(100); ❌ — can't call directly
```

---

### Constructor Chaining — `this()` and `base()`

Avoid duplicating code across overloaded constructors by chaining to another:

```csharp
public class Order
{
    public int Id { get; }
    public string Customer { get; }
    public DateTime CreatedAt { get; }

    // Primary constructor — all logic here
    public Order(int id, string customer, DateTime createdAt)
    {
        Id = id;
        Customer = customer;
        CreatedAt = createdAt;
    }

    // Overload chains to primary via this()
    public Order(int id, string customer)
        : this(id, customer, DateTime.UtcNow) { } // Chains to above

    // Child class chains to parent via base()
    public class PriorityOrder : Order
    {
        public int Priority { get; }
        public PriorityOrder(int id, string customer, int priority)
            : base(id, customer) // Calls Order(int, string)
        {
            Priority = priority;
        }
    }
}
```

---

## 3. Quick Reference Table

| Type | Access | Parameters | Called By | Primary Use |
|------|--------|-----------|-----------|------------|
| Default | public | None | `new T()` | Simple objects, deserialisation |
| Parameterised | public | 1+ | `new T(args)` | Enforce valid initial state |
| Copy | public | `T other` | `new T(existing)` | Manual deep copy |
| Static | *(none)* | None | CLR, once | One-time class-level setup |
| Private | private | Any | Internal/Factory | Singleton, factory method |

---

## 4. Interview Questions

1. **What happens to the default constructor when you define a parameterised constructor?**
   *The compiler auto-generates a default (no-arg) constructor only if you define **no** constructor at all. The moment you write any constructor, the compiler's auto-generated one disappears. So if your class has a parameterised constructor and callers also need `new MyClass()`, you must explicitly add a default constructor back.*

2. **What is a static constructor and when does it run?**
   *A static constructor (written as `static ClassName() { }`) is called automatically by the CLR exactly once, before the class is first used (first static access or first instance created), whichever comes first. It has no parameters, no access modifier, and you can't call it manually. The CLR guarantees it runs only once and is thread-safe.*

3. **What design pattern uses a private constructor?**
   *The **Singleton** pattern — a private constructor prevents anyone outside the class from calling `new`, so only the class itself can control creating the single instance. The **Factory Method** pattern also uses a private constructor to force all creation through named static factory methods (e.g., `Temperature.FromCelsius()`) so callers have clear intent.*

4. **What is constructor chaining? How is `this()` different from `base()`?**
   *Constructor chaining is calling one constructor from another to avoid duplicating initialisation logic. `this(...)` chains to **another constructor in the same class** — useful for overloads that share core setup. `base(...)` chains to a **constructor in the parent class** — ensures the parent's initialisation runs before the child's body.*

5. **Why is it better to validate in the constructor than with setters?**
   *A constructor runs when the object is created — it's your only guaranteed window to enforce invariants before the object is ever used. Validation in setters can be bypassed if someone uses object initialisers or reflection, and properties set in any order could leave the object in a temporarily invalid state. The constructor guarantees the object is valid from birth.*

---

## 5. Follow-up Questions

- Can a static constructor throw an exception?
  *(Yes, but the class becomes permanently unusable — subsequent attempts to use it will throw `TypeInitializationException`. Avoid throwing from static constructors.)*
- Can you have more than one static constructor?
  *(No — exactly one static constructor per class is allowed.)*
- What is the order when both a static constructor and an instance constructor are called?
  *(Static runs first — always — before any instance is created. Then the instance constructor runs.)*
- What is `Lazy<T>` and how does it relate to private constructors?
  ```csharp
  private static readonly Lazy<AppSettings> _lazy = new(() => new AppSettings());
  public static AppSettings Instance => _lazy.Value;
  // Thread-safe singleton without explicit locking
  ```
- In the copy constructor, what is the difference between a shallow copy and a deep copy?
  *(Shallow copy: copies references — both objects share the same nested objects. Deep copy: recursively copies nested objects — fully independent.)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Calling virtual method in a constructor
public class Animal
{
    public Animal() { Initialise(); } // Virtual call in ctor — dangerous!
    public virtual void Initialise() { Console.WriteLine("Animal"); }
}
public class Dog : Animal
{
    private string _breed = "Labrador";
    public override void Initialise() { Console.WriteLine(_breed); } // Prints null!
    // _breed hasn't been assigned yet when Animal() runs Initialise()
}

// MISTAKE 2: Expensive logic in constructor (file I/O, DB calls, etc.)
public class ReportService
{
    public ReportService() { _allData = LoadFromDatabase(); } // ❌ Slow startup, untestable
    // FIX: Use lazy loading or a factory/async factory method
}

// MISTAKE 3: Forgetting static constructor is called before fields are initialised
public class Config
{
    static Config() { Console.WriteLine(_path); } // _path is null here — static fields not yet assigned
    private static string _path = "/config";      // Assigned AFTER static ctor body? No — actually
    // In C# field initialisers run BEFORE the static constructor body. This is safe.
    // But: static field initialiser ORDER matters if they depend on each other
}

// MISTAKE 4: Thread-unsafe singleton without Lazy<T> or static constructor
public class Cache
{
    private static Cache? _instance;
    public static Cache Instance // ❌ Race condition: two threads both see null
    {
        get { _instance ??= new Cache(); return _instance; }
    }
    // FIX: Use static ctor or Lazy<T>
}
```

---

## 7. Real-World Usage

| Scenario | Constructor Type |
|----------|----------------|
| Entity/domain object (e.g., `Order`) | Parameterised — enforce invariants |
| Singleton (config, logger registry) | Private + static read-only field |
| Auto-mapped DTOs (JSON deserialisation) | Default (parameterless) |
| Legacy logger or connection objects | Static constructor for one-time setup |
| Named creation (Temperature, Money) | Private + static factory methods |
| base entity (EF Core) | Protected default + full parameterised |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Default, parameterised — syntax and purpose |
| **Level 2** | Static constructor, private constructor, constructor chaining |
| **Level 3** | Singleton with `Lazy<T>`, Factory Method pattern, copy constructor vs `record with` |
| **Level 4** | Virtual method in ctor pitfall, static field initialiser order, `TypeInitializationException` |

## 🔗 Connected Topics
- [Types of Classes](./types-of-class.md) — class modifiers affect constructor rules
- [Encapsulation](../oops/encapsulation.md) — constructors enforce object invariants
- [Inheritance](../oops/inheritance.md) — `base()` constructor chaining
- **Singleton Pattern** → [`LLDMaster.Patterns/01_Creational/`](../../LLDMaster.Patterns/01_Creational/)

*Created: April 2026 · Level: Beginner*
