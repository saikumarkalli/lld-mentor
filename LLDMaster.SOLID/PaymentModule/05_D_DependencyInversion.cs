/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  SECTION 1 — FILE HEADER                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  💎 SOLID PRINCIPLE: D — Dependency Inversion Principle (DIP)                ║
 * ║                                                                              ║
 * ║  🔗 OOP ORIGIN: "In your OOP module, GoodPaymentService_WithDIP was almost   ║
 * ║     right — you injected IPaymentGateway and IPaymentRepository.             ║
 * ║     But in the full orchestrator, three more dependencies were created       ║
 * ║     with `new` inside the class. DIP says: ALL of them. Every time."        ║
 * ║                                                                              ║
 * ║  THE ONE-LINE RULE (memorise this):                                          ║
 * ║  "High-level modules (business logic) must not depend on                     ║
 * ║   low-level modules (databases, APIs, email).                                ║
 * ║   Both must depend on abstractions (interfaces).                             ║
 * ║   The interface is OWNED by the HIGH-level module, not the low-level one."  ║
 * ║                                                                              ║
 * ║  That last line is the part most developers miss.                            ║
 * ║                                                                              ║
 * ║  WHAT GOES WRONG WITHOUT IT:                                                 ║
 * ║  PaymentOrchestrator creates its own StripeWebhookHandler, SqlRepository,   ║
 * ║  TwilioNotifier. To test "does ProcessPayment update the balance correctly"  ║
 * ║  you need a live Stripe account (charges real money), a real SQL Server      ║
 * ║  (creates real rows), and a Twilio account (sends SMS to real customers).   ║
 * ║  Running tests cost money and sent SMS to real customers. This happened.     ║
 * ║                                                                              ║
 * ║  THE PR SMELL:                                                               ║
 * ║  "In a code review, you'd spot this when you see `new ConcreteService()`    ║
 * ║   inside a business logic class, or a constructor parameter that is a        ║
 * ║   concrete class (SqlRepository, TwilioSender) rather than an interface."   ║
 * ║                                                                              ║
 * ║  CONNECTS TO NEXT:                                                           ║
 * ║  S+O+L+I+D = One idea: Easy to change, hard to break.                       ║
 * ║  Design Patterns (Phase 3) are SOLID principles in named, reusable forms.   ║
 * ║  You've already seen Strategy (OCP) and Repository (DIP). Next: Factory.    ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLDMaster.SOLID.PaymentModule.DIP;

// ─────────────────────────────────────────────────────────────────────────────
// Minimal shared types
// ─────────────────────────────────────────────────────────────────────────────

public record PaymentRequest_DIP(decimal Amount, string Token, string Email);
public record PaymentResult_DIP(bool IsSuccess, string TransactionId, string Message)
{
    public static PaymentResult_DIP Success(string txId) => new(true, txId, "Success.");
    public static PaymentResult_DIP Failure(string r)    => new(false, string.Empty, r);
}
public record Transaction_DIP(string Id, decimal Amount, string Status);
public record WebhookEvent(string EventType, string Payload);


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 2 — BEFORE (minimal violation)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ❌ Low-level concrete classes — simulated here (in production these call real APIs)
public class StripeWebhookHandler_Concrete
{
    private readonly string _apiKey;
    public StripeWebhookHandler_Concrete(string apiKey) { _apiKey = apiKey; }

    public void Handle(WebhookEvent evt)
    {
        // In reality: HTTP call to Stripe. Needs sk_live_xxx API key to even instantiate.
        Console.WriteLine($"   [Stripe REAL] Handling event={evt.EventType} with key {_apiKey[..8]}...");
    }
}

public class SqlTransactionRepository_Concrete
{
    private readonly string _connectionString;
    public SqlTransactionRepository_Concrete(string cs) { _connectionString = cs; }

    public void Save(Transaction_DIP tx)
    {
        // In reality: opens a SQL connection. Needs a running SQL Server.
        Console.WriteLine($"   [SQL REAL] INSERT INTO Transactions: id={tx.Id} amount={tx.Amount}");
    }

    public Transaction_DIP? GetById(string id)
    {
        Console.WriteLine($"   [SQL REAL] SELECT * FROM Transactions WHERE id={id}");
        return null;
    }
}

public class TwilioReceiptSender_Concrete
{
    private readonly string _accountSid;
    private readonly string _authToken;
    public TwilioReceiptSender_Concrete(string sid, string token)
    { _accountSid = sid; _authToken = token; }

    public void Send(string email, PaymentResult_DIP result)
    {
        // In reality: HTTP call to Twilio. Bills per SMS. Sends to REAL customers.
        Console.WriteLine($"   [Twilio REAL] Sending receipt SMS to {email} for tx={result.TransactionId}");
    }
}

