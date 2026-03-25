// ═══════════════════════════════════════════════════════════
// Pattern  : Factory Method
// Category : Creational
// Intent   : Define an interface for creating an object, but let subclasses decide which class to instantiate.
// Domain   : PaymentGateway — Stripe / PayPal / BankTransfer
// Kudvenkat: Video 9 — Factory Method Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// In a payment system, you support multiple gateways: Stripe, PayPal, Bank Transfer.
// Each gateway has its own SDK, auth flow, and error codes.
//
// Without Factory Method, every place that needs a gateway does:
//   if (type == "stripe") new StripeGateway(apiKey)
//   else if ...
// — duplicated everywhere, impossible to add a new gateway without touching 10 files.
//
// WHEN TO USE:
//   ✔ You don't know ahead of time which exact object to create (decided at runtime)
//   ✔ You want to encapsulate object creation logic in one place
//   ✔ Adding a new "product" (gateway) should require ZERO changes to existing code (Open/Closed)
//
// WHEN NOT TO USE:
//   ✘ Only one concrete type will ever exist — factory is overkill
//   ✘ Object creation is trivial (just `new Foo()`) with no variation

namespace LLDMaster.Patterns.Creational.FactoryMethod;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Factory Method)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — calling code knows too much about concrete types
// Every controller / service does this switch block.
// Add CryptoPayment tomorrow → grep all files for this block and update each one.

public class NaiveCheckoutService
{
    public void ProcessPayment(string gatewayType, decimal amount)
    {
        // 💥 PROBLEM: Open/Closed violation — must edit this method for every new gateway
        if (gatewayType == "stripe")
        {
            var stripe = new NaiveStripeGateway();
            stripe.Charge(amount);
        }
        else if (gatewayType == "paypal")
        {
            var paypal = new NaivePayPalGateway();
            paypal.Charge(amount);
        }
        // adding BankTransfer = edit this method again
    }
}

public class NaiveStripeGateway  { public void Charge(decimal amount) => Console.WriteLine($"[Stripe]  Charged {amount:C}"); }
public class NaivePayPalGateway  { public void Charge(decimal amount) => Console.WriteLine($"[PayPal]  Charged {amount:C}"); }

// ─────────────────────────────────────────────────────────────
// SECTION 2 — FACTORY METHOD (the right way)
// ─────────────────────────────────────────────────────────────

// ── Step 1: Define the Product interface ─────────────────────

/// <summary>Abstraction every payment gateway must honour.</summary>
public interface IPaymentGateway
{
    /// <summary>Charges the customer the specified amount.</summary>
    PaymentResult Charge(decimal amount, string currency, string referenceId);

    /// <summary>Refunds a previously captured transaction.</summary>
    PaymentResult Refund(string transactionId, decimal amount);
}

/// <summary>Immutable result returned by every gateway operation.</summary>
public sealed record PaymentResult(
    bool Success,
    string TransactionId,
    string Message
);

// ── Step 2: Concrete Products ─────────────────────────────────

/// <summary>
/// Stripe integration.
/// C# note: sealed prevents accidental inheritance that could break Stripe-specific behaviour.
/// </summary>
public sealed class StripeGateway : IPaymentGateway
{
    private readonly string _apiKey;

    // C# primary constructor
    public StripeGateway(string apiKey) => _apiKey = apiKey;

    public PaymentResult Charge(decimal amount, string currency, string referenceId)
    {
        // Real code: call Stripe SDK, handle StripeException
        Console.WriteLine($"[Stripe] Charging {amount:C} {currency} | ref={referenceId} | key={_apiKey[..4]}***");
        return new PaymentResult(true, $"stripe_txn_{referenceId}", "Charge succeeded");
    }

    public PaymentResult Refund(string transactionId, decimal amount)
    {
        Console.WriteLine($"[Stripe] Refunding {amount:C} for txn={transactionId}");
        return new PaymentResult(true, transactionId, "Refund initiated");
    }
}

/// <summary>PayPal integration.</summary>
public sealed class PayPalGateway : IPaymentGateway
{
    private readonly string _clientId;
    private readonly string _clientSecret;

    public PayPalGateway(string clientId, string clientSecret)
        => (_clientId, _clientSecret) = (clientId, clientSecret);

    public PaymentResult Charge(decimal amount, string currency, string referenceId)
    {
        Console.WriteLine($"[PayPal] Charging {amount:C} {currency} | ref={referenceId}");
        return new PaymentResult(true, $"paypal_txn_{referenceId}", "Order captured");
    }

    public PaymentResult Refund(string transactionId, decimal amount)
    {
        Console.WriteLine($"[PayPal] Refunding {amount:C} for txn={transactionId}");
        return new PaymentResult(true, transactionId, "Refund processed");
    }
}

/// <summary>Direct bank transfer (NEFT / RTGS / SWIFT).</summary>
public sealed class BankTransferGateway : IPaymentGateway
{
    private readonly string _bankCode;

    public BankTransferGateway(string bankCode) => _bankCode = bankCode;

    public PaymentResult Charge(decimal amount, string currency, string referenceId)
    {
        Console.WriteLine($"[Bank:{_bankCode}] Initiating transfer {amount:C} {currency} | ref={referenceId}");
        return new PaymentResult(true, $"bank_txn_{referenceId}", "Transfer initiated — T+1 settlement");
    }

