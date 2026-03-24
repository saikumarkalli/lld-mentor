# Bank Module — OOP Fundamentals

> **One evolving bank codebase. Eight concepts. Each file builds on the last.**
> Every file has a `❌ BEFORE` (the naive mistake) and a `✅ AFTER` (the OOP-correct way).
> Run `dotnet run` from `LLD.OOPs.Concepts/` to see all 8 demos in your terminal.

---

## The Big Picture — How These 8 Concepts Connect

```
┌─────────────────────────────────────────────────────────────────────┐
│  File 1: ENCAPSULATION                                              │
│  BankAccount owns its own data. Nothing gets in or out uninvited.   │
│                        │                                            │
│                        ▼                                            │
│  File 2: ABSTRACTION                                                │
│  BankService hides HOW it works. Callers see a clean 4-method API. │
│                        │                                            │
│                        ▼                                            │
│  File 3: INHERITANCE                                                │
│  SavingsAccount, CurrentAccount, FD all share a common AccountBase. │
│  Fix once in the base — fixed everywhere.                           │
│                        │                                            │
│                        ▼                                            │
│  File 4: POLYMORPHISM                                               │
│  account.CalculateInterest() on ANY account type. Zero if/else.    │
│  This is the PAYOFF for File 3's inheritance structure.             │
│                        │                                            │
│                        ▼                                            │
│  File 5: ABSTRACT CLASS vs INTERFACE                                │
│  When to share CODE (abstract class) vs CONTRACT (interface).       │
│  IInterestBearing, IOverdraftAllowed, ILoanable — mix and match.   │
│                        │                                            │
│                        ▼                                            │
│  File 6: COMPOSITION vs INHERITANCE                                 │
│  BankService HAS-A Logger. IS-NOT-A Logger.                         │
│  Inject capabilities; don't inherit them.                           │
│                        │                                            │
│                        ▼                                            │
│  File 7: INTERFACE SEGREGATION  (SOLID — the I)                    │
│  Fat interface → NotImplementedException. Thin interface → clean.  │
│                        │                                            │
│                        ▼                                            │
│  File 8: DEPENDENCY INVERSION  (SOLID — the D)                     │
│  Inject abstractions. Swap gateway in 1 line. Test without prod.   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Quick Reference — All 8 Concepts

| # | Concept | File | One-line essence | Violating this causes... |
|---|---------|------|-----------------|--------------------------|
| 1 | **Encapsulation** | `01_Encapsulation.cs` | The class is the gatekeeper. Data inside; methods are the only doors. | Fraud scripts set `IsLocked = false` on 847 accounts silently. ₹1.8 crore gone. |
| 2 | **Abstraction** | `02_Abstraction.cs` | Show callers the WHAT. Hide the HOW. Change the HOW freely, forever. | Mobile app coupled to a 200-line method. Risk team changes one parameter. Prod down Friday 11 PM. |
| 3 | **Inheritance** | `03_Inheritance.cs` | Fix the base once — every subclass gets it free. | RBI changes interest formula. Dev fixes 1 of 3 classes. ₹2.3 crore audit discrepancy for 6 months. |
| 4 | **Polymorphism** | `04_Polymorphism.cs` | Zero if/else. New account type = new class, zero changes to processors. | 11 switch-statements across 7 files. New account type → dev misses one → 3,200 support calls. |
| 5 | **Abstract Class vs Interface** | `05_AbstractClass_vs_Interface.cs` | Shared CODE → abstract class. Shared CONTRACT only → interface. | One fat interface forces `NotImplementedException` on 3 of 8 methods. Junior dev hits it in prod. |
| 6 | **Composition vs Inheritance** | `06_CompositionVsInheritance.cs` | If you can't say "[Child] IS-A [Parent]" confidently, compose instead. | `FlushBuffer()` leaks onto public REST API. Penetration test exploits it. 3-month migration to fix. |
| 7 | **Interface Segregation** | `07_InterfaceSegregation.cs` | Fat interface = fake stubs = ticking time bomb. Thin interfaces = honest contracts. | Monitoring script calls `RecalculateFees()` on a read-only dashboard. 14,000 accounts miss fee recalc silently. |
| 8 | **Dependency Inversion** | `08_DependencyInversion.cs` | Inject abstractions. High-level logic never touches concrete infrastructure. | Testing refund logic requires real Stripe key + live SQL + VPN. Test abandoned. Bug ships. 11 months undetected. |

---

## Detailed Concept Explanations

---

### 1. Encapsulation
**File:** [01_Encapsulation.cs](01_Encapsulation.cs)

**What it is:**
Bundling data (fields) and the rules that protect it (methods) inside a single class, and making the fields `private` so only the class can touch them. You interact through *methods* (controlled doors), not *fields* (open windows).

**The problem it solves:**
When fields are public, anyone — any other class, any script, any junior dev — can write invalid state directly. There is no enforcement. The compiler cannot protect you.

```csharp
// ❌ BEFORE — public fields, zero protection
account.Balance       += 1_000_000;  // fraud. Compiles fine.
account.IsLocked       = false;      // unfreezes a fraud-flagged account. Compiles fine.
account.FailedAttempts = 0;          // resets lockout bypass. Compiles fine.
Console.WriteLine(account.Pin);      // PIN printed to logs. Compiles fine.
```

```csharp
// ✅ AFTER — private fields, controlled access
account.Deposit(1_000m);             // goes through validation + audit log
account.VerifyPin("wrong");          // increments failed attempts internally
// account.Pin → compiler error: "Property 'Pin' has no getter"
// account.IsLocked = false → compiler error: no setter. Computed from FailedAttempts.
```

**Key techniques used:**
- `private` fields — the data is invisible to the outside
- **Write-only property** (`Pin` has `set` but no `get`) — you can assign but never read back
- **Computed property** (`IsLocked` has no setter, derived from `_failedAttempts`) — cannot be set directly
- `virtual Deposit()` / `virtual Withdraw()` — marked virtual so File 3's subclasses can extend the rules

**Mental model:**
A bank's core banking system. The teller cannot type a balance directly into the database. They initiate a Deposit transaction — which goes through fraud checks, audit logging, balance update. The system owns its data. You go through the door it provides, or not at all.

---

### 2. Abstraction
**File:** [02_Abstraction.cs](02_Abstraction.cs)

**What it is:**
Showing a simplified interface to the outside world and hiding the implementation details behind it. The caller presses one button; 60 lines of validation, fraud checks, SMS, and audit logging run invisibly.

**The problem it solves:**
Without abstraction, every caller is coupled to every implementation detail. Change one detail — swap SMS for email, add a new fraud check — and every caller potentially breaks.

```csharp
// ❌ BEFORE — NaiveBankService.ProcessWithdrawal() is 60 lines doing 6 jobs
// Step 1: validate inline
// Step 2: call fraud API inline
// Step 3: check daily limit inline
// Step 4: deduct balance inline
// Step 5: write audit log inline
// Step 6: send SMS inline
// To test balance deduction alone: need real fraud API + real SMS + real DB. Impossible.
```

```csharp
// ✅ AFTER — Caller only sees 4 methods on IBankService
IBankService service = new RetailBankService();
service.Deposit("ACC001", 10_000m);         // caller knows nothing about what happens inside
service.Withdraw("ACC001", 5_000m, "CUST"); // fraud, daily limit, audit, SMS — all invisible
```

**Key techniques used:**
- `IBankService` interface — the public contract (4 methods only)
- `BankServiceBase` abstract class — protected helpers invisible to callers (`GetFraudScore`, `LogAudit`, `NotifyCustomer`)
- `RetailBankService` — concrete implementation; override `NotifyCustomer()` to swap SMS → email
- `EmailNotificationBankService` — inherits everything, overrides one protected method. Zero caller impact.

**Mental model:**
An ATM machine. You press "Withdraw ₹5,000". The network call, fraud score check, daily limit validation, and audit log entry are all invisible behind the button. That button is the abstraction.

---

### 3. Inheritance
**File:** [03_Inheritance.cs](03_Inheritance.cs)

**What it is:**
A class acquiring the fields and methods of a parent class, then adding or overriding only what's unique to it. The parent holds shared code; the child only defines what's different.

**The problem it solves:**
Without inheritance, every account type is its own isolated class. The same `Deposit()`, the same fields, the same `GetStatement()` — copy-pasted everywhere. A bug fix must be applied in N places. Someone always misses one.

```csharp
// ❌ BEFORE — NaiveSavingsAccount and NaiveCurrentAccount
// Both have: AccountNumber, HolderName, Balance, OpenDate  (copy-pasted)
// Both have: Deposit(), GetStatement()                     (copy-pasted)
// RBI changes interest formula → dev fixes NaiveSavingsAccount
//                               → forgets NaiveCurrentAccount
//                               → ₹2.3 crore audit discrepancy
```

```csharp
// ✅ AFTER — one fix, all account types benefit
public abstract class AccountBase          // shared fields + Deposit() here
public class SavingsAccount  : AccountBase // only adds: MinimumBalance check, 3.5% interest
public class CurrentAccount  : AccountBase // only adds: overdraft allowance
public class FixedDepositAccount : AccountBase // only adds: maturity lock, 6.8% interest

