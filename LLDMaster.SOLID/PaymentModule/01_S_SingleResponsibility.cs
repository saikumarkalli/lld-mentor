/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  SECTION 1 — FILE HEADER                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  💎 SOLID PRINCIPLE: S — Single Responsibility Principle (SRP)               ║
 * ║                                                                              ║
 * ║  🔗 OOP ORIGIN: "In your OOP PaymentModule, GoodPaymentService_WithDIP was   ║
 * ║     clean — it just called a gateway and saved to a repo. That was good OOP. ║
 * ║     Then your team got 3 new requirements in 1 sprint. Here's what happened."║
 * ║                                                                              ║
 * ║  THE ONE-LINE RULE (memorise this):                                          ║
 * ║  "A class should have ONE reason to change.                                  ║
 * ║   Count the reasons. If it's more than one, SRP is violated."               ║
 * ║                                                                              ║
 * ║  WHAT GOES WRONG WITHOUT IT:                                                 ║
 * ║  A PaymentService that also handles logging, email, PDF receipts, and fraud  ║
 * ║  detection has 5 reasons to change.                                          ║
 * ║  — Marketing changes email template → you're in PaymentService              ║
 * ║  — Finance changes PDF format       → you're in PaymentService              ║
 * ║  — Security changes fraud rules     → you're in PaymentService              ║
 * ║  Every team is one edit away from breaking payments.                         ║
 * ║  In a PCI-DSS environment, every deployment needs security sign-off.         ║
 * ║  Marketing now needs security approval to change an email template.          ║
 * ║                                                                              ║
 * ║  THE PR SMELL:                                                               ║
 * ║  "In a code review, you'd spot this when you see method names that span      ║
 * ║   different domains: ProcessPayment() next to SendEmail() in the same class."║
 * ║                                                                              ║
 * ║  CONNECTS TO NEXT:                                                           ║
 * ║  "Fixing SRP means PaymentOrchestrator now calls IFraudDetector,            ║
 * ║   IReceiptEmailSender, etc. But what happens when you add a new payment      ║
 * ║   STEP? You edit PaymentOrchestrator. That's an OCP violation — File 02."   ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLDMaster.SOLID.PaymentModule.SRP;

// ─────────────────────────────────────────────────────────────────────────────
// Minimal shared types for this file — just enough to make the code compile
// and feel realistic. In a real project these live in a shared domain layer.
// ─────────────────────────────────────────────────────────────────────────────

public record PaymentRequest(decimal Amount, string Token, string Email, string AccountId);

public record PaymentResult(bool IsSuccess, string TransactionId, string Message)
{
    public static PaymentResult Success(string txId) =>
        new(true,  txId, "Payment processed successfully.");

    public static PaymentResult Failure(string reason) =>
        new(false, string.Empty, reason);
}

// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 2 — BEFORE (minimal violation)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ❌ BEFORE — Your OOP PaymentService, after 4 additional sprint requirements.
// Sprint 2 added email receipts. Sprint 3 added PDF invoices.
// Sprint 4 added fraud detection. Sprint 5 added Splunk logging.
// Nobody extracted a class each time — it was always "just one more method."

public class PaymentService_Bad
{
    // ─── Original OOP responsibility: process payments ✓ ─────────────────────
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        // Business logic: validate, charge, return result
        if (request.Amount <= 0)
            return PaymentResult.Failure("Amount must be positive.");

        LogToSplunk($"Processing payment for {request.Token}");
        var txId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return PaymentResult.Success(txId);
    }

    // ─── Requirement added sprint 2: "we need email receipts" ────────────────
    // 🚨 VIOLATION: S — email concerns don't belong in a payment class
    public void SendReceiptEmail(string email, PaymentResult result)
    {
        // In reality: SMTP setup, HTML template building, attachment logic (~40 lines)
        Console.WriteLine($"   [SMTP] Connecting to mail server...");
        Console.WriteLine($"   [SMTP] Sending receipt to {email} for tx={result.TransactionId}");
    }

    // ─── Requirement added sprint 3: "finance needs PDF invoices" ────────────
    // 🚨 VIOLATION: S — PDF generation is a separate domain entirely
    public byte[] GeneratePdfInvoice(PaymentResult result)
    {
        // In reality: PDF library instantiation, template rendering, font loading (~60 lines)
        Console.WriteLine($"   [PDF] Generating invoice for tx={result.TransactionId}");
        return Array.Empty<byte>(); // stub
    }

    // ─── Requirement added sprint 4: "add fraud detection" ───────────────────
    // 🚨 VIOLATION: S — fraud rules are a security concern, not a payment concern
    public bool IsFraudulent(PaymentRequest request)
    {
        // In reality: IP checking, velocity rules, blacklist lookup (~50 lines)
        Console.WriteLine($"   [Fraud] Checking request for token {request.Token}...");
        return false; // stub: always clean
    }

    // ─── Requirement added sprint 5: "log everything to Splunk" ──────────────
    // 🚨 VIOLATION: S — logging infrastructure is an ops concern
    private void LogToSplunk(string message)
    {
        // In reality: HTTP client, Splunk token, JSON serialisation
        Console.WriteLine($"   [Splunk] {message}");
    }
}

