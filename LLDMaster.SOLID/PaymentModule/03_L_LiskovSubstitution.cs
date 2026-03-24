/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  SECTION 1 — FILE HEADER                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  💎 SOLID PRINCIPLE: L — Liskov Substitution Principle (LSP)                 ║
 * ║                                                                              ║
 * ║  🔗 OOP ORIGIN: "In your OOP module, polymorphism let ProcessAll() call any  ║
 * ║     Payment subclass without knowing the type. That was beautiful. LSP is   ║
 * ║     the CONTRACT that makes that safe. OCP gave you the pattern.             ║
 * ║     LSP makes it trustworthy."                                               ║
 * ║                                                                              ║
 * ║  THE ONE-LINE RULE (memorise this):                                          ║
 * ║  "If you swap a subclass for its parent (or one implementation for another), ║
 * ║   the caller should NEVER know the difference and NEVER break."             ║
 * ║                                                                              ║
 * ║  Simpler: "Subclasses must KEEP EVERY PROMISE the parent made."             ║
 * ║                                                                              ║
 * ║  WHAT GOES WRONG WITHOUT IT:                                                 ║
 * ║  Your PaymentOrchestrator calls strategy.Execute() trusting it returns a    ║
 * ║  PaymentResult. If one strategy throws NotSupportedException instead —       ║
 * ║  the caller breaks. The CONTRACT was violated. The swap was not safe.        ║
 * ║                                                                              ║
 * ║  THE PR SMELL:                                                               ║
 * ║  "In a code review, you'd spot this when an override method throws a new     ║
 * ║   exception type, returns null when the parent guarantees non-null, or       ║
 * ║   forces the caller to type-check before calling (if payment is FixedDeposit)║
 * ║                                                                              ║
 * ║  CONNECTS TO NEXT:                                                           ║
 * ║  "LSP ensures implementations are swappable. But what if the interface itself║
 * ║   forces implementors to fake methods? IPaymentService with 10 methods —    ║
 * ║   and FraudDetector must implement all 10. That's ISP — File 04."           ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLDMaster.SOLID.PaymentModule.LSP;

// ─────────────────────────────────────────────────────────────────────────────
// Minimal shared types
// ─────────────────────────────────────────────────────────────────────────────

public record PaymentResult_LSP(bool IsSuccess, string TransactionId, string Message)
{
    public static PaymentResult_LSP Success() =>
        new(true, Guid.NewGuid().ToString("N")[..8].ToUpper(), "Success.");

    public static PaymentResult_LSP Failure(string reason) =>
        new(false, string.Empty, reason);
}

// ─────────────────────────────────────────────────────────────────────────────
// A shared caller — the polymorphic loop from your OOP module.
// It trusts the contract. When a subclass lies, the caller pays.
// ─────────────────────────────────────────────────────────────────────────────

public class PaymentBatchProcessor
{
    // ✅ This caller honours the parent's contract: Withdraw() returns PaymentResult. Always.
    // It does NOT catch exceptions — why would it? The contract says no exceptions.
    public void ProcessBatch(IEnumerable<Payment_LSP> payments, decimal amount)
    {
        foreach (var payment in payments)
        {
            // The caller trusts the parent promise.
            // If any child breaks that promise, this crashes.
            var result = payment.Withdraw(amount);
            Console.WriteLine($"   [{payment.GetType().Name}] {result.Message}");
        }
    }
}

// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 2 — BEFORE (minimal violation)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ❌ BEFORE — Parent promises one thing. FixedDepositPayment delivers something else.
// The caller (PaymentBatchProcessor) trusted the parent. The child lied.

// Parent contract — carefully read the promise made in the summary:
/// <summary>
/// Base class for all payment sources.
/// CONTRACT: Withdraw() ALWAYS returns a PaymentResult. It NEVER throws for business reasons.
/// Callers can iterate any collection of Payment objects without try-catch.
/// </summary>
public abstract class Payment_LSP
{
    public decimal Balance { get; protected set; }

    protected Payment_LSP(decimal initialBalance) { Balance = initialBalance; }

    // CONTRACT: returns PaymentResult (success or failure). Never throws.
    public virtual PaymentResult_LSP Withdraw(decimal amount)
    {
        if (amount > Balance)
            return PaymentResult_LSP.Failure($"Insufficient balance (have ₹{Balance}, need ₹{amount}).");

        Balance -= amount;
        return PaymentResult_LSP.Success();
    }
}

