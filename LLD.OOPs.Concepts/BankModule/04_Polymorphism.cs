/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏦 CONCEPT: Polymorphism                                                    ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 03_Inheritance.cs                                             ║
 * ║     We now have SavingsAccount, CurrentAccount, and FixedDepositAccount     ║
 * ║     all sharing AccountBase. Polymorphism is the PAYOFF for that work:      ║
 * ║     one method can process ALL account types without asking which type it   ║
 * ║     is dealing with. This is why the inheritance structure exists.           ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Polymorphism ("many forms") means the same method call produces different  ║
 * ║  behaviour depending on which object it's called on, at runtime.            ║
 * ║  account.CalculateInterest() returns 3.5% for SavingsAccount and 6.8% for  ║
 * ║  FixedDepositAccount — same call, different results. No if-else required.  ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  A bank teller's "process transaction" button. The teller presses the same  ║
 * ║  button whether you have a savings account, current account, or FD.        ║
 * ║  The banking system figures out which rules to apply based on YOUR account. ║
 * ║  The teller never needs a "if savings → do X, if FD → do Y" manual.        ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A bank's report generation service had a 400-line switch statement:        ║
 * ║  switch(account.Type) { case "savings": ... case "current": ... }           ║
 * ║  The bank launched a new Recurring Deposit product. The developer added     ║
 * ║  the new account class — but forgot to update all 11 switch statements      ║
 * ║  spread across 7 files. The RD accounts showed ₹0 interest on every       ║
 * ║  statement for two months. 3,200 customers called support.                  ║
 * ║                                                                              ║
 * ║  QUICK RECALL (your 2-year cheat code):                                     ║
 * ║  Zero if-else. New account type = new class, zero changes to processors.   ║
 * ║                                                                              ║
 * ║  HOW THIS CONNECTS TO THE NEXT CONCEPT:                                     ║
 * ║  Polymorphism works BECAUSE of Abstraction (File 2) + Inheritance (File 3). ║
 * ║  File 5 asks a deeper question: when do you use abstract CLASS vs           ║
 * ║  INTERFACE? Both enable polymorphism — the choice matters.                  ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.BankModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — What a beginner naturally writes
// "I need to handle each account type differently — just use if-else."
// This feels obvious and explicit. You can see exactly what's happening.
// The trap: every new account type forces you to find and edit this method.
// ─────────────────────────────────────────────────────────────────────────────

public class NaiveReportGenerator
{
    // ❌ This method gets touched EVERY time a new account type is added.
    // It knows too much about concrete types. It will never stop growing.
    public void GenerateMonthlyReport(object account)
    {
        decimal interest;
        string  accountType;

        if (account is NaiveSavingsAccount s)
        {
            interest    = s.Balance * 0.035m;  // ← hardcoded rate — drift risk
            accountType = "Savings";
        }
        else if (account is NaiveCurrentAccount c)
        {
            interest    = 0;
            accountType = "Current";
        }
        // ← Adding RecurringDeposit here? Open this file. Add another else-if.
        // ← Adding NRE Account?  Open this file. Add another else-if.
        // ← 5 account types today. 15 in 3 years. This method is 200 lines.
        // ← And you'll forget one. 3,200 support calls. See disaster story above.
        else
        {
            interest    = 0;
            accountType = "Unknown";
        }

        Console.WriteLine($"   [NAIVE] {accountType}: ₹{interest:N2} interest");
    }
}

// 💥 WHAT BREAKS:
// — Dev adds FixedDepositAccount class but forgets to update this method.
// — FD customers see ₹0 interest on their statements for 2 months.
// — 11 switch-statements across 7 files, all need updating. You'll miss one.

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — OOP-correct polymorphism
// GenerateMonthlyReport() has ZERO knowledge of account types.
// account.CalculateInterest() dispatches to the right implementation at runtime.
// Add RecurringDeposit = create one new class, zero changes to this method. Ever.
// ─────────────────────────────────────────────────────────────────────────────

public class BankReportGenerator
{
    /// <summary>
    /// Processes ALL account types with zero if-else.
    /// New account type? Create the class with CalculateInterest() override.
    /// This method never changes. Ever.
    /// </summary>
    public void GenerateMonthlyReport(IEnumerable<AccountBase> accounts)
    {
        Console.WriteLine("   ── Monthly Interest Report ──────────────────");
        decimal totalInterest = 0;

        foreach (var account in accounts)
        {
            // ← This one call does different things for different account types.
            // Runtime polymorphism: the CLR looks up the actual type at runtime
            // and calls the right override. No if-else. No switch. No type checks.
            decimal interest = account.CalculateInterest();
            totalInterest   += interest;

            Console.WriteLine($"   {account.GetAccountType(),-20} | {account.HolderName,-15} | ₹{interest:N2}");
        }

        Console.WriteLine($"   {"TOTAL",-20} | {"",15} | ₹{totalInterest:N2}");
        Console.WriteLine("   ─────────────────────────────────────────────");
    }
}