// 🚨 VIOLATION: S — PaymentService_Bad has 5 reasons to change:
//   1. Payment processing rules change  (business team)
//   2. Email template or SMTP changes   (marketing team)
//   3. PDF invoice format changes       (finance team)
//   4. Fraud detection rules change     (security team)
//   5. Logging destination changes      (devops team)
//
// 💥 CONSEQUENCE: Marketing's email change broke payment processing last Tuesday.
//    It was a merge conflict nobody noticed. ₹2.3 lakh in failed transactions.
//    Five teams own one file. Every sprint has merge conflicts in PaymentService.cs.


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 3 — WHY THIS VIOLATES SRP
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 🔍 THE REASONING:
//
// PROBLEM 1 — CHANGE AMPLIFICATION:
//   Five teams (Payments, Marketing, Finance, Security, DevOps) all own one class.
//   Every sprint produces merge conflicts in PaymentService_Bad.cs.
//   Developers start to fear touching it. They work around it.
//   Technical debt compounds sprint after sprint.
//
// PROBLEM 2 — TESTING IMPOSSIBILITY:
//   To unit-test ProcessPayment(), you must have: a working SMTP server, a PDF
//   library, a fraud API, and a Splunk endpoint. A unit test becomes an integration
//   test. CI pipelines need infra. Test coverage drops. Bugs go undetected.
//
// PROBLEM 3 — DEPLOYMENT COUPLING:
//   Email template change? Redeploy the entire payment processor.
//   In a PCI-DSS environment, every deployment of a payment service needs a
//   security sign-off review. Marketing now needs security approval to change a
//   font colour in an email. Teams slow down. Releases stack up. Tension grows.
//
// DESIGN DECISION: Extract each responsibility to its own class.
//   One class per reason to change. Name each class after its ONE job.
//   Rule of thumb: "If you can't name the class without using AND, SRP is violated."
//   PaymentService AND EmailSender → violation.
//   PaymentService (processes payments only) → SRP.
//   At a 50-person company, the cost of not fixing this is one engineer's week
//   every sprint spent on merge conflicts and reverting regressions.


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 4 — AFTER (SRP-correct)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ✅ AFTER — Same domain, five focused classes.
// Each class has ONE job. Each interface is owned by one team.
// PaymentOrchestrator is the ONLY class with multiple dependencies —
// its single responsibility IS orchestration. That's fine.

// ─── Responsibility 1: Process payments (business team owns this) ─────────────
public class PaymentService
{
    // ✅ ONE job: apply payment processing rules and invoke the gateway.
    //    This class changes ONLY when payment rules change.
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        if (request.Amount <= 0)
            return PaymentResult.Failure("Amount must be positive.");

        // Pure business logic — no email, no PDF, no fraud, no logging
        var txId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        Console.WriteLine($"   [Payment] Processed ₹{request.Amount} → tx={txId}");
        return PaymentResult.Success(txId);
    }
}

// ─── Responsibility 2: Send receipt emails (marketing team owns this) ─────────
public interface IReceiptEmailSender
{
    void SendReceipt(string email, PaymentResult result);
}

public class SmtpReceiptEmailSender : IReceiptEmailSender
{
    // ✅ Changes ONLY when SMTP config or email template changes
    public void SendReceipt(string email, PaymentResult result)
    {
        Console.WriteLine($"   [SMTP] Receipt sent to {email} for tx={result.TransactionId}");
    }
}

// ─── Responsibility 3: Generate PDF invoices (finance team owns this) ─────────
public interface IInvoiceGenerator
{
    byte[] GenerateInvoice(PaymentResult result);
}

public class PdfInvoiceGenerator : IInvoiceGenerator
{
    // ✅ Changes ONLY when PDF format or invoice library changes
    public byte[] GenerateInvoice(PaymentResult result)
    {
        Console.WriteLine($"   [PDF] Invoice generated for tx={result.TransactionId}");
        return Array.Empty<byte>();
    }
}

// ─── Responsibility 4: Detect fraud (security team owns this) ─────────────────
public interface IFraudDetector
{
    bool IsFraudulent(PaymentRequest request);
}

public class RuleBasedFraudDetector : IFraudDetector
{
    // ✅ Changes ONLY when fraud detection rules change
    public bool IsFraudulent(PaymentRequest request)
    {
        Console.WriteLine($"   [Fraud] Checked token {request.Token} — clean.");
        return false;
    }
}

// ─── Responsibility 5: Log payment events (devops team owns this) ─────────────
public interface IPaymentLogger
{
    void LogPayment(string message);
}

public class SplunkPaymentLogger : IPaymentLogger
{
    // ✅ Changes ONLY when the logging destination or format changes
    public void LogPayment(string message)
    {
        Console.WriteLine($"   [Splunk] {message}");
    }
}

