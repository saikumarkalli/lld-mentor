/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🧠 CONCEPT: Abstraction                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Abstraction means showing only what's necessary and hiding how it works     ║
 * ║  underneath. You define a contract ("what can you do?") separately from the  ║
 * ║  implementation ("how do you do it?"). Callers talk to the contract, not the ║
 * ║  implementation.                                                             ║
 * ║                                                                              ║
 * ║  MENTAL MODEL (the analogy to remember forever):                             ║
 * ║  A TV remote. You press "Volume Up" — you don't know if the TV uses          ║
 * ║  infrared, Bluetooth, or wifi. You don't care. The remote is the interface.  ║
 * ║  The TV brand (Sony, Samsung) is the implementation. Swap the TV, same       ║
 * ║  remote still works.                                                         ║
 * ║                                                                              ║
 * ║  THE PRODUCTION HORROR STORY (why this matters):                             ║
 * ║  A startup hardcoded Stripe's full HTTP API call inside their checkout       ║
 * ║  service. When Stripe changed their API version and deprecated an endpoint,  ║
 * ║  the fix touched 11 different files across 3 services. Two of those files    ║
 * ║  were found only after monitoring alerts fired in production. With a proper  ║
 * ║  abstraction, there would have been exactly one file to change.              ║
 * ║                                                                              ║
 * ║  QUICK RECALL (read this in 2 years and instantly remember):                 ║
 * ║  Program to an interface, not an implementation.                             ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.PaymentModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — How a beginner naturally writes this
// "I only need Stripe right now, so I'll just call it directly. I'll refactor
//  later if I need PayPal." — Famous last words. Later never comes.
//  When PayPal is needed, you copy-paste 30 lines and change 3 values.
// ─────────────────────────────────────────────────────────────────────────────

public class BadPaymentService_Before
{
    // ❌ HttpClient is embedded — this class knows HOW to talk to Stripe
    private readonly HttpClient _httpClient = new();

    public async Task<bool> ProcessPayment_Stripe(decimal amount, string cardToken)
    {
        // ❌ All 30 lines of Stripe-specific HTTP logic live right here
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk_live_STRIPE_SECRET");
        _httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");

        var payload = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("amount",   ((int)(amount * 100)).ToString()),
            new KeyValuePair<string, string>("currency", "inr"),
            new KeyValuePair<string, string>("source",   cardToken),
        });

        // ❌ Retry logic, URL, error parsing — all Stripe-specific, all inline
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var response = await _httpClient.PostAsync("https://api.stripe.com/v1/charges", payload);
            if (response.IsSuccessStatusCode) return true;
        }
        return false;
    }

    // ❌ When PayPal is required, you paste 28 near-identical lines here:
    public async Task<bool> ProcessPayment_PayPal(decimal amount, string cardToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer PAYPAL_SECRET");
        // ... 25 more lines almost identical to above, just different URLs and keys
        // ❌ Bug fixed in Stripe block? Fix it AGAIN here. And in Razorpay block. And in...
        return false; // placeholder
    }
}

// 💥 WHAT GOES WRONG:
// 1. Adding CryptoPayment = find this file, paste 30 more lines, hope you don't miss a header.
// 2. Stripe changes their API? Grep the whole codebase for "api.stripe.com" and pray you got all of them.
// 3. Unit testing is impossible — you need a live Stripe key just to test ProcessPayment.
// 4. The class name is "PaymentService" but it knows Stripe's auth headers by heart.
//    That's not a service. That's a Stripe wrapper with extra steps.


// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — The OOP-correct way
// Define a contract (IPaymentGateway). PaymentService talks only to the contract.
// Stripe and PayPal implement the contract independently. Zero duplication.
// Swap gateways by changing one constructor argument.
// ─────────────────────────────────────────────────────────────────────────────

// ✅ The contract — this is ALL that PaymentService will ever know about gateways
public interface IPaymentGateway
{
    /// <summary>Charge a card. Returns a gateway transaction ID on success.</summary>
    Task<string?> ChargeAsync(decimal amount, string cardToken);

    /// <summary>Refund a previously completed charge.</summary>
    Task<bool> RefundAsync(string gatewayTransactionId, decimal amount);

    /// <summary>Check current status of a charge from the gateway.</summary>
    Task<string> GetStatusAsync(string gatewayTransactionId);
}

// ✅ Abstract base hides the shared plumbing (HTTP setup, retry, logging).
//    Concrete classes only override the parts that differ per gateway.
public abstract class PaymentGatewayBase : IPaymentGateway
{
    protected readonly HttpClient HttpClient;
    protected abstract string BaseUrl { get; }
    protected abstract string AuthHeader { get; }