// ❌ BEFORE — PaymentOrchestrator: partial DIP (gateway injected, others hardwired)
// Your OOP module got IPaymentGateway right. But three dependencies crept back in.
public class PaymentOrchestrator_Bad_DIP
{
    // This one was injected in the OOP module ✓
    private readonly string _gatewayApiKey;

    // ❌ These three were added later — hardwired concrete classes
    // Cannot be swapped. Cannot be faked. Cannot be tested without real infrastructure.
    private readonly StripeWebhookHandler_Concrete _webhookHandler
        = new StripeWebhookHandler_Concrete(apiKey: "sk_live_hardcoded_key");

    private readonly SqlTransactionRepository_Concrete _repository
        = new SqlTransactionRepository_Concrete("Server=prod-sql;Database=payments;...");

    private readonly TwilioReceiptSender_Concrete _receiptSender
        = new TwilioReceiptSender_Concrete("AC_account_sid", "auth_token_hardcoded");

    public PaymentOrchestrator_Bad_DIP(string gatewayApiKey)
    {
        _gatewayApiKey = gatewayApiKey;
        // The 3 above are hardwired. Cannot be swapped. Cannot be faked for tests.
    }

    public PaymentResult_DIP Process(PaymentRequest_DIP request)
    {
        var txId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        var tx   = new Transaction_DIP(txId, request.Amount, "Completed");

        _repository.Save(tx);          // ← REAL SQL in every test run
        _receiptSender.Send(request.Email, PaymentResult_DIP.Success(txId)); // ← REAL SMS to customers

        return PaymentResult_DIP.Success(txId);
    }
}

// 🚨 VIOLATION: D — High-level PaymentOrchestrator_Bad_DIP depends on low-level concretions:
//               StripeWebhookHandler_Concrete, SqlTransactionRepository_Concrete,
//               TwilioReceiptSender_Concrete. All hardwired with `new`.
//
// 💥 CONSEQUENCE: Integration test suite sent 847 real SMS receipts to real customers.
//    ₹4,200 in Twilio charges. 847 confused customers. 3 hours of apology emails.
//    The tests were "passing" — they just happened to use production infrastructure.


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 3 — WHY THIS VIOLATES DIP
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 🔍 THE REASONING:
//
// PROBLEM 1 — THE DIRECTION OF OWNERSHIP (the subtle part most devs miss):
//   DIP says the interface is OWNED by the high-level module.
//   IPaymentGateway is defined in the Payments domain (high-level).
//   StripeGateway (low-level) implements it — Stripe conforms to Payments' rules.
//   If Stripe changes their API, StripeGateway changes. IPaymentGateway: unchanged.
//   The high-level module is PROTECTED from low-level infrastructure changes.
//
//   When you do `new StripeWebhookHandler_Concrete()` inside PaymentOrchestrator,
//   you've INVERTED this — low-level Stripe now controls high-level Payments.
//   The dependency arrow points the wrong way. That's the violation.
//
// PROBLEM 2 — TESTABILITY = FINANCIAL SAFETY:
//   Payment processing handles real money. Bugs = real money lost.
//   Untestable code = unverified financial logic = bugs that make it to production.
//   Every hardcoded `new ConcreteClass()` is a test you cannot write.
//   DIP is not architecture astronautics — it is the only way to safely test
//   financial logic without touching real APIs, real databases, real customers.
//
// PROBLEM 3 — THE `new` KEYWORD AS A DETECTOR:
//   `new` inside a class means "I am responsible for creating this dependency."
//   Creating = owning = cannot be replaced = cannot be tested.
//   The `new` keyword in business logic is a DIP violation detector.
//   Exception: `new` for value objects (new PaymentRequest(...)) is fine.
//   `new` for services, repositories, gateways — always a DIP violation.
//
// THE TWO DIP RULES:
//
// Rule 1 — High-level modules must NOT import from low-level modules.
//   PaymentOrchestrator must not reference StripeWebhookHandler_Concrete,
//   SqlTransactionRepository_Concrete, or TwilioReceiptSender_Concrete.
//   It should only depend on interfaces.
//
// Rule 2 — Interfaces belong to the HIGH-LEVEL module.
//   ITransactionRepository lives in the Payments domain (not Infrastructure).
//   SqlTransactionRepository lives in the Infrastructure layer.
//   SqlTransactionRepository DEPENDS ON Payments.Interfaces — not the reverse.
//   Dependency flows inward. Business logic is at the centre. Always.


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 4 — AFTER (DIP-correct)
// ╚══════════════════════════════════════════════════════════════════════════════╝

// ✅ AFTER — All interfaces owned by the Payments domain (high-level).
//    Low-level concretions implement high-level contracts.
//    PaymentOrchestrator has ZERO concrete dependencies.

// ─── Interfaces owned by the Payments domain ─────────────────────────────────
// (High-level modules define what they NEED — low-level modules conform to it.)

