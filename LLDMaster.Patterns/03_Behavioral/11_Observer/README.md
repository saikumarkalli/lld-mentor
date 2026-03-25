# 11 — Observer Pattern

> **In one line**: When one thing changes, automatically notify all interested parties — without the publisher knowing who they are.

---

## 🧠 What Is It?

Observer defines a **one-to-many dependency**: when the subject (publisher) changes state, all registered observers (subscribers) are notified automatically. The publisher doesn't know — or care — who is listening.

---

## 🎯 The Problem It Solves

When a payment succeeds, 4 systems must react:
- **Warehouse**: trigger order picking and packing
- **Email**: send payment receipt
- **Analytics**: record revenue metrics
- **Loyalty**: award points

**Without Observer**: `PaymentService` directly calls all 4:
```csharp
// ❌ PaymentService is tightly coupled to all downstream systems
public void ProcessPayment(string orderId, decimal amount) {
    // ... charge gateway ...
    warehouseService.TriggerPickPack(orderId);  // coupled
    emailService.SendReceipt(email, orderId);   // coupled
    analyticsService.RecordRevenue(amount);     // coupled
    loyaltyService.AwardPoints(customerId);     // coupled
}
// Adding "fraud analytics" → edit PaymentService. WRONG.
```

**With Observer**: `PaymentService` fires one event. Each system subscribes independently:
```csharp
// ✅ PaymentService knows NOTHING about Warehouse/Email/Analytics/Loyalty
await NotifyAllAsync(new PaymentProcessedEvent(orderId, amount, ...));
// Adding "FraudAnalytics" → create FraudAnalyticsObserver, subscribe it. PaymentService untouched.
```

---

## 💡 Analogy

**YouTube subscriptions**. When a creator uploads a video, ALL subscribers get a notification. The creator doesn't call each subscriber personally. Subscribers subscribe/unsubscribe whenever they want. The creator doesn't even know how many subscribers they have while uploading.

---

## 📐 Structure

```
PaymentService (Subject)
  + Subscribe(IPaymentObserver)
  + Unsubscribe(IPaymentObserver)
  + ProcessAsync()
       └──fires──▶ PaymentProcessedEvent
                        ├──▶ WarehouseObserver.OnPaymentProcessedAsync()
                        ├──▶ EmailObserver.OnPaymentProcessedAsync()
                        ├──▶ AnalyticsObserver.OnPaymentProcessedAsync()
                        └──▶ LoyaltyObserver.OnPaymentProcessedAsync()

«interface»
IPaymentObserver
  + Name: string
  + OnPaymentProcessedAsync(event, ct): Task
```

---

## ✅ When to Use

- **One event triggers multiple independent reactions**
- Publishers should be **decoupled** from subscribers
- Subscribers can be **added/removed at runtime** (enable/disable features)
- **Fan-out** patterns: one input → many outputs

## ❌ When NOT to Use

- The chain has **strict ordering dependencies** between subscribers — use a Saga/Pipeline
- Only **one subscriber** ever exists — just call it directly
- Reactions need to **affect the outcome** of the original operation — use Chain of Responsibility or Middleware instead
- High-throughput hot path — subscriber notification overhead matters

---

## 🔑 Top Interview Questions

### Q1: "How is Observer different from C# events?"

C# `event` is the **language feature** implementing the Observer **pattern**:
```csharp
// C# event = Observer under the hood
public event EventHandler<PaymentProcessedEvent>? PaymentProcessed;
// Subscribe:   paymentService.PaymentProcessed += OnPaymentProcessed;
// Unsubscribe: paymentService.PaymentProcessed -= OnPaymentProcessed;
// Notify:      PaymentProcessed?.Invoke(this, eventArgs);
```

The GoF Observer uses an explicit interface (`IPaymentObserver`). C# events use delegates. Both implement the same concept; the interface approach gives you more control (async, error isolation per subscriber).

### Q2: "What's the memory leak risk with Observer?"

When an observer **subscribes** to a subject, the subject holds a **reference** to the observer. If you never call `Unsubscribe()`, the subject prevents the observer from being garbage collected — even after the observer is "done."

```csharp
// ❌ Memory leak if this page/component is disposed but never unsubscribed
paymentService.Subscribe(analyticsObserver); // subject holds reference to observer

// ✅ Always unsubscribe when the observer is no longer needed
paymentService.Unsubscribe(analyticsObserver);
```

### Q3: "How does MediatR implement Observer?"

MediatR's `INotification` + `INotificationHandler<T>` is the Observer pattern via DI:
```csharp
// "Subject" publishes
await mediator.Publish(new PaymentProcessedEvent(orderId, amount));

// Each "Observer" is a separate handler class
public class WarehouseHandler : INotificationHandler<PaymentProcessedEvent> { ... }
public class EmailHandler    : INotificationHandler<PaymentProcessedEvent> { ... }
// Add new handler → register in DI. PaymentService untouched. ✅
```

### Q4: "In-process Observer vs Message Bus — when do you choose each?"

| | In-Process Observer | Message Bus (RabbitMQ, Azure SB) |
|---|---|---|
| Speed | Fast (same process) | Slower (network hop) |
| Reliability | If process crashes → notifications lost | Durable — messages survive crashes |
| Scale | Single process/pod | Multiple services, microservices |
| Transaction | Same DB transaction possible | At-least-once, idempotency needed |
| Example | MediatR notifications | Azure Service Bus, RabbitMQ topics |

Senior answer: *"For in-process fan-out where we can tolerate message loss on crash, I use MediatR. For cross-service events or where durability matters, I use Azure Service Bus with a 'payment.processed' topic."*

---

## 🆚 Common Confusions

**Observer vs Mediator**:
- Observer: subject knows it HAS subscribers; publisher and subscriber are directly linked (even if abstract)
- Mediator: no component knows about any other component; mediator is the central hub that coordinates

**Observer vs Event Bus**: Observer is in-process. Event Bus is often cross-process (message broker). They're the same concept at different scales.

---

## 📂 File in This Folder

**[11_PaymentObserver.cs](11_PaymentObserver.cs)** — Contains:
- `PaymentProcessedEvent` — immutable `record` event data
- `IPaymentObserver` — async interface
- `PaymentService` — subject with Subscribe/Unsubscribe + **parallel** async notification
- `WarehouseObserver`, `EmailNotificationObserver`, `AnalyticsObserver`, `LoyaltyObserver`
- Error isolation per observer — one failed observer never breaks payment
- Runtime unsubscribe demo (maintenance window scenario)

---

## 💼 FAANG Interview Tip

Show the **evolution** of Observer at scale:

> *"For in-process fan-out I'd use MediatR — `INotification` + `INotificationHandler<T>`. It's Observer via DI, easy to test, easy to add subscribers. When we need durability (payment events that must not be lost on process restart) or cross-service fan-out (warehouse and email run as separate microservices), I'd publish to Azure Service Bus with a 'payment.processed' topic. Each service subscribes to its own queue. The pattern is the same — the implementation scales."*
