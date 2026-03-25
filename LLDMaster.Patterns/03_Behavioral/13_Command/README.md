# 13 — Command Pattern

> **In one line**: Turn an operation into an object so you can undo it, queue it, log it, or replay it.

---

## 🧠 What Is It?

Command encapsulates a **request as an object**. The object knows how to execute the operation AND how to undo it. Because it's an object, it can be: stored in a queue, serialised to a database, replayed on failure, undone in reverse order.

---

## 🎯 The Problem It Solves

A payment admin panel needs:
- **Execute**: charge a customer
- **Undo**: refund (undo the charge)
- **Queue**: batch 100 charges for midnight processing
- **Replay**: re-run yesterday's failed transactions from the audit log

With a plain method call, NONE of this is possible:
```csharp
// ❌ Once the method returns, the operation is gone
paymentService.Charge("CUST-01", 2500m);
// How do you refund this? Call a separate method manually — completely disconnected.
// How do you queue 100 charges for midnight? You can't.
// How do you replay it from the log? You can't.
```

Command makes every operation a **first-class object**:
```csharp
// ✅ Operation is an object — storable, undoable, queueable
var chargeCmd = new ProcessPaymentCommand(gateway, "CUST-01", 2500m);

await processor.ExecuteAsync(chargeCmd);      // runs it + pushes to history stack
await processor.UndoLastAsync();              // pops stack → calls chargeCmd.UndoAsync() = refund

processor.Enqueue(chargeCmd2);               // store for midnight batch
await processor.ExecuteQueueAsync();         // drain the queue
```

---

## 💡 Analogy

A **restaurant order slip**. The waiter writes your order on a slip of paper (Command object). The slip can be:
- Handed to the kitchen (Execute)
- Cancelled before cooking starts (Undo)
- Put on hold behind other slips (Queue)
- Given back to the kitchen if it was lost (Replay)

The waiter doesn't cook. The kitchen (Receiver) does the actual work. The slip contains all the information needed to cook the order independently.

---

## 📐 Structure

```
«interface»
IPaymentCommand
  + CommandId: string
  + Description: string
  + ExecuteAsync(ct): Task
  + UndoAsync(ct):    Task

ProcessPaymentCommand                 ApplyDiscountCommand
  - gateway: PaymentGateway            - gateway: PaymentGateway
  - customerId, amount, currency       - orderId, discountAmount
  + ExecuteAsync() → Charge()          + ExecuteAsync() → ApplyDiscount()
  + UndoAsync()    → Refund()          + UndoAsync()    → ReverseDiscount()
                                            ↑ Receiver does the real work

PaymentCommandProcessor (Invoker)
  - Stack<IPaymentCommand> _history     ← LIFO for undo
  - Queue<IPaymentCommand> _pending     ← for batch processing
  + ExecuteAsync(cmd)    → Execute + push history
  + UndoLastAsync()      → pop history → Undo
  + Enqueue(cmd)         → add to queue
  + ExecuteQueueAsync()  → drain queue
```

**The 4 roles in Command** (interviewers love asking this):
1. **Client** — creates the Command object with all necessary info
2. **Invoker** — calls `Execute()` on the Command (doesn't know what it does internally)
3. **Command** — encapsulates the request, knows Execute + Undo
4. **Receiver** — the object that actually does the work (`PaymentGateway.ChargeAsync()`)

---

## ✅ When to Use

- Need **undo/redo** functionality (refund is the undo of a payment)
- Need to **queue or schedule** operations for later execution
- Need an **audit log that can be replayed** (all commands stored + re-executable)
- **Saga pattern** — each distributed transaction step is a Command with a compensating Undo
- **Batch processing** — accumulate commands and run them in one transaction

## ❌ When NOT to Use

- Simple one-time operations with no undo, queue, or replay requirement — overhead not worth it
- **Performance-critical hot paths** — creating a new object per operation has overhead
- The "undo" is naturally trivial (`x++` → `x--`) — Command adds no value

---

## 🔑 Top Interview Questions

### Q1: "What are the 4 roles in Command pattern?"

```
Client:    var cmd = new ProcessPaymentCommand(gateway, "CUST-01", 2500m);
Invoker:   await processor.ExecuteAsync(cmd);    // calls cmd.Execute(), manages history
Command:   class ProcessPaymentCommand { Execute() → Charge. Undo() → Refund }
Receiver:  class PaymentGateway { ChargeAsync(), RefundAsync() }
```

### Q2: "How does Command support undo in a payment system?"

Stack-based LIFO undo:
```csharp
// Execute pushes to stack
await processor.ExecuteAsync(chargeCmd);    // stack: [chargeCmd]
await processor.ExecuteAsync(discountCmd);  // stack: [chargeCmd, discountCmd]

// Undo pops from stack (LIFO — last in, first undone)
await processor.UndoLastAsync(); // pops discountCmd → reverses discount
await processor.UndoLastAsync(); // pops chargeCmd   → refunds charge
```

Execute = `PaymentGateway.ChargeAsync()`. Undo = `PaymentGateway.RefundAsync()`. They're paired inside the Command object.

### Q3: "How does Command enable the Saga pattern?"

In distributed transactions, each saga step is a Command with a compensating Undo:
```
Steps:      [ReserveInventory] → [ChargePayment] → [SendEmail]
Failure at [SendEmail]:
Undo in reverse: [RefundPayment.Undo()] → [ReleaseInventory.Undo()]
```
The Saga orchestrator is the Invoker — it executes Commands forward and runs Undo in reverse on failure.

### Q4: "How is Command different from Strategy?"

| | Command | Strategy |
|---|---|---|
| Encapsulates | A **request** (what to DO) | An **algorithm** (how to CALCULATE) |
| State | ✅ Stateful (remembers transaction ID for undo) | ❌ Usually stateless |
| Undo | ✅ Core feature | ❌ Not a concept |
| Example | `ProcessPaymentCommand.Execute/Undo` | `FreeTierFeeStrategy.Calculate()` |

### Q5: "Where is Command used in .NET?"

- **MediatR `IRequest` + `IRequestHandler`** = Command pattern
- **EF Core change tracking** — each change is a "command" that can be committed or rolled back
- **UI frameworks** — `ICommand` in WPF/MAUI for button actions with CanExecute/Execute

---

## 🆚 Common Confusions

**Command vs Strategy vs Observer**:
- Observer: *something happened* → notify subscribers
- Command: *do this thing* (and maybe undo it)
- Strategy: *use this algorithm* to compute something

---

## 📂 File in This Folder

**[13_PaymentCommand.cs](13_PaymentCommand.cs)** — Contains:
- `IPaymentCommand` — async Execute/Undo interface
- `PaymentGateway` — Receiver with in-memory transaction store
- `ProcessPaymentCommand` — stores transaction ID after Execute for Undo
- `ApplyDiscountCommand` — second command showing different Receiver methods
- `PaymentCommandProcessor` — Stack history + Queue for batch
- Demo: Execute → Undo (LIFO) + batch queue for midnight processing

---

## 💼 FAANG Interview Tip

Connect Command to **MediatR** and **Saga pattern**:

> *"In .NET I implement Command using MediatR's `IRequest<T>` + `IRequestHandler<T>` — that's the Command pattern built into every .NET app. For distributed transactions, Command is the foundation of the Saga pattern — each step is a Command with a compensating transaction as its Undo. If step 3 fails, we execute Undo() on steps 2 and 1 in reverse order."*

This shows you think at system-design level, not just class-diagram level.
