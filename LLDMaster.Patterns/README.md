# 🏦 LLD Mastery — Design Patterns (Payment Domain)

> **Goal**: Master all 15 GoF Design Patterns through a real-world payment processing system — interview-ready in C# 12 / .NET 10.

---

## 📖 What Is This Repo?

This is a hands-on, interview-focused study of all **15 Gang of Four (GoF) Design Patterns** implemented in **C# 12 / .NET 10** using an **e-commerce payment system** as the domain.

Every pattern file follows a consistent 4-section structure:
1. **WHY** — the real production problem it solves
2. **PROBLEM** — broken naive code showing the pain
3. **PATTERN** — clean implementation with C# best-practice notes
4. **DEMO** — runnable code wired to `Program.cs`

---

## 🎯 Why Design Patterns Matter in Interviews

The GoF book (Gang of Four, 1994) catalogued 23 recurring object-oriented design solutions. FAANG and senior product company interviews ask about them because they're testing:

- **OOP thinking** — do you think in abstractions, interfaces, and composition?
- **Trade-off reasoning** — when *not* to use a pattern is as important as when to use it
- **Communication** — can you explain complex ideas simply? (patterns give you a shared vocabulary)
- **Real-world application** — can you map a textbook pattern to a real system?

> 💡 **Key insight**: Interviewers don't ask "what is Singleton?" They ask "how would you design a payment session manager that's thread-safe and testable?" — your job is to recognise the pattern and apply it.

---

## 🗺️ The 15 Patterns at a Glance

| # | Pattern | Category | Payment Domain Example | One-Line Purpose |
|---|---------|----------|------------------------|-----------------|
| 01 | [Singleton](01_Creational/01_Singleton/) | Creational | `PaymentConfigManager` | One shared instance, global access |
| 02 | [Factory Method](01_Creational/02_FactoryMethod/) | Creational | `PaymentGatewayFactory` | Delegate object creation, stay Open/Closed |
| 03 | [Abstract Factory](01_Creational/03_AbstractFactory/) | Creational | `PaymentProviderFactory` | Create families of related objects together |
| 04 | [Builder](01_Creational/04_Builder/) | Creational | `PaymentRequestBuilder` | Build complex objects step-by-step, fluently |
| 05 | [Prototype](01_Creational/05_Prototype/) | Creational | `RecurringPaymentTemplate` | Clone instead of rebuild from scratch |
| 06 | [Adapter](02_Structural/06_Adapter/) | Structural | `LegacyBankPaymentAdapter` | Make incompatible interfaces work together |
| 07 | [Bridge](02_Structural/07_Bridge/) | Structural | `PaymentProcessor × Notification` | Decouple two independently varying dimensions |
| 08 | [Composite](02_Structural/08_Composite/) | Structural | `PaymentBundle` (split pay) | Treat single items and groups uniformly |
| 09 | [Decorator](02_Structural/09_Decorator/) | Structural | Logging + Fraud + Retry pipeline | Add behaviour dynamically without subclassing |
| 10 | [Facade](02_Structural/10_Facade/) | Structural | `CheckoutFacade` | One simple interface over a complex subsystem |
| 11 | [Observer](03_Behavioral/11_Observer/) | Behavioral | `PaymentProcessed` event | Notify many subscribers when state changes |
| 12 | [Strategy](03_Behavioral/12_Strategy/) | Behavioral | `PaymentFeeCalculator` | Swap algorithms at runtime |
| 13 | [Command](03_Behavioral/13_Command/) | Behavioral | `ProcessPaymentCommand + Undo` | Encapsulate request as object; support undo/queue |
| 14 | [Iterator](03_Behavioral/14_Iterator/) | Behavioral | `TransactionHistory` paging | Traverse collection without exposing internals |
| 15 | [Template Method](03_Behavioral/15_TemplateMethod/) | Behavioral | `PaymentReportGenerator` | Fixed algorithm skeleton, variable steps |

---

## 📂 Folder Structure

