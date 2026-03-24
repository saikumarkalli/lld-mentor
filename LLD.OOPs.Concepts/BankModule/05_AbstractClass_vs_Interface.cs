/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏦 CONCEPT: Abstract Class vs Interface                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: All previous files                                            ║
 * ║     Files 1–4 used AccountBase (abstract class) for shared structure and    ║
 * ║     IBankService (interface) for service contracts. Now we ask the          ║
 * ║     question that trips up every developer: WHEN do you use which?          ║
 * ║     This file is the permanent answer.                                       ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  An abstract class is a partial blueprint — it gives shared fields, shared  ║
 * ║  code, AND forces subclasses to implement certain methods. An interface is  ║
 * ║  a pure contract — it says "you can do THIS" with zero implementation.      ║
 * ║  Both enable polymorphism, but they model different things.                 ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  Abstract class = RBI banking licence template. Every bank that holds an   ║
 * ║  RBI licence shares the same legal structure, must have a capital reserve,  ║
 * ║  must run KYC. The template provides common ground AND enforces obligations. ║
 * ║  Interface = ISO certification. Completely unrelated entities (a bank, a   ║
 * ║  fintech, an NBFC) can each hold an ISO 9001 cert. The cert says "you meet  ║
 * ║  this standard" — it doesn't change your internal structure at all.         ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A developer created IFinancialProduct with 12 methods. LoanAccount,        ║
 * ║  SavingsAccount, and FixedDepositAccount all implemented it. Eight methods  ║
 * ║  were relevant to savings; three to loans; six to FD. Every class threw     ║
 * ║  NotImplementedException on the methods it didn't care about. A junior dev  ║
 * ║  caught a NotImplementedException in prod from a code path that called      ║
 * ║  loanAccount.CalculateMinimumBalance() — a method that made no sense for a ║
 * ║  loan but the fat interface required it. One fat interface, three classes   ║
 * ║  pretending to be something they're not.                                    ║
 * ║                                                                              ║
 * ║  QUICK RECALL (your 2-year cheat code):                                     ║
 * ║  Abstract class = shared DNA (IS-A). Interface = capability badge (CAN-DO). ║
 * ║  Shared CODE → abstract class. Shared CONTRACT only → interface.            ║
 * ║                                                                              ║
 * ║  HOW THIS CONNECTS TO THE NEXT CONCEPT:                                     ║
 * ║  Interfaces model capabilities. File 6 (Composition) shows why you should  ║
 * ║  INJECT those capabilities rather than inheriting them.                     ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.BankModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — The confusion that trips up every developer
// "I'm not sure if I need abstract class or interface — I'll use both to be safe."
// Result: double the methods to maintain, ambiguous contracts, duplicated code.
// ─────────────────────────────────────────────────────────────────────────────

// ❌ Developer creates an abstract class...
public abstract class NaiveBankProduct
{
    public abstract string  GetProductId();
    public abstract decimal GetBalance();
    public abstract decimal CalculateInterest();
    public abstract void    ApplyForLoan();          // ← makes no sense on a savings account
    public abstract decimal GetMinimumBalance();     // ← makes no sense on a loan
    public abstract string  GetMaturityDate();       // ← makes no sense on a current account
    public abstract decimal GetOverdraftLimit();     // ← only relevant for current accounts
    public abstract void    SendStatement(string email);
}

// ❌ ...and ALSO creates an interface with the same 8 methods
public interface INaiveBankProduct
{
    string  GetProductId();
    decimal GetBalance();
    decimal CalculateInterest();
    void    ApplyForLoan();
    decimal GetMinimumBalance();
    string  GetMaturityDate();
    decimal GetOverdraftLimit();
    void    SendStatement(string email);
}

// ❌ LoanAccount now has to implement BOTH, duplicating everything
public class NaiveLoanAccount : NaiveBankProduct, INaiveBankProduct
{
    public override string  GetProductId()      => "LOAN001";
    public override decimal GetBalance()        => 5_00_000m;
    public override decimal CalculateInterest() => 5_00_000m * 0.085m;  // 8.5% loan interest
    public override void    ApplyForLoan()      => Console.WriteLine("   Loan application submitted.");
    public override decimal GetMinimumBalance() => throw new NotImplementedException(); // ← makes no sense for loan
    public override string  GetMaturityDate()   => throw new NotImplementedException(); // ← loans don't mature like FDs
    public override decimal GetOverdraftLimit() => throw new NotImplementedException(); // ← not relevant
    public override void    SendStatement(string email) => Console.WriteLine($"   Loan statement → {email}");
}

