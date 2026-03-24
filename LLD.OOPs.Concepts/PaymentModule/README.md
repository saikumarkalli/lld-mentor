# Payment Module — OOP Fundamentals

> One domain. Seven concepts. Every file has a **BEFORE** (wrong) and **AFTER** (right).
> Run `dotnet run` from the project root to see all 7 demos in your terminal.

---

## Concept Map

| # | Concept | File | Mental Model | Violating this causes... |
|---|---------|------|--------------|--------------------------|
| 1 | **Encapsulation** | `01_Encapsulation.cs` | Bank vault — you don't reach in and grab cash, you go through the teller who enforces the rules | Invalid state silently accepted (`amount = -999`, `status = "Completed"` with no processing) |
| 2 | **Abstraction** | `02_Abstraction.cs` | Car dashboard — you press the accelerator, you don't rewire the fuel injector | Adding PayPal = copy-pasting 30 lines of HTTP code and introducing a second place to fix bugs |
| 3 | **Inheritance** | `03_Inheritance.cs` | Legal contract template — subclasses inherit the boilerplate and only fill in their unique clauses | A bug fix in shared logic must be applied in N places; someone always misses one |
| 4 | **Polymorphism** | `04_Polymorphism.cs` | Power strip — any plug that fits the socket just works, no adapter per device | Every new payment type forces you to open and edit the same `if/else` chain forever |
| 5 | **Composition vs Inheritance** | `05_CompositionVsInheritance.cs` | A car HAS-A engine; a car IS-NOT-AN engine — never inherit just to reuse code | PaymentService accidentally exposes Log() publicly; swapping loggers requires rewriting the class hierarchy |
| 6 | **Interface Segregation** | `06_InterfaceSegregation.cs` | Restaurant menu — don't hand the dessert menu to someone who only asked for drinks | Classes forced to implement 6 methods they don't need and throw `NotImplementedException` on all of them |
| 7 | **Dependency Inversion** | `07_DependencyInversion.cs` | Wall socket standard — your lamp doesn't care which power plant generated the electricity | You can't test `PaymentService` without a real Stripe key and a live SQL database |

---

## How to Read This Module

**Recommended study order:** files 1 → 7 in sequence. Each concept builds on the previous.

### What to focus on in each file

| File | Focus on this |
|------|---------------|
| `01_Encapsulation.cs` | How `private` fields + public methods create a *contract*. See how status transitions are enforced by the class itself. |
| `02_Abstraction.cs` | The `IPaymentGateway` interface. Notice how `PaymentService` has zero `using` statements for Stripe or HTTP. |
| `03_Inheritance.cs` | The `base.Validate()` call chain. Ask: "where is the bug-fix change made, and how many classes benefit?" |
| `04_Polymorphism.cs` | `ProcessAll()` — count how many `if` statements it contains. Then add a new payment type and count again. |
| `05_CompositionVsInheritance.cs` | The constructor signature of `PaymentService`. Compare what's public in the BEFORE vs AFTER. |
| `06_InterfaceSegregation.cs` | Count the `NotImplementedException` throws in the BEFORE. Then see how the AFTER eliminates every one. |
| `07_DependencyInversion.cs` | The `FakeGateway` class. This is how you write unit tests without touching a real database or API. |

### The "2-year test"
If you come back to this in 2 years and feel lost, read only **Section 4 (Demo)** of each file. The demo prints exactly what's happening as it runs.

---

## The 7 Mental Models

*Read this block in 2 minutes and rebuild the whole module in your head.*

```
1. ENCAPSULATION
   Bank vault. You go through the teller — the class — who enforces every rule.
   The cash (data) is never touched directly.

2. ABSTRACTION
   Car dashboard. You press accelerator; you don't rewire the fuel injector.
   Hide the HOW. Expose only the WHAT.

3. INHERITANCE
   Legal contract template. Subclasses inherit the boilerplate clauses
   and only fill in the parts unique to them.

4. POLYMORPHISM
   Power strip. Any plug that fits the socket just works.
   One call (payment.Execute()) — N behaviours — zero if/else.

5. COMPOSITION vs INHERITANCE
   A car HAS-A engine. A car IS-NOT-AN engine.
   If you can't say "[Child] IS-A [Parent]" with confidence, use composition.

6. INTERFACE SEGREGATION
   Restaurant menu split by course. You only hand the dessert menu
   to someone who actually wants dessert.

7. DEPENDENCY INVERSION
   Wall socket standard. Your lamp doesn't know which power plant
   generated the electricity — and it doesn't need to.
```

---

## Run the demos

```bash
cd LLD.OOPs.Concepts
dotnet run
```

> **Note on File 2 (Abstraction):** The demo makes a real HTTP call to Stripe to illustrate the concept.
> Without valid API credentials it will fail after 3 retries — that's expected and handled gracefully.
> The abstraction lesson (zero HTTP code in `PaymentService`) is already printed before the call.

---

## What's next

Once you're comfortable with all 7 files here, move to:

- `LLDMaster.SOLID/` — deep-dive into each SOLID principle with dedicated examples
- `LLDMaster.Patterns/` — Design patterns (Strategy, Factory, Observer, …) that *apply* these OOP concepts
- `LLDMaster.Problems/` — Full LLD interview problems (Parking Lot, BookMyShow, …) using everything above