// Fix CalculateInterest() base logic = fixed in ALL account types immediately
```

**Key techniques used:**
- `abstract` base class — cannot be instantiated; forces subclasses to implement `abstract` members
- `virtual` methods — base provides a sensible default; subclasses `override` only if they differ
- `base.Withdraw(amount)` — calls the parent's implementation, then adds to it
- `override` keyword — replaces the base behaviour for this specific subclass

**Mental model:**
An RBI-mandated bank account template. Every account in India must have an account number, holder name, opening date, and deposit/withdrawal rules. That's the template — `AccountBase`. A savings account then adds minimum balance rules. An FD adds a maturity lock. Each account *inherits* the standard template and fills in its own unique clauses.

---

### 4. Polymorphism
**File:** [04_Polymorphism.cs](04_Polymorphism.cs)

**What it is:**
The same method call producing different behaviour depending on the actual type of the object at runtime. `account.CalculateInterest()` returns 3.5% for `SavingsAccount` and 6.8% for `FixedDepositAccount` — same call, different results. No `if/else` required.

There are two kinds:
- **Runtime polymorphism** (method overriding) — resolved by the CLR at runtime based on actual object type
- **Compile-time polymorphism** (method overloading) — resolved by the compiler at compile time based on parameter signatures

**The problem it solves:**
Without polymorphism, you write `if (account is SavingsAccount) ... else if (account is CurrentAccount) ...` everywhere. Every new account type forces you to open and edit that file. In 3 years you have 15 account types and 400 lines of if-else spread across 11 files. Someone adds a new type, forgets one file, and 3,200 customers see ₹0 interest.

```csharp
// ❌ BEFORE — if/else that grows forever
if      (account is NaiveSavingsAccount s)  { interest = s.Balance * 0.035m; }
else if (account is NaiveCurrentAccount c)  { interest = 0; }
// Adding FixedDeposit → open this file → add another else-if
// Adding RecurringDeposit → open this file → add another else-if

