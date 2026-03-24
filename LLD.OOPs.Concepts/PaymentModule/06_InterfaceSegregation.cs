/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🧠 CONCEPT: Interface Segregation Principle (ISP)                           ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  No class should be forced to implement methods it doesn't need. Instead of  ║
 * ║  one large "do everything" interface, split it into small focused ones.      ║
 * ║  Each class implements only the interfaces relevant to what it actually does. ║
 * ║                                                                              ║
 * ║  MENTAL MODEL (the analogy to remember forever):                             ║
 * ║  A TV remote with 50 buttons. You only ever use 5. But you can't hide or     ║
 * ║  remove the 45 buttons you never touch. Now imagine a separate "Basic Remote" ║
 * ║  (5 buttons) and an "Advanced Remote" (full 50). You give guests the basic   ║
 * ║  one. Right tool, right job. That's ISP.                                     ║
 * ║                                                                              ║
 * ║  THE PRODUCTION HORROR STORY (why this matters):                             ║
 * ║  A reporting service implemented a fat IPaymentService with 10 methods. It   ║
 * ║  only needed 2. The other 8 threw NotImplementedException. A junior dev      ║
 * ║  looping over all IPaymentService instances called RecalculateFees() on the  ║
 * ║  reporting service in a cron job. It threw at 2 AM, woke the on-call, and    ║
 * ║  the root cause took 90 minutes to trace back to the wrong interface.        ║
 * ║                                                                              ║
 * ║  QUICK RECALL (read this in 2 years and instantly remember):                 ║
 * ║  This is the "I" in SOLID. Fat interfaces = forced NotImplementedException.  ║
 * ║  Small interfaces = classes implement only what they mean.                   ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 *
 *  NOTE: This is the I in SOLID — you'll master it fully in Phase 2 (SOLID module).
 *  Here we see it in context: fat interface → split interfaces → clean implementations.
 */

namespace LLD.OOPs.Concepts.PaymentModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — How a beginner naturally writes this
// "One interface to rule them all." Seems tidy at first.
// Every service that needs ANY of these methods gets ALL of them.
// ─────────────────────────────────────────────────────────────────────────────

// ❌ Fat interface: 8 methods crammed into one contract
public interface IBadPaymentService
{
    // Payment operations
    void ProcessPayment(decimal amount, string token);
    void Refund(string transactionId, decimal amount);

    // Reporting operations
    void GenerateReport(DateTime from, DateTime to);
    void ExportToCsv(string filePath);

    // Notification operations
    void SendNotification(string userId, string message);

    // Audit operations
    string GetAuditLog(string transactionId);
    void ArchiveTransaction(string transactionId);

    // Fee operations
    decimal RecalculateFees(string transactionId);
}

// ❌ ReadOnlyDashboard only needs GenerateReport and GetAuditLog.
//    But the interface forces it to "implement" 6 methods it has no business touching.
public class BadReadOnlyDashboard : IBadPaymentService
{
    // ✅ These two it actually implements:
    public void   GenerateReport(DateTime from, DateTime to) =>
        Console.WriteLine($"   Report: {from:d} → {to:d}");

    public string GetAuditLog(string transactionId) =>
        $"Audit: tx={transactionId} status=completed";

    // ❌ All of these are forced upon it. NotImplementedException is a lie in code.
    public void    ProcessPayment(decimal amount, string token)  => throw new NotImplementedException();
    public void    Refund(string transactionId, decimal amount)  => throw new NotImplementedException();
    public void    ExportToCsv(string filePath)                  => throw new NotImplementedException();
    public void    SendNotification(string userId, string msg)   => throw new NotImplementedException();
    public void    ArchiveTransaction(string transactionId)      => throw new NotImplementedException();
    public decimal RecalculateFees(string transactionId)         => throw new NotImplementedException();
}

// 💥 WHAT GOES WRONG:
//
//   List<IBadPaymentService> services = GetAllServices();
//   foreach (var svc in services)
//       svc.RecalculateFees("tx_001");   // 💥 Hits BadReadOnlyDashboard → NotImplementedException
//
// The caller assumed all IBadPaymentService implementors can recalculate fees.
// The fat interface made that assumption look reasonable. It wasn't.
// At 2 AM, the cron job fails and nobody knows why until they read a stack trace.


// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — The OOP-correct way
// Split the fat interface into focused, role-specific interfaces.
// Each class implements exactly the interfaces matching its responsibilities.
// ─────────────────────────────────────────────────────────────────────────────

// ✅ IPaymentProcessor — only payment operations
public interface IPaymentProcessor
{
    void ProcessPayment(decimal amount, string token);
    void Refund(string transactionId, decimal amount);
}

// ✅ IReportable — only reporting operations
public interface IReportable
{
    void   GenerateReport(DateTime from, DateTime to);
    void   ExportToCsv(string filePath);
}

// ✅ INotifiable — only notification operations
public interface INotifiable
{
    void SendNotification(string userId, string message);
}

