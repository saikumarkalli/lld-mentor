/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏦 CONCEPT: Inheritance                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 02_Abstraction.cs                                             ║
 * ║     We have IBankService as an abstraction for service behaviour. Now we    ║
 * ║     apply the same thinking to DATA: different account types share common   ║
 * ║     structure and behaviour — put it in one place they all inherit from.    ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Inheritance lets a class acquire the fields and methods of a parent class, ║
 * ║  then add or override only what's unique to it. The parent holds the shared ║
 * ║  code; the child only defines what's different. Fix a bug in the parent and ║
 * ║  every child benefits — instantly, everywhere.                               ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  An RBI-mandated account agreement template. Every bank account in India    ║
 * ║  must have an account number, holder name, opening date, and basic          ║
 * ║  deposit/withdrawal rules — those are in the template. A savings account    ║
 * ║  then adds minimum balance rules. A fixed deposit adds a maturity date.     ║
 * ║  Each account "inherits" the standard template and fills in its own clauses.║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  An NBFC had SavingsAccount and CurrentAccount as completely separate        ║
 * ║  classes — same fields, same Deposit(), same GetStatement(). RBI issued a  ║
 * ║  new circular: interest calculation must include daily closing balance, not ║
 * ║  monthly average. The developer updated SavingsAccount. Forgot               ║
 * ║  CurrentAccount. Both classes passed their own tests. Six months later an  ║
 * ║  audit found a ₹2.3 crore discrepancy in interest payouts. Two classes.    ║
 * ║  One fix. Developer updated one. Classic copy-paste inheritance debt.        ║
 * ║                                                                              ║
 * ║  QUICK RECALL (your 2-year cheat code):                                     ║
 * ║  One fix in the base class = fixed everywhere. Copy-paste code = bug in     ║
 * ║  N places, developer fixes N–1 of them, audit finds the last one.           ║
 * ║                                                                              ║
 * ║  HOW THIS CONNECTS TO THE NEXT CONCEPT:                                     ║
 * ║  SavingsAccount and CurrentAccount are now different TYPES that share a     ║
 * ║  base. File 4 (Polymorphism) shows why that matters: one method can process ║
 * ║  ALL account types without ever asking "which type am I dealing with?"      ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.BankModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — What a beginner naturally writes
// "Each account type is its own thing — just write separate classes."
// It feels clean at first. You have full control over each class.
// Then RBI changes the interest calculation rule and you have 3 places to fix.
// ─────────────────────────────────────────────────────────────────────────────

public class NaiveSavingsAccount
{
    public string  AccountNumber = string.Empty;  // ← same as NaiveCurrentAccount
    public string  HolderName    = string.Empty;  // ← same
    public decimal Balance;        // ← same
    public DateTime OpenDate;      // ← same
    public decimal MinimumBalance = 1_000m;

    public void Deposit(decimal amount)  => Balance += amount;   // ← same logic as NaiveCurrentAccount
    public void Withdraw(decimal amount) => Balance -= amount;   // ← same logic

    // ← BUG WAITING TO HAPPEN: RBI changes interest formula — must update BOTH classes.
    public decimal CalculateInterest() => Balance * 0.035m;
    public string  GetStatement()      => $"Savings | {AccountNumber} | ₹{Balance:N0}"; // ← same
}

public class NaiveCurrentAccount
{
    public string  AccountNumber = string.Empty;  // ← copy-pasted from NaiveSavingsAccount
    public string  HolderName    = string.Empty;  // ← copy-pasted
    public decimal Balance;        // ← copy-pasted
    public DateTime OpenDate;      // ← copy-pasted
    public decimal OverdraftLimit = 50_000m;

    public void Deposit(decimal amount)  => Balance += amount;   // ← copy-pasted
    public void Withdraw(decimal amount) => Balance -= amount;   // ← copy-pasted

    // ← Developer fixed the interest formula in NaiveSavingsAccount.
    //   This class was forgotten. ₹2.3 crore discrepancy found 6 months later.
    public decimal CalculateInterest() => 0m;  // ← intentionally 0 for Current, but this method
                                               //   was originally copy-pasted as 3.5% and took
                                               //   weeks to discover in the audit.
    public string GetStatement() => $"Current | {AccountNumber} | ₹{Balance:N0}"; // ← copy-pasted
}