// ✅ AFTER — zero if/else, extensible forever
foreach (var account in accounts)
{
    decimal interest = account.CalculateInterest(); // CLR dispatches to right override
    Console.WriteLine($"{account.GetAccountType()}: ₹{interest:N2}");
}
// Adding RecurringDeposit = create class with override. This loop — ZERO changes. Ever.
```

**Method overloading (compile-time polymorphism):**
```csharp
printer.PrintReceipt(account);                                   // overload 1
printer.PrintReceipt(account, "user@email.com");                 // overload 2
printer.PrintReceipt(account, "user@email.com", includeTax: true); // overload 3
// Same name. Compiler picks the right one based on arguments.
```

**Mental model:**
A bank teller's "process transaction" button. The teller presses the same button whether you have a savings account, current account, or FD. The banking system applies the right rules based on YOUR account type. The teller never needed a "if savings → do X, if FD → do Y" manual.

---

### 5. Abstract Class vs Interface
**File:** [05_AbstractClass_vs_Interface.cs](05_AbstractClass_vs_Interface.cs)

**What it is:**
The most commonly confused OOP question. Both enable polymorphism. Both define contracts. The difference is about *what they model*:

```
ABSTRACT CLASS = "What you ARE"  — shared identity + partial implementation
INTERFACE      = "What you CAN DO" — capability contract, zero implementation
```

**The permanent decision rule:**

| Question | Answer | Use |
|----------|--------|-----|
| Do all subclasses share FIELDS and/or CONCRETE CODE? | Yes | Abstract class |
| Do all subclasses only share a METHOD CONTRACT? | Yes | Interface |
| Can completely unrelated classes need this capability? | Yes | Interface |
| Is the relationship "Every X IS-A Y"? | Yes | Abstract class |
| Is the relationship "X CAN DO Y (regardless of what X is)"? | Yes | Interface |

**The bank example:**

```csharp
// Abstract class — AccountBase — because:
// ✅ All accounts IS-A bank account (identity)
// ✅ All accounts share FIELDS: AccountNumber, HolderName, Balance
// ✅ All accounts share Deposit() CODE — same logic for every type
// ✅ All accounts MUST implement CalculateInterest() — enforced by abstract

