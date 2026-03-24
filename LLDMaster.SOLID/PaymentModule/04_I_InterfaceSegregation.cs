/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  SECTION 1 — FILE HEADER                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  💎 SOLID PRINCIPLE: I — Interface Segregation Principle (ISP)               ║
 * ║                                                                              ║
 * ║  🔗 OOP ORIGIN: "In your OOP module you previewed this — IPaymentService     ║
 * ║     was split into IPaymentGateway and IPaymentRepository. You did it by     ║
 * ║     instinct. Now we formalise WHY with full SOLID reasoning."               ║
 * ║                                                                              ║
 * ║  THE ONE-LINE RULE (memorise this):                                          ║
 * ║  "No class should be forced to implement methods it doesn't use.             ║
 * ║   If it does, the interface is too fat. Split it."                           ║
 * ║                                                                              ║
 * ║  Simpler: "Interfaces should be small enough that every implementor          ║
 * ║            uses EVERY method."                                               ║
 * ║                                                                              ║
 * ║  WHAT GOES WRONG WITHOUT IT:                                                 ║
 * ║  A fat IPaymentService forces FraudDetector to implement ProcessPayment().   ║
 * ║  FraudDetector throws NotImplementedException — that's an LSP violation      ║
 * ║  caused by ISP. ISP violations cascade into LSP violations.                 ║
 * ║  Fix ISP and LSP violations in that area disappear automatically.            ║
 * ║                                                                              ║
 * ║  THE PR SMELL:                                                               ║
 * ║  "In a code review, you'd spot this when any class implementing an interface ║
 * ║   contains a NotImplementedException, or when an interface name contains AND.║
 * ║                                                                              ║
 * ║  CONNECTS TO NEXT:                                                           ║
 * ║  "Clean, focused interfaces are now ready to be injected.                    ║
 * ║   But WHERE are they created? Who controls which concrete class is used?     ║
 * ║   If PaymentOrchestrator does `new FraudDetectionService()` internally —     ║
 * ║   you can never test or swap it. That's Dependency Inversion — File 05."    ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLDMaster.SOLID.PaymentModule.ISP;

// ─────────────────────────────────────────────────────────────────────────────
// Minimal shared types
// ─────────────────────────────────────────────────────────────────────────────

public record PaymentRequest_ISP(decimal Amount, string Token, string AccountId);
public record PaymentResult_ISP(bool IsSuccess, string TransactionId, string Message)
{
    public static PaymentResult_ISP Success(string txId) => new(true, txId, "Success.");
    public static PaymentResult_ISP Failure(string r)    => new(false, string.Empty, r);
}
public record Transaction_ISP(string Id, decimal Amount, string Status, DateTime ProcessedAt);
public record DateRange(DateTime From, DateTime To);


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 2 — BEFORE (minimal violation)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ❌ BEFORE — One fat interface, two implementors that can't use it honestly.
// This interface grew because "all payment-related things live in IPaymentService."
// A reasonable instinct. A catastrophic result at scale.

public interface IPaymentService_Fat
{
    // Payment operations — PaymentProcessor uses these
    PaymentResult_ISP ProcessPayment(PaymentRequest_ISP request);
    PaymentResult_ISP Refund(string transactionId);

    // Reporting operations — AuditDashboard uses these
    List<Transaction_ISP> GetTransactionHistory(string accountId);
    byte[] ExportToCsv(DateRange range);

    // Fraud operations — FraudEngine uses these
    bool IsFraudulent(PaymentRequest_ISP request);
    void TrainFraudModel(List<Transaction_ISP> historicalData);

    // Notification operations — NotificationWorker uses these
    void SendReceipt(string email, PaymentResult_ISP result);
    void SendFraudAlert(string email, PaymentRequest_ISP request);
}

// ❌ Implementor 1: FraudDetectionService only needs fraud operations
public class FraudDetectionService_Bad : IPaymentService_Fat
{
    // ✅ These two it genuinely uses
    public bool IsFraudulent(PaymentRequest_ISP request)
    {
        Console.WriteLine($"   [Fraud] Checking token {request.Token}...");
        return false;
    }

    public void TrainFraudModel(List<Transaction_ISP> historicalData)
    {
        Console.WriteLine($"   [Fraud] Training on {historicalData.Count} transactions...");
    }

    // 🚨 Forced to implement — throws because it makes no sense here
    public PaymentResult_ISP ProcessPayment(PaymentRequest_ISP r)
        => throw new NotImplementedException("FraudDetectionService cannot process payments.");

