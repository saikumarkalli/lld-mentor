# 15 — Template Method Pattern

> **In one line**: Define the algorithm's skeleton in a base class; let subclasses fill in the variable steps without changing the order.

---

## 🧠 What Is It?

Template Method defines a **fixed algorithm structure** in a base class method. Specific steps are declared `abstract` — subclasses MUST override them. Optional steps are `virtual` (hooks). The skeleton method itself is NOT virtual — subclasses cannot change the ORDER of steps.

---

## 🎯 The Problem It Solves

Every payment report has the same pipeline:
1. `FetchData()` — query the DB
2. `FilterData()` — apply date/status filters
3. `AggregateMetrics()` — sum, count, group
4. `FormatReport()` — CSV / JSON / PDF
5. `DeliverReport()` — email + S3 upload

**Steps 1 and 5 are IDENTICAL** for every report. Only 2, 3, 4 vary by report type.

**Without Template Method** — copy-paste `DeliverReport()` in every class:
```csharp
class DailyReport   { void Generate() { ...; DeliverReport(); } } // copy
class MonthlyReport { void Generate() { ...; DeliverReport(); } } // paste
class ReconReport   { void Generate() { ...; DeliverReport(); } } // paste again
// Change email from BCC → CC → edit 3 classes. Classic DRY violation.
```

**With Template Method** — base class owns the skeleton:
```csharp
abstract class PaymentReportGenerator
{
    public void Run()  // NOT virtual — skeleton is fixed
    {
        var raw = FetchData();          // abstract → subclass implements
        var filtered = FilterData(raw); // abstract → subclass implements
        var data = AggregateMetrics(filtered);  // abstract
        var report = FormatReport(data);        // abstract
        OnBeforeDeliver(data);          // virtual HOOK — optional override
        DeliverReport(report, data);    // PRIVATE — never overrideable
    }

    protected abstract List<Transaction> FetchData();        // MUST override
    protected abstract string FormatReport(ReportData data); // MUST override
    protected virtual void OnBeforeDeliver(ReportData data) { } // CAN override
    private void DeliverReport(...) { /* email + S3 — same forever */ }
}
```

Change the delivery mechanism once → all 3 (and future) reports automatically get it.

---

## 💡 Analogy

A **cooking show format**. Every episode has the same structure: intro → ingredients → cooking steps → plating → taste test. The host (base class) owns this format — it NEVER changes. Each episode (subclass) overrides "ingredients" and "cooking steps" — but you always end with a taste test. The show structure (template) is fixed; the content (steps) varies.

---

## 📐 Structure

```
PaymentReportGenerator (abstract base)
  ├── Run()              ← NON-VIRTUAL — fixed skeleton, subclasses CANNOT override
  │    ├── FetchData()         ← abstract: MUST override
  │    ├── FilterData()        ← abstract: MUST override
  │    ├── AggregateMetrics()  ← abstract: MUST override
  │    ├── FormatReport()      ← abstract: MUST override
  │    ├── OnBeforeDeliver()   ← virtual HOOK: CAN override (default = no-op)
  │    └── DeliverReport()     ← private: NEVER overrideable
  │
  ├── DailyTransactionReport    → FetchData: today | Format: CSV
  ├── MonthlyRevenueReport      → FetchData: month | Format: JSON | OnBeforeDeliver: watermark
  └── ReconciliationReport      → FetchData: all | Filter: FAILED+PENDING | Format: investigation list
```

---

## ✅ When to Use

- Multiple classes share the **same sequence of steps** but with **different implementations** per step
- You want to prevent subclasses from changing the **order** of algorithm steps
- **Optional steps** (hooks) that some subclasses need but others can ignore
- Common setup/teardown logic that ALL subclasses should share

## ❌ When NOT to Use

- Only one implementation — just write it directly
- Algorithm varies **completely** between subclasses — forced shared skeleton becomes artificial
- You need **runtime algorithm swapping** — use Strategy instead (Template Method is fixed at compile time)
- Many subclasses with slightly different skeletons — inheritance hierarchy becomes hard to navigate

---

## 🔑 Top Interview Questions

