# 🏗️ Creational Patterns — Object Creation Mastery

> **Core question these patterns answer**: "How should I create objects?"

---

## What Are Creational Patterns?

Creational patterns decouple the **client** from the **concrete class** it instantiates. Instead of writing `new StripeGateway()` scattered across 20 files, the client asks for "a payment gateway" — the pattern decides which concrete class to create, how many instances exist, and how complex ones are assembled.

The 5 patterns here each solve a distinct creation problem:

| Problem | Pattern | Keyword |
|---------|---------|---------|
| "I need exactly one instance shared everywhere" | Singleton | **One** |
| "I don't know which class to create until runtime" | Factory Method | **Which** |
| "I need a whole family of related objects that work together" | Abstract Factory | **Family** |
| "My object has 12+ fields and complex validation" | Builder | **Step-by-step** |
| "Creating from scratch is expensive — can I clone?" | Prototype | **Clone** |

---

## 🧠 The Core Insight

```
WITHOUT patterns:          WITH patterns:
new StripeGateway()        factory.Create("stripe")
new PayPalGateway()        ↑
new BankGateway()          caller never knows which class
                           → testable, extensible, Open/Closed
```

The client asks for an **abstraction** (`IPaymentGateway`). The pattern returns the **right concrete class**. This is how you make code that's easy to change.

---

## 📊 Pattern Comparison (Interview Gold)

Interviewers LOVE asking you to compare these — here's the cheat sheet:

| Pattern | Creates | How many? | Decision point | Key C# tool |
|---------|---------|-----------|----------------|-------------|
| Singleton | One shared instance | 1 | App startup | `Lazy<T>`, `AddSingleton()` |
| Factory Method | One product | 1 per call | Runtime type (string/enum) | Interface + registry dict |
| Abstract Factory | Family of products | 1 per factory call | Environment/region | Multiple interfaces, DI |
| Builder | One complex object | 1 per `Build()` | Many optional fields | Fluent API, `record init` |
| Prototype | Copy of existing | 1 per `Clone()` | Expensive setup already done | `MemberwiseClone`, deep copy |

---

## 🆚 The Most Confused Pair: Factory Method vs Abstract Factory

This comes up in **every** interview:

**Factory Method** — creates **one product**, decides which concrete class:
```csharp
// Factory Method: one product
IPaymentGateway gw = factory.Create("stripe");  // returns StripeGateway
```

**Abstract Factory** — creates a **family of related products** that must work together:
```csharp
// Abstract Factory: whole family
IPaymentGateway gw      = factory.CreateGateway();       // DomesticGateway
IFraudChecker   fraud   = factory.CreateFraudChecker();  // DomesticFraudChecker
IInvoiceGenerator inv   = factory.CreateInvoiceGenerator(); // DomesticInvoice
// All three come from the SAME factory → guaranteed to work together
```

> **One sentence**: Factory Method answers "which class?" — Abstract Factory answers "which family?"

---

## 📂 Patterns in This Folder

| File | Pattern | What it demonstrates |
|------|---------|---------------------|
| [01_CartSessionManager.cs](01_Singleton/01_CartSessionManager.cs) | **Singleton** | 4 C# flavours: `volatile+lock`, `Lazy<T>`, static field, DI `AddSingleton()` |
| [02_PaymentGatewayFactory.cs](02_FactoryMethod/02_PaymentGatewayFactory.cs) | **Factory Method** | Dictionary registry, extensible without touching existing code |
| [03_PaymentProviderAbstractFactory.cs](03_AbstractFactory/03_PaymentProviderAbstractFactory.cs) | **Abstract Factory** | Domestic vs International payment families, family consistency |
| [04_PaymentRequestBuilder.cs](04_Builder/04_PaymentRequestBuilder.cs) | **Builder** | Fluent API, `Build()` validation, immutable `record`, Director class |
| [05_PaymentTemplatePrototype.cs](05_Prototype/05_PaymentTemplatePrototype.cs) | **Prototype** | `MemberwiseClone` vs deep clone, registry of master templates |

---

## 🔗 READMEs per Pattern

- [Singleton →](01_Singleton/README.md)
- [Factory Method →](02_FactoryMethod/README.md)
- [Abstract Factory →](03_AbstractFactory/README.md)
- [Builder →](04_Builder/README.md)
- [Prototype →](05_Prototype/README.md)
