/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏦 CONCEPT: Composition Over Inheritance                                    ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 05_AbstractClass_vs_Interface.cs                              ║
 * ║     File 5 showed that interfaces model capabilities. Composition is how   ║
 * ║     you actually USE those capabilities: instead of inheriting them, you    ║
 * ║     inject them. The class HAS-A logger; it IS-NOT-A logger.                ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Composition means building a class by holding references to other objects  ║
 * ║  (HAS-A), rather than inheriting from them (IS-A). You want logging?       ║
 * ║  Take an ILogger in the constructor. You want fraud checking? Take an       ║
 * ║  IFraudDetector. Each capability is a private dependency — not a parent.   ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  A bank branch office. It HAS-A security guard, HAS-A ATM machine, HAS-A  ║
 * ║  safe. It IS-NOT-A security guard. If you inherited from SecurityGuard,    ║
 * ║  the branch would expose PatrolBuilding() and ArrestIntruder() on its       ║
 * ║  public API. Worse, to upgrade from one security agency to another, you'd  ║
 * ║  need to change the branch's parent class. Composition just swaps the      ║
 * ║  guard object in the constructor. One line.                                  ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A banking backend team made BankTransactionService extend AuditLogger.     ║
 * ║  A year later the public REST API was generated from the class's public     ║
 * ║  methods. AuditLogger had a public FlushBuffer() method. It showed up as   ║
 * ║  POST /api/transactions/flush-buffer — visible to all API clients. A        ║
 * ║  penetration test found an external attacker could call it and corrupt the ║
 * ║  audit trail mid-transaction. Removing it was a breaking API change.        ║
 * ║  The fix took 3 months of migration coordination. A private _logger field  ║
 * ║  would have prevented this entirely.                                         ║
 * ║                                                                              ║
 * ║  QUICK RECALL (your 2-year cheat code):                                     ║
 * ║  "Favour composition over inheritance." — Gang of Four, 1994.               ║
 * ║  If you cannot say "[Child] IS-A [Parent]" with confidence, compose instead.║
 * ║                                                                              ║
 * ║  HOW THIS CONNECTS TO THE NEXT CONCEPT:                                     ║
 * ║  Composition works through injected interfaces. File 7 shows what happens  ║
 * ║  when those interfaces are too FAT — and how to fix them.                  ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.BankModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Inheritance abuse
// "TransactionService needs logging — let's just inherit from AuditLogger."
// One keyword gives you everything. Until it gives you TOO much.
// ─────────────────────────────────────────────────────────────────────────────

public class BankAuditLogger
{
    // ← A proper logger has many utility methods. Inheriting this class
    //   dumps ALL of them onto the public surface of whatever inherits it.
    public void Log(string message)       => Console.WriteLine($"   [FILE-LOG] {message}");
    public void LogError(string message)  => Console.WriteLine($"   [FILE-ERR] {message}");
    public void FlushBuffer()             => Console.WriteLine("   [FILE-LOG] Buffer flushed.");
    public void SetLogLevel(string level) => Console.WriteLine($"   [FILE-LOG] Log level: {level}");
    public void ArchiveLogs(string path)  => Console.WriteLine($"   [FILE-LOG] Archived to: {path}");
    // ... imagine 10 more public logger methods
}

// ❌ BankTransactionService extends a logger to get logging ability.
// "BankTransactionService IS-A AuditLogger" — does that sentence make sense? No.
public class NaiveBankTransactionService : BankAuditLogger
{
    public void ProcessTransfer(string fromId, string toId, decimal amount)
    {
        Log($"Transfer starting: {fromId} → {toId}, ₹{amount:N0}");  // ← inherited
        Console.WriteLine($"   [TRANSFER] ₹{amount:N0} from {fromId} to {toId}.");
        Log($"Transfer complete.");   // ← inherited
    }

