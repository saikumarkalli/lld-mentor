# 06 — Adapter Pattern

> **In one line**: Convert an incompatible interface into the one your code expects — without changing either side.

---

## 🧠 What Is It?

Adapter is a wrapper that **translates** one interface into another. It sits between your system and an incompatible class (usually a legacy or third-party component), making them work together without modifying either.

---

## 🎯 The Problem It Solves

Your company acquires a bank whose payment system is 10 years old. It has a SOAP/XML API:
```csharp
// Vendor code — YOU CANNOT CHANGE THIS
class LegacyBankPaymentSystem
{
    public string DebitAccount(string xmlPayload)  { ... }
    public string ReverseDebit(string xmlPayload)  { ... }
}
```

Your modern checkout pipeline expects:
```csharp
// Your system's interface — YOU CANNOT CHANGE THIS (other gateways use it)
interface IPaymentGateway
{
    PaymentResult Charge(decimal amount, string currency, string referenceId);
    PaymentResult Refund(string transactionId, decimal amount);
}
```

You can't change the bank system (vendor black box). You can't change `IPaymentGateway` (Stripe/PayPal already implement it). The Adapter sits between them:

```csharp
// Adapter: translates modern calls → legacy XML calls
class LegacyBankPaymentAdapter : IPaymentGateway
{
    private readonly LegacyBankPaymentSystem _bank;

    public PaymentResult Charge(decimal amount, string currency, string referenceId)
    {
        var xml = BuildDebitXml(amount, currency, referenceId); // translate
        var response = _bank.DebitAccount(xml);                 // call legacy
        return ParseResponse(response);                         // translate back
    }
}
```

---

## 💡 Analogy

A **UN translator**. The French delegate speaks French. The Japanese delegate speaks Japanese. The translator (Adapter) sits between them — neither delegate changes how they speak. The translator handles all the conversion. Remove the translator → they can't work together. Add the translator → they collaborate without knowing each other exists.

---

## 📐 Structure

```
«interface»
IPaymentGateway         «adapter»                  «adaptee»
  + Charge()  ◄──── LegacyBankPaymentAdapter ──── LegacyBankPaymentSystem
  + Refund()         + Charge()   ← translates →    + DebitAccount(xml)
                     + Refund()   ← translates →    + ReverseDebit(xml)

ModernCheckoutService(IPaymentGateway)
  ← completely unaware the bank uses XML
```

**Object Adapter** (composition — preferred in C#):
```csharp
class Adapter : ITarget
{
    private readonly Adaptee _adaptee; // composition
    public void Request() => _adaptee.SpecificRequest(); // translate
}
```

**Class Adapter** (inheritance — rarely used in C#):
```csharp
class Adapter : Adaptee, ITarget  // needs non-sealed Adaptee
{
    public void Request() => SpecificRequest(); // inherited method
}
```

---

## ✅ When to Use

- Integrating a **third-party or legacy system** you cannot modify
- Reusing an existing class whose interface doesn't match what you need
- **Progressive migration**: wrap the old system, replace its internals incrementally
- You want to isolate XML/SOAP/CSV parsing in one place (the Adapter)

## ❌ When NOT to Use

- You own both interfaces — just change one of them directly
- The translation is trivial — a direct call is fine
- The gap between interfaces is so large that the adapter becomes a God object

---

## 🔑 Top Interview Questions

### Q1: "Object Adapter vs Class Adapter — what's the difference and which do you prefer in C#?"

```
Object Adapter (composition):
  - Works with SEALED classes (most third-party code is sealed)
  - Can adapt multiple adaptees
  - ✅ Preferred in C# — composition over inheritance

Class Adapter (inheritance):
  - Requires adaptee to be non-sealed
  - Can override adaptee methods
  - ❌ Rarely viable in C# — most classes are sealed by default
```

### Q2: "How is Adapter different from Decorator?"

| | Adapter | Decorator |
|---|---|---|
| Changes interface? | ✅ Yes — translates | ❌ No — same interface |
| Adds behaviour? | ❌ No — just translates | ✅ Yes — logging, retry, etc. |
| Purpose | Compatibility | Extension |

One-liner: **Adapter changes the shape. Decorator adds to it.**

### Q3: "How is Adapter different from Facade?"

| | Adapter | Facade |
|---|---|---|
| Interface count | **One** incompatible interface | **Many** complex interfaces |
| Changes interface? | Yes (translates) | Yes (simplifies many into one) |
| Purpose | Make ONE incompatible thing work | Simplify MANY complex things |

### Q4: "Give a real .NET example of the Adapter pattern"

- `ILogger` adapters — Serilog adapts to `Microsoft.Extensions.Logging.ILogger`
- `HttpClient` wrapping old `HttpWebRequest` — same pattern
- EF Core providers — `DbContext` adapts to SQL Server, PostgreSQL etc via provider adapters
- `TextReader`/`TextWriter` — stream adapters in .NET itself

---

## 🆚 Common Confusions

### Adapter vs Facade vs Decorator

The quick mental test:
- **Something is incompatible** → Adapter (change the interface)
- **Something needs extra behaviour** → Decorator (same interface, more features)
- **Something is too complex** → Facade (simplify many things into one)

---

## 📂 File in This Folder

**[06_LegacyPaymentAdapter.cs](06_LegacyPaymentAdapter.cs)** — Contains:
- `LegacyBankPaymentSystem` — the vendor "black box" XML SOAP system
- `LegacyBankPaymentAdapter` — Object Adapter (composition), implements `IPaymentGateway`
- XML translation helpers isolated in private methods
- `ModernCheckoutService` — completely unaware of XML; works with any `IPaymentGateway`
- ASP.NET Core DI wiring comments

---

## 💼 FAANG Interview Tip

Real .NET engineers use Adapter **constantly** for third-party integrations. Show you know the difference between **Object Adapter** (composition, works with sealed classes) and **Class Adapter** (inheritance, rarely viable in C#). Saying *"In C#, I always use Object Adapter because most third-party classes are sealed"* signals practical production knowledge, not just textbook patterns.