public interface IWebhookHandler
{
    void Handle(WebhookEvent evt);
}

public interface ITransactionRepository
{
    void Save(Transaction_DIP transaction);
    Transaction_DIP? GetById(string id);
}

public interface IReceiptSender
{
    void Send(string email, PaymentResult_DIP result);
}

// ─── Production implementations (Infrastructure layer) ───────────────────────
// These live in a separate project/folder. They conform to domain contracts.

public class StripeWebhookHandler : IWebhookHandler
{
    // ✅ Stripe conforms to Payments' rules. Payments doesn't know Stripe exists.
    public void Handle(WebhookEvent evt)
    {
        Console.WriteLine($"   [Stripe] Handling webhook event={evt.EventType}");
    }
}

public class SqlTransactionRepository : ITransactionRepository
{
    private readonly Dictionary<string, Transaction_DIP> _store = new();

    // ✅ SQL conforms to Payments' ITransactionRepository contract.
    public void Save(Transaction_DIP tx)
    {
        _store[tx.Id] = tx;
        Console.WriteLine($"   [SQL] Saved tx={tx.Id} amount=₹{tx.Amount} status={tx.Status}");
    }

    public Transaction_DIP? GetById(string id) =>
        _store.TryGetValue(id, out var tx) ? tx : null;
}

public class TwilioReceiptSender : IReceiptSender
{
    // ✅ Twilio conforms to Payments' IReceiptSender contract.
    public void Send(string email, PaymentResult_DIP result)
    {
        Console.WriteLine($"   [Twilio] Receipt sent to {email} for tx={result.TransactionId}");
    }
}

// ─── Test doubles — zero real infrastructure ──────────────────────────────────
// These are why DIP matters: same interfaces, fake behaviour, no real APIs.

public class FakeWebhookHandler : IWebhookHandler
{
    public List<WebhookEvent> HandledEvents { get; } = new();
    public void Handle(WebhookEvent evt)
    {
        HandledEvents.Add(evt);
        Console.WriteLine($"   [FAKE Webhook] Recorded event={evt.EventType}");
    }
}

public class InMemoryTransactionRepository : ITransactionRepository
{
    // ✅ Dictionary, no SQL. Zero infrastructure. Tests run in milliseconds.
    private readonly Dictionary<string, Transaction_DIP> _store = new();

    public void Save(Transaction_DIP tx)
    {
        _store[tx.Id] = tx;
        Console.WriteLine($"   [InMemory] Saved tx={tx.Id}");
    }

    public Transaction_DIP? GetById(string id) =>
        _store.TryGetValue(id, out var tx) ? tx : null;
}

public class NoOpReceiptSender : IReceiptSender
{
    // ✅ Does nothing. Zero Twilio charges. Zero SMS to real customers.
    public void Send(string email, PaymentResult_DIP result)
    {
        Console.WriteLine($"   [NoOp] Receipt suppressed for {email} (test mode).");
    }
}

// ─── The orchestrator — ZERO concrete dependencies ───────────────────────────
public class PaymentOrchestrator_DIP
{
    private readonly IWebhookHandler       _webhookHandler;
    private readonly ITransactionRepository _repository;
    private readonly IReceiptSender        _receiptSender;

    // ✅ Constructor injection. Every parameter is an interface.
    //    ASP.NET Core DI equivalent (what you write in Program.cs every day):
    //    builder.Services.AddScoped<ITransactionRepository, SqlTransactionRepository>();
    //    builder.Services.AddScoped<IReceiptSender,         TwilioReceiptSender>();
    //    builder.Services.AddScoped<IWebhookHandler,        StripeWebhookHandler>();
    //    The framework reads registrations and injects concretions automatically.
    public PaymentOrchestrator_DIP(
        IWebhookHandler        webhookHandler,
        ITransactionRepository repository,
        IReceiptSender         receiptSender)
    {
        _webhookHandler = webhookHandler;
        _repository     = repository;
        _receiptSender  = receiptSender;
    }

    public PaymentResult_DIP Process(PaymentRequest_DIP request)
    {
        if (request.Amount <= 0)
            return PaymentResult_DIP.Failure("Amount must be positive.");

        // ✅ Pure business logic. Zero mentions of Stripe, SQL, or Twilio.
        var txId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        var tx   = new Transaction_DIP(txId, request.Amount, "Completed");

        _repository.Save(tx);
        _receiptSender.Send(request.Email, PaymentResult_DIP.Success(txId));

        Console.WriteLine($"   [Orchestrator] Payment complete. tx={txId}");
        return PaymentResult_DIP.Success(txId);
    }

    public void HandleWebhook(WebhookEvent evt)
    {
        // ✅ Business logic delegates to the injected handler — doesn't know it's Stripe
        _webhookHandler.Handle(evt);
    }
}


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 5 — PR REVIEW CHECKLIST
// ╚══════════════════════════════════════════════════════════════════════════════╝

