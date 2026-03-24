/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🧠 CONCEPT: Encapsulation                                                   ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Encapsulation means bundling data (fields) and the rules that govern that   ║
 * ║  data (methods) inside one class — and hiding the data so outsiders can't    ║
 * ║  break it. The object controls its own state; you can only change it through ║
 * ║  the doors the object provides.                                              ║
 * ║                                                                              ║
 * ║  MENTAL MODEL (the analogy to remember forever):                             ║
 * ║  A payment terminal at a coffee shop. You tap your card — that's the public  ║
 * ║  interface. You cannot reach inside the terminal and set "isApproved = true" ║
 * ║  yourself. The machine runs its own checks, then changes its own state.      ║
 * ║  You only get to interact through the slot it exposes.                       ║
 * ║                                                                              ║
 * ║  THE PRODUCTION HORROR STORY (why this matters):                             ║
 * ║  A fintech startup stored transaction status as a public string field.       ║
 * ║  A junior dev writing a UI dashboard accidentally wrote:                     ║
 * ║      transaction.Status = "Completed";                                       ║
 * ║  ...to make the UI look nice during a demo. It slipped into production.      ║
 * ║  Orders were marked Completed before payment was ever collected. The company ║
 * ║  shipped ₹4 lakh worth of goods for free before anyone noticed.             ║
 * ║                                                                              ║
 * ║  QUICK RECALL (read this in 2 years and instantly remember):                 ║
 * ║  The class owns its data — outsiders use doors (methods), not windows.       ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.PaymentModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — How a beginner naturally writes this
// Public fields feel convenient — you can just set whatever you need from anywhere.
// It works fine when you're the only developer, but the moment a second person
// touches this class, all bets are off. Nothing stops invalid data.
// ─────────────────────────────────────────────────────────────────────────────

public class BadPaymentTransaction
{
    // ❌ All fields are public — any code anywhere can write anything here
    public decimal Amount;       // ❌ Nothing stops Amount = -999
    public string Status;        // ❌ Can be set to any random string
    public string CardLast4;     // ❌ Card details exposed and writable from outside

    public BadPaymentTransaction(decimal amount, string cardLast4)
    {
        Amount = amount;
        Status = "Pending";
        CardLast4 = cardLast4;
    }
}

// 💥 WHAT GOES WRONG:
// The following code is perfectly legal — the compiler won't complain at all:
//
//   var tx = new BadPaymentTransaction(500m, "4242");
//   tx.Amount = -999;              // Silent negative amount — refund exploit
//   tx.Status = "Completed";       // Marks as done before any processing
//   tx.CardLast4 = "0000";         // Overwrites card data mid-transaction
//
// Status can also be set to complete nonsense:
//   tx.Status = "DefinitelyLegit"; // No validation, no error
//
// And you can never enforce that status moves FORWARD only (Pending → Completed).
// Someone can always go Completed → Pending → Completed to re-trigger rewards.


// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — The OOP-correct way
// Private fields, public properties with guards, and explicit methods are the
// only legal doors to change state. The class protects its own invariants.
// Status can only flow forward: Pending → Processing → Completed/Failed.
// ─────────────────────────────────────────────────────────────────────────────

public class PaymentTransaction
{
    // ✅ Private fields — nobody outside this class can touch these directly
    private decimal _amount;
    private string _status;
    private readonly string _cardLast4;   // readonly: set once in constructor, never again
    private readonly string _transactionId;

    /// <summary>Creates a new payment transaction in Pending state.</summary>
    /// <param name="amount">Must be greater than zero.</param>
    /// <param name="cardLast4">Last 4 digits of the card used.</param>
    public PaymentTransaction(decimal amount, string cardLast4)
    {
        // ✅ Validation happens at construction — you can never create a broken transaction
        if (amount <= 0)
            throw new ArgumentException($"Amount must be positive. Got: {amount}");

        if (cardLast4?.Length != 4 || !cardLast4.All(char.IsDigit))
            throw new ArgumentException("CardLast4 must be exactly 4 digits.");

        _amount = amount;
        _cardLast4 = cardLast4;
        _status = "Pending";                          // Always starts as Pending
        _transactionId = Guid.NewGuid().ToString("N")[..12].ToUpper();
    }

    // ✅ Properties expose data for reading, but not for arbitrary writing
    public decimal Amount => _amount;                 // Get only — nobody can change amount externally
    public string Status => _status;                  // Get only — only our methods can change this
    public string CardLast4 => $"****{_cardLast4}";  // Even the read is masked for security
    public string TransactionId => _transactionId;

    // ✅ These are the ONLY legal ways to change Status.
    //    Each method checks: "is this transition currently valid?"

    /// <summary>Move from Pending → Processing. Call this when gateway call starts.</summary>
    public void Authorize()
    {
        // ✅ Guard: only valid from Pending state
        if (_status != "Pending")
            throw new InvalidOperationException(
                $"Cannot authorize a transaction that is already '{_status}'.");

        _status = "Processing";
        Console.WriteLine($"[{_transactionId}] Authorized — now Processing.");
    }

    /// <summary>Move from Processing → Completed. Call this on gateway success response.</summary>
    public void Complete()
    {
        // ✅ Guard: must be Processing before Completing
        if (_status != "Processing")
            throw new InvalidOperationException(
                $"Cannot complete a transaction that is '{_status}'. It must be Processing first.");

        _status = "Completed";
        Console.WriteLine($"[{_transactionId}] ✅ Completed. Amount charged: ₹{_amount}");
    }

    /// <summary>Move from Processing → Failed. Call this on gateway failure/timeout.</summary>
    public void Fail(string reason)
    {
        if (_status != "Processing")
            throw new InvalidOperationException(
                $"Cannot fail a transaction that is '{_status}'.");

        _status = "Failed";
        Console.WriteLine($"[{_transactionId}] ❌ Failed. Reason: {reason}");
    }

    public override string ToString() =>
        $"Transaction {_transactionId} | {CardLast4} | ₹{_amount} | {_status}";
}


// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class EncapsulationDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  ENCAPSULATION DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        // --- BEFORE: showing the problem ---
        Console.WriteLine("❌ BEFORE — Bad transaction (no guards):");
        var bad = new BadPaymentTransaction(500m, "4242");
        bad.Amount = -999;              // Silent exploit — no error thrown
        bad.Status = "Completed";       // Skipped the entire payment process
        Console.WriteLine($"   Amount: {bad.Amount}, Status: {bad.Status}");
        Console.WriteLine("   ^ Amount is negative and status is Completed — no processing happened!\n");

        // --- AFTER: correct flow ---
        Console.WriteLine("✅ AFTER — Good transaction (state machine enforced):");
        var tx = new PaymentTransaction(1500m, "4242");
        Console.WriteLine($"   Created: {tx}");

        tx.Authorize();   // Pending → Processing
        tx.Complete();    // Processing → Completed
        Console.WriteLine($"   Final:   {tx}");

        // Proving you cannot go backwards or skip steps:
        Console.WriteLine("\n   Trying to Authorize an already-Completed transaction...");
        try
        {
            tx.Authorize(); // 💥 This will throw
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"   Caught expected error: {ex.Message}");
        }

        // Proving negative amounts are rejected at construction:
        Console.WriteLine("\n   Trying to create a transaction with Amount = -500...");
        try
        {
            var bad2 = new PaymentTransaction(-500m, "1234"); // 💥 Throws immediately
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"   Caught expected error: {ex.Message}");
        }

        Console.WriteLine("\n✅ Encapsulation — understood.");
    }
}
