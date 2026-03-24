/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🧠 CONCEPT: Composition Over Inheritance                                    ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Instead of inheriting behaviour from a parent class (IS-A), you build a     ║
 * ║  class by holding references to other objects that provide that behaviour    ║
 * ║  (HAS-A). Prefer composition over inheritance when the relationship isn't    ║
 * ║  truly "IS-A" — it keeps classes flexible and independently swappable.       ║
 * ║                                                                              ║
 * ║  MENTAL MODEL (the analogy to remember forever):                             ║
 * ║  A car HAS-A engine. It doesn't IS-A engine. If you modelled it as           ║
 * ║  Car extends Engine, your Car would inherit StartEngine(), StopEngine(),      ║
 * ║  SetFuelInjection()... none of which a car should expose directly. Worse,    ║
 * ║  to swap diesel → electric, you'd have to change the class hierarchy itself. ║
 * ║  With composition, you just replace the engine object in the constructor.    ║
 * ║                                                                              ║
 * ║  THE PRODUCTION HORROR STORY (why this matters):                             ║
 * ║  A team made OrderService extend AuditLogger to get logging behaviour. Six   ║
 * ║  months later, their public API exposed a Log() endpoint because OrderService ║
 * ║  inherited it. An external client called it and started writing garbage into ║
 * ║  the audit trail. Fixing it required a breaking API change and a customer    ║
 * ║  migration notice. A private _logger field would have prevented this entirely.║
 * ║                                                                              ║
 * ║  QUICK RECALL (read this in 2 years and instantly remember):                 ║
 * ║  Can you say "[Child] IS-A [Parent]" and mean it? If not — use composition.  ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.PaymentModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — How a beginner naturally writes this
// "PaymentService needs logging. Logger has logging. Let's just extend it!"
// This feels elegant — one keyword (extends) gives you everything.
// But PaymentService IS NOT A Logger. This is the smell.
// ─────────────────────────────────────────────────────────────────────────────

// ❌ The logger — a standalone utility class
public class FileLogger
{
    public void Log(string message)            => Console.WriteLine($"   [FILE-LOG] {message}");
    public void LogError(string message)       => Console.WriteLine($"   [FILE-ERR] {message}");
    public void LogWarning(string message)     => Console.WriteLine($"   [FILE-WARN] {message}");
    public void Flush()                        => Console.WriteLine("   [FILE-LOG] Flushing buffer...");
    public void SetLevel(string level)         => Console.WriteLine($"   [FILE-LOG] Level set to {level}");
    public void RotateLogFile()                => Console.WriteLine("   [FILE-LOG] Rotating log file...");
    // ... imagine 9 more logger methods
}

// ❌ PaymentService EXTENDS FileLogger to get logging for free
// Problem 1: PaymentService IS-NOT-A FileLogger. It's a service that processes payments.
// Problem 2: PaymentService now publicly exposes Log(), Flush(), RotateLogFile()...
// Problem 3: To swap to CloudLogger, you change the class declaration line — a breaking change.
public class BadPaymentService_WithInheritance : FileLogger
{
    // ❌ Inherits Log(), LogError(), Flush(), RotateLogFile()... ALL public!
    //    Any caller can do: paymentService.RotateLogFile() — that makes no sense.

    public void ProcessPayment(decimal amount, string token)
    {
        Log($"Processing payment of ₹{amount}");     // ← using inherited Log()
        // ... payment logic ...
        Log($"Payment completed for token {token}");
    }
}

// 💥 WHAT GOES WRONG:
//
//   var svc = new BadPaymentService_WithInheritance();
//   svc.ProcessPayment(500m, "tok_123");   // fine
//   svc.RotateLogFile();                   // ← EXPOSED! External code can call this.
//   svc.SetLevel("DEBUG");                 // ← EXPOSED! Callers can reconfigure logging.
//   svc.Flush();                           // ← EXPOSED! Makes no sense on a payment service.
//
// Also: want CloudLogger instead of FileLogger?
//   Change: public class BadPaymentService : FileLogger  →  : CloudLogger
//   That's a source-code change, a re-compile, a re-deploy. And if BadPaymentService
//   is already subclassed somewhere, you've just broken that hierarchy too.


// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — The OOP-correct way
// PaymentService HAS-A logger (injected). Logger and FraudChecker are swappable.
// No logger methods bleed onto PaymentService's public interface.
// ─────────────────────────────────────────────────────────────────────────────

// ✅ Logger abstraction — PaymentService depends on this, not a concrete class
public interface ILogger
{
    void Log(string message);
    void LogError(string message);
}

// ✅ FraudChecker abstraction — another collaborator injected via composition
public interface IFraudChecker
{
    bool IsSuspicious(decimal amount, string token);
}

// ✅ Concrete implementations — completely independent, swappable
public class ConsoleLogger : ILogger
{
    public void Log(string message)      => Console.WriteLine($"   [LOG] {message}");
    public void LogError(string message) => Console.WriteLine($"   [ERR] {message}");
}

public class CloudLogger : ILogger
{
    public void Log(string message)      => Console.WriteLine($"   [CLOUD-LOG] {message}");
    public void LogError(string message) => Console.WriteLine($"   [CLOUD-ERR] {message}");
}

public class BasicFraudChecker : IFraudChecker
{
    // Simple rule: flag anything over ₹50,000 as suspicious
    public bool IsSuspicious(decimal amount, string token) => amount > 50_000m;
}

// ✅ PaymentService HAS-A logger and HAS-A fraud checker.
//    Its public surface: ProcessPayment(). Nothing else.
//    Log() is private — callers have no idea logging is happening inside.
public class GoodPaymentService
{
    // ✅ Private — logger is an internal detail, invisible to callers
    private readonly ILogger _logger;
    private readonly IFraudChecker _fraudChecker;

    // ✅ Dependencies injected from outside — caller decides which implementations to use
    public GoodPaymentService(ILogger logger, IFraudChecker fraudChecker)
    {
        _logger       = logger;
        _fraudChecker = fraudChecker;
    }

    public bool ProcessPayment(decimal amount, string token)
    {
        _logger.Log($"Processing payment of ₹{amount} for token {token}");

        if (_fraudChecker.IsSuspicious(amount, token))
        {
            _logger.LogError($"Fraud check flagged ₹{amount} — aborting.");
            return false;
        }

        // ... payment gateway call would go here ...
        _logger.Log($"Payment ₹{amount} completed successfully.");
        return true;
    }

    // ✅ GoodPaymentService's public API has ONLY what a payment service should expose.
    //    No Log(), no Flush(), no RotateLogFile(). None of that leaked out.
}


// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class CompositionDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  COMPOSITION VS INHERITANCE DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        Console.WriteLine("❌ BEFORE — PaymentService extends FileLogger:");
        var bad = new BadPaymentService_WithInheritance();
        bad.ProcessPayment(500m, "tok_abc");
        // These are all valid calls because Log/Flush are public via inheritance:
        bad.Flush();              // ← should this exist on a payment service? No.
        bad.RotateLogFile();      // ← definitely not.
        Console.WriteLine("   ^ Callers can call Flush() and RotateLogFile() on a PaymentService. 😬\n");

        Console.WriteLine("✅ AFTER — Composition with ILogger injection:");
        Console.WriteLine("   Using ConsoleLogger:\n");
        var svc1 = new GoodPaymentService(new ConsoleLogger(), new BasicFraudChecker());
        svc1.ProcessPayment(1500m, "tok_visa");

        Console.WriteLine("\n   Swapping to CloudLogger — GoodPaymentService code unchanged:\n");
        // ✅ Swap logger in one line. No class hierarchy changes. No recompile of PaymentService.
        var svc2 = new GoodPaymentService(new CloudLogger(), new BasicFraudChecker());
        svc2.ProcessPayment(1500m, "tok_visa");

        Console.WriteLine("\n   Testing fraud detection (amount > ₹50,000):\n");
        var svc3 = new GoodPaymentService(new ConsoleLogger(), new BasicFraudChecker());
        svc3.ProcessPayment(75_000m, "tok_suspicious");

        Console.WriteLine();
        Console.WriteLine("   Rule of thumb:");
        Console.WriteLine("   'PaymentService IS-A Logger' → False → Use composition.");
        Console.WriteLine("   'CreditCardPayment IS-A Payment' → True → Use inheritance.");

        Console.WriteLine("\n✅ CompositionVsInheritance — understood.");
    }
}