    // 💥 WHAT BREAKS:
    // — Callers can now do: service.FlushBuffer()  ← exposes logger internals publicly
    // — Callers can now do: service.ArchiveLogs()  ← audit trail manipulation by anyone
    // — Callers can now do: service.SetLogLevel()  ← change logging behaviour from outside
    // — To swap from FileLogger to CloudLogger: must CHANGE the base class.
    //   That's a class hierarchy change — affects every test, every mock, everything.
    // — Unit testing ProcessTransfer() requires a real file system for logging.
}

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — Composition via constructor injection
// BankService HAS-A logger, HAS-A fraud detector, HAS-A notifier.
// All three are private. None are exposed publicly. Swap any one with one line.
// ─────────────────────────────────────────────────────────────────────────────

// ── Capability interfaces — the "badges" from File 5 ─────────────────────────

/// <summary>Audit logging capability. Any class that logs implements this.</summary>
public interface IBankAuditLogger
{
    void Log(string message);
    void LogError(string message);
}

/// <summary>Fraud detection capability. Injectable and swappable.</summary>
public interface IBankFraudDetector
{
    /// <returns>Risk score 0–100. Above 70 = block transaction.</returns>
    int GetRiskScore(string customerId, decimal amount);
}

/// <summary>Customer notification capability. SMS, Email, Push — all same interface.</summary>
public interface IBankNotifier
{
    void Notify(string customerId, string message);
}

// ── Three concrete IAuditLogger implementations — swap by changing one constructor argument ──

/// <summary>Development environment: logs to console, no persistence.</summary>
public class ConsoleBankAuditLogger : IBankAuditLogger
{
    public void Log(string msg)      => Console.WriteLine($"   [LOG]   {msg}");
    public void LogError(string msg) => Console.WriteLine($"   [ERR]   {msg}");
}

/// <summary>Production: logs to database. FlushBuffer(), ArchiveLogs() are PRIVATE — not on interface.</summary>
public class DatabaseAuditLogger : IBankAuditLogger
{
    public void Log(string msg)      => Console.WriteLine($"   [DB-LOG] {msg}");
    public void LogError(string msg) => Console.WriteLine($"   [DB-ERR] {msg}");

    // ← FlushBuffer() exists here but is NOT on IBankAuditLogger.
    //   It is PRIVATE to this class. Callers of GoodBankTransactionService
    //   cannot call it because they only hold IBankAuditLogger.
    private void FlushBuffer() { /* flushes to DB */ }
}

/// <summary>Unit tests: does absolutely nothing. No file system, no DB, no side effects.</summary>
public class NullAuditLogger : IBankAuditLogger
{
    public void Log(string msg)      { /* intentional no-op for tests */ }
    public void LogError(string msg) { /* intentional no-op for tests */ }
}

/// <summary>Default fraud detector: blocks anything over ₹5,00,000 from unknown customers.</summary>
public class BasicFraudDetector : IBankFraudDetector
{
    public int GetRiskScore(string customerId, decimal amount)
    {
        // Simulate: high-value transactions from unknown customers = elevated risk
        if (amount > 5_00_000m)
        {
            Console.WriteLine($"   [FRAUD] High-value alert: ₹{amount:N0} by {customerId}.");
            return 85;  // blocks
        }
        return 20;  // low risk
    }
}

/// <summary>SMS notifications for production.</summary>
public class SmsNotifier : IBankNotifier
{
    public void Notify(string customerId, string message) =>
        Console.WriteLine($"   [SMS] → {customerId}: {message}");
}

// ── The clean service — only what it needs, nothing extra ─────────────────────

/// <summary>
/// Bank transaction service. Composes three capabilities via constructor injection.
/// None of them are exposed publicly. None of them change this class's identity.
/// "GoodBankTransactionService IS-A AuditLogger?" — No.
/// "GoodBankTransactionService HAS-A IAuditLogger?" — Yes. That's composition.
/// </summary>
public class GoodBankTransactionService
{
    // ← All three are private. They are invisible to callers.
    //   No FlushBuffer(), no SetLogLevel(), no ArchiveLogs() leaking out.
    private readonly IBankAuditLogger   _logger;
    private readonly IBankFraudDetector _fraud;
    private readonly IBankNotifier      _notifier;