// ─── The Orchestrator: coordinates responsibilities (one team: platform) ───────
public class PaymentOrchestrator
{
    private readonly PaymentService    _paymentService;
    private readonly IFraudDetector    _fraudDetector;
    private readonly IReceiptEmailSender _emailSender;
    private readonly IInvoiceGenerator _invoiceGenerator;
    private readonly IPaymentLogger    _logger;

    // ✅ Constructor injection — each dependency is clearly named and owned
    public PaymentOrchestrator(
        PaymentService     paymentService,
        IFraudDetector     fraudDetector,
        IReceiptEmailSender emailSender,
        IInvoiceGenerator  invoiceGenerator,
        IPaymentLogger     logger)
    {
        _paymentService   = paymentService;
        _fraudDetector    = fraudDetector;
        _emailSender      = emailSender;
        _invoiceGenerator = invoiceGenerator;
        _logger           = logger;
    }

    // ✅ This class has ONE responsibility: orchestrate the payment flow.
    //    Each step is delegated. This method changes ONLY when the flow changes.
    public PaymentResult Execute(PaymentRequest request)
    {
        _logger.LogPayment($"Starting payment flow for token={request.Token}");

        // Step 1: fraud check — security team's logic, not ours
        if (_fraudDetector.IsFraudulent(request))
            return PaymentResult.Failure("Payment rejected — fraud detected.");

        // Step 2: core payment — business team's logic
        var result = _paymentService.ProcessPayment(request);
        if (!result.IsSuccess) return result;

        // Step 3: downstream actions — each team owns its own class
        _emailSender.SendReceipt(request.Email, result);
        _invoiceGenerator.GenerateInvoice(result);
        _logger.LogPayment($"Payment flow complete for tx={result.TransactionId}");

        return result;
    }
}


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 5 — PR REVIEW CHECKLIST
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 👁️ HOW TO SPOT THIS IN A REAL PR REVIEW:
// ✅ SAFE:  Class name is a single noun with no AND (PaymentService, not PaymentAndEmailService)
// ✅ SAFE:  Class has one cohesive group of methods around one concept
// ✅ SAFE:  You can describe the class's job in one sentence without using "and"
// 🚨 FLAG:  Method names suggest different domains in the same class (ProcessPayment + SendEmail)
// 🚨 FLAG:  Class has 5+ injected dependencies — it's likely doing too much
// 🚨 FLAG:  "We always change these two methods together" — they belong in the same class,
//           but the class also has OTHER methods → extract a new class
// 🚨 FLAG:  Private helpers that belong to a completely different domain (LogToSplunk inside PaymentService)
// 🚨 FLAG:  Multiple using directives for unrelated infrastructure (SMTP + PDF + SQL in one file)


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 6 — DEMO
// ╚══════════════════════════════════════════════════════════════════════════════╝

public static class SingleResponsibilityDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════════════════");
        Console.WriteLine("  S — Single Responsibility Principle");
        Console.WriteLine("══════════════════════════════════════════════════\n");

        var request = new PaymentRequest(1500m, "tok_visa_4242", "user@example.com", "ACC001");

        // ── BEFORE: showing the violation ────────────────────────────────────
        Console.WriteLine("❌ BEFORE — PaymentService does everything (5 reasons to change):\n");
        var badService = new PaymentService_Bad();
        var result = badService.ProcessPayment(request);
        badService.SendReceiptEmail(request.Email, result);   // marketing's code lives here
        badService.GeneratePdfInvoice(result);                 // finance's code lives here
        badService.IsFraudulent(request);                     // security's code lives here
        Console.WriteLine("\n   ^ 5 teams own one file. Merge conflicts every sprint.");
        Console.WriteLine("   ^ Marketing deploys an email change. Payment logic redeploys. Breaks.\n");

        // ── AFTER: showing the fix ────────────────────────────────────────────
        Console.WriteLine("✅ AFTER — Five focused classes, one orchestrator:\n");

        var orchestrator = new PaymentOrchestrator(
            paymentService:   new PaymentService(),
            fraudDetector:    new RuleBasedFraudDetector(),
            emailSender:      new SmtpReceiptEmailSender(),
            invoiceGenerator: new PdfInvoiceGenerator(),
            logger:           new SplunkPaymentLogger()
        );

        var finalResult = orchestrator.Execute(request);
        Console.WriteLine($"\n   Result: {finalResult.Message}");
        Console.WriteLine("\n   Each class: one job, one team, one reason to change.");
        Console.WriteLine("   Email template changes? Only SmtpReceiptEmailSender is touched.");
        Console.WriteLine("   Fraud rules change?     Only RuleBasedFraudDetector is touched.");
        Console.WriteLine("   PaymentService is untouched by either change.\n");

        Console.WriteLine("✅ Single Responsibility — understood.");
        Console.WriteLine("→ This creates the need for Open/Closed Principle (File 02).");
        Console.WriteLine("  PaymentOrchestrator now exists. What happens when a new payment STEP is added?");
    }
}
