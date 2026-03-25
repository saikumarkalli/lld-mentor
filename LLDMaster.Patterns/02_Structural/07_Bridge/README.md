# 07 — Bridge Pattern

> **In one line**: Decouple two independently varying dimensions so adding one doesn't require multiplying the other.

---

## 🧠 What Is It?

Bridge separates an **abstraction** (WHAT you do) from its **implementation** (HOW you do it), holding the implementation as an **injected reference** (the "bridge"). Both sides can vary independently — adding a new abstraction doesn't require touching implementations, and vice versa.

---

## 🎯 The Problem It Solves

You have 3 payment types × 4 notification channels:

```
Payment types:       Notification channels:
OnlineCardPayment    Email
WalletPayment        SMS
EmiPayment           Push
                     WhatsApp
```

Without Bridge: **3 × 4 = 12 classes**:
- `OnlineCardPaymentWithEmail`
- `OnlineCardPaymentWithSMS`
- `OnlineCardPaymentWithPush`
- `OnlineCardPaymentWithWhatsApp`
- `WalletPaymentWithEmail` ... × 3
- `EmiPaymentWithEmail` ... × 3

Add a 4th payment type → 4 more classes. Add "Telegram" channel → 3 more classes. This is the **class explosion** problem.

With Bridge: **3 + 4 = 7 classes**. Compose at runtime:
```csharp
new OnlineCardPayment(new SmsNotification())    // compose freely
new WalletPayment(new PushNotification())       // any combination
new EmiPayment(new WhatsAppNotification(), 6)   // new channel = 0 changes to payment classes
```

---

## 💡 Analogy

A **TV remote and TV brand**. The remote (abstraction) has Volume Up, Volume Down, Channel buttons. Samsung has one internal implementation; LG has another. The remote works with ANY TV via the bridge (the HDMI/IR protocol). Add a new remote model → doesn't affect TV brands. Add a new TV brand → doesn't affect remote models.

---

## 📐 Structure

```
«abstraction»                          «implementor»
PaymentProcessor                       INotificationChannel
  + Process()                            + Send(recipient, subject, body)
  # Notifier: INotificationChannel ──►  EmailNotification
                                         SmsNotification
  ↑ refined abstractions:               PushNotification
  OnlineCardPayment(notifier)           WhatsAppNotification
  WalletPayment(notifier)
  EmiPayment(notifier, instalments)

// Compose at startup:
new OnlineCardPayment(new EmailNotification())
//           ↑                    ↑
//      abstraction            implementor
//      (the WHAT)             (the HOW)
```

---

## ✅ When to Use

- **Two independently varying dimensions** in your class hierarchy (payment type × channel)
- You want to **switch the implementation at runtime** (user's notification preference)
- Want to **share one implementation** across multiple abstractions (SMS used by all payment types)
- Avoiding the **M × N class explosion** problem

## ❌ When NOT to Use

- Only **one dimension** varies — just use inheritance or Strategy
- The two sides are **tightly coupled** and don't really vary independently
- **YAGNI** — if you have 1 payment type and 1 channel today, don't over-engineer

---

## 🔑 Top Interview Questions

### Q1: "How is Bridge different from Strategy?"

This is the most common Bridge interview question:

| | Bridge | Strategy |
|---|---|---|
| Pattern type | **Structural** | **Behavioral** |
| What it does | Separates two class hierarchies (structure) | Swaps one algorithm at runtime (behavior) |
| Dimensions | Two hierarchies vary independently | One algorithm family |
| Analogy | TV remote × TV brand | GPS: fastest/avoid-tolls/scenic route |

**One-liner**: Bridge separates two structural hierarchies. Strategy replaces one algorithm.

### Q2: "What is the 'class explosion' problem and how does Bridge solve it?"

```
Without Bridge: M × N classes (payment types × channels)
With Bridge:    M + N classes (each side independent)

Adding WhatsApp:
  Without Bridge: add 3 classes (OnlineWithWhatsApp, WalletWithWhatsApp, EmiWithWhatsApp)
  With Bridge:    add 1 class (WhatsAppNotification), done ✅
```

### Q3: "How do you identify when to use Bridge in a system design?"

Look for **two independently varying dimensions** that would otherwise cause a class hierarchy to multiply. In code review, if you see `XWithY`, `XWithZ`, `WWithY`, `WWithZ` — that's Bridge territory.

### Q4: "How does Bridge relate to Dependency Injection?"

The Bridge pattern IS essentially constructor injection of the implementor. Modern .NET DI does Bridge naturally:
```csharp
// DI selects the "bridge" (notification channel) based on user preference
INotificationChannel channel = user.Preference switch {
    "sms"   => sp.GetRequiredService<SmsNotification>(),
    "push"  => sp.GetRequiredService<PushNotification>(),
    _       => sp.GetRequiredService<EmailNotification>(),
};
var processor = new OnlineCardPayment(channel); // bridge injected
```

---

## 🆚 Common Confusions

**Bridge vs Strategy**: Strategy is about behaviour — swapping algorithms. Bridge is about structure — preventing class hierarchies from multiplying. Strategy replaces one "leg" of the calculation. Bridge connects two separate "worlds" (payment domain + notification domain).

**Bridge vs Adapter**: Adapter fixes incompatibility between existing interfaces. Bridge designs incompatibility AWAY upfront — it's a design choice, not a retrofit.

---

## 📂 File in This Folder

**[07_PaymentNotificationBridge.cs](07_PaymentNotificationBridge.cs)** — Contains:
- `INotificationChannel` — Email, SMS, Push, WhatsApp implementations
- `PaymentProcessor` abstract class — holds the bridge reference
- `OnlineCardPayment`, `WalletPayment`, `EmiPayment` — refined abstractions
- Demo showing: adding `WhatsAppNotification` requires **zero** changes to payment classes

---

## 💼 FAANG Interview Tip

**Draw the 2D grid first**. Before explaining Bridge, sketch:

```
           | Email | SMS | Push | WhatsApp
-----------|-------|-----|------|----------
OnlineCard |       |     |      |
Wallet     |       |     |      |
EMI        |       |     |      |
```

Say: *"Without Bridge, every cell is a class — 12 classes. Bridge collapses the rows into 3 classes and columns into 4 classes, then composes them at runtime."* Making the interviewer SEE the explosion — then showing how Bridge collapses it — is a memorable, high-impact interview move.
