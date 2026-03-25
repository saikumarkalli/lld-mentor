// ═══════════════════════════════════════════════════════════
// Pattern  : Builder
// Category : Creational
// Intent   : Separate the construction of a complex object from its representation.
// Domain   : PaymentRequest — amount, currency, gateway, metadata, retry policy
// Kudvenkat: Video 11–13 — Builder Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// A PaymentRequest in production has 10–15 fields:
//   amount, currency, gateway, orderId, customerId, billingAddress,
//   metadata, retryPolicy, idempotencyKey, webhookUrl, ...
//
// Without Builder you get:
//   new PaymentRequest(1200, "INR", "stripe", "ORD-1", "CUST-1", null, null, null, null, null)
//   → positional parameters → wrong order → silent bugs
//   → or a 15-parameter constructor that nobody can read
//
// WHEN TO USE:
//   ✔ Object has many optional parameters (>3-4)
//   ✔ Object must be immutable once built (all validation in Build())
//   ✔ Construction steps must happen in a specific order
//   ✔ You want a readable, self-documenting call site
//
// WHEN NOT TO USE:
//   ✘ Object has 2-3 fields — just use a constructor or init properties
//   ✘ Object is mutable and fields change frequently after creation

namespace LLDMaster.Patterns.Creational.Builder;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Builder)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — telescoping constructor anti-pattern

public class NaivePaymentRequest
{
    // 💥 PROBLEM 1: which null is which? Positional args are opaque.
    // 💥 PROBLEM 2: adding a new field shifts all positions — breaks callers
    public NaivePaymentRequest(decimal amount, string currency, string gateway,
        string orderId, string? coupon, bool saveCard, int retryCount) { }
}

// Calling code:
//   new NaivePaymentRequest(1200, "INR", "stripe", "ORD-1", null, false, 3)
//   Did you mean retryCount=3 or retryCount=0? Is null the coupon or the webhook?
//   Nobody knows. Welcome to production bugs.

// ─────────────────────────────────────────────────────────────
// SECTION 2 — BUILDER (the right way)
// ─────────────────────────────────────────────────────────────

// ── The Product — immutable once built ────────────────────────

/// <summary>
/// Immutable payment request. Can only be created via <see cref="PaymentRequestBuilder"/>.
/// C# note: record provides structural equality and ToString() for free.
/// </summary>
public sealed record PaymentRequest
{
    public decimal   Amount          { get; init; }
    public string    Currency        { get; init; } = "INR";
    public string    GatewayType     { get; init; } = "stripe";
    public string    OrderId         { get; init; } = string.Empty;
    public string    CustomerId      { get; init; } = string.Empty;
    public string?   CouponCode      { get; init; }
    public string?   WebhookUrl      { get; init; }
    public string    IdempotencyKey  { get; init; } = Guid.NewGuid().ToString("N");
    public int       MaxRetries      { get; init; } = 3;
    public bool      SaveCard        { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = [];

    // Internal constructor — only the Builder can call this
    internal PaymentRequest() { }
}

// ── The Builder ────────────────────────────────────────────────

/// <summary>
/// Fluent builder for <see cref="PaymentRequest"/>.
/// C# note: each setter returns <c>this</c> — enables method chaining.
/// The final <see cref="Build"/> validates required fields and returns
/// an immutable object — "validate once, trust always".
/// </summary>
public sealed class PaymentRequestBuilder
{
    private decimal   _amount;
    private string    _currency       = "INR";
    private string    _gatewayType    = "stripe";
    private string    _orderId        = string.Empty;
    private string    _customerId     = string.Empty;
    private string?   _couponCode;
    private string?   _webhookUrl;
    private string    _idempotencyKey = Guid.NewGuid().ToString("N");
    private int       _maxRetries     = 3;
    private bool      _saveCard;
    private readonly Dictionary<string, string> _metadata = [];

    // ── Required fields ───────────────────────────────────────

    /// <summary>Sets the charge amount (required, must be > 0).</summary>
    public PaymentRequestBuilder ForAmount(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        _amount = amount;
        return this;
    }

    /// <summary>Sets the order being paid for (required).</summary>
    public PaymentRequestBuilder ForOrder(string orderId, string customerId)
    {
        _orderId    = orderId    ?? throw new ArgumentNullException(nameof(orderId));
        _customerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
        return this;
    }

    // ── Optional fields ───────────────────────────────────────

    /// <summary>Sets the target gateway. Defaults to "stripe".</summary>
    public PaymentRequestBuilder ViaGateway(string gatewayType)
    {
        _gatewayType = gatewayType;
        return this;
    }

    /// <summary>Sets the currency code. Defaults to "INR".</summary>
    public PaymentRequestBuilder InCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    /// <summary>Applies a coupon/promo code.</summary>
    public PaymentRequestBuilder WithCoupon(string couponCode)
    {
        _couponCode = couponCode;
        return this;
    }

    /// <summary>Sets the webhook URL for async payment events.</summary>
    public PaymentRequestBuilder NotifyAt(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
        return this;
    }

    /// <summary>Sets the idempotency key (auto-generated if not called).</summary>
    public PaymentRequestBuilder WithIdempotencyKey(string key)
    {
        _idempotencyKey = key;
        return this;
    }

    /// <summary>Configures retry behaviour on transient failures.</summary>
    public PaymentRequestBuilder WithRetries(int maxRetries)
    {
        _maxRetries = maxRetries;
        return this;
    }