    public PaymentResult_ISP Refund(string id)
        => throw new NotImplementedException("FraudDetectionService cannot issue refunds.");

    public List<Transaction_ISP> GetTransactionHistory(string id)
        => throw new NotImplementedException("FraudDetectionService has no transaction history.");

    public byte[] ExportToCsv(DateRange range)
        => throw new NotImplementedException();

    public void SendReceipt(string email, PaymentResult_ISP result)
        => throw new NotImplementedException();

    public void SendFraudAlert(string email, PaymentRequest_ISP request)
        => throw new NotImplementedException();
}

// ❌ Implementor 2: AuditReportingService is read-only — reporting only
public class AuditReportingService_Bad : IPaymentService_Fat
{
    private readonly List<Transaction_ISP> _audit = new();

    // ✅ These two it genuinely uses
    public List<Transaction_ISP> GetTransactionHistory(string accountId)
    {
        Console.WriteLine($"   [Audit] Fetching history for {accountId}...");
        return _audit.Where(t => t.Id.StartsWith(accountId)).ToList();
    }

    public byte[] ExportToCsv(DateRange range)
    {
        Console.WriteLine($"   [Audit] Exporting CSV from {range.From:d} to {range.To:d}...");
        return Array.Empty<byte>();
    }

    // 🚨 Forced to implement ProcessPayment — a read-only audit service cannot do this
    // In production: a bug called ProcessPayment() on the audit service.
    // ₹12,000 was charged twice. Refund took 3 weeks. Customer churned.
    public PaymentResult_ISP ProcessPayment(PaymentRequest_ISP r)
        => throw new NotImplementedException("AuditService is read-only. Cannot process payments.");

    public PaymentResult_ISP Refund(string id)
        => throw new NotImplementedException();

    public bool IsFraudulent(PaymentRequest_ISP r)
        => throw new NotImplementedException();

    public void TrainFraudModel(List<Transaction_ISP> data)
        => throw new NotImplementedException();

    public void SendReceipt(string email, PaymentResult_ISP result)
        => throw new NotImplementedException();

    public void SendFraudAlert(string email, PaymentRequest_ISP request)
        => throw new NotImplementedException();
}

// 🚨 VIOLATION: I — Both implementors are forced to fake 75% of the interface
//
// 💥 CONSEQUENCE 1: FraudDetectionService implements IPaymentService.
//    New developer sees it and thinks FraudService can process payments.
//    They call IPaymentService.ProcessPayment() — NotImplementedException.
//    45-minute debugging session. The interface lied about capabilities.
//
// 💥 CONSEQUENCE 2: Bug called ProcessPayment() on AuditReportingService.
//    ₹12,000 charged twice. Refund took 3 weeks. Customer churned.
//    A fat interface made an accidental misuse compile cleanly.


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 3 — WHY THIS VIOLATES ISP
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 🔍 THE REASONING:
//
// PROBLEM 1 — ISP VIOLATIONS CAUSE LSP VIOLATIONS:
//   Every NotImplementedException above is an LSP violation.
//   The parent contract (IPaymentService_Fat) promises: "ProcessPayment works."
//   The child delivers: "ProcessPayment throws." — LSP broken.
//   Split the fat interface → NotImplementedException disappears → LSP restored.
//   ISP and LSP are connected. The root cause is the fat interface, not the class.
//
// PROBLEM 2 — DEPENDENCY POLLUTION AND UNNECESSARY RECOMPILATION:
//   FraudDetectionService depends on IPaymentService_Fat.
//   IPaymentService_Fat changes because the CSV export format changed.
//   FraudDetectionService recompiles. Redeploys. For a change it doesn't care about.
//   In microservices: FraudService gets redeployed every time ReportingService changes.
//   Unnecessary deployment risk, wasted CI/CD minutes, on-call pages from false alarms.
//
// PROBLEM 3 — DISCOVERABILITY DAMAGE:
//   FraudDetectionService implements IPaymentService_Fat.
//   A new developer reads this, checks the interface — sees ProcessPayment(), Refund().
//   They assume FraudDetectionService can process payments.
//   They call it. NotImplementedException. 45-minute debugging session.
//   The interface ADVERTISED capabilities the class doesn't have.
//
// DESIGN DECISION: Split interfaces by consumer, not by implementor.
//   The question to ask: "Who calls this method?"
//   PaymentController calls: IPaymentProcessor
//   AuditDashboard calls: ITransactionReporter
//   FraudEngine calls: IFraudService
//   NotificationWorker calls: IPaymentNotifier
//
//   If the method splits naturally by caller, the interface split is correct.


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 4 — AFTER (ISP-correct)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ✅ AFTER — Four focused interfaces. Every method used by every implementor.
// NotImplementedException becomes structurally impossible.