    public PaymentResult Refund(string transactionId, decimal amount)
    {
        Console.WriteLine($"[Bank:{_bankCode}] Reversing {amount:C} for txn={transactionId}");
        return new PaymentResult(true, transactionId, "Reversal queued");
    }
}

// ── Step 3: Creator (Factory) ──────────────────────────────────

/// <summary>
/// Factory that creates the right <see cref="IPaymentGateway"/> based on configuration.
/// C# note: static factory is fine here because creation is stateless.
/// Use an interface IPaymentGatewayFactory if you need to mock in unit tests.
/// </summary>
public interface IPaymentGatewayFactory
{
    IPaymentGateway Create(string gatewayType);
}

/// <summary>
/// Reads config and returns the correct gateway.
/// NEW GATEWAY = add a new case + new class. Zero changes to existing code. ✅ Open/Closed.
/// </summary>
public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
{
    // In production: inject IConfiguration and read from appsettings
    private readonly Dictionary<string, Func<IPaymentGateway>> _registry;

    public PaymentGatewayFactory(string stripeKey, string paypalClientId, string paypalSecret, string bankCode)
    {
        _registry = new(StringComparer.OrdinalIgnoreCase)
        {
            ["stripe"]       = () => new StripeGateway(stripeKey),
            ["paypal"]       = () => new PayPalGateway(paypalClientId, paypalSecret),
            ["banktransfer"] = () => new BankTransferGateway(bankCode),
        };
    }

    /// <summary>Returns the gateway for <paramref name="gatewayType"/>.</summary>
    /// <exception cref="NotSupportedException">Thrown when an unknown gateway type is requested.</exception>
    public IPaymentGateway Create(string gatewayType)
    {
        if (_registry.TryGetValue(gatewayType, out var factory))
            return factory();

        // Fail fast — never silently fall through to a null gateway
        throw new NotSupportedException($"Payment gateway '{gatewayType}' is not registered.");
    }

    /// <summary>Registers a new gateway at runtime (plugin / feature-flag scenario).</summary>
    public void Register(string gatewayType, Func<IPaymentGateway> factory)
        => _registry[gatewayType] = factory;
}

// ── Step 4: Consumer — knows NOTHING about concrete types ──────

/// <summary>
/// Checkout service that is fully decoupled from gateway implementations.
/// To add CryptoPayment: create CryptoGateway, register it — this class is untouched.
/// </summary>
public sealed class CheckoutService(IPaymentGatewayFactory gatewayFactory)
{
    public PaymentResult ProcessPayment(string gatewayType, decimal amount, string currency, string orderId)
    {
        // Factory hides ALL the 'new' complexity
        var gateway = gatewayFactory.Create(gatewayType);
        return gateway.Charge(amount, currency, orderId);
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE NOTES
// ─────────────────────────────────────────────────────────────
// In ASP.NET Core:
//   builder.Services.AddSingleton<IPaymentGatewayFactory>(sp =>
//       new PaymentGatewayFactory(
//           config["Stripe:ApiKey"],
//           config["PayPal:ClientId"],
//           config["PayPal:ClientSecret"],
//           config["Bank:Code"]));
//   builder.Services.AddScoped<CheckoutService>();
//
// Controller just takes CheckoutService via constructor injection.
// Gateway type comes from the user's choice on the payment page.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class FactoryMethodDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Factory Method Pattern — Payment Gateway ===\n");

        // ── PROBLEM ────────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: Naive service (switch block everywhere) ──");
        var naive = new NaiveCheckoutService();
        naive.ProcessPayment("stripe", 99.99m);
        naive.ProcessPayment("paypal", 49.00m);
        Console.WriteLine("Adding a new gateway means editing NaiveCheckoutService. BAD.\n");

        // ── FACTORY METHOD ─────────────────────────────────────────────────
        Console.WriteLine("── Factory Method: CheckoutService knows zero concrete types ──");
        var factory = new PaymentGatewayFactory(
            stripeKey: "sk_test_ABC123",
            paypalClientId: "pp_client_XYZ",
            paypalSecret: "pp_secret_XYZ",
            bankCode: "HDFC");

        var checkout = new CheckoutService(factory);

        var r1 = checkout.ProcessPayment("stripe",       1200.00m, "INR", "ORD-001");
        var r2 = checkout.ProcessPayment("paypal",        450.00m, "USD", "ORD-002");
        var r3 = checkout.ProcessPayment("banktransfer", 5000.00m, "INR", "ORD-003");

        Console.WriteLine($"\nResults: {r1.Success}, {r2.Success}, {r3.Success}");

        // ── EXTENSIBILITY: add CryptoGateway without touching CheckoutService ─
        Console.WriteLine("\n── Registering CryptoGateway at runtime ──");
        factory.Register("crypto", () => new StripeGateway("crypto_key_mock")); // mock
        var r4 = checkout.ProcessPayment("crypto", 0.05m, "BTC", "ORD-004");
        Console.WriteLine($"Crypto result: {r4.Message}");

        Console.WriteLine("\n✅ Factory Method — understood.");
    }
}