// 💥 WHAT BREAKS:
// — Fix Deposit() input validation? Must find and fix it in N classes.
// — Add a transaction fee to GetStatement()? Fix in N classes.
// — RBI audit requires new field (BranchCode)? Add to N classes.
// — Each class has its own tests, but the SHARED logic has N sets of bugs.

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — OOP-correct inheritance
// AccountBase holds everything shared: fields, Deposit(), GetStatement().
// Each subclass only defines what's UNIQUE to it.
// Fix CalculateInterest() base logic = fixed in every account type, forever.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// The base class for all bank account types.
/// Fields and methods here are shared by every account — write them once.
/// Virtual/abstract methods are the "fill in your clause" hooks for subclasses.
/// Note: incorporates the encapsulation pattern from File 1 (BankAccount).
/// This is the evolved form used as a shared base across account types.
/// </summary>
public abstract class AccountBase
{
    protected decimal _balance;

    public string   AccountNumber { get; }
    public string   HolderName    { get; }
    public DateTime OpenDate      { get; }

    /// <summary>Balance is readable; only Deposit/Withdraw can change it.</summary>
    public decimal Balance => _balance;

    protected AccountBase(string accountNumber, string holderName, decimal openingBalance = 0)
    {
        if (openingBalance < 0) throw new ArgumentException("Opening balance cannot be negative.");
        AccountNumber = accountNumber;
        HolderName    = holderName;
        OpenDate      = DateTime.Today;
        _balance      = openingBalance;
    }

    /// <summary>
    /// Shared deposit logic. Works identically for every account type.
    /// Fix it here → fixed for Savings, Current, FD, and every future type.
    /// </summary>
    public virtual void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Deposit must be positive.");
        _balance += amount;
        Console.WriteLine($"   [{AccountNumber}] Deposited ₹{amount:N0}. Balance: ₹{_balance:N0}.");
    }

    /// <summary>
    /// Base withdrawal logic. Override in subclasses to add type-specific rules
    /// (minimum balance for savings, overdraft for current, lock for FD).
    /// </summary>
    public virtual void Withdraw(decimal amount)
    {
        if (amount <= 0)       throw new ArgumentException("Withdrawal must be positive.");
        if (amount > _balance) throw new InvalidOperationException(
            $"Insufficient balance. Balance: ₹{_balance:N0}, Requested: ₹{amount:N0}.");
        _balance -= amount;
        Console.WriteLine($"   [{AccountNumber}] Withdrew ₹{amount:N0}. Balance: ₹{_balance:N0}.");
    }

    /// <summary>
    /// Each subclass returns its annual interest on the current balance.
    /// Abstract — there is no sensible default; every type must define this.
    /// This is the method whose formula RBI changed. Fix it HERE = fixed everywhere.
    /// </summary>
    public abstract decimal CalculateInterest();

    /// <summary>Each subclass returns its human-readable type name.</summary>
    public abstract string GetAccountType();

    /// <summary>Shared statement format. Fix once — applies to all account types.</summary>
    public virtual string GetStatement() =>
        $"[{AccountNumber}] {HolderName} | {GetAccountType()} | ₹{_balance:N0} | Opened: {OpenDate:dd-MMM-yyyy}";
}

/// <summary>
/// Savings account. Adds minimum balance enforcement and 3.5% annual interest.
/// Inherits Deposit() and GetStatement() unchanged from AccountBase.
/// </summary>
public class SavingsAccount : AccountBase
{
    public decimal MinimumBalance { get; } = 1_000m;

    public SavingsAccount(string accountNumber, string holderName, decimal openingBalance = 1_000m)
        : base(accountNumber, holderName, openingBalance) { }

    // ← Only override what's UNIQUE to savings: minimum balance check.
    public override void Withdraw(decimal amount)
    {
        if (_balance - amount < MinimumBalance)
            throw new InvalidOperationException(
                $"Withdrawal would breach minimum balance of ₹{MinimumBalance:N0}. Available: ₹{_balance - MinimumBalance:N0}.");
        base.Withdraw(amount);  // ← calls AccountBase's Withdraw for the actual deduction
    }

    public override decimal CalculateInterest() => _balance * 0.035m;   // 3.5% p.a.
    public override string  GetAccountType()     => "Savings Account";
}

/// <summary>
/// Current account. No interest. Allows overdraft up to ₹50,000.
/// </summary>
public class CurrentAccount : AccountBase
{
    public decimal OverdraftLimit { get; } = 50_000m;

    public CurrentAccount(string accountNumber, string holderName, decimal openingBalance = 0)
        : base(accountNumber, holderName, openingBalance) { }