// Keeps the promise ✓
public class CreditCardPayment_LSP : Payment_LSP
{
    private const decimal CreditLimit = 50_000m;
    public CreditCardPayment_LSP() : base(CreditLimit) { }

    public override PaymentResult_LSP Withdraw(decimal amount)
    {
        if (amount > CreditLimit)
            // ✅ Returns a failure result — does NOT throw. Promise kept.
            return PaymentResult_LSP.Failure($"Exceeds credit limit of ₹{CreditLimit}.");

        Balance -= amount;
        return PaymentResult_LSP.Success();
    }
}

// Keeps the promise ✓
public class WalletPayment_LSP : Payment_LSP
{
    public WalletPayment_LSP(decimal walletBalance) : base(walletBalance) { }

    public override PaymentResult_LSP Withdraw(decimal amount)
    {
        if (amount > Balance)
            // ✅ Returns a failure result — does NOT throw. Promise kept.
            return PaymentResult_LSP.Failure($"Wallet balance ₹{Balance} insufficient.");

        Balance -= amount;
        return PaymentResult_LSP.Success();
    }
}

// ❌ BREAKS THE PROMISE — structurally IS-A Payment, behaviourally IS-NOT-A Payment
public class FixedDepositPayment_Bad : Payment_LSP
{
    private readonly DateTime _maturityDate;
    public FixedDepositPayment_Bad(decimal fdAmount, DateTime maturityDate)
        : base(fdAmount) { _maturityDate = maturityDate; }

    public override PaymentResult_LSP Withdraw(decimal amount)
    {
        // 🚨 THROWS instead of returning PaymentResult — BREAKS THE CONTRACT
        // The parent said "I never throw." This child says "I do."
        // Every caller of Payment_LSP.Withdraw() trusted the parent's promise.
        // This child violated it silently — the compiler won't catch this.
        if (DateTime.UtcNow < _maturityDate)
            throw new InvalidOperationException(
                $"FD cannot be withdrawn before maturity date {_maturityDate:yyyy-MM-dd}!");

        Balance -= amount;
        return PaymentResult_LSP.Success();
    }
}

// 🚨 VIOLATION: L — FixedDepositPayment_Bad:
//   Rule 1 violated — Strengthened the precondition (adds "before maturity date" rule)
//   Rule 2 violated — Changed exception behaviour (throws, parent guaranteed it won't)
//   Rule 3 violated — Throws a new exception type (InvalidOperationException) parent never declared
//
// 💥 CONSEQUENCE: A ₹45,000 FD withdrawal attempt crashed the payment batch loop.
//    The foreach in PaymentBatchProcessor hit the FD entry and threw.
//    Subsequent payments in the batch were never processed.
//    Customer support received 340 tickets in 2 hours.
//    The caller did NOTHING WRONG. The subclass lied.


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 3 — WHY THIS VIOLATES LSP
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 🔍 THE REASONING:
//
// PROBLEM 1 — BROKEN CONTRACT:
//   The parent promises: "Withdraw() returns a PaymentResult. Always."
//   The child delivers: "Withdraw() throws. Sometimes."
//   The caller believed the parent. The child lied. The caller pays the price.
//   This is not a child class — it's a trap wearing a parent's costume.
//   LSP is violated not by the code structure but by the behaviour contract.
//
// PROBLEM 2 — SILENT POLYMORPHISM FAILURE:
//   Your beautiful polymorphic loop from OOP (ProcessAll) worked perfectly —
//   until someone added FixedDepositPayment to the collection.
//   Polymorphism relies on LSP. Break LSP, break polymorphism.
//   The entire OOP investment unravels from one bad subclass.
//   There's no compile-time warning. It only fails at runtime, in production.
//
// PROBLEM 3 — THE WRONG INHERITANCE (IS-A is about behaviour, not structure):
//   FixedDepositPayment looks right structurally: it has Balance, it has Withdraw.
//   But behaviourally it's wrong: it doesn't honour the Withdraw contract.
//   LSP catches wrong IS-A relationships that structural OOP analysis misses.
//   The fix: FixedDepositPayment IS-NOT-A Payment. It's a FinancialProduct.
//
// THE FOUR LSP RULES — state these clearly:
//
// Rule 1 — Preconditions: Subclass CANNOT ADD new preconditions.
//   Parent: Withdraw(amount > 0) is valid.
//   Child cannot add: "AND amount < maturityDate allows it."
//   Adding preconditions = narrowing what callers can do = breaking callers.
//
// Rule 2 — Postconditions: Subclass CANNOT WEAKEN guarantees.
//   Parent guarantees: returns PaymentResult. Never throws for business reasons.
//   Child cannot weaken: "I throw instead of returning Failure()."
//   Weakening postconditions = callers receive less than promised.
//
// Rule 3 — Exception behaviour: Subclass can only throw exceptions the parent
//   declared, or subtypes of them. Never new exception types the caller can't handle.
//
// Rule 4 — Invariants: Subclass must preserve all parent invariants.
//   Parent: Balance never goes negative.
//   Child cannot allow: Balance = -∞.
//
// DESIGN DECISION: FixedDeposit should NOT extend Payment.
//   It is a financial product, not a payment method.
//   Fix wrong inheritance by separating hierarchies.
//   Use interfaces to model what a class CAN DO, not what it IS.


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 4 — AFTER (LSP-correct)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ✅ AFTER — Separate hierarchies. Honest interfaces. Every contract kept.