```
LLDMaster.Patterns/
├── README.md                                  ← you are here
├── Program.cs                                 ← runs all demos
├── 01_Creational/
│   ├── README.md                              ← category overview + comparison table
│   ├── 01_Singleton/
│   │   ├── README.md                          ← interview prep for this pattern
│   │   └── 01_PaymentConfigManager.cs
│   ├── 02_FactoryMethod/
│   │   ├── README.md
│   │   └── 02_PaymentGatewayFactory.cs
│   ├── 03_AbstractFactory/
│   │   ├── README.md
│   │   └── 03_PaymentProviderAbstractFactory.cs
│   ├── 04_Builder/
│   │   ├── README.md
│   │   └── 04_PaymentRequestBuilder.cs
│   └── 05_Prototype/
│       ├── README.md
│       └── 05_PaymentTemplatePrototype.cs
├── 02_Structural/
│   ├── README.md
│   ├── 06_Adapter/    → 06_LegacyPaymentAdapter.cs
│   ├── 07_Bridge/     → 07_PaymentNotificationBridge.cs
│   ├── 08_Composite/  → 08_PaymentBundleComposite.cs
│   ├── 09_Decorator/  → 09_PaymentProcessorDecorator.cs
│   └── 10_Facade/     → 10_CheckoutFacade.cs
└── 03_Behavioral/
    ├── README.md
    ├── 11_Observer/       → 11_PaymentObserver.cs
    ├── 12_Strategy/       → 12_PaymentFeeStrategy.cs
    ├── 13_Command/        → 13_PaymentCommand.cs
    ├── 14_Iterator/       → 14_TransactionIterator.cs
    └── 15_TemplateMethod/ → 15_PaymentReportGenerator.cs
```

---

## 🧭 How to Study This Repo

### Recommended Order
1. **Creational** (01–05) — start here, easiest to visualise
2. **Structural** (06–10) — about how classes relate; Decorator is the hardest
3. **Behavioral** (11–15) — most asked in FAANG; Observer + Strategy are must-knows

### How to Read Each File
Every `.cs` file has 4 clear sections marked with comments:
```
// SECTION 1 — WHY THIS PATTERN / WHEN TO USE
// SECTION 2 — THE PROBLEM (naive broken code)
// SECTION 3 — THE PATTERN (correct implementation)
// SECTION 4 — DEMO (runnable via Program.cs)
```

Read the **PROBLEM section first** — understanding the pain is more important than memorising the solution.

### How to Run
```bash
cd LLDMaster.Patterns
dotnet run
```

---

## ⚡ Interview Quick-Reference

| Pattern | Interviewer asks… | Classic follow-up |
|---------|-------------------|-------------------|
| Singleton | "How do you ensure one instance in a multi-threaded app?" | "How do you test a Singleton?" |
| Factory Method | "How do you add a new payment gateway without touching existing code?" | "What's the difference from Abstract Factory?" |
| Abstract Factory | "How do you support domestic and international payment families?" | "What's the cost of adding a new product?" |
| Builder | "How do you handle a 12-field object with mostly optional fields?" | "Where do you put validation?" |
| Prototype | "What's deep copy vs shallow copy?" | "When does MemberwiseClone cause bugs?" |
| Adapter | "How do you integrate a legacy XML bank API?" | "Object Adapter vs Class Adapter?" |
| Bridge | "How do you avoid class explosion with 3 types × 4 channels?" | "Bridge vs Strategy?" |
| Composite | "How do you model split payments (wallet + card)?" | "How is it different from a plain List?" |
| Decorator | "How do you add logging/retry to a payment processor without modifying it?" | "How do you register decorators in ASP.NET Core DI?" |
| Facade | "How do you keep your controller thin when checkout needs 5 services?" | "Facade vs Mediator?" |
| Observer | "How do you notify multiple systems when payment succeeds?" | "In-process vs message bus?" |
| Strategy | "How do you handle different fee structures per merchant tier?" | "Strategy vs Template Method?" |
| Command | "How do you implement refund as undo of a payment?" | "How does this relate to Saga pattern?" |
| Iterator | "How do you paginate 10,000 transactions without loading all into memory?" | "IEnumerable vs IQueryable?" |
| Template Method | "Your 3 report types share 80% logic — how do you structure them?" | "Template Method vs Strategy?" |

---

## 💼 5 FAANG Interview Tips

1. **State the problem first, then the pattern.** "The problem here is X. I'd use Y pattern because..." — never lead with the pattern name.

2. **Always mention trade-offs.** Every pattern has a cost. Singleton = hard to test. Decorator = deep nesting. Show you know both sides.

3. **Connect to SOLID principles.** Factory Method → Open/Closed. Strategy → Open/Closed + Single Responsibility. Decorator → Single Responsibility. Interviewers love SOLID connections.

4. **Mention the .NET ecosystem equivalent.** DI `AddSingleton()` = Singleton. Middleware = Decorator. MediatR = Command + Observer. `IEnumerable` = Iterator. This shows you don't just know patterns in isolation.

5. **Draw a box diagram.** In system design rounds, sketching the pattern structure (even ASCII art) while talking is a strong signal. It shows you think visually and communicate clearly.

---

## 🚀 Quick Start

```bash
# Clone and run
git clone <repo-url>
cd lld-mastery/LLDMaster.Patterns
dotnet run

# Run a specific pattern demo by commenting/uncommenting in Program.cs
```

**Tech stack**: C# 12 / .NET 10 | Primary constructors | Records | Async/await | XML docs