    public GoodBankTransactionService(
        IBankAuditLogger   logger,
        IBankFraudDetector fraud,
        IBankNotifier      notifier)
    {
        _logger   = logger;
        _fraud    = fraud;
        _notifier = notifier;
    }

    public void ProcessTransfer(string fromId, string toId, decimal amount, string customerId)
    {
        _logger.Log($"Transfer initiated: {fromId} → {toId}, ₹{amount:N0}.");

        int riskScore = _fraud.GetRiskScore(customerId, amount);
        if (riskScore > 70)
        {
            _logger.LogError($"Transfer BLOCKED. Fraud risk score: {riskScore}.");
            Console.WriteLine($"   [TRANSFER] Blocked: fraud risk too high ({riskScore}/100).");
            return;
        }

        // Simulate balance transfer
        Console.WriteLine($"   [TRANSFER] ₹{amount:N0} from {fromId} → {toId} completed.");
        _logger.Log($"Transfer complete: {fromId} → {toId}, ₹{amount:N0}.");
        _notifier.Notify(customerId, $"₹{amount:N0} transferred to {toId}. Reference: TXN{DateTime.Now.Ticks % 100000:D5}.");
    }
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

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — Inheriting from AuditLogger:");
        var naive = new NaiveBankTransactionService();
        naive.ProcessTransfer("ACC001", "ACC002", 10_000m);

        Console.WriteLine("\n   What callers can do to NaiveBankTransactionService:");
        naive.FlushBuffer();     // ← AuditLogger internals now public on a transaction service
        naive.ArchiveLogs("/var/log/bank/");  // ← external callers can manipulate audit trail
        Console.WriteLine("   ^ These methods make no sense on a transaction service.");
        Console.WriteLine("   A bank teller should not be able to call FlushBuffer().\n");

        // ── AFTER ── Production wiring ──────────────────────────────────────
        Console.WriteLine("✅ AFTER — Composition (production setup):");
        var prodService = new GoodBankTransactionService(
            logger:   new DatabaseAuditLogger(),
            fraud:    new BasicFraudDetector(),
            notifier: new SmsNotifier()
        );
        prodService.ProcessTransfer("ACC001", "ACC002", 50_000m, "CUST001");

        Console.WriteLine("\n   Swapping to console logger — ONE line change:\n");
        var devService = new GoodBankTransactionService(
            logger:   new ConsoleBankAuditLogger(),  // ← only this line changed
            fraud:    new BasicFraudDetector(),
            notifier: new SmsNotifier()
        );
        devService.ProcessTransfer("ACC003", "ACC004", 25_000m, "CUST002");

        Console.WriteLine("\n   High-value transfer — fraud detector blocks it:");
        devService.ProcessTransfer("ACC005", "ACC006", 8_00_000m, "CUST003");

        Console.WriteLine("\n   Unit test setup — NullAuditLogger, no real infrastructure:");
        var testService = new GoodBankTransactionService(
            logger:   new NullAuditLogger(),  // ← no logs, no file system, no DB
            fraud:    new BasicFraudDetector(),
            notifier: new SmsNotifier()
        );
        testService.ProcessTransfer("TEST001", "TEST002", 1_000m, "TEST-CUST");
        Console.WriteLine("   ^ Test ran with no real logging infrastructure.");

        Console.WriteLine("\n   The rule:");
        Console.WriteLine("   'GoodBankTransactionService IS-A AuditLogger' → False → Compose.");
        Console.WriteLine("   'SavingsAccount IS-A AccountBase'             → True  → Inherit.");

        Console.WriteLine("\n✅ CompositionVsInheritance — understood.");
    }
}