// ─── Payment hierarchy — only classes that honour the full Withdraw contract ──
// (CreditCardPayment_LSP and WalletPayment_LSP above already do. Reuse them.)

// ─── FixedDeposit — separate hierarchy, honest about what it can do ────────────

// A financial product that cannot be withdrawn before maturity
public class FixedDeposit
{
    public decimal Amount        { get; }
    public DateTime MaturityDate { get; }

    public FixedDeposit(decimal amount, DateTime maturityDate)
    {
        Amount        = amount;
        MaturityDate  = maturityDate;
    }

    // ✅ FD doesn't implement Withdraw — it can't. It implements Redeem.
    //    The method name is honest about what actually happens.
    public PaymentResult_LSP RedeemAtMaturity()
    {
        if (DateTime.UtcNow < MaturityDate)
            // ✅ Allowed here — this is FD's own contract, not Payment's
            return PaymentResult_LSP.Failure(
                $"FD matures on {MaturityDate:yyyy-MM-dd}. Cannot redeem early.");

        return PaymentResult_LSP.Success();
    }
}

// ─── IFundSource — if FD must appear alongside payments, use an honest interface ──
// The collection is no longer "all are Payments."
// The collection is "all are fund sources." That's honest.

public interface IFundSource
{
    // Contract: returns AvailabilityResult. Never throws. No preconditions beyond amount > 0.
    FundAvailabilityResult CheckAvailability(decimal amount);
}

public record FundAvailabilityResult(bool IsAvailable, string Reason);

// ✅ CreditCard IS-A Payment AND IS-A fund source
public class CreditCardFundSource : Payment_LSP, IFundSource
{
    private const decimal CreditLimit = 50_000m;
    public CreditCardFundSource() : base(CreditLimit) { }

    public override PaymentResult_LSP Withdraw(decimal amount)
    {
        if (amount > CreditLimit) return PaymentResult_LSP.Failure("Exceeds credit limit.");
        Balance -= amount;
        return PaymentResult_LSP.Success();
    }

    // ✅ Also implements IFundSource — genuine capability, no faking
    public FundAvailabilityResult CheckAvailability(decimal amount) =>
        amount <= Balance
            ? new FundAvailabilityResult(true,  $"₹{amount} available on credit card.")
            : new FundAvailabilityResult(false, $"Insufficient credit limit.");
}

// ✅ FixedDeposit IS-A fund source BUT IS-NOT-A Payment — honest hierarchy
public class FixedDepositFundSource : IFundSource
{
    private readonly FixedDeposit _fd;
    public FixedDepositFundSource(FixedDeposit fd) { _fd = fd; }

    // ✅ Implements IFundSource contract only — no Payment contract to violate
    public FundAvailabilityResult CheckAvailability(decimal amount)
    {
        if (DateTime.UtcNow < _fd.MaturityDate)
            return new FundAvailabilityResult(false,
                $"FD locked until {_fd.MaturityDate:yyyy-MM-dd}. Cannot use as fund source.");

        return amount <= _fd.Amount
            ? new FundAvailabilityResult(true,  $"₹{amount} available in matured FD.")
            : new FundAvailabilityResult(false, $"FD value ₹{_fd.Amount} less than required ₹{amount}.");
    }
}