// Interface — IInterestBearing — because:
// ✅ SavingsAccount earns interest (IS-A AccountBase)
// ✅ RecurringDeposit earns interest (IS-A AccountBase)
// ✅ LoanAccount charges interest (IS-NOT-A AccountBase — different base class!)
// → Only interfaces let you group them together without sharing a base class

// Interface — ILoanable — because:
// ✅ LoanAccount implements ILoanable
// ✅ LoanAccount IS-NOT-A bank account — it's a financial product, different hierarchy
// → You cannot use an abstract class here. Interfaces are the only option.

List<IInterestBearing> earners = [savingsAcc, recurringDeposit]; // works across hierarchies
```

**The confusion to avoid:**
```csharp
// ❌ WRONG: creating both an abstract class AND an interface with the same methods
// → double the methods to maintain
// → NaiveLoanAccount.GetMinimumBalance() → NotImplementedException (makes no sense on a loan)
// → ambiguous: which one do callers use?
```

**Mental model:**
Abstract class = RBI banking licence template. Every licensed bank shares the same legal structure, capital requirements, and KYC obligations. The template provides common ground AND enforces obligations.
Interface = ISO certification. A bank, a fintech, and an NBFC can all hold ISO 9001. The cert says "you meet this standard" — it doesn't change your internal structure.

---

### 6. Composition vs Inheritance
**File:** [06_CompositionVsInheritance.cs](06_CompositionVsInheritance.cs)

**What it is:**
When you want a class to *use* a capability (logging, fraud detection, notifications), you inject it as a dependency rather than inheriting it. The class **HAS-A** logger; it **IS-NOT-A** logger.

**The problem it solves:**
Inheriting from a utility class to "get" its behaviour exposes all of that class's methods on your public surface, permanently couples you to one specific implementation, and makes unit testing require real infrastructure.

```csharp
// ❌ BEFORE — BankTransactionService extends BankAuditLogger
public class NaiveBankTransactionService : BankAuditLogger
{
    // Now callers can do:
    service.FlushBuffer();          // ← logger internals leak onto a transaction service
    service.ArchiveLogs("/prod/");  // ← external callers can manipulate audit trail
    service.SetLogLevel("DEBUG");   // ← log behaviour changeable from outside
}
// Pentest finds FlushBuffer() on the public API. Audit trail corruptible. 3-month fix.
```

```csharp
// ✅ AFTER — inject the capability, keep it private
public class GoodBankTransactionService
{
    private readonly IBankAuditLogger   _logger;    // ← private. Invisible to callers.
    private readonly IBankFraudDetector _fraud;     // ← private. Invisible to callers.
    private readonly IBankNotifier      _notifier;  // ← private. Invisible to callers.

