# 02 — Factory Method Pattern

> **In one line**: Define an interface for creating an object, but let the factory decide which class to instantiate.

---

## 🧠 What Is It?

Factory Method replaces `new ConcreteClass()` at the call site with a factory call. The caller knows only the **abstraction** (`IPaymentGateway`). The factory decides which **concrete class** to return based on configuration, runtime data, or context.

The key insight: **adding a new concrete type requires zero changes to existing code** (Open/Closed Principle).

---

## 🎯 The Problem It Solves

Your checkout page supports Stripe, PayPal, and Bank Transfer. Without Factory Method, every service/controller has:

```csharp
// ❌ Repeated everywhere — tightly coupled to concrete types
if (gatewayType == "stripe")       return new StripeGateway(apiKey);
else if (gatewayType == "paypal")  return new PayPalGateway(clientId, secret);
else if (gatewayType == "bank")    return new BankGateway(bankCode);
```

Add "Crypto payment" tomorrow → find all 10 places with this switch and update each one. Miss one → runtime bug.

Factory Method puts ALL creation logic in **one place**:
```csharp
// ✅ CheckoutService knows NOTHING about Stripe/PayPal/Bank
var gateway = factory.Create(gatewayType);  // factory decides
gateway.Charge(amount, currency, orderId);   // caller uses abstraction
```

---

## 💡 Analogy

A pizza shop. You order "a Margherita" (you specify what you want). The kitchen decides **how** to make it — which oven, which recipe, which chef. You don't assemble the pizza yourself. The shop (factory) abstracts the creation.

---

## 📐 Structure

```
«interface»
IPaymentGateway          concrete products:
  + Charge()    ◄───────  StripeGateway
  + Refund()    ◄───────  PayPalGateway
                ◄───────  BankTransferGateway

«interface»
IPaymentGatewayFactory
  + Create(type) ──────►  PaymentGatewayFactory
                              ├── "stripe"  → new StripeGateway(...)
                              ├── "paypal"  → new PayPalGateway(...)
                              └── "bank"    → new BankTransferGateway(...)

CheckoutService(IPaymentGatewayFactory)   ← knows only interfaces
```

---

## ✅ When to Use

- The exact class to create isn't known until runtime (user selects payment method)
- Adding new "product" types must not require changing existing code
- Object creation involves complex setup (credentials, config) you want centralised
- You want to mock the factory in unit tests

## ❌ When NOT to Use

- Only one concrete type exists and no others are planned — a factory is overkill
- Object creation is trivially `new Foo()` with no variation
- Performance-critical hot path where the indirection overhead matters

---

## 🔑 Top Interview Questions

### Q1: "How is Factory Method different from just writing `new`?"

Three key differences:
1. **Open/Closed**: Add `CryptoGateway` → add one class + one registry entry. Zero changes to `CheckoutService`.
2. **Testability**: In tests, inject `MockPaymentGatewayFactory` → no real HTTP calls.
3. **Centralised creation**: Credentials, config, and construction logic live in ONE place.

### Q2: "How do you add a new gateway without touching `CheckoutService`?"

```csharp
// Just register it — CheckoutService is untouched
factory.Register("crypto", () => new CryptoGateway(config["Crypto:Key"]));

// CheckoutService works immediately:
checkout.ProcessPayment("crypto", 0.01m, "BTC", "ORD-001");
```

This is Open/Closed Principle in action: **open for extension, closed for modification**.

### Q3: "What's the difference between Factory Method and Abstract Factory?"

| | Factory Method | Abstract Factory |
|---|---|---|
| Creates | **One** product | A **family** of products |
| Example | One gateway (Stripe) | Gateway + FraudChecker + Invoice (all Domestic) |
| Interface | `IPaymentGatewayFactory` | `IPaymentProviderFactory` (creates multiple types) |
| Add a new type | Add one class | Add one class per product in family |

### Q4: "What's a Simple Factory and why isn't it a GoF pattern?"

Simple Factory is just a helper class with a static `Create()` method — not in GoF because it violates Open/Closed (you must modify the factory to add new types). Factory Method uses a registry/composition so you don't modify the factory class.

---

## 🆚 Common Confusions

### Factory Method vs Abstract Factory vs Simple Factory

```
Simple Factory:   one static class, one switch — NOT extensible, NOT GoF
Factory Method:   one interface, registry — extensible, Open/Closed ✅
Abstract Factory: multiple factories producing matching families ✅✅
```

---

## 📂 File in This Folder

**[02_PaymentGatewayFactory.cs](02_PaymentGatewayFactory.cs)** — Contains:
- `IPaymentGateway` interface + `PaymentResult` record
- `StripeGateway`, `PayPalGateway`, `BankTransferGateway` — concrete products
- `PaymentGatewayFactory` — dictionary-based registry with `Register()` for runtime extensibility
- `CheckoutService` — consumer that knows zero concrete types
- ASP.NET Core DI wiring comments

---

## 💼 FAANG Interview Tip

Show the **dictionary registry** approach — it's the production-grade Factory Method:

```csharp
private readonly Dictionary<string, Func<IPaymentGateway>> _registry = new()
{
    ["stripe"] = () => new StripeGateway(stripeKey),
    ["paypal"] = () => new PayPalGateway(clientId, secret),
};
public void Register(string type, Func<IPaymentGateway> factory) => _registry[type] = factory;
```

Saying "I use a dictionary registry so new gateways can be added without a `switch` statement" shows you understand **extensibility** — a top FAANG signal.