// ─── Interface 1: IPaymentProcessor — PaymentController's contract ────────────
public interface IPaymentProcessor
{
    PaymentResult_ISP ProcessPayment(PaymentRequest_ISP request);
    PaymentResult_ISP Refund(string transactionId);
}

// ─── Interface 2: ITransactionReporter — AuditDashboard's contract ───────────
public interface ITransactionReporter
{
    List<Transaction_ISP> GetTransactionHistory(string accountId);
    byte[] ExportToCsv(DateRange range);
}

// ─── Interface 3: IFraudService — FraudEngine's contract ─────────────────────
public interface IFraudService
{
    bool IsFraudulent(PaymentRequest_ISP request);
    void TrainFraudModel(List<Transaction_ISP> historicalData);
}

// ─── Interface 4: IPaymentNotifier — NotificationWorker's contract ────────────
public interface IPaymentNotifier
{
    void SendReceipt(string email, PaymentResult_ISP result);
    void SendFraudAlert(string email, PaymentRequest_ISP request);
}

// ✅ Clean implementor 1: FraudDetectionService — 2 methods, both used
public class FraudDetectionService : IFraudService
{
    public bool IsFraudulent(PaymentRequest_ISP request)
    {
        Console.WriteLine($"   [Fraud] Checking token {request.Token} — clean.");
        return false;
    }

    public void TrainFraudModel(List<Transaction_ISP> historicalData)
    {
        Console.WriteLine($"   [Fraud] Training model on {historicalData.Count} transactions.");
    }
    // ✅ Zero NotImplementedException. Structurally impossible with IFraudService.
}

// ✅ Clean implementor 2: AuditReportingService — 2 methods, both used
public class AuditReportingService : ITransactionReporter
{
    private readonly List<Transaction_ISP> _audit;
    public AuditReportingService(List<Transaction_ISP> audit) { _audit = audit; }

    public List<Transaction_ISP> GetTransactionHistory(string accountId)
    {
        Console.WriteLine($"   [Audit] Fetching history for {accountId}.");
        return _audit;
    }

    public byte[] ExportToCsv(DateRange range)
    {
        Console.WriteLine($"   [Audit] Exporting CSV for {range.From:d}–{range.To:d}.");
        return Array.Empty<byte>();
    }
    // ✅ Cannot accidentally call ProcessPayment() — it's not in ITransactionReporter
}

// ✅ Clean implementor 3: PaymentGatewayService — 2 methods, both used
public class PaymentGatewayService : IPaymentProcessor
{
    public PaymentResult_ISP ProcessPayment(PaymentRequest_ISP request)
    {
        Console.WriteLine($"   [Gateway] Processing ₹{request.Amount} for token {request.Token}...");
        return PaymentResult_ISP.Success(Guid.NewGuid().ToString("N")[..8].ToUpper());
    }

    public PaymentResult_ISP Refund(string transactionId)
    {
        Console.WriteLine($"   [Gateway] Refunding tx={transactionId}...");
        return PaymentResult_ISP.Success(transactionId);
    }
}

// ✅ Clean implementor 4: NotificationService — 2 methods, both used
public class NotificationService : IPaymentNotifier
{
    public void SendReceipt(string email, PaymentResult_ISP result)
    {
        Console.WriteLine($"   [Notify] Receipt sent to {email} for tx={result.TransactionId}.");
    }

    public void SendFraudAlert(string email, PaymentRequest_ISP request)
    {
        Console.WriteLine($"   [Notify] Fraud alert sent to {email} for token {request.Token}.");
    }
}

// ✅ A class CAN implement multiple focused interfaces — when BOTH are genuinely its job.
//    FullPaymentService both processes AND notifies. Neither is faked.
public class FullPaymentService : IPaymentProcessor, IPaymentNotifier
{
    public PaymentResult_ISP ProcessPayment(PaymentRequest_ISP request)
    {
        Console.WriteLine($"   [Full] Processing ₹{request.Amount}...");
        return PaymentResult_ISP.Success(Guid.NewGuid().ToString("N")[..8].ToUpper());
    }

    public PaymentResult_ISP Refund(string transactionId)
    {
        Console.WriteLine($"   [Full] Refunding tx={transactionId}...");
        return PaymentResult_ISP.Success(transactionId);
    }

