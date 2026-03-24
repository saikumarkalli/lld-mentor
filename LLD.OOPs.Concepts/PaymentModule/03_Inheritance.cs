/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🧠 CONCEPT: Inheritance                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Inheritance lets one class (child) reuse the data and behaviour of another  ║
 * ║  (parent), while adding or overriding only what makes it unique. It models   ║
 * ║  the real-world "IS-A" relationship: a CreditCardPayment IS-A Payment.       ║
 * ║                                                                              ║
 * ║  MENTAL MODEL (the analogy to remember forever):                             ║
 * ║  Vehicles. Every vehicle has wheels, an engine, and can Move(). A car IS-A   ║
 * ║  vehicle. A bus IS-A vehicle. You don't write "has wheels" in both — you     ║
 * ║  put it in Vehicle once. Car and Bus only describe what's unique to them.    ║
 * ║                                                                              ║
 * ║  THE PRODUCTION HORROR STORY (why this matters):                             ║
 * ║  A payments team had 4 payment classes with copy-pasted Validate() logic.    ║
 * ║  Security found a bypass: Amount could be set to 0.001 to exploit a          ║
 * ║  rounding bug. They patched 3 of 4 classes. The WalletPayment class — the    ║
 * ║  least-used one — was missed. For 6 weeks, users could transfer fractions   ║
 * ║  of a rupee and trigger the rounding exploit. One class = one fix forever.   ║
 * ║                                                                              ║
 * ║  QUICK RECALL (read this in 2 years and instantly remember):                 ║
 * ║  Write shared logic ONCE in the parent. Children only describe their diff.   ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.PaymentModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — How a beginner naturally writes this
// Three separate classes. Each one feels complete and self-contained.
// They look fine today, but are a maintenance trap the moment you have 3+ types.
// ─────────────────────────────────────────────────────────────────────────────

public class BadCreditCardPayment
{
    // ❌ These 4 fields appear in ALL THREE classes — pure duplication
    public decimal Amount   { get; set; }
    public string  Currency { get; set; } = "INR";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CardNumber { get; set; } = "";
    public DateTime ExpiryDate { get; set; }

    public bool Validate()
    {
        // ❌ Amount > 0 check is copy-pasted in all 3 classes
        if (Amount <= 0) return false;
        if (ExpiryDate < DateTime.UtcNow) return false; // card-specific
        return true;
    }
}

public class BadUpiPayment
{
    // ❌ Exact same 3 fields again
    public decimal Amount   { get; set; }
    public string  Currency { get; set; } = "INR";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string VirtualPaymentAddress { get; set; } = ""; // e.g. sai@okicici

    public bool Validate()
    {
        if (Amount <= 0) return false;    // ❌ Same check, third copy
        // ❌ Bug fixed here: Amount > 0 changed to Amount >= 1 (minimum ₹1).
        //    Did you remember to fix it in BadCreditCardPayment? BadWalletPayment?
        if (!VirtualPaymentAddress.Contains('@')) return false; // UPI-specific
        return true;
    }
}

public class BadWalletPayment
{
    // ❌ Same 3 fields, third time
    public decimal Amount    { get; set; }
    public string  Currency  { get; set; } = "INR";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal WalletBalance { get; set; }

    public bool Validate()
    {
        if (Amount <= 0) return false;            // ❌ The copy that got missed during the security fix
        if (WalletBalance < Amount) return false; // wallet-specific
        return true;
    }
}

// 💥 WHAT GOES WRONG:
// Security patches the Amount > 0 rule across 3 files. Developer fixes 2, forgets 1.
// That 1 becomes a production vulnerability for 6 weeks.
//
// Now add NetBankingPayment. You copy-paste 3 fields AGAIN. And again for Crypto.
// After 5 payment types, changing Currency from "INR" to an enum = 5 file edits.


// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — The OOP-correct way
// Base class holds everything common. Subclasses override only their unique rules.
// Fix Validate() in the base = fixed for every payment type, forever.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Base class for all payment types. Holds shared state and common validation.
/// </summary>
public abstract class Payment
{
    // ✅ Shared fields defined ONCE — all subclasses inherit them automatically
    public decimal  Amount    { get; protected set; }  // protected: subclasses can read/set, outsiders can't
    public string   Currency  { get; protected set; } = "INR";
    public DateTime CreatedAt { get; }                 = DateTime.UtcNow;
    public string   PaymentId { get; }                 = Guid.NewGuid().ToString("N")[..8].ToUpper();