    public GoodBankTransactionService(IBankAuditLogger logger, IBankFraudDetector fraud, IBankNotifier notifier)
    { ... }
    // No FlushBuffer(), no ArchiveLogs(), no SetLogLevel() on the public API. Ever.
}
```

**Swapping implementations:**
```csharp
// Dev environment
new GoodBankTransactionService(new ConsoleBankAuditLogger(), ...);

// Production
new GoodBankTransactionService(new DatabaseAuditLogger(), ...);

// Unit tests — no real infrastructure needed
new GoodBankTransactionService(new NullAuditLogger(), ...);
// ↑ NullAuditLogger does nothing. Tests run in milliseconds without touching a file system.
```

**The test:**
> "SavingsAccount IS-A AuditLogger?" → **No** → Use composition.
> "SavingsAccount IS-A AccountBase?" → **Yes** → Use inheritance.

**Mental model:**
A bank branch HAS-A security guard, HAS-A ATM, HAS-A safe. It IS-NOT-A security guard. If it *inherited* from SecurityGuard, the branch would expose `PatrolBuilding()` and `ArrestIntruder()` on its public API. To swap agencies, you'd rewrite the class hierarchy. Composition just swaps the guard object. One constructor argument.

---

### 7. Interface Segregation
**File:** [07_InterfaceSegregation.cs](07_InterfaceSegregation.cs)

**What it is:**
"Clients should not be forced to depend on interfaces they do not use." — Robert C. Martin (the **I** in SOLID)

When an interface has 10 methods and a class only needs 2, that class is forced into a contract that doesn't fit. It stubs out 8 methods with `NotImplementedException`. Those stubs become production incidents.

**The problem it solves:**
One fat interface means every class signs a contract it can't fully honour. Every stubbed method is a land mine waiting to be called.

```csharp
// ❌ BEFORE — IBankOperations has 10 methods
public class NaiveComplianceDashboard : IBankOperations
{
    public string GenerateReport(...)  => "...";      // ← only uses this
    public string GetAuditLog(...)     => "...";      // ← and this

    // Forces 8 more fake stubs:
    public void ProcessDeposit(...)    => throw new NotImplementedException();
    public void RecalculateFees(...)   => throw new NotImplementedException();
    // ... 6 more lies

    // Monitoring script calls RecalculateFees() on all IBankOperations → explodes.
    // 14,000 accounts miss fee recalculation. Silent failure.
}
```

```csharp
// ✅ AFTER — 5 focused interfaces. Each class signs only what it means.
public interface ITransactable  { ProcessDeposit, ProcessWithdrawal, Transfer, Refund }
public interface IReportable    { GenerateReport, ExportToCsv }
public interface IAuditable     { GetAuditLog, ArchiveTransactions }
public interface INotifiable    { SendNotification }
public interface IFeeCalculable { RecalculateFees, GetCurrentFee }

// ComplianceDashboard only implements IReportable + IAuditable.
// Monitoring script iterates IFeeCalculable[] — dashboard is NEVER in that list.
// NotImplementedException is structurally impossible.
```

**Mental model:**
A bank's job description bulletin board. One massive flyer: "BANK TELLER — must process deposits, issue loans, write audit reports, manage the server room, conduct fraud investigations, and maintain ATM hardware." No one person can do all of that. Split into focused role descriptions: Teller, Loan Officer, IT Admin, Compliance. Each person signs only the description relevant to them.

---

### 8. Dependency Inversion
**File:** [08_DependencyInversion.cs](08_DependencyInversion.cs)

**What it is:**
"High-level modules should not depend on low-level modules. Both should depend on abstractions." — Robert C. Martin (the **D** in SOLID)

`GoodPaymentProcessor` is high-level (business logic). `StripeBankGateway` is low-level (infrastructure). The processor should depend only on `IBankPaymentGateway` — never on `StripeBankGateway` directly. This lets you swap Stripe for PayPal, or a fake gateway for tests, without touching business logic.

**The problem it solves:**
When business logic creates its own concrete dependencies with `new`, it's permanently glued to them. You cannot test the logic without spinning up real infrastructure. You cannot swap implementations without editing business code.

```csharp
// ❌ BEFORE — hard-coded dependencies
public class NaivePaymentProcessor
{
    private readonly StripePaymentGateway _gateway = new();  // ← permanent glue
    private readonly SqlTransactionRepo   _repo    = new();  // ← permanent glue