    public void SendReceipt(string email, PaymentResult_ISP result)
    {
        Console.WriteLine($"   [Full] Receipt sent to {email}.");
    }

    public void SendFraudAlert(string email, PaymentRequest_ISP request)
    {
        Console.WriteLine($"   [Full] Fraud alert sent to {email}.");
    }
    // ✅ Does NOT implement IFraudService or ITransactionReporter — still ISP-compliant
}


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 5 — PR REVIEW CHECKLIST
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 👁️ HOW TO SPOT THIS IN A REAL PR REVIEW:
// ✅ SAFE:  Every method in the interface is used by every implementor
// ✅ SAFE:  Interface has 2–4 methods, all belonging to the same domain and caller
// ✅ SAFE:  You can describe every implementor's job without mentioning the interface's other methods
// 🚨 FLAG:  Any NotImplementedException in an interface implementation (ISP + LSP violation)
// 🚨 FLAG:  Interface name has AND in it (IPaymentAndFraudService)
// 🚨 FLAG:  Interface has methods from obviously different domains (ProcessPayment + ExportCsv)
// 🚨 FLAG:  A class implements an interface but genuinely uses < 50% of its methods
// 🚨 FLAG:  "I'll just add this method to IPaymentService" — first ask: who are the other implementors?
//           Does every existing implementor actually need this new method?


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 6 — DEMO
// ╚══════════════════════════════════════════════════════════════════════════════╝

public static class InterfaceSegregationDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════════════════");
        Console.WriteLine("  I — Interface Segregation Principle");
        Console.WriteLine("══════════════════════════════════════════════════\n");

        var request = new PaymentRequest_ISP(1500m, "tok_visa_4242", "ACC001");

        // ── BEFORE: the violation ─────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — Fat interface forces NotImplementedException:\n");

        IPaymentService_Fat fraud_bad = new FraudDetectionService_Bad();
        fraud_bad.IsFraudulent(request);   // ✓ works
        fraud_bad.TrainFraudModel(new List<Transaction_ISP>()); // ✓ works

        Console.WriteLine("   Calling ProcessPayment() on FraudDetectionService_Bad:");
        try
        {
            fraud_bad.ProcessPayment(request); // 💥 NotImplementedException
        }
        catch (NotImplementedException ex)
        {
            Console.WriteLine($"   💥 {ex.Message}");
            Console.WriteLine("   ^ New developer called this legitimately. Interface advertised the capability.\n");
        }

        // ── AFTER: the fix ────────────────────────────────────────────────────
        Console.WriteLine("✅ AFTER — Focused interfaces. Every method used. NotImplementedException impossible:\n");

        // FraudEngine only knows about IFraudService — cannot accidentally call ProcessPayment()
        IFraudService fraudService = new FraudDetectionService();
        fraudService.IsFraudulent(request);
        fraudService.TrainFraudModel(new List<Transaction_ISP>());
        Console.WriteLine("   ^ FraudDetectionService: only sees IFraudService. ProcessPayment doesn't exist here.");

        // AuditDashboard only knows about ITransactionReporter
        ITransactionReporter reporter = new AuditReportingService(new List<Transaction_ISP>());
        reporter.GetTransactionHistory("ACC001");
        reporter.ExportToCsv(new DateRange(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow));
        Console.WriteLine("   ^ AuditReportingService: only sees ITransactionReporter. Cannot process payments.");

        // PaymentController only knows about IPaymentProcessor
        IPaymentProcessor processor = new PaymentGatewayService();
        var result = processor.ProcessPayment(request);
        processor.Refund(result.TransactionId);

        // NotificationWorker only knows about IPaymentNotifier
        IPaymentNotifier notifier = new NotificationService();
        notifier.SendReceipt("user@example.com", result);

        Console.WriteLine();
        Console.WriteLine("   Each consumer depends on exactly the interface it needs.");
        Console.WriteLine("   Reporting service changes? Only ITransactionReporter recompiles.");
        Console.WriteLine("   FraudService is unaffected. Zero wasted deployments.\n");

        Console.WriteLine("✅ Interface Segregation — understood.");
        Console.WriteLine("→ This creates the need for Dependency Inversion Principle (File 05).");
        Console.WriteLine("  Clean focused interfaces are ready. But WHO creates the concretions?");
        Console.WriteLine("  If PaymentOrchestrator does `new FraudDetectionService()` — you can never swap it.");
    }
}