// 💥 WHAT BREAKS:
// — NaiveLoanAccount throws NotImplementedException on 3 of 8 methods.
// — A junior dev calls loanAccount.GetMinimumBalance() — runtime crash, prod incident.
// — You have to maintain the same 8-method signature in TWO places (class + interface).
// — No clarity on which one callers should use. Are you expecting NaiveBankProduct or INaiveBankProduct?

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — THE PERMANENT RULE
//
//   ABSTRACT CLASS  = "What you ARE" — shared DNA, partial implementation
//                     Use when: subclasses share FIELDS and/or CONCRETE CODE
//                     Rule: "Does every X share actual CODE?"  → Abstract class
//
//   INTERFACE       = "What you CAN DO" — capability contract, zero DNA
//                     Use when: unrelated types share a CONTRACT but no code
//                     Rule: "Does every X only share a CONTRACT?" → Interface
//
//   BOTH            = Abstract class + Interface (common in .NET BCL: Stream, IDisposable)
//                     Use when: you have shared code AND unrelated classes need the contract
//
// The bank example makes this crystal clear:
// ─────────────────────────────────────────────────────────────────────────────

// ── Abstract class: shared DNA for account types ─────────────────────────────
// AccountBase is already defined in 03_Inheritance.cs — we use it here.
// Key reminder of why it's an abstract class, not an interface:
//
//   ✅ Every account type IS-A bank account (identity)
//   ✅ Every account shares fields: AccountNumber, HolderName, Balance, OpenDate
//   ✅ Every account shares Deposit() logic — same code, write once
//   ✅ Every account MUST implement CalculateInterest() — enforced by abstract
//
// If it were an interface, each account type would re-implement Deposit() separately.
// That's the bug factory from File 3's disaster story. Abstract class is correct here.

// ── Interfaces: capability badges — mix-and-match across unrelated classes ────

/// <summary>
/// Capability: this product earns interest.
/// SavingsAccount and FixedDepositAccount earn interest. CurrentAccount does NOT.
/// LoanAccount earns interest (the bank earns it from the borrower) — but it IS-NOT-A BankAccount.
/// An interface lets all three participate in interest calculations without sharing a base class.
/// </summary>
public interface IInterestBearing
{
    /// <summary>Annual interest rate as a decimal (e.g. 0.035 for 3.5%).</summary>
    decimal GetInterestRate();

    /// <summary>Calculates interest earned on current balance.</summary>
    decimal CalculateInterest();
}

/// <summary>
/// Capability: this account allows spending beyond its balance.
/// Only CurrentAccount and BusinessAccount get this badge.
/// SavingsAccount and FixedDepositAccount never implement this interface at all.
/// </summary>
public interface IOverdraftAllowed
{
    decimal GetOverdraftLimit();
    void    RequestOverdraftIncrease(decimal requestedLimit);
}

/// <summary>
/// Capability: this product can be used as a loan instrument.
/// LoanAccount implements this. SavingsAccount does not. FixedDepositAccount does not.
/// Crucially: LoanAccount IS-NOT-A bank account — it's a completely different base class.
/// An interface is the ONLY way to include it in interest/EMI calculations alongside accounts.
/// </summary>
public interface ILoanable
{
    decimal GetOutstandingAmount();
    decimal GetMonthlyEmi();
    void    ApplyForLoan(decimal amount, int tenureMonths);
}

// ── Concrete classes that show mix-and-match ─────────────────────────────────

/// <summary>
/// A recurring deposit — IS-A account (has the AccountBase DNA), AND earns interest.
/// Implements both AccountBase (inherited) and IInterestBearing (capability).
/// </summary>
public class RecurringDeposit : AccountBase, IInterestBearing
{
    private readonly decimal _monthlyInstalment;

    public RecurringDeposit(string accountNumber, string holderName, decimal monthlyInstalment)
        : base(accountNumber, holderName, openingBalance: 0)
    {
        _monthlyInstalment = monthlyInstalment;
    }

    // ← AccountBase forces us to implement these abstract methods
    public override decimal CalculateInterest() => _balance * GetInterestRate();
    public override string  GetAccountType()     => "Recurring Deposit";

    // ← IInterestBearing adds the rate query — not on AccountBase, specific to interest-earners
    public decimal GetInterestRate() => 0.055m;   // 5.5% p.a.

    public void MakeMonthlyInstalment()
    {
        _balance += _monthlyInstalment;
        Console.WriteLine($"   [{AccountNumber}] Instalment ₹{_monthlyInstalment:N0} deposited. Total: ₹{_balance:N0}.");
    }
}

/// <summary>
/// A loan account — IS-NOT-A bank account (no AccountBase, no Balance in the bank's favour).
/// It IS-A financial product that is loanable. Interface-only — no abstract class inheritance.
/// This is IMPOSSIBLE to model with pure inheritance. Interfaces make it clean.
/// </summary>
public class LoanAccount : ILoanable
{
    public  string  LoanId            { get; }
    public  string  BorrowerName      { get; }
    private decimal _outstandingAmount;
    private int     _remainingMonths;

    public LoanAccount(string loanId, string borrowerName)
    {
        LoanId       = loanId;
        BorrowerName = borrowerName;
    }