    // To test ProcessPayment(): need real Stripe key + live SQL Server + VPN to prod.
    // Test was abandoned. Bug shipped. 11 months undetected.
}
```

```csharp
// ✅ AFTER — dependencies injected from outside
public class GoodPaymentProcessor
{
    private readonly IBankPaymentGateway      _gateway;
    private readonly IBankTransactionRepository _repo;

    public GoodPaymentProcessor(IBankPaymentGateway gateway, IBankTransactionRepository repo)
    {
        _gateway = gateway;
        _repo    = repo;
    }
}

// Production: real Stripe, real SQL
var prod = new GoodPaymentProcessor(new StripeBankGateway(), new SqlBankTransactionRepository());

// Swap to PayPal: zero changes to GoodPaymentProcessor
var paypal = new GoodPaymentProcessor(new PayPalBankGateway(), new SqlBankTransactionRepository());

// Unit tests: fake everything — no network, no DB, runs in microseconds
var test = new GoodPaymentProcessor(new FakeBankGateway(shouldSucceed: true), new InMemoryTransactionRepository());
```

**The ASP.NET Core connection:**
You've been using Dependency Inversion every time you wrote:
```csharp
builder.Services.AddScoped<IBankPaymentGateway, StripeBankGateway>();
builder.Services.AddScoped<IBankTransactionRepository, SqlBankTransactionRepository>();
```
The DI container reads those registrations and automatically injects the right concrete class into `GoodPaymentProcessor`'s constructor. That's the framework doing DIP for you.

**Mental model:**
NEFT/RTGS payment rails. Your bank doesn't build its own wire-transfer infrastructure. It depends on the RBI NEFT interface (abstraction). Whether the underlying infrastructure uses batch processing or real-time RTGS settlement — the bank's payment code doesn't change. The interface never changes. The implementation underneath does. That's dependency inversion.

---

## The 8 Mental Models — 2-Minute Recall Block

Read this before an interview or after a long break. Rebuild the entire module in your head.

```
1. ENCAPSULATION
   Bank vault with a teller. You submit a request (method). The teller
   checks the rules and updates the vault (private field). You never
   reach into the vault directly.

2. ABSTRACTION
   ATM machine. One button. 60 lines invisible behind it.
   Show callers the WHAT. Hide the HOW. Change the HOW freely.

3. INHERITANCE
   RBI account template. Savings, Current, FD all inherit the standard
   clauses and fill in their own. Fix the template → fixed everywhere.

4. POLYMORPHISM
   Bank teller's "process transaction" button. Same button, every account
   type. System applies the right rules at runtime. Zero if/else.

5. ABSTRACT CLASS vs INTERFACE
   Abstract class = RBI licence template (shared DNA, partial impl).
   Interface = ISO certification (capability badge, zero DNA).
   Shared CODE → abstract. Shared CONTRACT only → interface.

6. COMPOSITION vs INHERITANCE
   Branch HAS-A security guard. IS-NOT-A security guard.
   Inject capabilities as private fields. Never inherit them.
   Can't say "X IS-A Y" confidently? Compose.

7. INTERFACE SEGREGATION  (SOLID — I)
   Job description bulletin board. Don't hand the loan officer role
   description to the teller. Each person signs only their own role.
   Fat interface = NotImplementedException = prod incident.

8. DEPENDENCY INVERSION  (SOLID — D)
   NEFT rails. Your bank uses the RBI interface, not Kotak's internal
   wire system. Depend on the interface. Swap the implementation freely.
   ASP.NET Core's builder.Services.AddScoped() does this for you.
