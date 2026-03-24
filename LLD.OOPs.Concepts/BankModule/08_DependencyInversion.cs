/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏦 CONCEPT: Dependency Inversion                                            ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 07_InterfaceSegregation.cs                                    ║
 * ║     File 7 showed how to split fat interfaces into focused ones.            ║
 * ║     File 6 showed composition: inject interfaces, not concrete classes.     ║
 * ║     Dependency Inversion is the architectural principle that FORMALISES     ║
 * ║     this: high-level modules should depend on abstractions, not on the      ║
 * ║     concrete implementations that do the actual work.                       ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  "High-level modules should not depend on low-level modules.                ║
 * ║   Both should depend on abstractions." — Robert C. Martin (SOLID's D)      ║
 * ║  PaymentProcessor is high-level (business logic). SqlRepository is          ║
 * ║  low-level (infrastructure). PaymentProcessor should only know about        ║
 * ║  ITransactionRepository — never about SqlRepository specifically.           ║
 * ║  This lets you swap SQL for Cosmos DB, or a FakeRepository for tests,      ║
 * ║  without touching a single line of PaymentProcessor.                        ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  NEFT/RTGS payment rails. Banks don't build their own wire-transfer         ║
 * ║  infrastructure. They depend on the RBI NEFT interface (abstraction).       ║
 * ║  Whether the underlying infrastructure is batch processing or RTGS          ║
 * ║  real-time — the bank's payment code doesn't change. The interface never    ║
 * ║  changes. The implementation underneath does. That's dependency inversion. ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A fintech's PaymentService had:                                             ║
 * ║    private StripeClient _stripe = new StripeClient(apiKey: "live_sk_...");  ║
 * ║    private SqlDatabase  _db     = new SqlDatabase("Server=prod-sql-01..."); ║
 * ║  The team wanted to write a unit test for the refund logic. To run the      ║
 * ║  test, they needed a real Stripe account, a real production database, and  ║
 * ║  a VPN into the prod network. A developer spent 3 days trying to mock the  ║
 * ║  SqlDatabase constructor. Eventually the test was abandoned. The refund     ║
 * ║  logic shipped with zero unit tests. Prod had a bug for 11 months.         ║
 * ║                                                                              ║
 * ║  QUICK RECALL (your 2-year cheat code):                                     ║
 * ║  This is the D in SOLID. ASP.NET Core's builder.Services.AddScoped() does  ║
 * ║  exactly this injection automatically. You've been using DIP without        ║
 * ║  knowing it every time you used dependency injection in .NET.               ║
 * ║                                                                              ║
 * ║  HOW THIS CONNECTS TO WHAT'S NEXT:                                          ║
 * ║  You now have all 8 OOP fundamentals. Phase 2: SOLID principles in depth.  ║
 * ║  Phase 3: Design patterns (Strategy, Factory, Observer) that apply all of  ║
 * ║  these. Phase 4: Full LLD problems (Parking Lot, BookMyShow, UPI System).  ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.BankModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Hard-coded concrete dependencies inside the class
// "Why accept an interface when I know exactly which database and gateway I need?"
// Feels simpler. Until you try to test without prod infrastructure.
// ─────────────────────────────────────────────────────────────────────────────

// Simulated concrete dependencies — in real life these have real constructors
// that require connection strings, API keys, and network access.
public class StripePaymentGateway
{
    public string Charge(decimal amount, string token)
    {
        Console.WriteLine($"   [STRIPE] Charging ₹{amount:N0} with token {token}...");
        return $"stripe_ch_{Guid.NewGuid():N}";  // simulated charge ID
    }
}

public class SqlTransactionRepo
{
    public void Save(string transactionId, decimal amount, string status) =>
        Console.WriteLine($"   [SQL] INSERT INTO transactions VALUES ('{transactionId}', {amount}, '{status}')");
}

/// <summary>
/// ❌ NaivePaymentProcessor creates its own dependencies with 'new'.
/// To test Refund(): need a real Stripe account and a live SQL database.
/// To swap to PayPal: edit this class's internals.
/// To swap to MongoDB: edit this class's internals.
/// Every infrastructure change requires touching business logic. Always.
/// </summary>
public class NaivePaymentProcessor
{
    // ← 'new' here means this class owns the dependency forever.
    //   It cannot be swapped. It cannot be mocked. It cannot be tested in isolation.
    private readonly StripePaymentGateway _gateway = new();      // ← hard dependency
    private readonly SqlTransactionRepo   _repo    = new();      // ← hard dependency

    public void ProcessPayment(decimal amount, string cardToken)
    {
        var chargeId = _gateway.Charge(amount, cardToken);
        _repo.Save(chargeId, amount, "Completed");
        Console.WriteLine($"   [NAIVE] Payment processed. Charge: {chargeId}");
    }

