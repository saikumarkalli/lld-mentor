/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  SECTION 1 — FILE HEADER                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  💎 SOLID PRINCIPLE: O — Open/Closed Principle (OCP)                         ║
 * ║                                                                              ║
 * ║  🔗 OOP ORIGIN: "In your OOP module, PaymentProcessor.ProcessAll() had zero  ║
 * ║     if-else. It called payment.Validate() and payment.Execute() on whatever  ║
 * ║     Payment object it received. That was OCP working silently.               ║
 * ║     But your PaymentOrchestrator from File 01 grows a new if-else every time ║
 * ║     a new payment TYPE is added. Here's why that's dangerous."               ║
 * ║                                                                              ║
 * ║  THE ONE-LINE RULE (memorise this):                                          ║
 * ║  "Open for EXTENSION (add new behaviour),                                    ║
 * ║   Closed for MODIFICATION (don't edit existing code to add it).             ║
 * ║   New feature = new file. Not an edit to an old file."                       ║
 * ║                                                                              ║
 * ║  WHAT GOES WRONG WITHOUT IT:                                                 ║
 * ║  Every new payment type (UPI, crypto, BNPL, wallet) means opening            ║
 * ║  PaymentOrchestrator and adding another if-else block.                       ║
 * ║  Each edit risks breaking existing payment types.                            ║
 * ║  In a payment system, breaking existing flows = financial loss + RBI scrutiny║
 * ║                                                                              ║
 * ║  THE PR SMELL:                                                               ║
 * ║  "In a code review, you'd spot this when a PR adds a new payment type but    ║
 * ║   also modifies an existing switch or if-else in the core processor."        ║
 * ║                                                                              ║
 * ║  CONNECTS TO NEXT:                                                           ║
 * ║  "IPaymentStrategy lets us swap payment implementations.                     ║
 * ║   But can we TRULY swap them without the caller knowing?                     ║
 * ║   What if CryptoStrategy.Execute() throws differently than CreditCard?       ║
 * ║   That's the contract problem — File 03 (LSP)."                             ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLDMaster.SOLID.PaymentModule.OCP;

// ─────────────────────────────────────────────────────────────────────────────
// Minimal shared types — just enough to make the violation and fix tangible
// ─────────────────────────────────────────────────────────────────────────────

public enum PaymentType { CreditCard, Upi, Crypto, Bnpl, Wallet }

public record PaymentRequest_OCP(PaymentType Type, decimal Amount, string Token);

public record PaymentResult_OCP(bool IsSuccess, string TransactionId, string Message)
{
    public static PaymentResult_OCP Success(string txId) =>
        new(true, txId, "Payment processed successfully.");

    public static PaymentResult_OCP Failure(string reason) =>
        new(false, string.Empty, reason);
}

public class UnsupportedPaymentTypeException(PaymentType type)
    : Exception($"No handler registered for payment type: {type}");


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 2 — BEFORE (minimal violation)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ❌ BEFORE — PaymentOrchestrator after 3 payment types have been added.
// Each sprint someone opened this file and added one more else-if block.
// The method grows with every new type. Every edit is a regression risk.

public class PaymentOrchestrator_Bad
{
    public PaymentResult_OCP Process(PaymentRequest_OCP request)
    {
        // Original: just credit card — this method was clean on day one
        if (request.Type == PaymentType.CreditCard)
        {
            Console.WriteLine($"   [CC] Validating card expiry for token {request.Token}...");
            Console.WriteLine($"   [CC] Charging card: ₹{request.Amount}");
            return PaymentResult_OCP.Success(NewTxId());
        }
        // Sprint 2: UPI added — this file was opened and edited
        else if (request.Type == PaymentType.Upi)
        {
            Console.WriteLine($"   [UPI] Verifying VPA {request.Token}...");
            Console.WriteLine($"   [UPI] Sending UPI push: ₹{request.Amount}");
            return PaymentResult_OCP.Success(NewTxId());
        }
        // Sprint 3: Crypto added — this file was opened and edited again
        else if (request.Type == PaymentType.Crypto)
        {
            Console.WriteLine($"   [Crypto] Validating wallet {request.Token}...");
            Console.WriteLine($"   [Crypto] Broadcasting transaction: ₹{request.Amount}");
            return PaymentResult_OCP.Success(NewTxId());
        }
        // Sprint 4: BNPL — open this file again, add another block, re-test everything
        // Sprint 5: Wallet — open this file again...
        // This method GROWS FOREVER. It is never "closed."

        return PaymentResult_OCP.Failure($"Unknown payment type: {request.Type}");
    }

    private static string NewTxId() => Guid.NewGuid().ToString("N")[..8].ToUpper();
}

// 🚨 VIOLATION: O — adding BNPL requires editing Process() again
//                   every edit is a fresh regression risk for CreditCard, UPI, Crypto
//
// 💥 CONSEQUENCE: UPI payment broke when Crypto was added (shared variable bug).
//    Took 4 hours to find. All 3 payment types were down during Diwali sale.
//    Two teams edited Process() in the same sprint. Merge conflict.
//    One team's changes silently overwrote the other's.


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 3 — WHY THIS VIOLATES OCP
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 🔍 THE REASONING:
//
// PROBLEM 1 — REGRESSION RISK ON EVERY EDIT:
//   Every time Process() is opened, there's a chance of breaking existing branches.
//   Shared local variables appear over time. An off-by-one error in BNPL code
//   corrupts the UPI branch. The more branches, the higher the risk per edit.
//   At 6 payment types, a developer must understand ALL of them to safely add one.
//
// PROBLEM 2 — TEAM COLLISION AT SCALE:
//   UPI team and Crypto team work in the same sprint. Both edit Process().
//   Even with careful merging, one team's changes can overwrite the other's.
//   This is a structural problem — two teams competing for one method.
//   OCP fixes this by giving each team their own file.
//
// PROBLEM 3 — REVIEW BURDEN DOUBLES WITH EVERY TYPE:
//   To review the BNPL addition, a reviewer must understand CreditCard, UPI,
//   and Crypto code to safely approve it. New features require full comprehension
//   of all existing features. Review time compounds. PRs sit unreviewed.
//
// DESIGN DECISION: Strategy pattern — your first Design Patterns preview.
//   Each payment type is a strategy. PaymentOrchestrator selects the strategy.
//   Adding BNPL = create BnplPaymentStrategy.cs. Zero edits to the orchestrator.
//   The orchestrator method NEVER changes again. Open for extension. Closed for modification.
//   (You'll formalise Strategy Pattern in Phase 3. Here you're seeing it born.)


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 4 — AFTER (OCP-correct)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ✅ AFTER — Strategy pattern. Each payment type is an independent class.
// Adding a new type = new file, zero edits to PaymentOrchestrator.

// ✅ The contract every payment strategy must fulfil
public interface IPaymentStrategy
{
    // CanHandle() lets the orchestrator find the right strategy at runtime
    bool CanHandle(PaymentType type);

    // Execute() processes the payment — every strategy honours this contract
    PaymentResult_OCP Execute(PaymentRequest_OCP request);
}

// ✅ Strategy 1 — Credit Card. Created once. Never touched when UPI or BNPL is added.
public class CreditCardStrategy : IPaymentStrategy
{
    public bool CanHandle(PaymentType type) => type == PaymentType.CreditCard;

    public PaymentResult_OCP Execute(PaymentRequest_OCP request)
    {
        Console.WriteLine($"   [CC] Validating card expiry for token {request.Token}...");
        Console.WriteLine($"   [CC] Charging card: ₹{request.Amount}");
        return PaymentResult_OCP.Success(NewTxId());
    }

    private static string NewTxId() => Guid.NewGuid().ToString("N")[..8].ToUpper();
}

// ✅ Strategy 2 — UPI. Created once. The CC strategy above is unaffected by this file existing.
public class UpiStrategy : IPaymentStrategy
{
    public bool CanHandle(PaymentType type) => type == PaymentType.Upi;

    public PaymentResult_OCP Execute(PaymentRequest_OCP request)
    {
        Console.WriteLine($"   [UPI] Verifying VPA {request.Token}...");
        Console.WriteLine($"   [UPI] Sending UPI push: ₹{request.Amount}");
        return PaymentResult_OCP.Success(NewTxId());
    }

    private static string NewTxId() => Guid.NewGuid().ToString("N")[..8].ToUpper();
}

// ✅ Strategy 3 — Crypto. New file, zero edits to anything above.
public class CryptoStrategy : IPaymentStrategy
{
    public bool CanHandle(PaymentType type) => type == PaymentType.Crypto;

    public PaymentResult_OCP Execute(PaymentRequest_OCP request)
    {
        Console.WriteLine($"   [Crypto] Validating wallet {request.Token}...");
        Console.WriteLine($"   [Crypto] Broadcasting transaction: ₹{request.Amount}");
        return PaymentResult_OCP.Success(NewTxId());
    }

    private static string NewTxId() => Guid.NewGuid().ToString("N")[..8].ToUpper();
}

// ✅ Adding BNPL tomorrow: create this class, register in DI. DONE. Nothing else changes.
public class BnplStrategy : IPaymentStrategy
{
    public bool CanHandle(PaymentType type) => type == PaymentType.Bnpl;

    public PaymentResult_OCP Execute(PaymentRequest_OCP request)
    {
        Console.WriteLine($"   [BNPL] Checking credit limit for account {request.Token}...");
        Console.WriteLine($"   [BNPL] Deferring ₹{request.Amount} over 3 months...");
        return PaymentResult_OCP.Success(NewTxId());
    }

    private static string NewTxId() => Guid.NewGuid().ToString("N")[..8].ToUpper();
}

// ✅ The orchestrator — this Process() method NEVER changes again.
// In ASP.NET Core you'd register each strategy with DI and inject IEnumerable<IPaymentStrategy>.
// builder.Services.AddScoped<IPaymentStrategy, CreditCardStrategy>();
// builder.Services.AddScoped<IPaymentStrategy, UpiStrategy>();
// builder.Services.AddScoped<IPaymentStrategy, BnplStrategy>(); // ← just add this line
public class PaymentOrchestrator_OCP
{
    private readonly IEnumerable<IPaymentStrategy> _strategies;

    public PaymentOrchestrator_OCP(IEnumerable<IPaymentStrategy> strategies)
    {
        _strategies = strategies;
    }

    // ✅ This method is CLOSED. It will never be edited to add a new payment type.
    //    Open for extension: add BnplStrategy → it works.
    //    Closed for modification: Process() body stays exactly as written here, forever.
    public PaymentResult_OCP Process(PaymentRequest_OCP request)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(request.Type))
            ?? throw new UnsupportedPaymentTypeException(request.Type);

        return strategy.Execute(request);
    }
}


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 5 — PR REVIEW CHECKLIST
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 👁️ HOW TO SPOT THIS IN A REAL PR REVIEW:
// ✅ SAFE:  Adding a new payment type creates a new .cs file — nothing else is modified
// ✅ SAFE:  Core processing method has no if/switch on payment type
// ✅ SAFE:  Existing tests all pass after the new type is added (zero regression)
// 🚨 FLAG:  switch(paymentType) or if(type == "x") in a processing method
// 🚨 FLAG:  PR description says "I just need to add one more else-if" — classic OCP violation
// 🚨 FLAG:  A method grows by 20+ lines every sprint (it is open for modification, never closed)
// 🚨 FLAG:  Existing tests break when a new feature is added (modification, not extension)
// 🚨 FLAG:  Two developers editing the same method in the same sprint (structural team collision)


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 6 — DEMO
// ╚══════════════════════════════════════════════════════════════════════════════╝