// 👁️ HOW TO SPOT THIS IN A REAL PR REVIEW:
// ✅ SAFE:  Every constructor parameter is an interface (IRepository, IGateway, ISender)
// ✅ SAFE:  Zero `new ConcreteService()` inside business logic classes
// ✅ SAFE:  Business logic can be instantiated in a test with fake implementations — no real APIs
// 🚨 FLAG:  `new` keyword applied to a service/repo/gateway inside a class body
// 🚨 FLAG:  Constructor parameter is a concrete class (SqlRepository, TwilioSender)
// 🚨 FLAG:  `using` statement importing a low-level namespace inside a high-level class
//           (e.g., `using Stripe;` inside PaymentOrchestrator — low-level leaking in)
// 🚨 FLAG:  Static method calls: PaymentLogger.Log() — cannot be injected, cannot be faked
// 🚨 FLAG:  "I'll just hardcode this for now" — always a DIP violation and always a test blocker


// ╔══════════════════════════════════════════════════════════════════════════════╗
//  SECTION 6 — CAPSTONE DEMO (all 5 SOLID principles working together)
// ╚══════════════════════════════════════════════════════════════════════════════╝

public static class DependencyInversionDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════════════════");
        Console.WriteLine("  D — Dependency Inversion Principle");
        Console.WriteLine("══════════════════════════════════════════════════\n");

        var request = new PaymentRequest_DIP(1500m, "tok_visa_4242", "user@example.com");

        // ── BEFORE: the violation ─────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — Hardwired concrete dependencies (simulated):\n");
        var badOrchestrator = new PaymentOrchestrator_Bad_DIP("sk_live_hardcoded_key");
        badOrchestrator.Process(request);
        Console.WriteLine("\n   ^ In real code: that just hit a SQL Server and sent an SMS.");
        Console.WriteLine("   ^ To run this test you need: live SQL Server + Twilio + Stripe.");
        Console.WriteLine("   ^ On CI at midnight, Twilio had an outage. 847 customers got blank SMSes.\n");

        // ── AFTER: production wiring ──────────────────────────────────────────
        Console.WriteLine("✅ AFTER — Production wiring (Stripe + SQL + Twilio injected):\n");
        var prodOrchestrator = new PaymentOrchestrator_DIP(
            webhookHandler: new StripeWebhookHandler(),
            repository:     new SqlTransactionRepository(),
            receiptSender:  new TwilioReceiptSender()
        );
        prodOrchestrator.Process(request);
        prodOrchestrator.HandleWebhook(new WebhookEvent("payment.succeeded", "{}"));

        // ── AFTER: test wiring (zero real APIs) ───────────────────────────────
        Console.WriteLine("\n✅ AFTER — Test wiring (zero real infrastructure, zero charges, zero SMS):\n");
        var fakeWebhook    = new FakeWebhookHandler();
        var fakeRepo       = new InMemoryTransactionRepository();
        var noOpReceipt    = new NoOpReceiptSender();

        var testOrchestrator = new PaymentOrchestrator_DIP(fakeWebhook, fakeRepo, noOpReceipt);
        var result = testOrchestrator.Process(request);

        // Verify the result — this is what a unit test would assert:
        Console.WriteLine($"\n   Test assertion: IsSuccess={result.IsSuccess} (expected: True)");
        Console.WriteLine($"   Test assertion: TransactionId set={!string.IsNullOrEmpty(result.TransactionId)}");

        // Verify the saved transaction can be retrieved:
        var saved = fakeRepo.GetById(result.TransactionId);
        Console.WriteLine($"   Test assertion: transaction saved to repo={saved is not null}");

        Console.WriteLine("\n   Zero Twilio charges. Zero SQL Server. Runs in < 1ms. Free. Safe.");
        Console.WriteLine("   This is why your controllers are testable in ASP.NET Core.");
        Console.WriteLine("   You've been writing DIP. Now you know what it's called.\n");

        // ── ASP.NET Core bridge ───────────────────────────────────────────────
        Console.WriteLine("   ASP.NET Core DI equivalent (what you write in Program.cs):");
        Console.WriteLine("   builder.Services.AddScoped<ITransactionRepository, SqlTransactionRepository>();");
        Console.WriteLine("   builder.Services.AddScoped<IReceiptSender,         TwilioReceiptSender>();");
        Console.WriteLine("   builder.Services.AddScoped<IWebhookHandler,        StripeWebhookHandler>();");
        Console.WriteLine("   // Framework injects concretions automatically. Zero `new` in business logic.\n");

        Console.WriteLine("✅ Dependency Inversion — understood.");
        Console.WriteLine("→ S+O+L+I+D = One idea: Easy to change, hard to break.");
    }
}