    // 💥 To test this method:
    // — You need a real Stripe API key (production or test)
    // — You need a SQL Server running and accessible
    // — You cannot run this test offline
    // — You cannot test what happens when Stripe returns an error
    //   (you'd have to cause a real Stripe failure)
    // — Zero unit tests exist for this class. 11 months of undetected bugs.
}

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — Depend on abstractions. Inject concrete classes from outside.
// GoodPaymentProcessor doesn't know if it's talking to Stripe, PayPal, or a fake.
// Swap gateway in one line. Test with a fake that returns hardcoded responses.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Payment gateway abstraction. GoodPaymentProcessor only knows about this.
/// Whether the concrete implementation calls Stripe, PayPal, or returns fake data
/// is completely invisible to GoodPaymentProcessor.
/// </summary>
public interface IBankPaymentGateway
{
    /// <returns>Charge ID if successful, null if gateway rejected the payment.</returns>
    string? Charge(decimal amount, string cardToken);

    /// <returns>true if refund succeeded.</returns>
    bool Refund(string chargeId, decimal amount);
}

/// <summary>
/// Transaction persistence abstraction. Could be SQL, MongoDB, Cosmos DB, or in-memory.
/// GoodPaymentProcessor never knows which.
/// </summary>
public interface IBankTransactionRepository
{
    void   SaveTransaction(string transactionId, decimal amount, string status);
    string GetStatus(string transactionId);
}

// ── Concrete implementations — swappable via the constructor ─────────────────

/// <summary>Production Stripe implementation.</summary>
public class StripeBankGateway : IBankPaymentGateway
{
    public string? Charge(decimal amount, string cardToken)
    {
        Console.WriteLine($"   [STRIPE] Charging ₹{amount:N0} for token {cardToken}...");
        return $"stripe_ch_{Guid.NewGuid():N}";
    }

    public bool Refund(string chargeId, decimal amount)
    {
        Console.WriteLine($"   [STRIPE] Refunding ₹{amount:N0} for charge {chargeId}...");
        return true;
    }
}

/// <summary>Production PayPal implementation. GoodPaymentProcessor never changes to support this.</summary>
public class PayPalBankGateway : IBankPaymentGateway
{
    public string? Charge(decimal amount, string cardToken)
    {
        Console.WriteLine($"   [PAYPAL] Authorising ₹{amount:N0} with order token {cardToken}...");
        return $"paypal_ord_{Guid.NewGuid():N}";
    }

    public bool Refund(string chargeId, decimal amount)
    {
        Console.WriteLine($"   [PAYPAL] Issuing refund ₹{amount:N0} for order {chargeId}...");
        return true;
    }
}

/// <summary>
/// Production SQL repository.
/// </summary>
public class SqlBankTransactionRepository : IBankTransactionRepository
{
    private readonly Dictionary<string, (decimal Amount, string Status)> _store = [];

    public void SaveTransaction(string txId, decimal amount, string status)
    {
        _store[txId] = (amount, status);
        Console.WriteLine($"   [SQL] Saved transaction {txId} — ₹{amount:N0} — {status}.");
    }

    public string GetStatus(string txId) =>
        _store.TryGetValue(txId, out var t) ? t.Status : "Not Found";
}

/// <summary>
/// Fake gateway for unit tests. Returns hardcoded responses.
/// No network. No API keys. Runs in microseconds.
/// This is what makes testing business logic cheap and fast.
/// </summary>
public class FakeBankGateway : IBankPaymentGateway
{
    private readonly bool _shouldSucceed;

    /// <param name="shouldSucceed">Set to false to test payment failure paths.</param>
    public FakeBankGateway(bool shouldSucceed = true) => _shouldSucceed = shouldSucceed;

    public string? Charge(decimal amount, string cardToken)
    {
        Console.WriteLine($"   [FAKE] Charge called: ₹{amount:N0}, token={cardToken}. Returning {(_shouldSucceed ? "success" : "failure")}.");
        return _shouldSucceed ? $"fake_ch_{Guid.NewGuid():N}" : null;
    }

    public bool Refund(string chargeId, decimal amount)
    {
        Console.WriteLine($"   [FAKE] Refund called: ₹{amount:N0}. Returning {_shouldSucceed}.");
        return _shouldSucceed;
    }
}

/// <summary>In-memory repository for tests — no real database needed.</summary>
public class InMemoryTransactionRepository : IBankTransactionRepository
{
    private readonly Dictionary<string, (decimal Amount, string Status)> _store = [];

    public void SaveTransaction(string txId, decimal amount, string status)
    {
        _store[txId] = (amount, status);
        Console.WriteLine($"   [IN-MEM] Saved transaction {txId}.");
    }

    public string GetStatus(string txId) =>
        _store.TryGetValue(txId, out var t) ? t.Status : "Not Found";
}

