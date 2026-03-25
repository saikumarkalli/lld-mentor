# 04 — Builder Pattern

> **In one line**: Construct a complex object step-by-step using a fluent API, with validation at the final `Build()` call.

---

## 🧠 What Is It?

Builder separates the **construction** of a complex object from its **representation**. Instead of one giant constructor with 12 parameters, you chain readable method calls and produce a validated, immutable object at the end.

---

## 🎯 The Problem It Solves

A `PaymentRequest` in production has 12+ fields: `amount`, `currency`, `gateway`, `orderId`, `customerId`, `coupon`, `webhookUrl`, `idempotencyKey`, `maxRetries`, `saveCard`, `metadata`...

**Option A — Telescoping constructors** (anti-pattern):
```csharp
new PaymentRequest(1200, "INR", "stripe", "ORD-1", "CUST-1", null, null, null, 3, false, null)
//                                                   ↑     ↑     ↑     ↑     ↑  ↑      ↑
//                       which null is which?   coupon? webhook? key? ...impossible to read
```

**Option B — Public setters** (no validation timing):
```csharp
var req = new PaymentRequest();
req.Amount = 1200;
req.GatewayType = "stripe";
// Caller forgets to set OrderId → null reference at gateway call — fails at runtime
```

**Builder solution**:
```csharp
var request = new PaymentRequestBuilder()
    .ForAmount(1200m)
    .ForOrder("ORD-1", "CUST-1")     // required — clear intent
    .ViaGateway("stripe")
    .WithCoupon("SAVE20")            // optional — readable
    .Build();                        // ← ALL validation fires HERE, before hitting gateway
```

---

## 💡 Analogy

Ordering a custom laptop on Dell.com. You configure: processor, RAM, storage, colour, warranty — each step separately. You don't call `new Dell(i9, 32GB, 2TB, silver, 3yr)` as one unreadable constructor. The configurator (Builder) walks you through steps, validates the combination, then builds your laptop.

---

## 📐 Structure

```
PaymentRequestBuilder (Builder)
  ├── ForAmount(amount)       → returns this
  ├── ForOrder(id, custId)    → returns this
  ├── ViaGateway(type)        → returns this  (optional)
  ├── WithCoupon(code)        → returns this  (optional)
  ├── WithRetries(n)          → returns this  (optional)
  └── Build()                 → validates → returns PaymentRequest

PaymentRequest (Product — immutable record)
  ← only constructable via Builder

PaymentRequestDirector (optional)
  ← pre-built recipes: BuildDomesticPayment(), BuildStripeIntlPayment()
```

---

## ✅ When to Use

- Object has **more than 3–4 optional fields** (positional constructors become unreadable)
- Object must be **immutable** once created (all validation in `Build()`)
- Construction has **steps that must happen in order**
- You want a **self-documenting call site** (`.ForOrder()` is clearer than position 4)

## ❌ When NOT to Use

- Object has 2–3 fields — just use a constructor or `record` with `init` properties
- Object is mutable and fields change frequently after creation
- No optional fields — Builder adds boilerplate with no benefit

---

## 🔑 Top Interview Questions

### Q1: "What's the difference between Builder and a constructor?"

| | Constructor | Builder |
|---|---|---|
| Readability | Poor with 5+ params | Self-documenting (`.ForAmount()`) |
| Optional fields | All or nothing | Each is optional with sane defaults |
| Validation | At construction | In `Build()` — after all fields are set |
| Immutability | Possible | Enforced — object only exists after `Build()` |

### Q2: "Why put validation in `Build()`, not in each setter?"

Validation in individual setters is premature — you're checking fields in isolation. In `Build()` you can validate **combinations**:
```csharp
// Can only validate this cross-field rule in Build(), not in individual setters:
if (_gatewayType == "stripe" && _currency == "INR" && _amount > 500_000)
    throw new InvalidOperationException("Stripe INR charges capped at ₹5L. Use bank transfer.");
```

Fail-fast at `Build()` = **guaranteed valid object reaches the gateway**.

### Q3: "What is a Director and when do you need it?"

Director encapsulates common build sequences so callers don't have to chain 8 methods:
```csharp
public static class PaymentRequestDirector
{
    // Same 8-step chain used in 15 places → put it here once
    public static PaymentRequest BuildDomesticPayment(string orderId, string custId, decimal amount)
        => new PaymentRequestBuilder()
            .ForAmount(amount).ForOrder(orderId, custId)
            .ViaGateway("razorpay").InCurrency("INR")
            .WithRetries(3).AddMetadata("channel", "web")
            .Build();
}
```

### Q4: "How does C# `record` with `init` properties differ from Builder?"

```csharp
// Record init — good for simple DTOs with few fields, no cross-field validation
var req = new PaymentRequest { Amount = 1200, OrderId = "ORD-1" }; // OrderId could be null

// Builder — better for complex objects with validation and many optional fields
var req = new PaymentRequestBuilder().ForAmount(1200).ForOrder("ORD-1", "CUST-1").Build(); // validated
```

---

## 🆚 Common Confusions

**Builder vs Fluent API**: Builder IS implemented as a fluent API, but the defining feature is the **`Build()` step** that validates and produces an immutable object. A fluent API without a `Build()` validation step is just method chaining.

**Builder vs Abstract Factory**: Builder constructs **one complex object** in steps. Abstract Factory creates **multiple simple related objects** in one call.

---

## 📂 File in This Folder

**[04_PaymentRequestBuilder.cs](04_PaymentRequestBuilder.cs)** — Contains:
- `PaymentRequest` — immutable `record` with `internal` constructor (Builder only)
- `PaymentRequestBuilder` — fluent API with required/optional steps + cross-field validation in `Build()`
- `PaymentRequestDirector` — `BuildDomesticPayment()` and `BuildInternationalStripePayment()` recipes

---

## 💼 FAANG Interview Tip

Show **immutability** — the object returned by `Build()` should be a `record` or have `init`-only properties. Saying *"I make `PaymentRequest` a `record` with `internal` constructor — the only way to create it is through the Builder, which guarantees it's always valid"* is a strong senior-engineer signal.
