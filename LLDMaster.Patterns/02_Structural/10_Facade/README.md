# 10 — Facade Pattern

> **In one line**: Provide one simple method that orchestrates a complex subsystem — callers don't need to know the details.

---

## 🧠 What Is It?

Facade provides a **simplified, unified interface** to a complex subsystem. It orchestrates calls to multiple subsystems on behalf of the caller. The subsystems still exist in full — the Facade just hides them behind a clean entry point.

---

## 🎯 The Problem It Solves

Placing an order involves 5 subsystems: Inventory, Payment, Invoice, Email, Loyalty.

**Without Facade** — every controller/handler duplicates the orchestration:
```csharp
// ❌ Controller doing 50 lines of plumbing — every checkout endpoint
var inStock = inventoryService.IsInStock(productId, qty);
if (!inStock) return BadRequest("Out of stock");

var charge = paymentService.Charge(amount, "INR", orderId, gatewayType);
if (!charge.Success) return BadRequest(charge.Message);

inventoryService.ReserveStock(productId, qty);
var invoiceNo = invoiceService.Generate(orderId, customerId, amount);
emailService.SendConfirmation(customerEmail, orderId, invoiceNo, amount);
var points = loyaltyService.AwardPoints(customerId, amount);
// + error handling for each step × every endpoint = chaos
```

**With Facade** — controller is 3 lines:
```csharp
// ✅ Controller: thin, readable, testable
var result = checkoutFacade.PlaceOrder(request);
return result.Success ? Ok(result) : BadRequest(result.Message);
```

All orchestration logic, error handling, and subsystem knowledge lives in ONE class: `CheckoutFacade`.

---

## 💡 Analogy

A **travel agent**. You say "I want to go to Paris for a week, budget £2000." They book flights, hotel, transfers, travel insurance. You don't call each airline, each hotel, each insurance company yourself. The travel agent (Facade) handles the entire subsystem. Add "restaurant reservations" to the service → you still call the travel agent once.

---

## 📐 Structure

```
«controller / caller»
    └──▶ CheckoutFacade.PlaceOrder(dto)    ← ONE entry point
              ├──▶ InventoryService.IsInStock()
              ├──▶ PaymentService.Charge()
              ├──▶ InventoryService.ReserveStock()
              ├──▶ InvoiceService.Generate()
              ├──▶ EmailService.SendConfirmation()
              └──▶ LoyaltyService.AwardPoints()

The subsystems STILL EXIST and can be used directly.
Facade is a convenience layer, not a replacement.
```

---

## ✅ When to Use

- **Simplifying a complex subsystem** for the most common use case
- Defining a **single entry point** to a workflow (checkout, refund, cancellation, onboarding)
- **Layered architecture**: the "Application Service" or "Use Case" layer is the Facade
- Reducing coupling between UI/API layer and domain logic

## ❌ When NOT to Use

- All callers need full, granular control over individual steps — don't hide them
- The "subsystem" is already just one service — Facade adds no value
- You'd be hiding important errors or skipping steps for some callers

---

## 🔑 Top Interview Questions

### Q1: "How is Facade different from Adapter?"

| | Facade | Adapter |
|---|---|---|
| Interfaces involved | **Many** complex interfaces | **One** incompatible interface |
| Purpose | Simplify | Translate/make compatible |
| Changes interface? | Creates a NEW simplified one | Translates existing to expected |

### Q2: "Does Facade replace the subsystem?"

**No** — and this is a common misconception. The subsystem classes still exist. A power user or integration test can still call `inventoryService.IsInStock()` directly. Facade just provides a simpler path for common callers.

### Q3: "Where is Facade in Clean Architecture / ASP.NET Core?"

The **Application Service Layer** (or Use Case / Interactor layer) IS the Facade pattern:
```
API Controller  →  Application Service (Facade)  →  Domain Services + Repositories
```
In .NET, classes like `OrderApplicationService`, `CheckoutUseCase`, or `PaymentOrchestrator` are Facades.

### Q4: "How is Facade different from Mediator?"

| | Facade | Mediator |
|---|---|---|
| Direction | Unidirectional (caller → subsystems) | Bidirectional (components ↔ mediator) |
| Coupling | Facade knows subsystems; callers don't | No component knows about another |
| Use case | Simplify external access | Decouple internal component communication |

MediatR's `IRequest` + `IRequestHandler` is the Mediator pattern — not Facade. But the Application Service that uses MediatR internally can be a Facade from the controller's perspective.

### Q5: "What's the 'adding a step' advantage of Facade?"

Without Facade, adding "send analytics event" after payment = edit every controller that places orders.
With Facade, adding it = edit `CheckoutFacade.PlaceOrder()` in ONE place. All callers automatically get it.

---

## 🆚 Common Confusions

**Facade vs Service Layer in .NET**: They're essentially the same thing described differently. "Application Service" in DDD = "Facade" in GoF. The Facade pattern is the formal design pattern name for what .NET developers naturally call an "orchestration service" or "use case class."

**Facade vs God Object**: A God Object knows everything and does everything with no clear boundaries. A Facade has **a clear, limited API** (`PlaceOrder`, `CancelOrder`, `RefundOrder`) and delegates ALL real work to dedicated subsystem classes. The distinction is: does it DO the work, or does it COORDINATE others doing the work?

---

## 📂 File in This Folder

**[10_CheckoutFacade.cs](10_CheckoutFacade.cs)** — Contains:
- `InventoryService`, `PaymentService`, `InvoiceService`, `EmailService`, `LoyaltyService` — the subsystems
- `CheckoutFacade` — orchestrates all 5, handles failure at each step, produces `OrderConfirmation`
- `PlaceOrderRequest` + `OrderConfirmation` — clean DTOs in/out
- `CancelOrder()` — shows the refund flow through the same Facade
- ASP.NET Core controller example (3-line controller body)

---

## 💼 FAANG Interview Tip

In system design rounds, name the pattern explicitly: *"The checkout endpoint will call an Application Service — which is essentially the Facade pattern. The service orchestrates inventory reservation, payment charging, invoice generation, and notification. The controller stays thin. Adding a new step like 'analytics event' means changing only the Application Service."*

Connecting **GoF pattern name** to **Clean Architecture layers** shows senior-level thinking.
