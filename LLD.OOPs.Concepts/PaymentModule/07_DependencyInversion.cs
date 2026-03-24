/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🧠 CONCEPT: Dependency Inversion Principle (DIP)                            ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  High-level modules (business logic) should not depend on low-level modules  ║
 * ║  (database, HTTP clients, external APIs). Both should depend on abstractions ║
 * ║  (interfaces). This way, you can swap out low-level details without touching ║
 * ║  a single line of business logic.                                            ║
 * ║                                                                              ║
 * ║  MENTAL MODEL (the analogy to remember forever):                             ║
 * ║  A power socket and a plug. Your laptop doesn't hard-wire into the building's ║
 * ║  electrical circuit. It plugs into a standard socket (the interface). The    ║
 * ║  building can switch from coal power to solar — your laptop doesn't care.    ║
 * ║  The socket is the abstraction both sides agree on.                          ║
 * ║                                                                              ║
 * ║  THE PRODUCTION HORROR STORY (why this matters):                             ║
 * ║  A startup built their entire checkout service with hard-coded Stripe and     ║
 * ║  SQL Server dependencies inside business logic. When they were acquired, the ║
 * ║  acquirer required PayPal and PostgreSQL. The rewrite took 6 weeks instead  ║
 * ║  of 2 days — every business rule was tangled with SQL queries and Stripe     ║
 * ║  objects. With DIP, swapping implementations is a one-line constructor call. ║
 * ║                                                                              ║
 * ║  QUICK RECALL (read this in 2 years and instantly remember):                 ║
 * ║  This is the D in SOLID. Depend on interfaces, not concrete classes.          ║
 * ║  ASP.NET Core's builder.Services.AddScoped<IService, ConcreteService>()      ║
 * ║  is DIP in action — you've been using it without knowing.                    ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 *
 *  NOTE: This is the D in SOLID — the full SOLID suite lives in Phase 2.
 *  ASP.NET Core's DI container (builder.Services.AddScoped / AddSingleton)
 *  automates this injection at startup. You've been using DIP without knowing it.
 */

namespace LLD.OOPs.Concepts.PaymentModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — How a beginner naturally writes this
// Dependencies created with 'new' inside the class.
// Feels clean and self-contained — the class provides everything it needs.
// The cost: you cannot test it, swap it, or extend it without opening this file.
// ─────────────────────────────────────────────────────────────────────────────

// ❌ The concrete dependencies — their internals don't matter for this lesson
public class StripeGateway_V2
{
    public bool Charge(decimal amount, string token)
    {
        Console.WriteLine($"   [Stripe] Charging ₹{amount} for token {token}");
        return true; // pretend it always succeeds
    }
}

public class SqlPaymentRepository
{
    public void Save(string transactionId, decimal amount, string status)
    {
        // ❌ Requires a real SQL Server connection string to even instantiate
        Console.WriteLine($"   [SQL] Saving tx={transactionId} amount={amount} status={status}");
    }
}

// ❌ PaymentService creates its dependencies with 'new' inside itself
public class BadPaymentService_HardCoded
{
    // ❌ Hard-coded concrete types — these two lines are the problem
    private readonly StripeGateway_V2     _gateway = new StripeGateway_V2();
    private readonly SqlPaymentRepository _repo    = new SqlPaymentRepository();

    public bool ProcessPayment(decimal amount, string token)
    {
        var success = _gateway.Charge(amount, token);
        if (success)
        {
            var txId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            _repo.Save(txId, amount, "Completed");
            return true;
        }
        return false;
    }
}

// 💥 WHAT GOES WRONG:
//
// 1. TESTING IS IMPOSSIBLE WITHOUT REAL INFRASTRUCTURE:
//    var svc = new BadPaymentService_HardCoded();
//    svc.ProcessPayment(100m, "test_token");
//    ↑ This hits the real Stripe API. You need a live API key.
//    ↑ This hits real SQL Server. You need a running database.
//    ↑ Unit tests become integration tests. CI pipeline requires infra. Slow, flaky, expensive.
//
// 2. SWAPPING IS A SURGERY:
//    Want PayPal instead of Stripe? Open this class, find the field, change StripeGateway_V2
//    to PayPalGateway_V2. That's a change to business logic to swap infrastructure.
//    Every class that uses PaymentService now needs re-testing.
//
// 3. IF STRIPE IS DOWN, YOUR WHOLE TEST SUITE FAILS:
//    CI runs at midnight. Stripe has a brief outage. All tests fail.
//    The failure isn't in your code — but you have no way to isolate it.


// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — The OOP-correct way
// Depend on interfaces, not concrete classes.
// Inject dependencies from the outside (constructor injection).
// PaymentService has ZERO knowledge of Stripe, SQL, or any infrastructure.
// ─────────────────────────────────────────────────────────────────────────────

// ✅ The contracts — business logic depends on these, not on Stripe or SQL
public interface IPaymentGateway_V2
{
    bool Charge(decimal amount, string token);
    bool Refund(string transactionId, decimal amount);
}

public interface IPaymentRepository
{
    void Save(string transactionId, decimal amount, string status);
    string? FindById(string transactionId);
}

// ✅ Real implementations — only created at the application's entry point (Program.cs / DI container)
public class StripeGatewayImpl : IPaymentGateway_V2
{
    public bool Charge(decimal amount, string token)
    {
        Console.WriteLine($"   [Stripe] Charged ₹{amount} for {token}");
        return true;
    }
    public bool Refund(string txId, decimal amount)
    {
        Console.WriteLine($"   [Stripe] Refunded ₹{amount} for {txId}");
        return true;
    }
}