// ✅ IAuditable — only audit/archive operations
public interface IAuditable
{
    string GetAuditLog(string transactionId);
    void   ArchiveTransaction(string transactionId);
}

// ✅ IFeeCalculable — only fee operations
public interface IFeeCalculable
{
    decimal RecalculateFees(string transactionId);
}

// ─────────────────────────────────────────────────────────────────────────────
// ✅ Now each class implements ONLY what it needs
// ─────────────────────────────────────────────────────────────────────────────

// ✅ ReadOnlyDashboard: only needs to report and audit — implements exactly 2 interfaces
public class ReadOnlyDashboard : IReportable, IAuditable
{
    public void   GenerateReport(DateTime from, DateTime to) =>
        Console.WriteLine($"   [Dashboard] Report: {from:d} → {to:d}");

    public void   ExportToCsv(string filePath) =>
        Console.WriteLine($"   [Dashboard] Exported to {filePath}");

    public string GetAuditLog(string transactionId) =>
        $"[Dashboard] Audit: tx={transactionId} status=completed";

    public void   ArchiveTransaction(string transactionId) =>
        Console.WriteLine($"   [Dashboard] Archived {transactionId}");

    // ✅ No ProcessPayment, no RecalculateFees, no fake NotImplementedException.
    //    The interface doesn't even mention them, so there's nothing to "not implement".
}

// ✅ FullPaymentService: the real deal — implements all relevant interfaces
public class FullPaymentService : IPaymentProcessor, IReportable, INotifiable, IAuditable, IFeeCalculable
{
    public void    ProcessPayment(decimal amount, string token) =>
        Console.WriteLine($"   [PaymentSvc] Charged ₹{amount} for token {token}");

    public void    Refund(string transactionId, decimal amount) =>
        Console.WriteLine($"   [PaymentSvc] Refunded ₹{amount} for {transactionId}");

    public void    GenerateReport(DateTime from, DateTime to) =>
        Console.WriteLine($"   [PaymentSvc] Report: {from:d} → {to:d}");

    public void    ExportToCsv(string filePath) =>
        Console.WriteLine($"   [PaymentSvc] Exported to {filePath}");

    public void    SendNotification(string userId, string message) =>
        Console.WriteLine($"   [PaymentSvc] Notified user {userId}: {message}");

    public string  GetAuditLog(string transactionId) =>
        $"[PaymentSvc] Audit: tx={transactionId}";

    public void    ArchiveTransaction(string transactionId) =>
        Console.WriteLine($"   [PaymentSvc] Archived {transactionId}");

    public decimal RecalculateFees(string transactionId)
    {
        Console.WriteLine($"   [PaymentSvc] Recalculated fees for {transactionId}");
        return 2.5m; // example fee
    }
}

// ✅ NotificationService: only sends notifications
public class NotificationService : INotifiable
{
    public void SendNotification(string userId, string message) =>
        Console.WriteLine($"   [NotifSvc] SMS to {userId}: {message}");
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

        Console.WriteLine("❌ BEFORE — Fat interface forces 6 NotImplementedException stubs:\n");
        var badDash = new BadReadOnlyDashboard();
        badDash.GenerateReport(DateTime.Today.AddDays(-7), DateTime.Today); // works
        try
        {
            badDash.RecalculateFees("tx_001"); // 💥 boom
        }
        catch (NotImplementedException)
        {
            Console.WriteLine("   💥 RecalculateFees() threw NotImplementedException on a dashboard!\n");
        }

        Console.WriteLine("✅ AFTER — Split interfaces, each class implements only what it needs:\n");

        var dashboard = new ReadOnlyDashboard();
        dashboard.GenerateReport(DateTime.Today.AddDays(-7), DateTime.Today);
        Console.WriteLine($"   {dashboard.GetAuditLog("tx_789")}");
        // dashboard.ProcessPayment() ← won't even compile. Interface doesn't include it.

        Console.WriteLine();
        var fullSvc = new FullPaymentService();
        fullSvc.ProcessPayment(1500m, "tok_visa");
        fullSvc.SendNotification("user_42", "Payment of ₹1500 received.");
        Console.WriteLine($"   Fee: ₹{fullSvc.RecalculateFees("tx_001")}");

        Console.WriteLine();
        Console.WriteLine("   A cron job running over IFeeCalculable instances:");
        IFeeCalculable[] feeServices = { fullSvc }; // ✅ ReadOnlyDashboard can't even get in this list
        foreach (var svc in feeServices)
            Console.WriteLine($"   Recalculated: ₹{svc.RecalculateFees("tx_002")}");

        Console.WriteLine();
        Console.WriteLine("   NOTE: This is the I in SOLID.");
        Console.WriteLine("         You'll go deeper into all 5 principles in Phase 2.");

        Console.WriteLine("\n✅ InterfaceSegregation — understood.");
    }
}
