/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏦 CONCEPT: Encapsulation                                                   ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: Nothing yet — this is the foundation everything else          ║
 * ║     stands on. Data must be owned by the class before it can be             ║
 * ║     abstracted (File 2), inherited (File 3), or polymorphically called       ║
 * ║     (File 4). Every subsequent file depends on this principle.               ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Encapsulation means bundling data (fields) and the rules that govern it    ║
 * ║  inside one class — and making fields private so only the class can touch   ║
 * ║  them. You interact through methods (doors), not fields (windows you climb  ║
 * ║  through). The object controls its own state; invalid state becomes          ║
 * ║  impossible by design.                                                       ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  A bank's core banking system. You, the teller, cannot open a terminal and  ║
 * ║  type a new balance directly into the database. You initiate a Deposit       ║
 * ║  transaction — which goes through fraud checks, audit logging, and only then ║
 * ║  updates the balance. The system owns its own data. You go through the door  ║
 * ║  it provides, or you don't go at all.                                        ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A bank's internal support tool had a BankAccount object with a public      ║
 * ║  IsLocked field. A support engineer ran a batch cleanup script that set      ║
 * ║  IsLocked = false on 847 accounts — including 23 accounts the fraud team    ║
 * ║  had frozen during an active investigation. The fraudsters noticed within    ║
 * ║  45 minutes. ₹1.8 crore was withdrawn before anyone caught it. The script   ║
 * ║  compiled fine. The type system never complained. There was no guard.        ║
 * ║                                                                              ║
 * ║  QUICK RECALL (your 2-year cheat code):                                     ║
 * ║  The class is the gatekeeper. Data lives inside; methods are the only       ║
 * ║  doors. No one bypasses the gatekeeper.                                      ║
 * ║                                                                              ║
 * ║  HOW THIS CONNECTS TO THE NEXT CONCEPT:                                     ║
 * ║  Now that BankAccount owns and protects its data, File 2 (Abstraction) will ║
 * ║  hide HOW the banking service enforces its rules — callers only see a       ║
 * ║  clean 4-method interface, not the 60 lines behind it.                      ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.BankModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — What a beginner naturally writes
// Public fields feel like "flexibility". You can set anything from anywhere.
// It works fine when you're the only developer. The moment a second person
// touches this class, or a script runs against 847 accounts, all bets are off.
// ─────────────────────────────────────────────────────────────────────────────

public class NaiveBankAccount
{
    public decimal Balance;          // ← anyone can set this to any value — no audit
    public string  Pin = "1234";     // ← PIN is publicly readable. Print it to logs?
    public bool    IsLocked;         // ← anyone can unlock a fraud-flagged account
    public int     FailedAttempts;   // ← reset this and bypass the lockout mechanism
    public string  AccountNumber;
    public string  HolderName;

    public NaiveBankAccount(string accountNumber, string holderName)
    {
        AccountNumber = accountNumber;
        HolderName    = holderName;
    }
}

// 💥 WHAT BREAKS:
// All four of these compile cleanly. The type system cannot protect you.
//
//   account.Balance += 1_000_000;   // fraud — no audit trail, no validation
//   Console.WriteLine(account.Pin); // security breach — PIN printed to logs
//   account.IsLocked = false;       // unfreezes a fraud-investigation account silently
//   account.FailedAttempts = 0;     // resets lockout counter — bypasses brute-force guard

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — OOP-correct encapsulation
// Every field is private. The class is the ONLY thing that can change state.
// Callers use Deposit(), Withdraw(), VerifyPin(), Unlock() — never touch fields.
// Invalid state is not just discouraged — it is structurally impossible.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A bank account where all state changes go through controlled methods.
/// Balance cannot go negative. PIN cannot be read after creation.
/// Account auto-locks after 3 failed PIN attempts; only Unlock() resets it.
/// </summary>
public class BankAccount
{
    // All private — the class is the only thing that reads or writes these.
    private decimal       _balance;
    private string        _pin = null!;    // assigned via Pin setter in constructor — never null after ctor
    private int           _failedAttempts;
    private readonly List<string> _auditLog = [];

    // Immutable after construction — set once in constructor, never writable again.
    public string AccountNumber { get; }
    public string HolderName    { get; }

    /// <summary>Balance is readable by anyone, but only Deposit/Withdraw can change it.</summary>
    public decimal Balance => _balance;