public class PayPalGatewayImpl : IPaymentGateway_V2
{
    public bool Charge(decimal amount, string token)
    {
        Console.WriteLine($"   [PayPal] Charged ₹{amount} for {token}");
        return true;
    }
    public bool Refund(string txId, decimal amount)
    {
        Console.WriteLine($"   [PayPal] Refunded ₹{amount} for {txId}");
        return true;
    }
}

public class SqlPaymentRepositoryImpl : IPaymentRepository
{
    private readonly Dictionary<string, (decimal amount, string status)> _store = new();

    public void Save(string transactionId, decimal amount, string status)
    {
        _store[transactionId] = (amount, status);
        Console.WriteLine($"   [SQL] Saved tx={transactionId} amount={amount} status={status}");
    }

    public string? FindById(string transactionId) =>
        _store.TryGetValue(transactionId, out var r) ? $"tx={transactionId} amount={r.amount} status={r.status}" : null;
}

// ✅ Fake implementations — used ONLY in tests, zero real infrastructure needed
public class FakePaymentGateway : IPaymentGateway_V2
{
    // ✅ Hardcoded responses — tests control exactly what the gateway returns
    public bool AlwaysSucceed { get; set; } = true;

    public bool Charge(decimal amount, string token)
    {
        Console.WriteLine($"   [FAKE] Charge called with ₹{amount} → returning {AlwaysSucceed}");
        return AlwaysSucceed;
    }

    public bool Refund(string txId, decimal amount)
    {
        Console.WriteLine($"   [FAKE] Refund called for {txId} → returning {AlwaysSucceed}");
        return AlwaysSucceed;
    }
}

public class InMemoryPaymentRepository : IPaymentRepository
{
    // ✅ In-memory store — no SQL Server, no connection string, no setup
    private readonly Dictionary<string, (decimal amount, string status)> _store = new();

    public void Save(string transactionId, decimal amount, string status) =>
        _store[transactionId] = (amount, status);

    public string? FindById(string transactionId) =>
        _store.TryGetValue(transactionId, out var r) ? $"tx={transactionId} amount={r.amount} status={r.status}" : null;
}

// ✅ The business logic class — clean, zero infrastructure awareness
public class GoodPaymentService_WithDIP
{
    private readonly IPaymentGateway_V2 _gateway;
    private readonly IPaymentRepository _repo;

    // ✅ Constructor injection — caller decides which implementations to wire in
    //    In ASP.NET Core, builder.Services.AddScoped<IPaymentGateway_V2, StripeGatewayImpl>()
    //    makes the framework do this wiring automatically at runtime.
    public GoodPaymentService_WithDIP(IPaymentGateway_V2 gateway, IPaymentRepository repo)
    {
        _gateway = gateway;
        _repo    = repo;
    }

    public bool ProcessPayment(decimal amount, string token)
    {
        // ✅ Pure business logic. Not a single mention of Stripe, SQL, or HTTP.
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.");

        var success = _gateway.Charge(amount, token);
        if (success)
        {
            var txId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            _repo.Save(txId, amount, "Completed");
            Console.WriteLine($"   Payment successful. Transaction: {txId}");
            return true;
        }

        Console.WriteLine("   Payment failed at gateway.");
        return false;
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

        Console.WriteLine("❌ BEFORE — Dependencies hard-coded with 'new' inside the class:");
        var bad = new BadPaymentService_HardCoded();
        bad.ProcessPayment(500m, "tok_bad");
        Console.WriteLine("   ^ Works — but try testing this without Stripe creds and SQL Server.\n");

        // ──────────────────────────────────────────────────
        Console.WriteLine("✅ AFTER — Production wiring (Stripe + SQL):\n");
        var prodService = new GoodPaymentService_WithDIP(
            gateway: new StripeGatewayImpl(),
            repo:    new SqlPaymentRepositoryImpl()
        );
        prodService.ProcessPayment(1500m, "tok_visa_4242");

        // ──────────────────────────────────────────────────
        Console.WriteLine("\n✅ AFTER — Swap to PayPal in one line (zero changes to GoodPaymentService):\n");
        var paypalService = new GoodPaymentService_WithDIP(
            gateway: new PayPalGatewayImpl(),        // ← one line change
            repo:    new SqlPaymentRepositoryImpl()
        );
        paypalService.ProcessPayment(1500m, "tok_paypal_abc");

        // ──────────────────────────────────────────────────
        Console.WriteLine("\n✅ AFTER — Unit test scenario (no real API, no real database):\n");
        var fakeGateway = new FakePaymentGateway { AlwaysSucceed = true };
        var fakeRepo    = new InMemoryPaymentRepository();
        var testService = new GoodPaymentService_WithDIP(fakeGateway, fakeRepo);

        Console.WriteLine("   Test: successful payment");
        testService.ProcessPayment(750m, "test_token_ok");

        Console.WriteLine();
        fakeGateway.AlwaysSucceed = false; // ← Simulate gateway failure
        Console.WriteLine("   Test: gateway failure scenario");
        testService.ProcessPayment(750m, "test_token_fail");

        Console.WriteLine();
        Console.WriteLine("   ASP.NET Core equivalent (what the DI container does for you):");
        Console.WriteLine("   builder.Services.AddScoped<IPaymentGateway_V2, StripeGatewayImpl>();");
        Console.WriteLine("   builder.Services.AddScoped<IPaymentRepository,  SqlPaymentRepositoryImpl>();");
        Console.WriteLine("   // → Framework auto-injects these into GoodPaymentService constructor.");
        Console.WriteLine("   // → You've been using Dependency Inversion without realising it.");

        Console.WriteLine("\n✅ DependencyInversion — understood.");
    }
}