### Q1: "What is a 'hook' in Template Method?"

A hook is a `virtual` method with a **default no-op (empty) implementation**. Subclasses CAN override it but don't have to:

```csharp
// Hook — optional step. Default: does nothing.
protected virtual void OnBeforeDeliver(ReportData data) { }

// MonthlyRevenueReport overrides it:
protected override void OnBeforeDeliver(ReportData data)
    => Console.WriteLine($"Adding CONFIDENTIAL watermark to {data.ReportName}");

// DailyTransactionReport doesn't override it → base no-op runs → no watermark
```

Hooks give subclasses **opt-in** extension points without forcing them to implement everything.

### Q2: "How is Template Method different from Strategy?"

This is the most common Template Method interview question:

| | Template Method | Strategy |
|---|---|---|
| OOP mechanism | **Inheritance** (IS-A) | **Composition** (HAS-A) |
| What varies | **Steps** within a fixed skeleton | The **whole algorithm** |
| Runtime swap? | ❌ No — type chosen at instantiation | ✅ Yes — inject different strategy |
| GoF principle | Inheritance-based extension | Composition-based extension |
| Modern preference | Less preferred (tight coupling) | More preferred ("favour composition") |

**When to choose**: If the sequence of steps is truly fixed and you're building a family of related classes, Template Method. If you need runtime flexibility or the algorithms are unrelated, Strategy.

### Q3: "Why should the skeleton method (`Run()`) be non-virtual?"

Making `Run()` non-virtual (or `sealed` when overriding) **prevents subclasses from changing the algorithm order**. The whole point of Template Method is that the STRUCTURE is fixed; only specific steps are variable. If `Run()` were virtual, a subclass could override it and skip the `DeliverReport()` step — defeating the purpose.

```csharp
// Non-virtual = subclasses cannot override Run()
public void Run() { ... }  // intentionally non-virtual

// Or if the base is already abstract and you're in a mid-level subclass:
public sealed override void Run() { ... }  // explicitly prevent further overriding
```

### Q4: "Where is Template Method used in .NET?"

- `Stream.Read()` / `Stream.Write()` — abstract methods; `FileStream`, `NetworkStream`, `MemoryStream` implement them. The stream framework owns the buffering/positioning logic.
- `DbContext.OnModelCreating()` — EF Core calls this as a hook during model building.
- `HttpMessageHandler` — ASP.NET Core test double for `HttpClient`; overrides `SendAsync()`.
- `Controller.OnActionExecuting()` — ASP.NET Core filter hooks.

---

## 🆚 Common Confusions

**Template Method vs Abstract Class**: Every Template Method uses an abstract class, but not every abstract class uses Template Method. Template Method specifically means: one concrete method owns the algorithm skeleton and calls abstract/virtual methods as steps.

**Template Method vs Factory Method**: Factory Method is sometimes implemented inside a Template Method — the `FetchData()` step might use a Factory Method to get the right data source. They're composable.

---

## 📂 File in This Folder

**[15_PaymentReportGenerator.cs](15_PaymentReportGenerator.cs)** — Contains:
- `PaymentReportGenerator` — abstract base with `Run()` skeleton, 4 abstract steps, 1 virtual hook, 1 private `DeliverReport()`
- `DailyTransactionReport` — today's transactions, CSV format
- `MonthlyRevenueReport` — monthly JSON summary + `OnBeforeDeliver` watermark hook
- `ReconciliationReport` — failed/pending only, investigation format
- All 3 run the same `Run()` skeleton; only their overridden steps differ

---

## 💼 FAANG Interview Tip

The `abstract` vs `virtual` vs non-virtual distinction is what separates juniors from seniors in Template Method questions:

> *"I declare the skeleton method as non-virtual — the algorithm order is fixed and subclasses must not change it. Steps that MUST be implemented by every subclass are `abstract`. Steps that have a sensible default but CAN be overridden are `virtual` hooks — like `OnBeforeDeliver()` for adding a watermark on the CFO report. The delivery step is `private` — no subclass can touch it."*

Showing you know exactly which C# keyword maps to which Template Method role is a strong senior signal.
