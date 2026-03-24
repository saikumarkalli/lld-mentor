/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏦 CONCEPT: Interface Segregation                                           ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 06_CompositionVsInheritance.cs                                ║
 * ║     File 6 showed that composition works through injected interfaces.       ║
 * ║     But what if the interface you're injecting is too fat? Classes that     ║
 * ║     only need 2 of 10 methods are forced to stub out 8 of them.             ║
 * ║     Interface Segregation is the fix: split fat interfaces into focused ones.║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  "Clients should not be forced to depend on interfaces they do not use."    ║
 * ║  — Robert C. Martin (SOLID's I principle)                                   ║
 * ║  When an interface has 10 methods and a class only needs 2, the class is    ║
 * ║  dragged into a contract it doesn't belong to. Split the interface so each  ║
 * ║  client only sees the methods it actually needs.                             ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  A bank's job description bulletin board. One massive flyer:                ║
 * ║  "BANK TELLER — must be able to: process deposits, issue loans, write       ║
 * ║  audit reports, manage the server room, conduct fraud investigations,       ║
 * ║  and maintain the ATM hardware." No one person can do all of that.         ║
 * ║  Split into focused roles: Teller, Loan Officer, IT Admin, Compliance.     ║
 * ║  Each person signs only the job description relevant to them.               ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A bank's compliance dashboard implemented IBankOperations (10 methods)    ║
 * ║  because the IBankOperations.GetAuditLog() was the one method it needed.   ║
 * ║  The other 9 methods threw NotImplementedException. Six months later a      ║
 * ║  monitoring script called RecalculateFees() on every IBankOperations        ║
 * ║  instance in the system, including the dashboard. The dashboard threw.     ║
 * ║  The monitoring script failed silently. Fee recalculation missed 14,000    ║
 * ║  accounts that month. Cause: one fat interface, one forced fake method.    ║
 * ║                                                                              ║
 * ║  QUICK RECALL (your 2-year cheat code):                                     ║
 * ║  This is the I in SOLID. Fat interface = NotImplementedException waiting    ║
 * ║  to happen. Thin interfaces = each class signs only what it means.          ║
 * ║                                                                              ║
 * ║  HOW THIS CONNECTS TO THE NEXT CONCEPT:                                     ║
 * ║  Thin interfaces make it easy to inject only what a class needs.            ║
 * ║  File 8 (Dependency Inversion) shows the full picture: inject abstractions ║
 * ║  into constructors so you can swap implementations and test in isolation.  ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.BankModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — One fat interface that everyone is forced to implement
// "IBankOperations covers everything a bank does — just implement it."
// Feels complete and organized. Until 6 of 10 methods are fake stubs.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// ❌ Fat interface. Forces every implementor to care about ALL bank operations.
/// A read-only dashboard only needs GetAuditLog() and GenerateReport().
/// But to implement IBankOperations it must stub out 8 other methods.
/// </summary>
public interface IBankOperations
{
    void    ProcessDeposit(string accountId, decimal amount);
    void    ProcessWithdrawal(string accountId, decimal amount);
    void    ProcessTransfer(string fromId, string toId, decimal amount);
    void    Refund(string transactionId);
    string  GenerateReport(DateTime from, DateTime to);
    void    SendNotification(string customerId, string message);
    void    ArchiveTransactions(DateTime before);
    string  GetAuditLog(string accountId);
    void    RecalculateFees(string accountId);
    decimal GetExchangeRate(string currencyCode);
}

/// <summary>
/// ❌ Read-only compliance dashboard. Needs only GetAuditLog() and GenerateReport().
/// But IBankOperations forces it to implement all 10 methods.
/// Result: 8 NotImplementedExceptions waiting to explode.
/// </summary>
public class NaiveComplianceDashboard : IBankOperations
{
    public string GenerateReport(DateTime from, DateTime to) =>
        $"[DASHBOARD] Compliance report: {from:dd-MMM} → {to:dd-MMM}";

    public string GetAuditLog(string accountId) =>
        $"[DASHBOARD] Audit log for {accountId}";

    // ← Everything below is a lie. This class cannot do any of these.
    //   The compiler forces it. The fat interface demands it.
    public void    ProcessDeposit(string a, decimal amt)   => throw new NotImplementedException();
    public void    ProcessWithdrawal(string a, decimal amt) => throw new NotImplementedException();
    public void    ProcessTransfer(string f, string t, decimal a) => throw new NotImplementedException();
    public void    Refund(string txId)                     => throw new NotImplementedException();
    public void    SendNotification(string c, string m)    => throw new NotImplementedException();
    public void    ArchiveTransactions(DateTime b)         => throw new NotImplementedException();
    public void    RecalculateFees(string a)               => throw new NotImplementedException();  // ← caused the disaster
    public decimal GetExchangeRate(string c)               => throw new NotImplementedException();
}

// 💥 WHAT BREAKS:
// — Monitoring script calls RecalculateFees() on every IBankOperations in the system.
// — NaiveComplianceDashboard.RecalculateFees() throws NotImplementedException.
// — Script silently swallows the exception. 14,000 accounts miss fee recalculation.
// — The bug hides because the dashboard "implemented" the interface. It just lied.

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — Split into focused, single-purpose interfaces
// Each class only implements the interfaces it genuinely fulfills.
// No more NotImplementedException stubs. No more lying contracts.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Capability: can process core banking transactions.</summary>
public interface ITransactable
{
    void ProcessDeposit(string accountId, decimal amount);
    void ProcessWithdrawal(string accountId, decimal amount);
    void ProcessTransfer(string fromId, string toId, decimal amount);
    void Refund(string transactionId);
}

/// <summary>Capability: can generate reports. Read-only — no state changes.</summary>
public interface IReportable
{
    string GenerateReport(DateTime from, DateTime to);
    string ExportToCsv(DateTime from, DateTime to);
}

/// <summary>Capability: can query and archive audit data. Read-heavy.</summary>
public interface IAuditable
{
    string GetAuditLog(string accountId);
    void   ArchiveTransactions(DateTime olderThan);
}

/// <summary>Capability: can send notifications to customers.</summary>
public interface INotifiable
{
    void SendNotification(string customerId, string message);
}

/// <summary>Capability: can calculate and recalculate transaction fees.</summary>
public interface IFeeCalculable
{
    void    RecalculateFees(string accountId);
    decimal GetCurrentFee(string accountId);
}

// ── Implementations — each class signs only what it can genuinely fulfill ─────

/// <summary>
/// Full bank service. Genuinely implements all capabilities.
/// No stubs. No NotImplementedException. Every method does real work.
/// </summary>
public class FullBankService : ITransactable, IReportable, IAuditable, INotifiable, IFeeCalculable
{
    public void   ProcessDeposit(string a, decimal amt)       => Console.WriteLine($"   [BANK] Deposited ₹{amt:N0} into {a}.");
    public void   ProcessWithdrawal(string a, decimal amt)    => Console.WriteLine($"   [BANK] Withdrew ₹{amt:N0} from {a}.");
    public void   ProcessTransfer(string f, string t, decimal a) => Console.WriteLine($"   [BANK] Transferred ₹{a:N0}: {f} → {t}.");
    public void   Refund(string txId)                         => Console.WriteLine($"   [BANK] Refunded transaction {txId}.");
    public string GenerateReport(DateTime f, DateTime t)      { var r = $"[BANK] Report: {f:dd-MMM} → {t:dd-MMM}"; Console.WriteLine($"   {r}"); return r; }
    public string ExportToCsv(DateTime f, DateTime t)         { var r = $"[BANK] CSV export: {f:dd-MMM} → {t:dd-MMM}"; Console.WriteLine($"   {r}"); return r; }
    public string GetAuditLog(string a)                       { var r = $"[BANK] Audit: {a}"; Console.WriteLine($"   {r}"); return r; }
    public void   ArchiveTransactions(DateTime b)             => Console.WriteLine($"   [BANK] Archived transactions before {b:dd-MMM-yyyy}.");
    public void   SendNotification(string c, string m)        => Console.WriteLine($"   [SMS] → {c}: {m}");
    public void   RecalculateFees(string a)                   => Console.WriteLine($"   [BANK] Fees recalculated for {a}.");
    public decimal GetCurrentFee(string a)                    => 25m;
}

/// <summary>
/// Compliance read-only dashboard. Only implements IReportable + IAuditable.
/// Completely honest about what it can do. No stubs. No NotImplementedException.
/// A monitoring script iterating IFeeCalculable will NEVER include this — correctly.
/// </summary>
public class ComplianceDashboard : IReportable, IAuditable
{
    // ← Only 4 methods. All genuine. Nothing hidden. Nothing lying.
    public string GenerateReport(DateTime f, DateTime t)
    {
        var r = $"[DASHBOARD] Compliance report: {f:dd-MMM} → {t:dd-MMM}";
        Console.WriteLine($"   {r}");
        return r;
    }

    public string ExportToCsv(DateTime f, DateTime t)
    {
        var r = $"[DASHBOARD] CSV: {f:dd-MMM} → {t:dd-MMM}";
        Console.WriteLine($"   {r}");
        return r;
    }

    public string GetAuditLog(string accountId)
    {
        var r = $"[DASHBOARD] Audit trail for {accountId}: OK";
        Console.WriteLine($"   {r}");
        return r;
    }

    public void ArchiveTransactions(DateTime olderThan) =>
        Console.WriteLine($"   [DASHBOARD] Archived transactions older than {olderThan:dd-MMM-yyyy}.");
}

// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class InterfaceSegregationDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  INTERFACE SEGREGATION DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — Fat interface, 8 NotImplementedExceptions on a dashboard:");
        var naive = new NaiveComplianceDashboard();
        Console.WriteLine($"   {naive.GenerateReport(DateTime.Today.AddDays(-7), DateTime.Today)}");
        Console.WriteLine($"   {naive.GetAuditLog("ACC001")}");

        Console.WriteLine("\n   A monitoring script now calls RecalculateFees() on all IBankOperations:");
        try
        {
            ((IBankOperations)naive).RecalculateFees("ACC001");
        }
        catch (NotImplementedException)
        {
            Console.WriteLine("   💥 NaiveComplianceDashboard.RecalculateFees() → NotImplementedException.");
            Console.WriteLine("   Script swallowed the exception. 14,000 accounts missed fee recalc.\n");
        }

        // ── AFTER ── Full service ─────────────────────────────────────────
        Console.WriteLine("✅ AFTER — FullBankService implements all 5 interfaces genuinely:");
        var fullService = new FullBankService();
        fullService.ProcessDeposit("ACC001", 10_000m);
        fullService.ProcessTransfer("ACC001", "ACC002", 5_000m);
        fullService.SendNotification("CUST001", "Transfer of ₹5,000 completed.");
        fullService.RecalculateFees("ACC001");

        // ── AFTER ── Dashboard with thin interfaces ───────────────────────
        Console.WriteLine("\n✅ AFTER — ComplianceDashboard implements only IReportable + IAuditable:");
        var dashboard = new ComplianceDashboard();
        dashboard.GenerateReport(DateTime.Today.AddDays(-30), DateTime.Today);
        dashboard.GetAuditLog("ACC002");
        dashboard.ArchiveTransactions(DateTime.Today.AddYears(-2));

        // ── Monitoring script — safe now ──────────────────────────────────
        Console.WriteLine("\n   Monitoring script iterates IFeeCalculable — dashboard is NOT in the list:");
        IFeeCalculable[] feeProcessors = [fullService];
        // ← ComplianceDashboard is correctly absent — it doesn't implement IFeeCalculable.
        //   The monitoring script can NEVER accidentally call RecalculateFees() on it.
        foreach (var processor in feeProcessors)
        {
            processor.RecalculateFees("ACC001");
            Console.WriteLine($"   Fee: ₹{processor.GetCurrentFee("ACC001"):N0}");
        }

        Console.WriteLine("\n   NOTE: This is the I in SOLID.");
        Console.WriteLine("   You will go deeper into all 5 SOLID principles in Phase 2.");

        Console.WriteLine("\n✅ InterfaceSegregation — understood.");
    }
}
