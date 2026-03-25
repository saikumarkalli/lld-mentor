# 03 — Abstract Factory Pattern

> **In one line**: Create families of related objects that are guaranteed to work together, without knowing their concrete classes.

---

## 🧠 What Is It?

Abstract Factory produces **entire families** of objects. The factory contract returns multiple related products — and they're guaranteed compatible because they all come from the same concrete factory.

If Factory Method answers "**which** class?", Abstract Factory answers "**which family**?"

---

## 🎯 The Problem It Solves

Your payment system supports two environments:
- **Domestic** → uses RazorPay gateway, GST invoice format, ₹ threshold fraud rules
- **International** → uses Stripe gateway, VAT invoice format, $ threshold fraud rules

Each environment needs THREE objects: `IPaymentGateway` + `IFraudChecker` + `IInvoiceGenerator`.

**The rule**: these three MUST match. Using a `DomesticGateway` with an `InternationalFraudChecker` = wrong currency threshold, wrong tax rules → silent billing bugs in production.

```csharp
// ❌ Nothing stops a developer from mixing families — subtle runtime bug
var gateway = new DomesticGateway();           // ₹ pricing
var fraud   = new InternationalFraudChecker(); // $ threshold — MISMATCH!
```

Abstract Factory **enforces family consistency** at compile time:
```csharp
// ✅ All objects from the same factory = guaranteed to work together
IPaymentProviderFactory factory = new DomesticPaymentProviderFactory();
var gateway = factory.CreateGateway();          // DomesticGateway
var fraud   = factory.CreateFraudChecker();     // DomesticFraudChecker ← MUST match
var invoice = factory.CreateInvoiceGenerator(); // DomesticInvoice ← MUST match
```

---

## 💡 Analogy

IKEA furniture families. You buy from the **"BILLY" section** — bookcase, desk, wardrobe. All same wood finish, same screw sizes, same colour. They fit together perfectly. If you mix a BILLY shelf with PAX wardrobe rails, they don't fit. Abstract Factory = the IKEA section that ensures you buy **all matching pieces**.

---

## 📐 Structure

```
«interface»
IPaymentProviderFactory
  + CreateGateway()           «interface» IPaymentGateway
  + CreateFraudChecker()  ──► «interface» IFraudChecker
  + CreateInvoiceGenerator()  «interface» IInvoiceGenerator

DomesticPaymentProviderFactory          InternationalPaymentProviderFactory
  ├── CreateGateway()     → DomesticGateway    ├── CreateGateway()  → IntlGateway
  ├── CreateFraudChecker()→ DomesticFraud      ├── CreateFraudChecker()→ IntlFraud
  └── CreateInvoice()     → DomesticInvoice    └── CreateInvoice()  → IntlInvoice

OrderPaymentProcessor(IPaymentProviderFactory)  ← knows ZERO concrete types
```

---

## ✅ When to Use

- Objects must be used **together** and mixing would be a bug (families)
- Multiple "environments" or "themes" with the same structure (domestic/intl, dev/prod)
- Swap the entire family via a single config change
- You want to guarantee product compatibility at compile time

## ❌ When NOT to Use

- Products are independent — use Factory Method per product
- Only one family exists now and a second is not planned (YAGNI)
- Adding new products happens frequently — every new product requires updating ALL factory implementations

---

## 🔑 Top Interview Questions

### Q1: "What's the difference between Abstract Factory and Factory Method?"

| Dimension | Factory Method | Abstract Factory |
|-----------|---------------|-----------------|
| Products created | **1** | **N** (a family) |
| Compatibility | N/A | Products in family **guaranteed** compatible |
| When to use | One varying type | Multiple types that must match |
| C# example | `IPaymentGatewayFactory.Create()` | `IPaymentProviderFactory.CreateGateway()` + `CreateFraudChecker()` + … |

### Q2: "What's the weakness of Abstract Factory?"

Adding a **new product** to the family (e.g., add `ILoyaltyProvider`) requires:
1. Adding to `IPaymentProviderFactory` interface
2. Implementing in `DomesticPaymentProviderFactory`
3. Implementing in `InternationalPaymentProviderFactory`
4. Implementing in every future factory

This is an **Open/Closed violation** — you must modify all existing factories. Mention this in interviews — showing trade-offs is a strong signal.

### Q3: "How do you swap the entire payment family in ASP.NET Core?"

```csharp
// One config line changes the entire payment family:
IPaymentProviderFactory factory = config["Payment:Mode"] == "international"
    ? new InternationalPaymentProviderFactory()
    : new DomesticPaymentProviderFactory();

builder.Services.AddSingleton(factory);
// All services using IPaymentProviderFactory now use the new family
```

### Q4: "Where is Abstract Factory used in .NET itself?"

- `DbProviderFactory` — creates `DbConnection`, `DbCommand`, `DbDataAdapter` for a specific DB provider (SQL Server, PostgreSQL, MySQL). All three always match.
- UI themes — `IControlFactory` returning themed buttons, inputs, dialogs that all match.

---

## 🆚 Common Confusions

**Abstract Factory vs Factory Method**

The simplest test: count the products.
- One product created → Factory Method
- Two or more products that must be consistent → Abstract Factory

**Abstract Factory vs Builder**

- Builder: constructs **one complex object** step-by-step
- Abstract Factory: creates **multiple related objects** (each simple to create, but must match)

---

## 📂 File in This Folder

**[03_PaymentProviderAbstractFactory.cs](03_PaymentProviderAbstractFactory.cs)** — Contains:
- `IPaymentProviderFactory` + `IPaymentGateway` + `IFraudChecker` + `IInvoiceGenerator`
- `DomesticPaymentProviderFactory` — full domestic family
- `InternationalPaymentProviderFactory` — full international family
- `OrderPaymentProcessor` — consumer that orchestrates with ZERO concrete knowledge
- ASP.NET Core config-based family switching

---

## 💼 FAANG Interview Tip

The **weakness** question ("what's the downside of Abstract Factory?") is what separates senior candidates from juniors. Lead with: *"The cost of Abstract Factory is that adding a new product type to the family requires changing every factory implementation. For our payment domain, we have 3 products — that's manageable. If the number of product types grows rapidly, I'd consider a different approach."*

Showing you know the trade-off demonstrates senior engineering judgment.
