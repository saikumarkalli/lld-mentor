# 05 — Prototype Pattern

> **In one line**: Create new objects by cloning an approved master template instead of building from scratch.

---

## 🧠 What Is It?

Prototype creates new objects by **copying** (cloning) an existing object called the prototype. You build and validate the master once; every subsequent creation is a clone of that master with only the changed fields overridden.

---

## 🎯 The Problem It Solves

A subscription platform charges users the same recurring payment every month: same gateway (Stripe), same currency (INR), same webhook URL, same retry policy, same metadata. Only the amount or billing date might change.

**Without Prototype** — rebuild the full config every billing cycle:
```csharp
// Every billing cycle, specify ALL 10 fields again
// Miss one field → wrong webhook, wrong retry count, silent billing bug
var config = new PaymentConfig {
    GatewayType = "stripe", Currency = "INR", MaxRetries = 3,
    WebhookUrl = "https://...", SaveCard = true,
    CustomerId = customerId, Amount = amount,
    // ... 4 more fields
};
```

**With Prototype** — clone the approved template, override only what changes:
```csharp
// Master template configured and approved ONCE
var template = registry.GetClone("netflix_monthly"); // deep clone
template.Amount = currentMonthAmount;                // only this changes
template.Description = $"Netflix - {DateTime.UtcNow:MMMM yyyy}";
// All other 10 fields are correct — already validated in the master
gateway.Charge(template);
```

---

## 💡 Analogy

A **rubber stamp**. You make one master stamp (the prototype) with the company logo, address, and layout — carefully designed and approved. To create 1000 letters, you stamp 1000 times — each impression is a clone of the master. You don't hand-design each letter. The master is **never modified**.

---

## 📐 Structure

```
RecurringPaymentTemplate (Prototype)
  ├── ShallowClone()  → MemberwiseClone() — fast, but shares reference-type fields
  └── DeepClone()     → new object with all fields copied independently

PaymentTemplateRegistry
  ├── Register("netflix_monthly", masterTemplate)
  └── GetClone("netflix_monthly") → always returns a DeepClone

Usage:
  master (NEVER mutated)
    └── clone1 → override Amount, Description → charge March billing
    └── clone2 → override Amount, Description → charge April billing
```

---

## ✅ When to Use

- Object construction is expensive (validation, DB lookup, complex defaults) and you have many similar objects
- Most fields are shared; only a few differ per instance (templates, configurations)
- You want to snapshot and restore object state
- 1000+ similar objects created per day (recurring billing, batch processing)

## ❌ When NOT to Use

- Objects have no shared structure — cloning adds no value over `new`
- Object graph is deeply nested and complex — deep copy becomes error-prone
- Circular references in the object graph — deep copy can cause infinite loops

---

## 🔑 Top Interview Questions

### Q1: "What is the difference between shallow copy and deep copy?"

This is **always asked** when Prototype comes up:

```csharp
// SHALLOW COPY via MemberwiseClone()
// - Value types (int, decimal, bool): copied by VALUE ✅
// - Reference types (List, Dictionary, string[]): copied by REFERENCE ⚠️

var clone = (MyObject)MemberwiseClone(); // fast, but...
clone.Metadata["key"] = "new value";     // also mutates the ORIGINAL's Metadata!
```

```csharp
// DEEP COPY — manually copy every reference-type field
return new RecurringPaymentTemplate {
    Amount     = this.Amount,          // value type → safe
    GatewayType = this.GatewayType,   // string is immutable → safe
    Metadata   = new Dictionary<string, string>(this.Metadata), // NEW dict → safe ✅
};
```

**Rule of thumb**: `MemberwiseClone()` is safe only if ALL fields are value types or immutable reference types (like `string`). The moment you have a mutable collection → use deep clone.

### Q2: "How does C# `MemberwiseClone` work?"

`MemberwiseClone()` is a `protected` method in `Object`. It copies all fields:
- **Value types** (`int`, `decimal`, `bool`, `struct`): copied by value — independent ✅
- **Reference types** (`List`, `Dictionary`, `object`): the **reference** is copied — both original and clone point to the same object ⚠️

```csharp
public IPaymentTemplate ShallowClone()
{
    var clone = (RecurringPaymentTemplate)MemberwiseClone(); // protected → need to expose
    clone.IdempotencyKey = Guid.NewGuid().ToString("N"); // always regenerate per clone
    return clone;
}
```

### Q3: "What is a Prototype Registry?"

A dictionary that stores named master templates. Callers get a clone by name — they never access the master directly:
```csharp
public sealed class PaymentTemplateRegistry
{
    private readonly Dictionary<string, RecurringPaymentTemplate> _templates = [];

    public void Register(string key, RecurringPaymentTemplate master) => _templates[key] = master;

    public RecurringPaymentTemplate GetClone(string key)
        => (RecurringPaymentTemplate)_templates[key].DeepClone(); // always a new copy
}
```

### Q4: "How do you implement deep clone in C#?"

```csharp
// Option 1: Manual (most readable, most control)
return new RecurringPaymentTemplate { ..., Metadata = new(this.Metadata) };

// Option 2: JSON serialisation (easy, slower, works for simple POCOs)
return JsonSerializer.Deserialize<RecurringPaymentTemplate>(JsonSerializer.Serialize(this))!;

// Option 3: ICloneable (avoid — interface contract doesn't specify shallow vs deep)
```

---

## 🆚 Common Confusions

**Prototype vs Copy Constructor**: A copy constructor is explicit (`new MyClass(existing)`) and part of the class API. Prototype uses `MemberwiseClone` + a cloning interface and is more dynamic. For C# production code, a well-named `DeepClone()` method is often clearer than `ICloneable`.

**Prototype vs Builder**: Builder constructs a new object from scratch with validation. Prototype clones an existing valid object and overrides specific fields. If the object is already known-valid, cloning is faster and safer.

---

## 📂 File in This Folder

**[05_PaymentTemplatePrototype.cs](05_PaymentTemplatePrototype.cs)** — Contains:
- `RecurringPaymentTemplate` — with both `ShallowClone()` and `DeepClone()` implementations
- **Shallow clone danger demo** — proves that `MemberwiseClone` + Dictionary mutation affects the original
- `PaymentTemplateRegistry` — named template store
- `PrototypeDemo.Run()` — shows master unchanged after 2 clone cycles

---

## 💼 FAANG Interview Tip

**Always bring up shallow vs deep copy** — it's the only real "trap" in Prototype and every interviewer who asks about Prototype will probe it. Lead with:

> *"The main risk with Prototype is accidentally doing a shallow copy when you have reference-type fields — like a `Dictionary<string, string>` for metadata. `MemberwiseClone` will copy the reference, so mutating the clone's dictionary mutates the master's dictionary. In production, I always implement a `DeepClone()` that explicitly creates new instances of all mutable reference fields."*
