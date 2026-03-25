# 08 — Composite Pattern

> **In one line**: Compose objects into trees and treat individual items and groups through the same interface.

---

## 🧠 What Is It?

Composite lets you treat a **single object** (leaf) and a **group of objects** (composite) identically through a common interface. The client calls `Process()` once — whether it's one payment or a bundle of 10 nested payments doesn't matter.

---

## 🎯 The Problem It Solves

A customer can pay for an order in many ways:
- Single card payment (leaf)
- Split: ₹500 wallet + ₹1200 card (composite of 2 leaves)
- Bundle: MacBook + subscription bundle (Netflix + Prime) (composite of leaf + composite)

Without Composite, the checkout must **type-check**:
```csharp
// ❌ Type-checking everywhere — can't nest bundles inside bundles
if (payment is SinglePayment single) { process(single); }
else if (payment is List<SinglePayment> bundle) { foreach (p in bundle) process(p); }
// What about bundle of bundles? → another else if. Never ends.
```

With Composite, every `IPaymentComponent` has `Process()`:
```csharp
// ✅ Same call for leaf, bundle, or deeply nested bundle of bundles
payment.Process(); // recursively handles any depth
payment.TotalAmount; // always correct — recursively summed
```

---

## 💡 Analogy

A **corporate org chart**. A `CEO` node has `VPs` which have `Managers` which have `Employees` (leaves). When you ask "how many people does this VP manage?", the answer recursively counts everything below them. You don't write separate code for "count one person" vs "count a whole department" — `GetHeadcount()` works the same way on any node.

---

## 📐 Structure

```
«interface»
IPaymentComponent
  + Name: string
  + TotalAmount: decimal
  + Process(): PaymentSummary
  + Display(indent)

SinglePayment (Leaf)          PaymentBundle (Composite)
  - no children               - List<IPaymentComponent> _children
  - just executes             - Add(component): PaymentBundle
  - TotalAmount = its amount  - TotalAmount = sum of children's amounts
                              - Process() → recurse all children

Client: checkout.Checkout(payment)
  ← doesn't know if it's a Leaf or Composite
  ← calls payment.Process() → works for any depth
```

---

## ✅ When to Use

- **Tree/hierarchical structure** where the parts and the whole should be treated the same
- **Recursive operations**: sum all amounts, count all items, display a hierarchy
- You want the client to be **unaware** of whether it's dealing with a single item or a group
- Nodes can contain other nodes of the same type (bundles containing bundles)

## ❌ When NOT to Use

- Flat list only — a plain `List<T>` and LINQ are sufficient
- Children are **very different types** — a uniform interface becomes forced and leaky
- You need **type-specific operations** on children — Composite forces you to use lowest-common-denominator interface

---

## 🔑 Top Interview Questions

### Q1: "What's the difference between Leaf and Composite?"

```csharp
// Leaf: terminal node — executes directly, has no children
public sealed class SinglePayment : IPaymentComponent
{
    public PaymentSummary Process()
    {
        // execute the charge — no recursion
        return new PaymentSummary(Name, TotalAmount, Success: true, Children: []);
    }
}

// Composite: branch node — delegates to children
public sealed class PaymentBundle : IPaymentComponent
{
    private readonly List<IPaymentComponent> _children = [];

    public PaymentSummary Process()
    {
        var childSummaries = _children.Select(c => c.Process()).ToList(); // RECURSION
        return new PaymentSummary(Name, TotalAmount, ...childSummaries);
    }
}
```

### Q2: "How does Composite enable recursive algorithms?"

```csharp
// TotalAmount is NEVER stored — always computed recursively
public decimal TotalAmount => _children.Sum(c => c.TotalAmount);
//                                               ↑
//                            each child may also be a bundle → recursion
```

This is the core power: the algorithm automatically handles any depth of nesting because the `TotalAmount` property on a `PaymentBundle` calls `TotalAmount` on each child — which may itself be a `PaymentBundle` calling `TotalAmount` on its children.

### Q3: "What's the downside of Composite?"

1. **Can make design overly general** — if some components shouldn't contain children, the interface is misleading
2. **Hard to restrict composition** — if you want to prevent "bundles inside bundles", Composite makes that difficult without additional validation
3. **Type safety** — the `IPaymentComponent` interface must be the lowest-common-denominator; leaf-specific operations can't be in the interface

### Q4: "Where is Composite used in .NET?"

- **Expression trees** (`Expression<Func<T>>`) — binary, unary, and parameter expressions all implement `Expression`
- **UI controls hierarchy** — `Control` in WinForms: `Form` contains `Panel` contains `Button` (all `Control`)
- **File system abstraction** — `FileSystemInfo` base, `FileInfo` (leaf), `DirectoryInfo` (composite containing more `FileSystemInfo`)
- **XML/JSON document models** — `XElement` contains child `XElement` nodes

---

## 🆚 Common Confusions

**Composite vs List\<T\>**: A plain `List<T>` holds only one type. Composite's power is **mixing leaves and composites in the same collection** — a `PaymentBundle` can contain both `SinglePayment` leaves and other `PaymentBundle` composites. That's the tree structure.

**Composite vs Strategy**: Composite is about **structure** (how objects are organized). Strategy is about **behaviour** (what algorithm is used). Completely different concerns.

---

## 📂 File in This Folder

**[08_PaymentBundleComposite.cs](08_PaymentBundleComposite.cs)** — Contains:
- `IPaymentComponent` — the uniform interface
- `SinglePayment` — leaf: executes directly
- `PaymentBundle` — composite: fluent `.Add()`, recursive `TotalAmount`, recursive `Process()`
- `CheckoutService` — consumer that calls `payment.Process()` regardless of structure
- Demo: single → split (wallet+card) → nested bundle (MacBook + subscriptions bundle)

---

## 💼 FAANG Interview Tip

Always mention the **`TotalAmount` computed property** pattern:

> *"The composite's `TotalAmount` is never stored — it's always `_children.Sum(c => c.TotalAmount)`. This is the key to Composite: recursive aggregation happens naturally because every node, whether leaf or composite, exposes the same interface. I don't need to write special code for 'sum a bundle' vs 'sum a single item' — the recursion handles any depth."*

This shows you understand the **recursive nature** of Composite, not just the class hierarchy.