// ✅ A caller using IFundSource — every implementor honours the contract
public class FundAvailabilityChecker
{
    public void CheckAll(IEnumerable<IFundSource> sources, decimal amount)
    {
        foreach (var source in sources)
        {
            // ✅ Safe polymorphism — every IFundSource honours this contract
            var result = source.CheckAvailability(amount);
            Console.WriteLine($"   [{source.GetType().Name}] Available={result.IsAvailable}: {result.Reason}");
        }
    }
}


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 5 — PR REVIEW CHECKLIST
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 👁️ HOW TO SPOT THIS IN A REAL PR REVIEW:
// ✅ SAFE:  Every override returns the same type and honours the same exception contract
// ✅ SAFE:  Swapping any implementation in a unit test keeps all assertions green
// ✅ SAFE:  No caller needs a try-catch that wasn't there before this class existed
// 🚨 FLAG:  override method throws a new exception type the parent never declared
// 🚨 FLAG:  override method returns null when the parent's summary guarantees non-null
// 🚨 FLAG:  Subclass has overrides that make no conceptual sense ("FD.Withdraw — invalid!")
// 🚨 FLAG:  Caller adds type-checking to work around a subclass:
//            if (payment is FixedDepositPayment) skip; else payment.Withdraw(amount);
//            — this IS-A relationship is wrong. The hierarchy needs fixing, not the caller.
// 🚨 FLAG:  NotImplementedException in an override (ISP violation too — File 04 covers this)


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 6 — DEMO
// ╚══════════════════════════════════════════════════════════════════════════════╝

public static class LiskovSubstitutionDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════════════════");
        Console.WriteLine("  L — Liskov Substitution Principle");
        Console.WriteLine("══════════════════════════════════════════════════\n");

        // ── BEFORE: the violation ─────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — FixedDepositPayment_Bad breaks the Payment contract:\n");

        var batch_bad = new List<Payment_LSP>
        {
            new CreditCardPayment_LSP(),
            new WalletPayment_LSP(5000m),
            new FixedDepositPayment_Bad(50_000m, DateTime.UtcNow.AddMonths(6)), // ← the trap
        };

        var processor = new PaymentBatchProcessor();
        Console.WriteLine("   Processing batch — will crash on FixedDeposit entry:");
        try
        {
            processor.ProcessBatch(batch_bad, 1000m); // 💥 throws on FD entry
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"   💥 CRASH: {ex.Message}");
            Console.WriteLine("   ^ CreditCard and Wallet processed fine. FD entry killed the loop.");
            Console.WriteLine("   ^ Subsequent payments in this batch were NEVER processed.\n");
        }

        // ── AFTER: correct hierarchies ────────────────────────────────────────
        Console.WriteLine("✅ AFTER — Correct hierarchies. Every contract kept:\n");

        Console.WriteLine("   Payment batch (only true Payment implementors):");
        var safeBatch = new List<Payment_LSP>
        {
            new CreditCardPayment_LSP(),
            new WalletPayment_LSP(5000m),
            // ✅ FixedDeposit is NOT in this list — honest collection
        };
        processor.ProcessBatch(safeBatch, 1000m); // ✅ no crash, no surprises

        Console.WriteLine("\n   Fund source check (honest interface — FD participates correctly):");
        var fundSources = new List<IFundSource>
        {
            new CreditCardFundSource(),
            new FixedDepositFundSource(new FixedDeposit(50_000m, DateTime.UtcNow.AddMonths(6))), // locked
            new FixedDepositFundSource(new FixedDeposit(20_000m, DateTime.UtcNow.AddDays(-1))),  // matured
        };

        var checker = new FundAvailabilityChecker();
        checker.CheckAll(fundSources, 5_000m);

        Console.WriteLine("\n   Swap CreditCardFundSource for FixedDepositFundSource — caller unaffected.");
        Console.WriteLine("   Every IFundSource honours CheckAvailability(). No crashes. No type-checking.\n");

        Console.WriteLine("✅ Liskov Substitution — understood.");
        Console.WriteLine("→ This creates the need for Interface Segregation Principle (File 04).");
        Console.WriteLine("  LSP ensures implementations are swappable.");
        Console.WriteLine("  But what if the interface itself forces implementors to fake methods?");
    }
}