// ── Method Overloading — compile-time polymorphism ───────────────────────────
// The SAME method name does different things based on the PARAMETERS at compile time.
// This is different from runtime polymorphism above — resolved by the compiler, not CLR.

public class ReceiptPrinter
{
    /// <summary>Basic receipt — console only.</summary>
    public void PrintReceipt(AccountBase account)
    {
        Console.WriteLine($"\n   ── Receipt ──────────────────────────────────");
        Console.WriteLine($"   Account : {account.AccountNumber}");
        Console.WriteLine($"   Holder  : {account.HolderName}");
        Console.WriteLine($"   Type    : {account.GetAccountType()}");
        Console.WriteLine($"   Balance : ₹{account.Balance:N0}");
        Console.WriteLine($"   ─────────────────────────────────────────────");
    }

    /// <summary>
    /// Overload 1 — same method name, adds email delivery.
    /// Compiler picks this one when you pass an email string.
    /// </summary>
    public void PrintReceipt(AccountBase account, string emailAddress)
    {
        PrintReceipt(account);  // reuse the base receipt printing
        Console.WriteLine($"   [EMAIL] Receipt sent to: {emailAddress}");
    }

    /// <summary>
    /// Overload 2 — same method name, adds tax summary option.
    /// Compiler picks this one when you pass email + bool.
    /// </summary>
    public void PrintReceipt(AccountBase account, string emailAddress, bool includeTaxSummary)
    {
        PrintReceipt(account, emailAddress);   // reuse overload 1
        if (includeTaxSummary)
        {
            decimal tds = account.CalculateInterest() * 0.10m;  // 10% TDS on interest
            Console.WriteLine($"   [TAX] TDS @ 10% on interest: ₹{tds:N2}");
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class PolymorphismDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  POLYMORPHISM DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — NaiveReportGenerator (if-else per type):");
        var naive     = new NaiveReportGenerator();
        var naiveSav  = new NaiveSavingsAccount  { AccountNumber = "SAV001", Balance = 50_000 };
        var naiveCur  = new NaiveCurrentAccount  { AccountNumber = "CUR001", Balance = 50_000 };

        naive.GenerateMonthlyReport(naiveSav);
        naive.GenerateMonthlyReport(naiveCur);
        Console.WriteLine("   ← FixedDeposit not in the if-chain: would show ₹0 silently.\n");

        // ── AFTER ── Runtime Polymorphism ────────────────────────────────────
        Console.WriteLine("✅ AFTER — BankReportGenerator (zero if-else):\n");

        // These account types are from File 3 — we reuse them directly.
        AccountBase[] portfolio =
        [
            new SavingsAccount       ("SAV002", "Priya Mehta",    50_000m),
            new CurrentAccount       ("CUR002", "Raj Enterprises", 0),
            new FixedDepositAccount  ("FD001",  "Anita Rao",      1_00_000m, 12),
        ];

        var reporter = new BankReportGenerator();
        reporter.GenerateMonthlyReport(portfolio);

        Console.WriteLine("\n   Adding a new account type (hypothetical RecurringDeposit):");
        Console.WriteLine("   → Create RecurringDeposit : AccountBase with its own CalculateInterest().");
        Console.WriteLine("   → Add it to the portfolio array.");
        Console.WriteLine("   → BankReportGenerator.GenerateMonthlyReport() — ZERO CHANGES.");

        // ── AFTER ── Method Overloading (compile-time polymorphism) ──────────
        Console.WriteLine("\n✅ AFTER — ReceiptPrinter overloads (compile-time polymorphism):\n");

        var printer  = new ReceiptPrinter();
        var account  = new SavingsAccount("SAV003", "Sneha Kapoor", 75_000m);

        Console.WriteLine("   Overload 1 — basic receipt:");
        printer.PrintReceipt(account);

        Console.WriteLine("\n   Overload 2 — receipt + email:");
        printer.PrintReceipt(account, "sneha@email.com");

        Console.WriteLine("\n   Overload 3 — receipt + email + tax summary:");
        printer.PrintReceipt(account, "sneha@email.com", includeTaxSummary: true);

        Console.WriteLine("\n   Same method name 'PrintReceipt' — compiler picks the right one based on arguments.");
        Console.WriteLine("   That's compile-time polymorphism (overloading) vs runtime polymorphism (overriding).");

        Console.WriteLine("\n✅ Polymorphism — understood.");
    }
}
