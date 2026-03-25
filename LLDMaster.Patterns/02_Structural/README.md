# 🔧 Structural Patterns — Composing Classes & Objects

> **Core question these patterns answer**: "How should I compose classes and objects into larger structures?"

---

## What Are Structural Patterns?

Structural patterns deal with **relationships** between classes — how they're connected, wrapped, and composed. They don't change behaviour in isolation; they change the **shape** of your system: how interfaces align, how objects delegate, how complexity gets hidden.

The 5 patterns here solve distinct structural problems:

| Problem | Pattern | Keyword |
|---------|---------|---------|
| "Two interfaces don't match — I can't change either" | Adapter | **Translate** |
| "Two dimensions each have multiple variants — class explosion" | Bridge | **Decouple** |
| "Treat single items and groups the same way" | Composite | **Tree** |
| "Add cross-cutting behaviour without modifying the class" | Decorator | **Wrap & Extend** |
| "Simplify a complex subsystem for common callers" | Facade | **Simplify** |

---

## 🧠 The Core Insight

```
Adapter   → wraps ONE object to CHANGE its interface
Decorator → wraps ONE object to EXTEND its behaviour (same interface)
Facade    → wraps MANY objects to SIMPLIFY them into ONE entry point
Composite → treats LEAVES and COMPOSITES as the same interface (tree)
Bridge    → connects TWO hierarchies without one owning the other
```

---

## 📊 Pattern Comparison (Interview Gold)

The most common interview confusion is **Adapter vs Decorator vs Facade**. All three involve "wrapping" but for completely different reasons:

| Pattern | Wraps? | Changes Interface? | Adds Behaviour? | Number of Wrappees |
|---------|--------|-------------------|-----------------|-------------------|
| **Adapter** | ✅ One object | ✅ Yes (translates) | ❌ No | 1 (the adaptee) |
| **Decorator** | ✅ One object | ❌ No (same interface) | ✅ Yes | 1 (nested chain) |
| **Facade** | ❌ Orchestrates | ✅ New simplified interface | ❌ No | Many subsystems |
| **Composite** | ❌ Contains children | ❌ No | ❌ No | 0..N children |
| **Bridge** | ❌ Holds reference | ❌ No | ❌ No | 1 implementor |

---

## 🆚 The Most Confused Pairs

### Adapter vs Decorator
```
Adapter:   INewInterface  ← AdapterClass(adaptee)  ← wraps legacy, changes interface
Decorator: ISameInterface ← DecoratorClass(inner)  ← wraps same, adds logging/retry/etc.
```
**Key test**: Does the interface change? Yes → Adapter. No → Decorator.

### Decorator vs Proxy
Both wrap the same interface. The difference is **purpose**:
- Decorator: **adds behaviour** (logging, retry, caching)
- Proxy: **controls access** (auth check, lazy load, remote call)

---

## 📂 Patterns in This Folder

| File | Pattern | What it demonstrates |
|------|---------|---------------------|
| [06_LegacyPaymentAdapter.cs](06_Adapter/06_LegacyPaymentAdapter.cs) | **Adapter** | Object Adapter (composition) wrapping legacy XML bank API |
| [07_PaymentNotificationBridge.cs](07_Bridge/07_PaymentNotificationBridge.cs) | **Bridge** | 3 payment types × 4 notification channels, no class explosion |
| [08_PaymentBundleComposite.cs](08_Composite/08_PaymentBundleComposite.cs) | **Composite** | Split payments, nested subscription bundles, recursive totals |
| [09_PaymentProcessorDecorator.cs](09_Decorator/09_PaymentProcessorDecorator.cs) | **Decorator** | Logging → FraudCheck → Retry → Audit pipeline via chained wrappers |
| [10_CheckoutFacade.cs](10_Facade/10_CheckoutFacade.cs) | **Facade** | `PlaceOrder()` orchestrating Inventory + Payment + Invoice + Email + Loyalty |

---

## 🔗 READMEs per Pattern

- [Adapter →](06_Adapter/README.md)
- [Bridge →](07_Bridge/README.md)
- [Composite →](08_Composite/README.md)
- [Decorator →](09_Decorator/README.md)
- [Facade →](10_Facade/README.md)