```

---

## How to Study This Module

### Recommended order
Files 1 → 8 in sequence. Each concept explicitly references the previous file and previews the next. Reading out of order loses the narrative.

### What to focus on in each file

| File | What to trace through |
|------|-----------------------|
| `01_Encapsulation.cs` | Find the write-only `Pin` setter and the computed `IsLocked`. Understand why both have no setters. |
| `02_Abstraction.cs` | Count how many methods `IBankService` exposes vs how many lines `NaiveBankService.ProcessWithdrawal()` has. |
| `03_Inheritance.cs` | Find `base.Withdraw(amount)` in `SavingsAccount`. Trace what runs first (subclass) vs what's delegated (base). |
| `04_Polymorphism.cs` | Count `if` statements in `BankReportGenerator.GenerateMonthlyReport()`. Answer: zero. Then count them in `NaiveReportGenerator`. |
| `05_AbstractClass_vs_Interface.cs` | Find `LoanAccount`. It implements `ILoanable`. It does NOT extend `AccountBase`. Ask: why? |
| `06_CompositionVsInheritance.cs` | Find the three loggers: `ConsoleBankAuditLogger`, `DatabaseAuditLogger`, `NullAuditLogger`. Each swappable in one constructor argument. |
| `07_InterfaceSegregation.cs` | Count `NotImplementedException` throws in `NaiveComplianceDashboard`. Then count them in `ComplianceDashboard`. |
| `08_DependencyInversion.cs` | Find `FakeBankGateway`. Understand how it lets you test payment failure without causing a real Stripe error. |

### The "2-year test"
If you come back after a long break and feel lost:
1. Read only the **FILE HEADER COMMENT** of each file (Section 1) — 2 minutes
2. Read the **BUILDS ON** line — understand the chain
3. Run `dotnet run` and watch the output — 3 minutes

That's all you need to rebuild the mental model.

---

## Classes Defined in This Module

```
BankModule/
├── BankAccount              — File 1: encapsulated account (write-only PIN, computed lock)
├── AccountBase              — File 3: abstract base for all account types
├── SavingsAccount           — File 3: min balance ₹1,000 | 3.5% interest
├── CurrentAccount           — File 3: overdraft up to ₹50,000 | 0% interest
├── FixedDepositAccount      — File 3: locked until maturity | 6.8% interest
├── IBankService             — File 2: 4-method service contract
├── BankServiceBase          — File 2: protected helpers (fraud, audit, notify)
├── RetailBankService        — File 2: concrete implementation
├── IInterestBearing         — File 5: earns interest (Savings, FD, RecurringDeposit)
├── IOverdraftAllowed        — File 5: can go into overdraft (Current, Business)
├── ILoanable                — File 5: loan instrument (LoanAccount — NOT an AccountBase)
├── RecurringDeposit         — File 5: AccountBase + IInterestBearing
├── LoanAccount              — File 5: ILoanable only, not a bank account
├── IBankAuditLogger         — File 6: logging capability
├── IBankFraudDetector       — File 6: fraud scoring capability
├── IBankNotifier            — File 6: notification capability
├── GoodBankTransactionService — File 6: composes all three via constructor injection
├── ITransactable            — File 7: core transaction operations
├── IReportable              — File 7: report generation
├── IAuditable               — File 7: audit trail access
├── IFeeCalculable           — File 7: fee recalculation
├── ComplianceDashboard      — File 7: implements only IReportable + IAuditable
├── IBankPaymentGateway      — File 8: payment gateway abstraction
├── IBankTransactionRepository — File 8: persistence abstraction
├── GoodPaymentProcessor     — File 8: injects both; knows nothing about Stripe or SQL
└── FakeBankGateway          — File 8: returns hardcoded responses for unit testing
```

---

## What's Next

| Phase | Module | What you'll learn |
|-------|--------|-------------------|
| **Phase 2** | `LLDMaster.SOLID/` | All 5 SOLID principles in depth — each gets its own module |
| **Phase 3** | `LLDMaster.Patterns/` | Design patterns: Strategy, Factory, Observer, Decorator — applying OOP in standard solutions |
| **Phase 4** | `LLDMaster.Problems/` | Full LLD problems: Parking Lot, BookMyShow, UPI System, ATM Machine — everything combined |
