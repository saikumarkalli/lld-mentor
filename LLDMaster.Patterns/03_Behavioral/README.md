# 🧠 Behavioral Patterns — Communication & Responsibility

> **Core question these patterns answer**: "How do objects communicate, and who is responsible for what?"

---

## What Are Behavioral Patterns?

Behavioral patterns define **how objects interact** — who talks to whom, how requests are passed, how algorithms are selected, and how state changes propagate. These are the patterns most commonly asked in **FAANG and senior product company interviews** because they reflect real architectural decisions in distributed systems.

---

## The 5 Patterns at a Glance

| Pattern | Core Question | Payment Example | Key Mechanism |
|---------|---------------|----------------|---------------|
| **Observer** | Who needs to know when X happens? | PaymentProcessed → 4 systems react | Subscribe/Unsubscribe |
| **Strategy** | Which algorithm should run here? | Fee: flat/percentage/tiered by merchant | Inject interchangeable algorithm |
| **Command** | Can I undo or queue this operation? | ProcessPayment + Refund (undo) | Execute/Undo objects |
| **Iterator** | How do I traverse without knowing the storage? | Paginated transaction history | `yield return` / IEnumerator |
| **Template Method** | Steps are the same, details differ? | Daily/Monthly/Reconciliation reports | abstract + fixed skeleton |

---

## 🧠 The Core Insight

```
Observer:         subject → publishes event → many subscribers react (1:N)
Strategy:         context.Run() → delegates to injected algorithm (1:1, swappable)
Command:          operation → encapsulated as object → can be queued/undone
Iterator:         collection exposes cursor → caller never sees internals
Template Method:  base class owns order of steps → subclass fills in details
```

---

## 📊 Most Confused Pairs (Interview Gold)

### Strategy vs Template Method

| | Strategy | Template Method |
|---|---|---|
| Mechanism | **Composition** (inject algorithm) | **Inheritance** (override steps) |
| Granularity | Swaps the **whole** algorithm | Swaps **individual steps** |
| OOP principle | Prefer composition over inheritance | Inheritance-based |
| Runtime swap? | ✅ Yes (inject different strategy) | ❌ No (type set at compile time) |

### Observer vs C# Event

C# `event EventHandler` IS the Observer pattern as a language feature. The pattern is the concept; the language feature is one implementation.

### Command vs Strategy

- **Command** = "do this thing" — encapsulates a REQUEST with state, can undo, can queue
- **Strategy** = "calculate it this way" — encapsulates an ALGORITHM, usually stateless

### Observer vs MediatR

MediatR's `INotification` + `INotificationHandler<T>` = Observer pattern. The library is an implementation; the pattern is the architecture.

---

## 📂 Patterns in This Folder

| File | Pattern | What it demonstrates |
|------|---------|---------------------|
| [11_PaymentObserver.cs](11_Observer/11_PaymentObserver.cs) | **Observer** | PaymentProcessed event → Warehouse, Email, Analytics, Loyalty — parallel async notification |
| [12_PaymentFeeStrategy.cs](12_Strategy/12_PaymentFeeStrategy.cs) | **Strategy** | 5 fee strategies: flat, percentage, flat+%, free-tier, tiered — runtime swap |
| [13_PaymentCommand.cs](13_Command/13_PaymentCommand.cs) | **Command** | ProcessPaymentCommand + Undo (refund) + batch queue + Stack-based history |
| [14_TransactionIterator.cs](14_Iterator/14_TransactionIterator.cs) | **Iterator** | `yield return`, filtered/paginated iteration, custom `IEnumerator<T>` cursor |
| [15_PaymentReportGenerator.cs](15_TemplateMethod/15_PaymentReportGenerator.cs) | **Template Method** | Fixed pipeline skeleton, 3 report types, hooks for optional steps |

---

## 🔗 READMEs per Pattern

- [Observer →](11_Observer/README.md)
- [Strategy →](12_Strategy/README.md)
- [Command →](13_Command/README.md)
- [Iterator →](14_Iterator/README.md)
- [Template Method →](15_TemplateMethod/README.md)