    protected PaymentGatewayBase()
    {
        HttpClient = new HttpClient();
        // ✅ Auth is set up once in the base — subclasses just provide the value
        HttpClient.DefaultRequestHeaders.Add("Authorization", AuthHeader);
    }

    public abstract Task<string?> ChargeAsync(decimal amount, string cardToken);
    public abstract Task<bool>    RefundAsync(string gatewayTransactionId, decimal amount);
    public abstract Task<string>  GetStatusAsync(string gatewayTransactionId);

    // ✅ Shared retry logic lives here ONCE — all gateways get it for free
    protected async Task<HttpResponseMessage> PostWithRetryAsync(string endpoint, HttpContent content)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            var response = await HttpClient.PostAsync($"{BaseUrl}{endpoint}", content);
            if (response.IsSuccessStatusCode) return response;
            if (attempt < 3) await Task.Delay(200 * attempt); // backoff
        }
        throw new Exception($"Gateway {GetType().Name} failed after 3 attempts.");
    }
}

// ✅ StripeGateway is now a clean ~10 line class — only Stripe-specific stuff here
public class StripeGateway : PaymentGatewayBase
{
    protected override string BaseUrl    => "https://api.stripe.com/v1/";
    protected override string AuthHeader => "Bearer sk_live_STRIPE_SECRET";

    public override async Task<string?> ChargeAsync(decimal amount, string cardToken)
    {
        var payload = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("amount",   ((int)(amount * 100)).ToString()),
            new KeyValuePair<string, string>("currency", "inr"),
            new KeyValuePair<string, string>("source",   cardToken),
        });
        var response = await PostWithRetryAsync("charges", payload);
        // Parse Stripe-specific JSON response — only Stripe knows its schema
        return response.IsSuccessStatusCode ? $"stripe_ch_{Guid.NewGuid():N}" : null;
    }

    public override Task<bool>   RefundAsync(string id, decimal amount) => Task.FromResult(true);
    public override Task<string> GetStatusAsync(string id)              => Task.FromResult("succeeded");
}

// ✅ PayPalGateway is equally clean — different URLs, same pattern
public class PayPalGateway : PaymentGatewayBase
{
    protected override string BaseUrl    => "https://api.paypal.com/v2/";
    protected override string AuthHeader => "Bearer PAYPAL_OAUTH_TOKEN";

    public override async Task<string?> ChargeAsync(decimal amount, string cardToken)
    {
        // PayPal-specific JSON body format — entirely different from Stripe
        var json = $@"{{""amount"":{{""value"":""{amount}"",""currency_code"":""INR""}}}}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await PostWithRetryAsync("payments/captures", content);
        return response.IsSuccessStatusCode ? $"paypal_cap_{Guid.NewGuid():N}" : null;
    }

    public override Task<bool>   RefundAsync(string id, decimal amount) => Task.FromResult(true);
    public override Task<string> GetStatusAsync(string id)              => Task.FromResult("COMPLETED");
}

// ✅ PaymentService has ZERO knowledge of Stripe, PayPal, HTTP, or headers.
//    It only knows IPaymentGateway — the contract. Pure business logic.
public class PaymentService
{
    private readonly IPaymentGateway _gateway; // ✅ Talks to the interface, not an implementation

    public PaymentService(IPaymentGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<string?> ProcessPayment(decimal amount, string cardToken)
    {
        // ✅ No if/else for Stripe vs PayPal. Just call the contract.
        Console.WriteLine($"   Charging ₹{amount} via {_gateway.GetType().Name}...");
        var transactionId = await _gateway.ChargeAsync(amount, cardToken);
        if (transactionId != null)
            Console.WriteLine($"   Gateway responded: {transactionId}");
        return transactionId;
    }
}


// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class AbstractionDemo
{
    public static async Task Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  ABSTRACTION DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        Console.WriteLine("❌ BEFORE — PaymentService knows Stripe's headers, URLs, retry logic.");
        Console.WriteLine("   Adding PayPal = copy 30 lines, change 3 values, bugs now live in 2 places.\n");

        // ✅ AFTER — swap gateway by changing one line
        Console.WriteLine("✅ AFTER — Using Stripe gateway:");
        var stripeService = new PaymentService(new StripeGateway());
        await stripeService.ProcessPayment(1500m, "tok_visa_4242");

        Console.WriteLine("\n✅ AFTER — Swapped to PayPal gateway (PaymentService code unchanged):");
        var paypalService = new PaymentService(new PayPalGateway());
        await paypalService.ProcessPayment(1500m, "tok_visa_4242");

        Console.WriteLine("\n   Adding CryptoGateway tomorrow = write ONE new class.");
        Console.WriteLine("   PaymentService, StripeGateway, PayPalGateway — zero changes.");

        Console.WriteLine("\n✅ Abstraction — understood.");
    }
}