    public decimal GetOutstandingAmount() => _outstandingAmount;
    public decimal GetMonthlyEmi()        => _remainingMonths > 0
        ? Math.Round(_outstandingAmount / _remainingMonths, 2) : 0;

    public void ApplyForLoan(decimal amount, int tenureMonths)
    {
        _outstandingAmount = amount;
        _remainingMonths   = tenureMonths;
        Console.WriteLine($"   [{LoanId}] Loan of ₹{amount:N0} approved for {tenureMonths} months. EMI: ₹{GetMonthlyEmi():N0}/month.");
    }

    public override string ToString() =>
        $"[{LoanId}] {BorrowerName} | Loan | Outstanding: ₹{_outstandingAmount:N0} | EMI: ₹{GetMonthlyEmi():N0}";
}

// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class AbstractVsInterfaceDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  ABSTRACT CLASS vs INTERFACE DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — Fat interface + abstract class doing the same job:");
        var naiveLoan = new NaiveLoanAccount();
        naiveLoan.ApplyForLoan();
        Console.WriteLine($"   Balance: ₹{naiveLoan.GetBalance():N0}");
        try
        {
            naiveLoan.GetMinimumBalance();  // makes no sense on a loan
        }
        catch (NotImplementedException)
        {
            Console.WriteLine("   naiveLoan.GetMinimumBalance() → NotImplementedException 💥");
            Console.WriteLine("   LoanAccount was forced to implement a method it can never fulfill.\n");
        }

        // ── AFTER ── Abstract class: shared DNA ──────────────────────────────
        Console.WriteLine("✅ AFTER — Abstract class for shared DNA (IS-A):\n");

        var rd = new RecurringDeposit("RD001", "Vikram Nair", monthlyInstalment: 5_000m);
        rd.MakeMonthlyInstalment();
        rd.MakeMonthlyInstalment();
        rd.MakeMonthlyInstalment();
        Console.WriteLine($"   {rd.GetStatement()}");
        Console.WriteLine($"   Interest rate: {rd.GetInterestRate():P1} | Annual interest: ₹{rd.CalculateInterest():N0}");

        // ── AFTER ── Interface: capability across unrelated classes ───────────
        Console.WriteLine("\n✅ AFTER — IInterestBearing groups all interest-earning products:\n");

        // RecurringDeposit and a future SavingsAccountV2 both implement IInterestBearing.
        // Note: SavingsAccount from File 3 has CalculateInterest() but doesn't declare
        // IInterestBearing because File 3 was written before this interface existed.
        // In a real codebase you'd retroactively add ': IInterestBearing' to SavingsAccount.
        // Here we use two RecurringDeposit instances to show the concept cleanly.
        var rdAcc1 = new RecurringDeposit("RD010", "Asha Bhatt",  3_000m);
        var rdAcc2 = new RecurringDeposit("RD011", "Vikram Nair", 5_000m);
        rdAcc1.MakeMonthlyInstalment();
        rdAcc1.MakeMonthlyInstalment();
        rdAcc2.MakeMonthlyInstalment();
        rdAcc2.MakeMonthlyInstalment();
        rdAcc2.MakeMonthlyInstalment();

        IInterestBearing[] interestEarners = [rdAcc1, rdAcc2];
        // ← CurrentAccount is NOT in this list — it earns no interest, doesn't implement IInterestBearing.
        // ← LoanAccount could be added here if it earns interest (bank's perspective).

        Console.WriteLine("   Products earning interest this year:");
        decimal totalInterest = 0;
        foreach (var product in interestEarners)
        {
            decimal interest = product.CalculateInterest();
            totalInterest   += interest;
            Console.WriteLine($"   Rate: {product.GetInterestRate():P1} | Interest: ₹{interest:N0}");
        }
        Console.WriteLine($"   Total interest to disburse: ₹{totalInterest:N0}");

        Console.WriteLine("\n✅ AFTER — ILoanable: LoanAccount is a product, not an account:\n");

        var loan = new LoanAccount("LN001", "Suresh Patel");
        loan.ApplyForLoan(5_00_000m, tenureMonths: 60);
        Console.WriteLine($"   {loan}");
        Console.WriteLine("   LoanAccount has no AccountBase DNA — it's not a bank account.");
        Console.WriteLine("   But it can participate anywhere ILoanable is expected.");

        Console.WriteLine("\n   The permanent rule:");
        Console.WriteLine("   ┌────────────────────────────────────────────────────────┐");
        Console.WriteLine("   │  Shared CODE   → Abstract class (AccountBase)          │");
        Console.WriteLine("   │  Shared CONTRACT only → Interface (IInterestBearing)   │");
        Console.WriteLine("   │  Ask: 'Does every X IS-A Y?' → Abstract class          │");
        Console.WriteLine("   │  Ask: 'Can X DO Y (regardless of what X is)?' → Interface│");
        Console.WriteLine("   └────────────────────────────────────────────────────────┘");

        Console.WriteLine("\n✅ AbstractClass vs Interface — understood.");
    }
}