public static class OpenClosedDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════════════════");
        Console.WriteLine("  O — Open/Closed Principle");
        Console.WriteLine("══════════════════════════════════════════════════\n");

        // ── BEFORE: the violation ─────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — if-else grows with every payment type:\n");
        var badOrchestrator = new PaymentOrchestrator_Bad();
        badOrchestrator.Process(new PaymentRequest_OCP(PaymentType.CreditCard, 1500m, "tok_cc"));
        badOrchestrator.Process(new PaymentRequest_OCP(PaymentType.Upi,        500m,  "sai@okicici"));
        badOrchestrator.Process(new PaymentRequest_OCP(PaymentType.Crypto,     999m,  "0x742d35Cc"));
        Console.WriteLine("\n   ^ Adding BNPL = open this file, add else-if, risk breaking all three above.\n");

        // ── AFTER: the fix ────────────────────────────────────────────────────
        Console.WriteLine("✅ AFTER — Strategy pattern. Process() never changes again:\n");

        // Simulate what ASP.NET Core DI does at startup:
        var strategies = new List<IPaymentStrategy>
        {
            new CreditCardStrategy(),
            new UpiStrategy(),
            new CryptoStrategy(),
            new BnplStrategy(),   // ← added BNPL. Zero edits to PaymentOrchestrator_OCP.
        };

        var orchestrator = new PaymentOrchestrator_OCP(strategies);

        orchestrator.Process(new PaymentRequest_OCP(PaymentType.CreditCard, 1500m, "tok_cc"));
        orchestrator.Process(new PaymentRequest_OCP(PaymentType.Upi,        500m,  "sai@okicici"));
        orchestrator.Process(new PaymentRequest_OCP(PaymentType.Crypto,     999m,  "0x742d35Cc"));
        orchestrator.Process(new PaymentRequest_OCP(PaymentType.Bnpl,       2000m, "ACC001"));

        Console.WriteLine("\n   BNPL was added: new file created, zero existing lines edited.");
        Console.WriteLine("   CreditCard, UPI, Crypto: untouched, cannot have regressed.\n");

        Console.WriteLine("✅ Open/Closed — understood.");
        Console.WriteLine("→ This creates the need for Liskov Substitution Principle (File 03).");
        Console.WriteLine("  IPaymentStrategy lets us swap strategies — but are the swaps truly safe?");
        Console.WriteLine("  What if CryptoStrategy throws where CreditCardStrategy returns a result?");
    }
}
