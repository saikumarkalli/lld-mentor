# SOLID Principles — Payment Module

> **One idea: "Your code should be easy to change without breaking things."**
> Each SOLID principle attacks a different reason code becomes hard to change.

---

## 1. THE SOLID CONNECTION MAP

```
┌─────────────────────────────────────────────────────────────────┐
│         One idea: "Easy to change, hard to break"               │
├──────────┬──────────────────────────────────────────────────────┤
│ S — SRP  │ Hard to change because ONE class does TOO MUCH       │
│ O — OCP  │ Hard to change because adding features BREAKS code   │
│ L — LSP  │ Hard to change because SWAPPING implementations fails│
│ I — ISP  │ Hard to change because interfaces FORCE unused code  │
│ D — DIP  │ Hard to change because classes are HARDWIRED to impls│
└──────────┴──────────────────────────────────────────────────────┘
```

---

## 2. THE OOP → SOLID EVOLUTION TABLE

| OOP Concept       | SOLID Principle | What It Adds                                          |
|-------------------|-----------------|-------------------------------------------------------|
| Encapsulation     | SRP             | Limits **what** a class owns (one reason to change)   |
| Abstraction       | DIP + ISP       | Defines **how** interfaces are owned and sized        |
| Polymorphism      | OCP + LSP       | Defines the **rules** for safe substitution           |
| Composition       | DIP             | Formalises the **direction** of dependency injection  |

Your OOP code was SOLID in disguise. These principles name the instincts you built.

---

## 3. FILES IN THIS MODULE

| File | Principle | OOP Class Evolved |
|------|-----------|-------------------|
| `00_Overview.cs`              | All 5 (mental map)       | —                                |
| `01_S_SingleResponsibility.cs`| Single Responsibility    | `GoodPaymentService_WithDIP`     |
| `02_O_OpenClosed.cs`          | Open/Closed              | `PaymentProcessor.Process()`     |
| `03_L_LiskovSubstitution.cs`  | Liskov Substitution      | `PaymentBase` hierarchy          |
| `04_I_InterfaceSegregation.cs`| Interface Segregation    | `IPaymentService` (fat)          |
| `05_D_DependencyInversion.cs` | Dependency Inversion     | `GoodPaymentService_WithDIP`     |

---

## 4. QUICK RECALL CARD

```
S — "One reason to change"
     Count the reasons a class could change. If > 1, extract a class.

O — "New file, not an edit"
     Adding a feature should create a new .cs file, not modify an existing one.

L — "Swap safely, caller unaffected"
     Subclasses must keep every promise the parent made. No new throws, no null returns.

I — "Every method used by every implementor"
     If any implementor throws NotImplementedException, the interface is too fat. Split it.

D — "Zero `new` for services in business logic"
     `new ConcreteService()` inside a class = DIP violation + untestable.
```

---

## 5. PR REVIEW MASTER CHECKLIST

Paste this into your PR template or keep it open during reviews.

### S — Single Responsibility
- ✅ Class name is a single noun with no AND
- ✅ Class has one cohesive group of methods
- 🚨 Method names span different domains (ProcessPayment + SendEmail in one class)
- 🚨 Class has 5+ injected dependencies
- 🚨 Private helpers that belong to a different domain
- 🚨 Multiple `using` directives for unrelated infrastructure in one file

### O — Open/Closed
- ✅ Adding a new payment type creates a new file — nothing else modified
- ✅ Core processing method has no if/switch on payment type
- 🚨 `switch(paymentType)` or `if(type == "x")` in a processing method
- 🚨 PR description says "I just need to add one more else-if"
- 🚨 Existing tests break when a new feature is added

### L — Liskov Substitution
- ✅ Every override returns the same type and honours the same exception contract
- ✅ Swapping any implementation keeps all unit test assertions green
- 🚨 Override throws a new exception type the parent never declared
- 🚨 Override returns null when parent guarantees non-null
- 🚨 Caller adds `if (x is SubClass) skip` to work around a subclass
- 🚨 `NotImplementedException` in an override

### I — Interface Segregation
- ✅ Every method in the interface is genuinely used by every implementor
- ✅ Interface has 2–4 methods, all in the same domain
- 🚨 `NotImplementedException` in any interface implementation
- 🚨 Interface name contains AND (IPaymentAndFraudService)
- 🚨 A class implements an interface but uses fewer than 50% of its methods
- 🚨 "I'll just add this method to IPaymentService" — check all implementors first

### D — Dependency Inversion
- ✅ Constructor parameters are all interfaces (IRepository, IGateway, ISender)
- ✅ Zero `new ConcreteService()` inside business logic classes
- ✅ Business logic class can be instantiated in a test with only fake dependencies
- 🚨 `new` applied to a service/repo/gateway inside a class body
- 🚨 Constructor parameter is a concrete class (SqlRepository, TwilioSender)
- 🚨 `using Stripe;` or `using SqlClient;` inside a business logic file
- 🚨 Static method calls: `PaymentLogger.Log()` — cannot be injected, cannot be faked

---

## 6. THE CHAIN OF REASONING

```
SRP   → "Extract responsibilities into focused classes"
          ↓ creates the need for
OCP   → "Each class should be extendable without modification"
          ↓ creates the need for
LSP   → "Swapped implementations must honour the original contract"
          ↓ creates the need for
ISP   → "Interfaces must be small enough that no implementor fakes a method"
          ↓ creates the need for
DIP   → "Focused interfaces must be injected, never hardwired"
          ↓ closes the loop back to
SRP   → "Each class has one job and zero hardwired infrastructure"
```

---

## 7. WHAT'S NEXT — DESIGN PATTERNS (Phase 3)

Design Patterns are SOLID principles in named, reusable forms.
You've already seen them — you just haven't named them yet:

| Pattern you used | Where | SOLID principle behind it |
|-----------------|-------|--------------------------|
| **Strategy**    | `IPaymentStrategy` in OCP file | OCP + DIP |
| **Repository**  | `ITransactionRepository` in DIP file | DIP + ISP |
| **Factory**     | Coming in Phase 3 | OCP + DIP |
| **Decorator**   | Coming in Phase 3 | OCP + SRP |
| **Observer**    | Coming in Phase 3 | OCP + DIP |

You're not starting from zero in Phase 3. You're naming what you already know.