    protected Payment(decimal amount, string currency = "INR")
    {
        // ✅ This check now protects ALL subclasses — written once
        if (amount <= 0)
            throw new ArgumentException($"Amount must be positive. Got: {amount}");
        Amount   = amount;
        Currency = currency;
    }

    /// <summary>
    /// Validates the payment. Base checks amount; subclasses add their own checks.
    /// Call base.Validate() inside overrides to keep the base check.
    /// </summary>
    public virtual bool Validate()
    {
        // ✅ The Amount > 0 check lives exactly here, one time, for all types
        return Amount > 0;
    }

    /// <summary>Subclasses must implement how they actually execute the charge.</summary>
    public abstract void Execute();

    public override string ToString() =>
        $"[{PaymentId}] {GetType().Name} | {Currency} {Amount:F2} | Created: {CreatedAt:HH:mm:ss}";
}

// ✅ CreditCardPayment only adds what's unique to credit cards
public class CreditCardPayment : Payment
{
    public string   CardNumber  { get; }
    public string   CardHolder  { get; }
    public DateTime ExpiryDate  { get; }

    public CreditCardPayment(decimal amount, string cardNumber, string cardHolder, DateTime expiryDate)
        : base(amount)  // ✅ Calls the parent constructor — no duplicate Amount validation
    {
        CardNumber  = cardNumber;
        CardHolder  = cardHolder;
        ExpiryDate  = expiryDate;
    }

    public override bool Validate()
    {
        if (!base.Validate()) return false;           // ✅ Run the base check first
        if (ExpiryDate < DateTime.UtcNow)             // Card-specific: expiry check
        {
            Console.WriteLine("   Card is expired.");
            return false;
        }
        return true;
    }

    public override void Execute() =>
        Console.WriteLine($"   Charging card {CardNumber[^4..].PadLeft(CardNumber.Length, '*')} for {Currency} {Amount}");
}

// ✅ UpiPayment only adds VPA format validation
public class UpiPayment : Payment
{
    public string VirtualPaymentAddress { get; }  // e.g., sai@okicici

    public UpiPayment(decimal amount, string vpa) : base(amount)
    {
        VirtualPaymentAddress = vpa;
    }

    public override bool Validate()
    {
        if (!base.Validate()) return false;           // ✅ Inherit the base Amount check

        // UPI-specific: VPA must be in format localPart@bankhandle
        var parts = VirtualPaymentAddress.Split('@');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            Console.WriteLine($"   Invalid VPA format: {VirtualPaymentAddress}");
            return false;
        }
        return true;
    }

    public override void Execute() =>
        Console.WriteLine($"   Sending UPI push to {VirtualPaymentAddress} for {Currency} {Amount}");
}

// ✅ WalletPayment only adds balance check
public class WalletPayment : Payment
{
    public decimal WalletBalance { get; }

    public WalletPayment(decimal amount, decimal walletBalance) : base(amount)
    {
        WalletBalance = walletBalance;
    }

    public override bool Validate()
    {
        if (!base.Validate()) return false;           // ✅ Inherit the base check

        // Wallet-specific: balance must cover the amount
        if (WalletBalance < Amount)
        {
            Console.WriteLine($"   Insufficient wallet balance. Balance: {WalletBalance}, Required: {Amount}");
            return false;
        }
        return true;
    }

    public override void Execute() =>
        Console.WriteLine($"   Debiting wallet (balance: {WalletBalance}) for {Currency} {Amount}");
}


// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class InheritanceDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  INHERITANCE DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        Console.WriteLine("❌ BEFORE — 3 separate classes, same fields and Validate() in each.");
        Console.WriteLine("   Fix Amount check in one? Must find and fix the others too.\n");

        Console.WriteLine("✅ AFTER — All payments inherit from Payment base:\n");

        // Each payment validates and executes through the shared base
        Payment[] payments =
        {
            new CreditCardPayment(1500m, "4111111111114242", "Sai Kumar", DateTime.UtcNow.AddYears(2)),
            new UpiPayment(500m, "sai@okicici"),
            new WalletPayment(200m, walletBalance: 150m),  // ← will fail: insufficient balance
        };

        foreach (var payment in payments)
        {
            Console.WriteLine($"   Processing: {payment}");
            if (payment.Validate())
                payment.Execute();
            else
                Console.WriteLine($"   Skipped — validation failed.");
            Console.WriteLine();
        }

        Console.WriteLine("   Notice: CreditCardPayment and UpiPayment validated fine.");
        Console.WriteLine("   WalletPayment failed its own balance check.");
        Console.WriteLine("   The Amount > 0 base check protected all three for free.\n");

        Console.WriteLine("✅ Inheritance — understood.");
    }
}
