# 09 — Decorator Pattern

> **In one line**: Attach additional responsibilities to an object dynamically by wrapping it in decorator objects that implement the same interface.

---

## 🧠 What Is It?

Decorator wraps an object with another object **that implements the same interface**. The wrapper adds ONE responsibility, then delegates to the wrapped object. Chain multiple wrappers for a pipeline of behaviours — each independently testable and replaceable.

---

## 🎯 The Problem It Solves

A payment processor in production needs: **logging + fraud check + retry + audit trail**.

**Option 1 — One God class**: 400-line method doing everything. Add "notification" → edit the class again. Violates Single Responsibility.

**Option 2 — Inheritance**: `LoggingFraudCheckRetryAuditPaymentProcessor extends BaseProcessor`. Adding "caching" → `LoggingFraudCheckRetryCachingAuditPaymentProcessor`. Name gets absurd. Can't reuse just logging without all the rest.

**Decorator solution** — each wrapper adds ONE thing:
```csharp
IPaymentProcessor pipeline =
    new LoggingDecorator(           // layer 1: logs
        new FraudCheckDecorator(    // layer 2: fraud screen
            new RetryDecorator(     // layer 3: retry on failure
                new AuditDecorator( // layer 4: audit trail
                    new RealPaymentProcessor())))); // core: gateway call

// All layers: same IPaymentProcessor interface
// Add "CachingDecorator" → wrap it anywhere in the chain — zero changes to others
```

---

## 💡 Analogy

**Birthday cake decorations**. Start with a plain sponge (core processor). Wrap it in frosting (logging decorator). Add candles (fraud check decorator). Add sprinkles (retry decorator). Each layer adds something. You can add or skip any layer independently. The sponge doesn't know what decorations it has.

---

## 📐 Structure

```
«interface»
IPaymentProcessor
  + ChargeAsync(request): Task<PaymentResult>

RealPaymentProcessor    ← concrete component (the "sponge")
  + ChargeAsync()       ← actual gateway call

PaymentProcessorDecorator (abstract)   ← base decorator
  - Inner: IPaymentProcessor           ← the wrapped processor
  + ChargeAsync() = abstract

LoggingDecorator(inner)
  + ChargeAsync() { log → inner.ChargeAsync() → log result }

FraudCheckDecorator(inner)
  + ChargeAsync() { if flagged → block. else inner.ChargeAsync() }

RetryDecorator(inner, maxRetries)
  + ChargeAsync() { try inner.ChargeAsync() × maxRetries with backoff }

AuditDecorator(inner)
  + ChargeAsync() { inner.ChargeAsync() → write audit record }

Chain: Logging(FraudCheck(Retry(Audit(RealProcessor))))
```

---

## ✅ When to Use

- Adding **cross-cutting concerns** without modifying the target class (logging, caching, retry, auth)
- Need **multiple combinations** of behaviours (some callers want logging+retry, others just retry)
- Must respect **Open/Closed**: extend without modifying
- You want each concern to be **independently testable** (test just the retry logic, mock everything else)

## ❌ When NOT to Use

- Only one "extra" behaviour needed — just add it to the class directly
- Order of decorators is very complex and fragile — consider a Pipeline/Chain of Responsibility instead
- Performance critical path — each wrapper call adds a virtual dispatch overhead

---

## 🔑 Top Interview Questions

### Q1: "How is Decorator different from inheritance?"

| | Decorator | Inheritance |
|---|---|---|
| When applied? | **Runtime** (compose at startup) | Compile time |
| Combinations | Any combination, any order | Fixed hierarchy |
| Reusability | Each decorator is reusable alone | Tightly coupled to parent |
| Class count | O(M + N) | O(M × N) |

Decorator is **"composition over inheritance"** applied to extension.

### Q2: "How is Decorator different from Adapter?"

```
Adapter:   INewInterface ← AdapterClass(adaptee) ← changes the interface
Decorator: ISameInterface ← DecoratorClass(inner) ← same interface, adds behaviour
```

**Key**: Adapter changes the interface. Decorator keeps the same interface.

### Q3: "How do you register Decorators in ASP.NET Core DI?"

```csharp
// Using Scrutor (the idiomatic .NET way):
builder.Services.AddScoped<IPaymentProcessor, RealPaymentProcessor>();
builder.Services.Decorate<IPaymentProcessor, AuditDecorator>();
builder.Services.Decorate<IPaymentProcessor, RetryDecorator>();
builder.Services.Decorate<IPaymentProcessor, FraudCheckDecorator>();
builder.Services.Decorate<IPaymentProcessor, LoggingDecorator>();
// Order: last-registered = outermost = first to execute
```

Without Scrutor, build manually in Program.cs:
```csharp
builder.Services.AddScoped<IPaymentProcessor>(sp =>
    new LoggingDecorator(
        new FraudCheckDecorator(
            new RetryDecorator(
                new AuditDecorator(sp.GetRequiredService<RealPaymentProcessor>())))));
```

### Q4: "What's the connection between Decorator and ASP.NET Core middleware?"

ASP.NET Core's `app.Use()` / `app.UseMiddleware<T>()` **IS** the Decorator pattern:
```csharp
app.Use(async (ctx, next) => { /* before */ await next(); /* after */ });
```
Each middleware wraps the next — same interface (`RequestDelegate`), adds behaviour, delegates to the inner one. Every `Use()` call = another decorator in the chain.

### Q5: "What is Decorator vs Proxy?"

Both wrap the same interface. The difference is **intent**:
- **Decorator** → adds **new behaviour** (logging, retry, transformation)
- **Proxy** → **controls access** to the same behaviour (auth check, lazy init, remote call, caching)

---

## 🆚 Common Confusions

**Decorator vs Composite**: Both involve trees of same-interface objects. Composite is about **structure** (tree of parts). Decorator is about **behaviour** (chain of wrappers). Composite aggregates; Decorator augments.

---

## 📂 File in This Folder

**[09_PaymentProcessorDecorator.cs](09_PaymentProcessorDecorator.cs)** — Contains:
- `IPaymentProcessor` + `RealPaymentProcessor` — core component
- `PaymentProcessorDecorator` — abstract base (holds `Inner`, provides pass-through)
- `LoggingDecorator` → `FraudCheckDecorator` → `RetryDecorator` → `AuditDecorator`
- `AuditDecorator` with audit log accessible for verification in tests
- Full pipeline composition + demo with fraud block + audit log output

---

## 💼 FAANG Interview Tip

Connect Decorator to two things interviewers love:
1. **Scrutor + ASP.NET Core DI** — `services.Decorate<T, TDecorator>()` — shows real production knowledge
2. **ASP.NET Core Middleware = Decorator** — every `.Use()` in `Program.cs` is a decorator

Say: *"I implement this using the Decorator pattern — each decorator adds one concern (logging, fraud, retry). In ASP.NET Core I'd use Scrutor for clean DI registration. Interestingly, ASP.NET Core's middleware pipeline is itself the Decorator pattern."*
