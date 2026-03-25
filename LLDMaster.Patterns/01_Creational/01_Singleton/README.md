# 01 — Singleton Pattern

> **In one line**: Ensure a class has exactly one instance and provide a global point of access to it.

---

## 🧠 What Is It?

Singleton ensures that **only one instance** of a class exists for the lifetime of the application. Any code that asks for an instance always gets the **same object** — not a new copy.

---

## 🎯 The Problem It Solves

`PaymentConfigManager` holds Stripe/PayPal API keys loaded from environment. If two services each do `new PaymentConfigManager()`, they get **different objects** — one may have stale keys after a rotation, causing silent auth failures. This is a real production bug.

```csharp
// BUG — two separate objects, two separate config loads
var configA = new NaivePaymentConfigManager();  // loads from env
var configB = new NaivePaymentConfigManager();  // loads again — separate object
// If Stripe rotates keys mid-session, configA and configB can diverge → auth failures
```

Singleton fixes this: `PaymentConfigManager.Instance` always returns the **same object**.

---

## 💡 Analogy

A country's central bank (RBI, Federal Reserve). There's **one** central bank per country. Every commercial bank, every transaction, refers to the same institution. You don't spin up a new central bank per transaction.

---

## 📐 Structure

```
Client A ──┐
            ├──▶  Singleton.Instance  ──▶  [The One Object]
Client B ──┘
               ↑
           always the same reference
```

---

## ✅ When to Use

- Configuration manager loaded once at startup
- Database connection pool (one pool, many connections from it)
- Logger instance shared across the application
- Payment session manager (one cart per session)
- Application-wide cache

## ❌ When NOT to Use

- When you need one instance **per user/request** — use DI scoped lifetime instead
- In distributed systems — each pod/node gets its own "singleton" (not truly one)
- When the class holds mutable state that causes test pollution (tests share the instance)
- When you're just avoiding passing a parameter — that's lazy design, not Singleton

---

## 🔑 Top Interview Questions

### Q1: "How do you make Singleton thread-safe in C#?"

Three approaches, each valid:

```csharp
// ✅ Option 1: Lazy<T> — PREFERRED in .NET 8+
private static readonly Lazy<ExchangeRateCache> _lazy = new(() => new());
public static ExchangeRateCache Instance => _lazy.Value;
// Lazy<T> with default LazyThreadSafetyMode.ExecutionAndPublication = thread-safe + lazy

// ✅ Option 2: Static field initialiser (eager, but CLR guarantees thread-safety)
private static readonly PaymentAuditLogger _instance = new();
public static PaymentAuditLogger Instance => _instance;

// ✅ Option 3: Double-checked locking (classic, verbose)
private static volatile PaymentConfigManager? _instance;
private static readonly object _lock = new();
public static PaymentConfigManager Instance {
    get {
        if (_instance is null) lock (_lock) { _instance ??= new(); }
        return _instance;
    }
}
// volatile keyword is critical — prevents CPU reordering stale null reads
```

### Q2: "What's the problem with Singleton in unit tests?"

Tests **share state** from the previous test. If test 1 logs audit entries and test 2 expects an empty log — it fails. You can't reset a static Singleton between tests.

**Solution**: Use the **DI-friendly Singleton** (`services.AddSingleton<ICartSession, CartSession>()`). The DI container can be reset per test.

### Q3: "How does ASP.NET Core handle Singleton lifetime?"

```csharp
builder.Services.AddSingleton<ICartSession, CartSession>();
// Container creates ONE instance for the app's lifetime
// Injected into controllers — never use 'new' in app code
```

This is preferred over static `Instance` because the class is **testable** (you can inject a mock) and **mockable** (interface-based).

### Q4: "Is Singleton an anti-pattern?"

**Nuanced answer** (what interviewers want to hear):

- **Static Singleton** (`Foo.Instance`) = often an anti-pattern. Hides dependencies, impossible to mock, causes test pollution.
- **DI Singleton** (`AddSingleton()`) = NOT an anti-pattern. It's how .NET's own services work. The container manages the lifetime; the class itself is a normal class.

---

## 🆚 Common Confusions

### Singleton vs Static Class

| | Singleton | Static Class |
|---|---|---|
| Can implement interface? | ✅ Yes | ❌ No |
| Can be mocked in tests? | ✅ Yes (DI version) | ❌ No |
| Can be lazy-initialised? | ✅ Yes | ❌ No (loaded at startup) |
| Can have instance methods? | ✅ Yes | ❌ (only static) |

> **Rule**: If the class needs to implement an interface or be injected as a dependency → use Singleton via DI. If it's pure utility functions with no state → static class is fine.

---

## 📂 File in This Folder

**[01_PaymentConfigManager.cs](01_PaymentConfigManager.cs)** — Contains 4 implementations:
- `PaymentConfigManager` — double-checked locking with `volatile` (Stripe/PayPal API keys)
- `ExchangeRateCache` — `Lazy<T>` (preferred) (USD/EUR/GBP → INR rates)
- `PaymentAuditLogger` — static field (eager) (append-only payment event log)
- `ICartSession` + `CartSession` — DI-friendly (production recommended)

---

## 💼 FAANG Interview Tip

When asked about Singleton, **lead with the DI approach**:

> *"In production .NET code, I'd register it as `services.AddSingleton<ICartSession, CartSession>()`. The container ensures one instance per app lifetime, while the class stays testable. The static `Instance` pattern is useful when you don't have a DI container — I'd then use `Lazy<T>` for thread-safe lazy initialisation."*

This signals you know **real .NET** production patterns, not just textbook GoF.