    // ← Pin has a SETTER but NO getter.
    // You can assign a new PIN, but you can NEVER read the PIN back out.
    // Try `account.Pin` in code → compiler error: "Property 'Pin' has no getter."
    // This is write-only encapsulation — a real security pattern used in production.
    public string Pin
    {
        set
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 4 || !value.All(char.IsDigit))
                throw new ArgumentException("PIN must be exactly 4 digits.");
            _pin = value;
            _auditLog.Add($"[{DateTime.Now:HH:mm:ss}] PIN changed.");
        }
    }

    // ← IsLocked is COMPUTED, not stored. It cannot be set directly.
    // No setter means no script can call account.IsLocked = false.
    // The ONLY way to unlock is through Unlock() — which represents a manager action.
    public bool IsLocked => _failedAttempts >= 3;

    public BankAccount(string accountNumber, string holderName, string initialPin, decimal openingBalance = 0)
    {
        if (openingBalance < 0) throw new ArgumentException("Opening balance cannot be negative.");

        AccountNumber = accountNumber;
        HolderName    = holderName;
        _balance      = openingBalance;
        Pin           = initialPin;   // ← goes through the setter — triggers validation

        _auditLog.Add($"[{DateTime.Now:HH:mm:ss}] Account opened. Opening balance: ₹{openingBalance:N0}.");
    }

    /// <summary>
    /// Deposits money into the account. Rejects zero or negative amounts.
    /// Every deposit is recorded in the immutable audit log.
    /// </summary>
    public virtual void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException($"Deposit amount must be positive. Got: ₹{amount}.");
        if (IsLocked)    throw new InvalidOperationException("Account is locked. Contact your branch manager.");

        _balance += amount;
        _auditLog.Add($"[{DateTime.Now:HH:mm:ss}] DEPOSIT ₹{amount:N0}. New balance: ₹{_balance:N0}.");
    }

    /// <summary>
    /// Withdraws money. Enforces: amount > 0, sufficient balance, account not locked.
    /// Marked virtual so subclasses (SavingsAccount, CurrentAccount) can extend the
    /// rules — e.g. minimum balance check, overdraft allowance.
    /// </summary>
    public virtual void Withdraw(decimal amount)
    {
        if (amount <= 0)       throw new ArgumentException($"Withdrawal must be positive. Got: ₹{amount}.");
        if (IsLocked)          throw new InvalidOperationException("Account is locked. Contact your branch manager.");
        if (amount > _balance) throw new InvalidOperationException(
            $"Insufficient balance. Balance: ₹{_balance:N0}, Requested: ₹{amount:N0}.");

        _balance -= amount;
        _auditLog.Add($"[{DateTime.Now:HH:mm:ss}] WITHDRAWAL ₹{amount:N0}. New balance: ₹{_balance:N0}.");
    }

    /// <summary>
    /// Verifies the PIN. Increments failed attempts on mismatch.
    /// After 3 failures IsLocked becomes true automatically — no external call needed.
    /// </summary>
    public bool VerifyPin(string input)
    {
        if (input == _pin)
        {
            _failedAttempts = 0;  // reset only on success
            return true;
        }

        _failedAttempts++;
        _auditLog.Add($"[{DateTime.Now:HH:mm:ss}] FAILED PIN attempt #{_failedAttempts}.");

        if (IsLocked)
            _auditLog.Add($"[{DateTime.Now:HH:mm:ss}] ⚠ ACCOUNT LOCKED after {_failedAttempts} failed attempts.");

        return false;
    }

    /// <summary>
    /// Manager-only action. Clears failed attempts and unlocks the account.
    /// In production: requires manager ID, reason, and dual approval. Simplified here.
    /// </summary>
    public void Unlock(string managerNote)
    {
        _failedAttempts = 0;
        _auditLog.Add($"[{DateTime.Now:HH:mm:ss}] UNLOCKED. Manager note: {managerNote}.");
    }

    /// <summary>
    /// Virtual — subclasses override with their own interest rates.
    /// Base account has no interest; this method exists so File 3 can override it.
    /// </summary>
    public virtual decimal CalculateInterest() => 0m;

    /// <summary>Virtual — subclasses return their human-readable type name.</summary>
    public virtual string GetAccountType() => "Bank Account";

    /// <summary>Returns the immutable audit trail for this account.</summary>
    public IReadOnlyList<string> GetAuditLog() => _auditLog.AsReadOnly();

    public override string ToString() =>
        $"[{AccountNumber}] {HolderName} | {GetAccountType()} | ₹{_balance:N0} | {(IsLocked ? "🔒 LOCKED" : "✅ Active")}";
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

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — public fields, no guards:");
        var bad = new NaiveBankAccount("ACC001", "Rahul Sharma");

        bad.Balance       += 1_000_000;   // fraud — compiles fine
        bad.Pin            = "0000";       // PIN overwritten, still readable
        bad.IsLocked       = false;        // fraud freeze bypassed silently
        bad.FailedAttempts = 0;            // lockout reset silently

        Console.WriteLine($"   Balance set directly to ₹{bad.Balance:N0} — no audit, no validation.");
        Console.WriteLine($"   PIN is readable: \"{bad.Pin}\" ← anyone can print this to logs.");
        Console.WriteLine($"   IsLocked was set to {bad.IsLocked} — fraud team's work undone.\n");

        // ── AFTER ───────────────────────────────────────────────────────────
        Console.WriteLine("✅ AFTER — private fields, every change goes through a method:\n");

        var account = new BankAccount("ACC002", "Priya Mehta", "4821", openingBalance: 10_000m);
        Console.WriteLine($"   Created: {account}");

        account.Deposit(5_000m);
        Console.WriteLine($"   After ₹5,000 deposit:     {account}");

        account.Withdraw(3_000m);
        Console.WriteLine($"   After ₹3,000 withdrawal:  {account}");

        // Show PIN is write-only — the following line would be a COMPILER ERROR:
        // Console.WriteLine(account.Pin); ← Property 'Pin' has no getter.
        Console.WriteLine("\n   account.Pin has no getter — compiler prevents reading it.");

        Console.WriteLine("\n   Simulating 3 wrong PIN attempts:");
        Console.WriteLine($"   VerifyPin(\"0000\"): {account.VerifyPin("0000")}");
        Console.WriteLine($"   VerifyPin(\"1111\"): {account.VerifyPin("1111")}");
        Console.WriteLine($"   VerifyPin(\"2222\"): {account.VerifyPin("2222")}");
        Console.WriteLine($"   Account state after 3 failures: {account}");

        Console.WriteLine("\n   Trying to deposit while locked:");
        try { account.Deposit(100); }
        catch (InvalidOperationException ex) { Console.WriteLine($"   Caught: {ex.Message}"); }

        account.Unlock("Branch Manager Singh — customer verified with ID at counter.");
        Console.WriteLine($"   After manager unlock: {account}");

        Console.WriteLine("\n   Full audit trail:");
        foreach (var entry in account.GetAuditLog())
            Console.WriteLine($"   {entry}");

        Console.WriteLine("\n✅ Encapsulation — understood.");
    }
}