    /// <summary>Instructs the gateway to tokenise and save the card for future use.</summary>
    public PaymentRequestBuilder SaveCardForFutureUse()
    {
        _saveCard = true;
        return this;
    }

    /// <summary>Adds a metadata key-value pair (e.g., source channel, campaign id).</summary>
    public PaymentRequestBuilder AddMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Validates all required fields and returns the immutable <see cref="PaymentRequest"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
    public PaymentRequest Build()
    {
        if (_amount <= 0)           throw new InvalidOperationException("Amount is required. Call ForAmount().");
        if (string.IsNullOrWhiteSpace(_orderId))    throw new InvalidOperationException("OrderId is required. Call ForOrder().");
        if (string.IsNullOrWhiteSpace(_customerId)) throw new InvalidOperationException("CustomerId is required. Call ForOrder().");

        return new PaymentRequest
        {
            Amount         = _amount,
            Currency       = _currency,
            GatewayType    = _gatewayType,
            OrderId        = _orderId,
            CustomerId     = _customerId,
            CouponCode     = _couponCode,
            WebhookUrl     = _webhookUrl,
            IdempotencyKey = _idempotencyKey,
            MaxRetries     = _maxRetries,
            SaveCard       = _saveCard,
            Metadata       = new Dictionary<string, string>(_metadata),
        };
    }
}

// ── Optional: Director class for common pre-built configurations ──

/// <summary>
/// Director encapsulates common builder recipes.
/// C# note: Director is optional — useful when the same build sequence
/// is used in many places (avoids duplicating builder chains).
/// </summary>
public static class PaymentRequestDirector
{
    /// <summary>Builds a standard domestic payment request.</summary>
    public static PaymentRequest BuildDomesticPayment(string orderId, string customerId, decimal amount)
        => new PaymentRequestBuilder()
            .ForAmount(amount)
            .ForOrder(orderId, customerId)
            .ViaGateway("razorpay")
            .InCurrency("INR")
            .WithRetries(3)
            .AddMetadata("channel", "web")
            .Build();

    /// <summary>Builds an international Stripe payment with saved card.</summary>
    public static PaymentRequest BuildInternationalStripePayment(string orderId, string customerId, decimal amount)
        => new PaymentRequestBuilder()
            .ForAmount(amount)
            .ForOrder(orderId, customerId)
            .ViaGateway("stripe")
            .InCurrency("USD")
            .SaveCardForFutureUse()
            .WithRetries(2)
            .NotifyAt("https://api.myshop.com/webhooks/stripe")
            .AddMetadata("channel", "mobile")
            .Build();
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// In a payment API handler:
//
//   var request = new PaymentRequestBuilder()
//       .ForAmount(dto.Amount)
//       .ForOrder(dto.OrderId, currentUser.Id)
//       .ViaGateway(dto.PreferredGateway)
//       .InCurrency(dto.Currency)
//       .WithCoupon(dto.CouponCode)
//       .Build();    ← all validation fires here, before hitting the gateway
//
//   await paymentService.ProcessAsync(request, cancellationToken);
//
// The PaymentRequest that reaches the gateway is GUARANTEED valid.
// No more null-checks scattered across the payment pipeline.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class BuilderDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Builder Pattern — PaymentRequest ===\n");

        // ── PROBLEM ─────────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: telescoping constructor ──");
        // new NaivePaymentRequest(1200, "INR", "stripe", "ORD-1", null, false, 3)
        // Which null is the coupon? Is 3 retries or something else?
        Console.WriteLine("new NaivePaymentRequest(1200, \"INR\", \"stripe\", \"ORD-1\", null, false, 3) — unreadable!\n");

        // ── BUILDER: minimal required fields ─────────────────────────────────
        Console.WriteLine("── Builder: minimal required fields ──");
        var simple = new PaymentRequestBuilder()
            .ForAmount(500m)
            .ForOrder("ORD-101", "CUST-42")
            .Build();
        Console.WriteLine(simple);

        // ── BUILDER: fully configured ─────────────────────────────────────────
        Console.WriteLine("\n── Builder: fully configured ──");
        var full = new PaymentRequestBuilder()
            .ForAmount(2_999.00m)
            .ForOrder("ORD-102", "CUST-43")
            .ViaGateway("stripe")
            .InCurrency("USD")
            .WithCoupon("SAVE20")
            .NotifyAt("https://api.shop.com/webhooks/payment")
            .WithRetries(2)
            .SaveCardForFutureUse()
            .AddMetadata("campaign", "summer_sale")
            .AddMetadata("channel", "mobile_app")
            .Build();
        Console.WriteLine(full);

        // ── DIRECTOR: pre-built recipes ─────────────────────────────────────
        Console.WriteLine("\n── Director: pre-built domestic payment ──");
        var domestic = PaymentRequestDirector.BuildDomesticPayment("ORD-103", "CUST-44", 1_500m);
        Console.WriteLine(domestic);

        // ── VALIDATION in Build() ──────────────────────────────────────────
        Console.WriteLine("\n── Build() catches missing required fields ──");
        try
        {
            var bad = new PaymentRequestBuilder().Build(); // missing amount + order
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Caught: {ex.Message}");
        }

        Console.WriteLine("\n✅ Builder — understood.");
    }
}