/// <summary>
/// The clean, testable payment processor.
/// Depends on abstractions — knows nothing about Stripe, SQL, or PayPal specifically.
/// To test: pass FakeBankGateway + InMemoryTransactionRepository. Zero real infrastructure.
/// To go live: pass StripeBankGateway + SqlBankTransactionRepository. Zero code changes here.
///
/// ASP.NET Core equivalent:
///   builder.Services.AddScoped&lt;IBankPaymentGateway, StripeBankGateway&gt;();
///   builder.Services.AddScoped&lt;IBankTransactionRepository, SqlBankTransactionRepository&gt;();
///   // → The framework auto-injects these into GoodPaymentProcessor's constructor.
///   // → You've been using Dependency Inversion every time you used .NET DI.
/// </summary>
public class GoodPaymentProcessor
{
    private readonly IBankPaymentGateway      _gateway;
    private readonly IBankTransactionRepository _repo;

    // ← Dependencies injected from OUTSIDE. This class never calls 'new' on them.
    //   Who creates StripeBankGateway? The caller. Or the DI container. Not us.
    public GoodPaymentProcessor(IBankPaymentGateway gateway, IBankTransactionRepository repo)
    {
        _gateway = gateway;
        _repo    = repo;
    }

    public string? ProcessPayment(decimal amount, string cardToken)
    {
        var chargeId = _gateway.Charge(amount, cardToken);

        if (chargeId is null)
        {
            Console.WriteLine("   Payment failed at gateway.");
            return null;
        }

        _repo.SaveTransaction(chargeId, amount, "Completed");
        Console.WriteLine($"   Payment successful. Transaction: {chargeId[..12]}...");
        return chargeId;
    }

    public bool ProcessRefund(string chargeId, decimal amount)
    {
        var success = _gateway.Refund(chargeId, amount);
        var status  = success ? "Refunded" : "RefundFailed";
        _repo.SaveTransaction($"REF-{chargeId}", amount, status);
        return success;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class DependencyInversionDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  DEPENDENCY INVERSION DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — Hard-coded dependencies with 'new' inside the class:");
        var naive = new NaivePaymentProcessor();
        naive.ProcessPayment(5_000m, "tok_visa_4242");
        Console.WriteLine("   ↑ Works. But try testing this without Stripe API key + SQL Server.\n");

        // ── AFTER ── Production wiring ──────────────────────────────────────
        Console.WriteLine("✅ AFTER — Production: Stripe + SQL, wired from OUTSIDE:");
        var prodProcessor = new GoodPaymentProcessor(
            gateway: new StripeBankGateway(),
            repo:    new SqlBankTransactionRepository()
        );
        var chargeId = prodProcessor.ProcessPayment(15_000m, "tok_visa_4242");

        Console.WriteLine("\n   Swap to PayPal — GoodPaymentProcessor code unchanged:");
        var paypalProcessor = new GoodPaymentProcessor(
            gateway: new PayPalBankGateway(),       // ← only this line changes
            repo:    new SqlBankTransactionRepository()
        );
        paypalProcessor.ProcessPayment(15_000m, "paypal_token_abc");

        // ── AFTER ── Test scenarios — no real infrastructure ─────────────────
        Console.WriteLine("\n✅ AFTER — Unit test scenario (no real API, no real database):\n");

        Console.WriteLine("   Test 1: successful payment");
        var testProcessor = new GoodPaymentProcessor(
            gateway: new FakeBankGateway(shouldSucceed: true),   // ← fake, returns hardcoded
            repo:    new InMemoryTransactionRepository()          // ← in-memory, no SQL
        );
        testProcessor.ProcessPayment(7_500m, "tok_test");

        Console.WriteLine("\n   Test 2: gateway failure — testing the failure code path");
        var failProcessor = new GoodPaymentProcessor(
            gateway: new FakeBankGateway(shouldSucceed: false),  // ← simulates Stripe error
            repo:    new InMemoryTransactionRepository()
        );
        failProcessor.ProcessPayment(7_500m, "tok_test");

        Console.WriteLine("\n   ─────────────────────────────────────────────────────");
        Console.WriteLine("   ASP.NET Core does this automatically:");
        Console.WriteLine("     builder.Services.AddScoped<IBankPaymentGateway, StripeBankGateway>();");
        Console.WriteLine("     builder.Services.AddScoped<IBankTransactionRepository, SqlBankTransactionRepository>();");
        Console.WriteLine("     // → Framework injects these into GoodPaymentProcessor constructor.");
        Console.WriteLine("     // → You have been using Dependency Inversion every time you");
        Console.WriteLine("     //   registered services in Program.cs. Now you know why it works.");
        Console.WriteLine("   ─────────────────────────────────────────────────────");

        Console.WriteLine("\n✅ DependencyInversion — understood.");
    }
}