    // ← Only override what's UNIQUE to current: overdraft allowance.
    public override void Withdraw(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Withdrawal must be positive.");
        if (amount > _balance + OverdraftLimit)
            throw new InvalidOperationException($"Exceeds balance + overdraft limit of ₹{OverdraftLimit:N0}.");
        _balance -= amount;
        var suffix = _balance < 0 ? $" (₹{Math.Abs(_balance):N0} in overdraft)" : "";
        Console.WriteLine($"   [{AccountNumber}] Withdrew ₹{amount:N0}. Balance: ₹{_balance:N0}{suffix}.");
    }

    public override decimal CalculateInterest() => 0m;   // current accounts earn no interest
    public override string  GetAccountType()     => "Current Account";
}

/// <summary>
/// Fixed deposit. High interest rate but locked until maturity.
/// Withdraw() throws before maturity — by design.
/// </summary>
public class FixedDepositAccount : AccountBase
{
    public DateTime MaturityDate { get; }

    public FixedDepositAccount(string accountNumber, string holderName, decimal principal, int tenureMonths)
        : base(accountNumber, holderName, principal)
    {
        MaturityDate = DateTime.Today.AddMonths(tenureMonths);
    }

    // ← Override: FD withdrawals are blocked before maturity.
    public override void Withdraw(decimal amount)
    {
        if (DateTime.Today < MaturityDate)
            throw new InvalidOperationException(
                $"FD locked until {MaturityDate:dd-MMM-yyyy}. Early withdrawal not allowed.");
        base.Withdraw(amount);
    }

    public override decimal CalculateInterest() => _balance * 0.068m;   // 6.8% p.a.
    public override string  GetAccountType()     => "Fixed Deposit";

    public override string GetStatement() =>
        base.GetStatement() + $" | Matures: {MaturityDate:dd-MMM-yyyy}";
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

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — Copy-pasted classes:");
        var naiveSavings = new NaiveSavingsAccount { AccountNumber = "SAV001", Balance = 50_000 };
        var naiveCurrent = new NaiveCurrentAccount { AccountNumber = "CUR001", Balance = 50_000 };

        // Simulate: RBI changes interest formula — developer fixes Savings, forgets Current.
        // NaiveSavingsAccount.CalculateInterest() is now correct.
        // NaiveCurrentAccount.CalculateInterest() has the old wrong value.
        Console.WriteLine($"   Savings interest: ₹{naiveSavings.CalculateInterest():N0}  ← updated");
        Console.WriteLine($"   Current interest: ₹{naiveCurrent.CalculateInterest():N0}  ← formula was wrong for 6 months before anyone noticed");
        Console.WriteLine("   ₹2.3 crore discrepancy. Audit failed.\n");

        // ── AFTER ───────────────────────────────────────────────────────────
        Console.WriteLine("✅ AFTER — Shared base class, one fix propagates everywhere:\n");

        var savings = new SavingsAccount("SAV002", "Priya Mehta", 10_000m);
        var current = new CurrentAccount("CUR002", "Raj Enterprises", 5_000m);
        var fd      = new FixedDepositAccount("FD001", "Anita Rao", 1_00_000m, tenureMonths: 12);

        Console.WriteLine("   Account statements (GetStatement from AccountBase — shared):");
        Console.WriteLine($"   {savings.GetStatement()}");
        Console.WriteLine($"   {current.GetStatement()}");
        Console.WriteLine($"   {fd.GetStatement()}\n");

        Console.WriteLine("   Deposit ₹5,000 into savings (uses AccountBase.Deposit — inherited):");
        savings.Deposit(5_000m);

        Console.WriteLine("\n   Withdraw ₹14,500 from savings (minimum balance ₹1,000 enforced):");
        try { savings.Withdraw(14_500m); }
        catch (InvalidOperationException ex) { Console.WriteLine($"   Caught: {ex.Message}"); }
        savings.Withdraw(14_000m);  // this one succeeds (leaves ₹1,000 minimum)

        Console.WriteLine("\n   Current account overdraft:");
        current.Withdraw(8_000m);   // goes into overdraft

        Console.WriteLine("\n   FD early withdrawal blocked:");
        try { fd.Withdraw(10_000m); }
        catch (InvalidOperationException ex) { Console.WriteLine($"   Caught: {ex.Message}"); }

        Console.WriteLine("\n   Annual interest (RBI formula — one fix in base reaches all):");
        AccountBase[] accounts = [savings, current, fd];
        foreach (var acc in accounts)
            Console.WriteLine($"   {acc.GetAccountType(),-18} | Interest: ₹{acc.CalculateInterest():N0}");

        Console.WriteLine("\n✅ Inheritance — understood.");
    }
}
