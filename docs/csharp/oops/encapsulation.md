# Encapsulation

> **⚡ Core Idea (30 seconds):** Encapsulation means **hiding internal state and exposing only what's necessary**. A class controls its data through access modifiers — objects don't let you reach in and mutate their guts directly.

**Domain:** `C#` · **Level:** `Beginner → Intermediate` · **Tags:** `#oop` `#encapsulation` `#access-modifiers`

> 🔗 **Code Implementation:** [`LLDMaster.OOP/1.Encapsulation/BankAccount.cs`](../../LLDMaster.OOP/1.Encapsulation/BankAccount.cs)

---

## 1. Core Idea

Think of a bank account. You don't have direct access to the vault — you interact through a teller (methods: Deposit, Withdraw). The teller enforces rules. That's encapsulation: **protect state, expose behavior with rules**.

Without encapsulation: any code can set `balance = -99999`. With it, the `Withdraw` method enforces invariants before allowing change.

---

## 2. Deep Explanation

### Access Modifiers

| Modifier | Accessible From |
|----------|----------------|
| `private` | Same class only |
| `protected` | Same class + derived classes |
| `internal` | Same assembly |
| `protected internal` | Same assembly OR derived classes |
| `private protected` | Same class AND derived from same assembly |
| `public` | Everywhere |

### Properties vs Public Fields

Properties are **syntactic sugar for get/set methods**, enabling:
- Validation logic in setters
- Computed values in getters
- Breaking changes to internal representation without breaking callers

```csharp
// BAD: Public field — no invariant control
public decimal Balance; // Anyone can set Balance = -1000000;

// GOOD: Property with validation
private decimal _balance;
public decimal Balance
{
    get => _balance;
    private set
    {
        if (value < 0) throw new InvalidOperationException("Balance cannot be negative");
        _balance = value;
    }
}
```

### Auto-Properties and Init
```csharp
public class User
{
    public int Id { get; init; }        // Set only at construction (C# 9+)
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; } = DateTime.UtcNow; // Get-only (readonly)
}
```

---

## 3. Code Examples

### Basic (Reference the Existing Code)

The `BankAccount` implementation in `LLDMaster.OOP/1.Encapsulation/` demonstrates the pattern. Key principles applied:

```csharp
public class BankAccount
{
    private decimal _balance;          // Hidden state
    private string _accountNumber;     // Immutable after construction

    public BankAccount(string accountNumber, decimal initialBalance)
    {
        _accountNumber = accountNumber;
        _balance = initialBalance >= 0
            ? initialBalance
            : throw new ArgumentException("Initial balance cannot be negative");
    }

    public decimal Balance => _balance; // Read-only exposure

    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Deposit must be positive");
        _balance += amount;
    }

    public bool Withdraw(decimal amount)
    {
        if (amount <= 0 || amount > _balance) return false;
        _balance -= amount;
        return true;
    }
}
```

### Real-World: Domain Entity Invariant Protection
```csharp
public class Order
{
    private readonly List<OrderLine> _lines = new();
    public IReadOnlyList<OrderLine> Lines => _lines; // Expose as read-only

    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public decimal Total => _lines.Sum(l => l.Total); // Computed, always consistent

    public void AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot modify a submitted order");

        var existing = _lines.FirstOrDefault(l => l.ProductId == product.Id);
        if (existing != null)
            existing.IncreaseQuantity(quantity);
        else
            _lines.Add(new OrderLine(product, quantity));
    }

    public void Submit()
    {
        if (!_lines.Any()) throw new InvalidOperationException("Cannot submit empty order");
        Status = OrderStatus.Submitted;
    }
}
```

---

## 4. Interview Questions

1. **What is encapsulation and why does it matter in real projects?**
2. **What's the difference between a property and a public field?**
3. **Why would you expose a `List<T>` as `IReadOnlyList<T>`?**
4. **What is the `init` accessor (C# 9) and when is it useful?**
5. **How does `private set` differ from `init`?**

---

## 5. Follow-up Questions

- If I expose `IReadOnlyList<T>`, can the caller still modify it?
  *(Yes — if they cast to `List<T>`. For true protection, expose `IEnumerable<T>` or use `AsReadOnly()`.)*
- How does encapsulation relate to the **Single Responsibility Principle**?
- What is the difference between `internal` and `private protected`?
- How do you enforce domain invariants across aggregate roots in DDD with encapsulation?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Exposing mutable collections
public List<Order> Orders { get; set; } = new(); // Caller can call .Clear()!
// FIX:
public IReadOnlyList<Order> Orders => _orders;

// MISTAKE 2: Anemic domain model — all public setters, no behavior
public class Product
{
    public decimal Price { get; set; }  // ❌ No validation
    public int Stock { get; set; }      // ❌ Allows Stock = -1000
}

// MISTAKE 3: Property does too much — violating least surprise
public string Name
{
    get => _name;
    set {
        _name = value;
        SendEmailNotification(); // ❌ Unexpected side-effect in setter
    }
}
```

---

## 7. Real-World Usage

| Scenario | Encapsulation Applied |
|----------|----------------------|
| Domain entities (DDD) | Private collections, controlled state transitions |
| Repository pattern | Hidden DB context, exposed query interface |
| Configuration | `IOptions<T>` exposes settings, hides binding details |
| Builder pattern | Construction steps hidden, only `.Build()` public |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | Access modifiers, properties vs fields |
| **Level 2** | `IReadOnlyList<T>`, `init`, invariant enforcement |
| **Level 3** | DDD aggregates, anemic vs rich domain models |
| **Level 4** | Defensive copying, `record` types for immutability |

## 🔗 Connected Topics
- [Abstraction](./abstraction.md) | [Inheritance](./inheritance.md) | [Polymorphism](./polymorphism.md)
- [SOLID — SRP](../../LLDMaster.SOLID/) — classes that break encapsulation often violate SRP

*Created: April 2026 · Code Reference: [`LLDMaster.OOP/1.Encapsulation/`](../../LLDMaster.OOP/1.Encapsulation/)*
