# 12 — Strategy Pattern

> **In one line**: Encapsulate interchangeable algorithms behind a common interface and inject the one you need at runtime.

---

## 🧠 What Is It?

Strategy defines a **family of algorithms**, encapsulates each one in its own class, and makes them interchangeable. The client (context) holds a reference to a strategy and calls it — it never knows which concrete algorithm is running.

---

## 🎯 The Problem It Solves

Payment fee calculation varies by merchant tier:
- **Startup**: free up to ₹1L/month, then 2%
- **Growth**: 1.5% flat
- **Enterprise**: tiered (2% for small, 1.5% medium, 1% large)
- **Corporate**: flat ₹10 per transaction

**Without Strategy** — one method with a `switch`:
```csharp
// ❌ Add "CryptoGateway tier" → edit this method. Add "NGO tier" → edit again.
public decimal Calculate(string tier, decimal amount) => tier switch {
    "startup"    => amount <= 100_000m ? 0m : amount * 0.02m,
    "growth"     => amount * 0.015m,
    "enterprise" => amount switch { ... },
    "corporate"  => 10m,
    _            => throw new NotSupportedException(tier),
};
// Unit testing enterprise logic = instantiate whole calculator
```

**With Strategy** — each rule is its own testable class:
```csharp
// ✅ Add new tier → add new class. Zero changes to PaymentFeeCalculator.
var calculator = new PaymentFeeCalculator(
    merchant.Tier switch {
        "startup"    => new FreeTierFeeStrategy(100_000m, 0.02m),
        "enterprise" => new TieredFeeStrategy(),
        _            => new PercentageFeeStrategy(0.015m),
    });
calculator.CalculateTotal(orderAmount); // strategy decides algorithm
```

---

## 💡 Analogy

**GPS navigation**. You enter "directions to airport." The app asks: fastest route, avoid tolls, or scenic route? You pick one — the **strategy**. The GPS (context) uses whatever route-finding algorithm you chose. Switching from "avoid tolls" to "fastest" doesn't change the GPS — it just swaps the algorithm.

---

## 📐 Structure

```
«interface»
IPaymentFeeStrategy
  + Name: string
  + Calculate(amount, currency): FeeResult

FlatFeeStrategy(₹10)                  ← corporate tier
PercentageFeeStrategy(1.5%)           ← growth tier
FlatPlusPercentageFeeStrategy(₹25+1.5%)  ← PayPal-style
FreeTierFeeStrategy(≤₹1L free, 2%)    ← startup tier
TieredFeeStrategy(2%/1.5%/1%)         ← enterprise tier

PaymentFeeCalculator (Context)
  - _strategy: IPaymentFeeStrategy    ← injected, can be swapped
  + SetStrategy(strategy)             ← runtime swap
  + CalculateTotal(amount, currency)  ← delegates to strategy
```

---

## ✅ When to Use

- **Multiple variations of the same algorithm** (fee rules, shipping costs, discount calculations)
- Algorithm must be **selected/switched at runtime** (by config, user preference, merchant tier)
- **Eliminate conditionals** — replace `if/switch` chains with polymorphism
- Each variation needs to be **independently unit-tested**

## ❌ When NOT to Use

- Only ONE algorithm exists and no variation is planned — just write it inline (YAGNI)
- Algorithms share so much state that isolating them into separate classes is artificial
- The "algorithms" are trivially different (e.g., just different constants) — use a config value instead

---

## 🔑 Top Interview Questions

### Q1: "How is Strategy different from a plain if/switch?"

| | if/switch | Strategy |
|---|---|---|
| Adding new rule | Edit existing method | Add new class (Open/Closed ✅) |
| Unit testing | Test through the whole class | Test each strategy in isolation |
| Runtime swap | Conditional only | `calculator.SetStrategy(newStrategy)` |
| Readability | Gets messier with each case | Each rule is clear and self-contained |

### Q2: "How is Strategy different from Template Method?"

| | Strategy | Template Method |
|---|---|---|
| OOP mechanism | **Composition** (inject strategy) | **Inheritance** (override steps in subclass) |
| Granularity | Swaps **entire algorithm** | Swaps **individual steps** within fixed skeleton |
| Runtime swap? | ✅ Yes | ❌ No — determined at instantiation |
| GoF principle | Favour composition | Favour abstraction |

**One-liner**: Strategy replaces the whole function. Template Method replaces parts of it.

### Q3: "How is Strategy different from Command?"

| | Strategy | Command |
|---|---|---|
| Purpose | Encapsulate an **algorithm** | Encapsulate a **request** (with undo/queue) |
| State | Usually **stateless** (pure calculation) | Usually **stateful** (knows what it did) |
| Undo? | ❌ Not a concern | ✅ Core feature |
| Example | `FeeStrategy.Calculate()` | `ProcessPaymentCommand.Execute/Undo()` |

### Q4: "Where is Strategy used in .NET itself?"

```csharp
// IComparer<T> — comparison strategy
Array.Sort(items, new ByAmountDescending()); // inject sort strategy

// StringComparer — comparison strategy
new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // inject compare strategy

// JsonSerializerOptions — serialisation strategy
JsonSerializer.Serialize(obj, new JsonSerializerOptions { ... });
```

---

## 🆚 Common Confusions

**Strategy vs State**:
- Strategy: context holds ONE strategy, chosen externally, stays the same during operation
- State: context's behaviour changes automatically as its internal state transitions — like a finite state machine

**Strategy vs Policy (in Clean Architecture)**: In Clean Architecture, "business rules" or "policies" are often Strategy implementations. The vocabulary differs; the pattern is the same.

---

## 📂 File in This Folder

**[12_PaymentFeeStrategy.cs](12_PaymentFeeStrategy.cs)** — Contains:
- `IPaymentFeeStrategy` + `FeeResult` record
- 5 concrete strategies: `FlatFeeStrategy`, `PercentageFeeStrategy`, `FlatPlusPercentageFeeStrategy`, `FreeTierFeeStrategy`, `TieredFeeStrategy`
- `PaymentFeeCalculator` — context with `SetStrategy()` for runtime swap
- Demo showing the same calculator switching between 5 strategies, with different amounts

---

## 💼 FAANG Interview Tip

Connect Strategy to **Open/Closed Principle** explicitly:

> *"The key advantage of Strategy over a switch statement is Open/Closed Principle — I can add a 'crypto merchant' fee tier by creating one new class `CryptoFeeStrategy` and registering it. Zero changes to `PaymentFeeCalculator`. With a switch, I'd edit the existing method and potentially break existing logic for other tiers. In a payment system where fee logic is audited, minimising changes to existing code is critical."*
