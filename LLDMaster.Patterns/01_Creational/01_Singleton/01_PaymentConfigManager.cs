// ═══════════════════════════════════════════════════════════
// Pattern  : Singleton
// Category : Creational
// Intent   : Ensure a class has only one instance and provide a global point of access to it.
// Domain   : Payment Module — PaymentConfigManager, ExchangeRateCache, CartSession
// Kudvenkat: Video 2–7 — Singleton Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// In a payment system, several resources must exist exactly ONCE:
//
//   PaymentConfigManager  → holds Stripe/PayPal API keys loaded from appsettings.
//                           Two instances = one has stale keys = silent auth failures.
//
//   ExchangeRateCache     → fetches USD/EUR/INR rates from an external API.
//                           Two instances = two HTTP calls every request = rate-limit ban.
//
//   CartSession           → one cart per user session.
//                           Two instances = items added in checkout disappear in summary.
//
// The Singleton pattern solves all three: guarantee ONE instance, share it globally.
//
// WHEN TO USE:
//   ✔ The resource must be shared and consistent across the whole app (config, cache, session)
//   ✔ Creating multiple instances causes bugs (duplicate HTTP calls, stale state, file locks)
//   ✔ The "one instance" rule must be enforced at the language level, not by convention
//
// WHEN NOT TO USE:
//   ✘ "One instance per request" or "one per user" — use DI Scoped/Transient instead
//   ✘ Distributed apps (each pod gets its own Singleton — not truly one)
//   ✘ When the class holds mutable state that causes test pollution between test runs

namespace LLDMaster.Patterns.Creational.Singleton;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Singleton)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — each service creates its own instance, state is fragmented

/// <summary>
/// NAIVE PaymentConfigManager — do NOT use.
/// Every service that creates this gets its OWN copy loaded independently.
/// </summary>
public class NaivePaymentConfigManager
{
    // 💥 PROBLEM 1: each caller loads config independently from disk/env
    // 💥 PROBLEM 2: if Stripe rotates keys mid-session, half the app uses old key, half new
    // 💥 PROBLEM 3: no single place to update — bug hunting nightmare
    public string StripeApiKey   { get; } = Environment.GetEnvironmentVariable("STRIPE_KEY") ?? "sk_test_default";
    public string PayPalClientId { get; } = Environment.GetEnvironmentVariable("PAYPAL_ID")  ?? "pp_client_default";
    public decimal MaxChargeLimit { get; } = 500_000m;

    public NaivePaymentConfigManager()
        => Console.WriteLine("[NaiveConfig] Loaded from env — a new copy created!");
}

// 💥 BUG DEMONSTRATION (conceptual):
//   var serviceA = new NaivePaymentConfigManager();  // loads from env
//   var serviceB = new NaivePaymentConfigManager();  // loads again — two separate objects
//   serviceA == serviceB → FALSE — different references, different memory, potential drift

// ─────────────────────────────────────────────────────────────
// SECTION 2 — SINGLETON IMPLEMENTATIONS (4 C# flavours)
// ─────────────────────────────────────────────────────────────

// ══════════════════════════════════════════════════════════════
// FLAVOUR A — volatile + double-checked locking
// USE WHEN: you need lazy init + understand the threading mechanics
// PAYMENT USE CASE: PaymentConfigManager — loaded lazily on first payment request
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Payment configuration Singleton — holds gateway credentials and limits.
/// Loaded ONCE from environment/appsettings; every service shares the same object.
///
/// C# notes:
///   • <c>volatile</c> — prevents CPU instruction reordering; ensures the null-check
///     reads a fully constructed object, not a half-initialised one on another CPU core.
///   • Double-checked locking — outer null-check avoids expensive lock on every call;
///     inner null-check handles the race condition between the two checks.
///   • <c>sealed</c> — subclasses could call base constructor and break the one-instance rule.
/// </summary>
public sealed class PaymentConfigManager
{
    private static volatile PaymentConfigManager? _instance;
    private static readonly object _lock = new();

    // ── Configuration properties (immutable after construction) ──────────────
    public string   StripeApiKey      { get; }
    public string   StripeWebhookSecret { get; }
    public string   PayPalClientId    { get; }
    public string   PayPalClientSecret { get; }
    public string   BankCode          { get; }
    public decimal  MaxSingleCharge   { get; }   // fraud guard: ₹5,00,000
    public int      MaxRetryAttempts  { get; }
    public string   WebhookBaseUrl    { get; }

    // Private constructor — loads configuration ONCE
    private PaymentConfigManager()
    {
        // In production: inject IConfiguration and read from appsettings.json
        StripeApiKey       = Environment.GetEnvironmentVariable("STRIPE_KEY")    ?? "sk_test_ABC123";
        StripeWebhookSecret = Environment.GetEnvironmentVariable("STRIPE_WHSEC") ?? "whsec_XYZ";
        PayPalClientId     = Environment.GetEnvironmentVariable("PAYPAL_ID")     ?? "pp_client_XYZ";
        PayPalClientSecret = Environment.GetEnvironmentVariable("PAYPAL_SECRET") ?? "pp_secret_XYZ";
        BankCode           = Environment.GetEnvironmentVariable("BANK_CODE")     ?? "HDFC";
        MaxSingleCharge    = 500_000m;
        MaxRetryAttempts   = 3;
        WebhookBaseUrl     = "https://api.myshop.com/webhooks";
        Console.WriteLine("[PaymentConfigManager] Config loaded exactly once.");
    }

    /// <summary>The one and only PaymentConfigManager instance.</summary>
    public static PaymentConfigManager Instance
    {
        get
        {
            if (_instance is null)                  // ← fast path: no lock on every call
            {
                lock (_lock)
                {
                    _instance ??= new PaymentConfigManager(); // ← one-time init
                    // ??= is null-coalescing assignment: if _instance is still null, create it
                    // The inner check handles the thread that was waiting on lock — it
                    // sees the instance already created by the thread before it.
                }
            }
            return _instance;
        }
    }

    /// <summary>Returns the base webhook URL for the given gateway.</summary>
    public string GetWebhookUrl(string gatewayType)
        => $"{WebhookBaseUrl}/{gatewayType.ToLowerInvariant()}";
}

// ══════════════════════════════════════════════════════════════
// FLAVOUR B — Lazy<T>  ← PREFERRED in .NET 8+
// USE WHEN: you want lazy init with ZERO threading boilerplate
// PAYMENT USE CASE: ExchangeRateCache — one shared rate table for all payment processors
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Exchange rate cache Singleton.
/// All payment processors (Stripe, PayPal, BankTransfer) share ONE rate table.
/// Two instances = two HTTP calls to the rates API per request = rate-limit ban.
///
/// C# note: <c>Lazy&lt;T&gt;</c> with default <c>LazyThreadSafetyMode.ExecutionAndPublication</c>
/// is thread-safe + lazy — the factory runs at most once, even under concurrent access.
/// This is cleaner and more idiomatic than the volatile+lock approach for most cases.
/// </summary>
public sealed class ExchangeRateCache
{
    // Lazy<T> — the CLR guarantees the factory runs exactly once, thread-safely
    private static readonly Lazy<ExchangeRateCache> _lazy =
        new(() => new ExchangeRateCache());

    private readonly Dictionary<string, decimal> _rates;
    private DateTime _loadedAt;

    private ExchangeRateCache()
    {
        // In production: call an exchange rate API (e.g., Open Exchange Rates)
        // Here we simulate rates loaded at startup
        _rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = 83.12m,    // 1 USD = ₹83.12
            ["EUR"] = 90.45m,    // 1 EUR = ₹90.45
            ["GBP"] = 105.30m,   // 1 GBP = ₹105.30
            ["AED"] = 22.63m,    // 1 AED = ₹22.63
        };
        _loadedAt = DateTime.UtcNow;
        Console.WriteLine($"[ExchangeRateCache] Rates loaded at {_loadedAt:HH:mm:ss}");
    }

    /// <summary>The one shared exchange rate cache.</summary>
    public static ExchangeRateCache Instance => _lazy.Value;

    /// <summary>Converts <paramref name="amount"/> from foreign currency to INR.</summary>
    public decimal ToInr(decimal amount, string currencyCode)
    {
        if (!_rates.TryGetValue(currencyCode, out var rate))
            throw new NotSupportedException($"Currency '{currencyCode}' not in cache.");
        return Math.Round(amount * rate, 2);
    }

    /// <summary>Returns the INR rate for the given currency.</summary>
    public decimal GetRate(string currencyCode)
        => _rates.GetValueOrDefault(currencyCode.ToUpperInvariant(), 1m);

    /// <summary>Indicates when the rates were last refreshed.</summary>
    public DateTime LoadedAt => _loadedAt;
}

// ══════════════════════════════════════════════════════════════
// FLAVOUR C — Static field initialiser (eager)
// USE WHEN: instance is cheap to create and you want the SIMPLEST possible Singleton
// PAYMENT USE CASE: PaymentAuditLogger — one logger, writes to one destination
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Payment audit logger Singleton.
/// Writes every charge/refund event to one shared log store.
/// Multiple instances = out-of-order log entries, possible file-write conflicts.
///
/// C# note: The CLR guarantees static field initialisation is thread-safe and runs
/// exactly once (before the type is first used). No lock needed. Eager = created
/// at class-load time, not on first access — fine when creation is cheap.
/// </summary>
public sealed class PaymentAuditLogger
{
    // CLR initialises this once, thread-safely. No volatile or lock needed.
    private static readonly PaymentAuditLogger _instance = new();

    private readonly List<AuditEntry> _log = [];

    private PaymentAuditLogger()
        => Console.WriteLine("[PaymentAuditLogger] Audit logger initialised.");

    /// <summary>The one shared audit logger.</summary>
    public static PaymentAuditLogger Instance => _instance;

    /// <summary>Records a payment event in the immutable audit log.</summary>
    public void Log(string orderId, string customerId, decimal amount, string status, string transactionId)
    {
        var entry = new AuditEntry(DateTime.UtcNow, orderId, customerId, amount, status, transactionId);
        _log.Add(entry);
        Console.WriteLine($"  [AUDIT] {entry.Timestamp:HH:mm:ss} | {orderId} | {status} | ₹{amount:N2} | {transactionId}");
    }

    /// <summary>Returns all audit entries (read-only).</summary>
    public IReadOnlyList<AuditEntry> GetLog() => _log.AsReadOnly();
}

/// <summary>Immutable audit log entry.</summary>
public sealed record AuditEntry(
    DateTime Timestamp,
    string   OrderId,
    string   CustomerId,
    decimal  Amount,
    string   Status,
    string   TransactionId
);

// ══════════════════════════════════════════════════════════════
// FLAVOUR D — DI-friendly Singleton  ← PRODUCTION RECOMMENDED
// USE WHEN: ASP.NET Core app — always prefer this over static Instance
// PAYMENT USE CASE: CartSession — one cart per session, testable, mockable
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Cart session interface — abstracts the cart for testability.
/// C# note: interface-based Singleton via DI means you can inject
/// a MockCartSession in unit tests — impossible with static Instance.
/// </summary>
public interface ICartSession
{
    void AddItem(CartItem item);
    void RemoveItem(string productId);
    IReadOnlyList<CartItem> GetItems();
    decimal GetTotal();
    void Clear();
    string SessionId { get; }
}

/// <summary>Cart item in the payment flow.</summary>
public sealed record CartItem(
    string  ProductId,
    string  ProductName,
    int     Quantity,
    decimal UnitPrice
)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>
/// Concrete cart session registered as Singleton in DI.
/// The DI container manages the lifetime — no static member needed here.
/// Registered as: <c>builder.Services.AddSingleton&lt;ICartSession, CartSession&gt;()</c>
///
/// C# note: class is NOT sealed here intentionally — DI containers can generate
/// proxy subclasses for AOP (e.g., lazy proxies, interceptors). If you don't need
/// that, add sealed for a tiny perf gain and to prevent unintended inheritance.
/// </summary>
public class CartSession : ICartSession
{
    private readonly List<CartItem> _items = [];

    // DI container calls this constructor — it is public
    public CartSession() => SessionId = Guid.NewGuid().ToString("N")[..12];

    public string SessionId { get; }

    public void AddItem(CartItem item)
    {
        // Merge quantity if same product already in cart
        var existing = _items.FirstOrDefault(i => i.ProductId == item.ProductId);
        if (existing is not null)
        {
            _items.Remove(existing);
            _items.Add(existing with { Quantity = existing.Quantity + item.Quantity });
        }
        else
        {
            _items.Add(item);
        }
        Console.WriteLine($"  [Cart:{SessionId}] Added {item.ProductName} ×{item.Quantity} @ ₹{item.UnitPrice}");
    }

    public void RemoveItem(string productId)
    {
        _items.RemoveAll(i => i.ProductId == productId);
        Console.WriteLine($"  [Cart:{SessionId}] Removed product {productId}");
    }

    public IReadOnlyList<CartItem> GetItems() => _items.AsReadOnly();

    public decimal GetTotal() => _items.Sum(i => i.LineTotal);

    public void Clear()
    {
        _items.Clear();
        Console.WriteLine($"  [Cart:{SessionId}] Cleared.");
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE IN ASP.NET CORE
// ─────────────────────────────────────────────────────────────
//
// Program.cs / Startup.cs:
//
//   // DI-friendly Singleton (RECOMMENDED for all three below):
//   builder.Services.AddSingleton<ICartSession, CartSession>();
//
//   // Or register the static-based ones as singletons via factory overload:
//   builder.Services.AddSingleton(_ => PaymentConfigManager.Instance);
//   builder.Services.AddSingleton(_ => ExchangeRateCache.Instance);
//   builder.Services.AddSingleton(_ => PaymentAuditLogger.Instance);
//
// Controller / Service — inject via constructor (never use 'new' or .Instance in app code):
//
//   public class PaymentController(
//       ICartSession cart,
//       PaymentConfigManager config,   // or wrap in an interface
//       ExchangeRateCache rateCache,
//       PaymentAuditLogger audit)
//   {
//       public IActionResult Checkout()
//       {
//           var items = cart.GetItems();
//           var key   = config.StripeApiKey;          // same config everywhere
//           var inr   = rateCache.ToInr(items.Sum(i => i.LineTotal), "USD");
//           audit.Log(orderId, userId, inr, "PENDING", "");
//           // ...
//       }
//   }
//
// LIFETIME COMPARISON:
//   AddSingleton()  → ONE instance for entire app lifetime  ← use for config, cache, logger
//   AddScoped()     → ONE instance per HTTP request         ← use for DbContext, Unit of Work
//   AddTransient()  → NEW instance every time injected      ← use for lightweight stateless services

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class SingletonDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Singleton Pattern — Payment Module ===\n");

        // ── PROBLEM: naive config creates multiple instances ──────────────────
        Console.WriteLine("── PROBLEM: NaivePaymentConfigManager (multiple instances) ──");
        var naiveA = new NaivePaymentConfigManager();
        var naiveB = new NaivePaymentConfigManager();
        Console.WriteLine($"naiveA == naiveB (ReferenceEquals): {ReferenceEquals(naiveA, naiveB)}");
        Console.WriteLine("Two separate config loads. If env changes mid-run → they diverge. BAD.\n");

        // ── FLAVOUR A: volatile+lock → PaymentConfigManager ───────────────────
        Console.WriteLine("── Flavour A: volatile+lock → PaymentConfigManager ──");
        var configA = PaymentConfigManager.Instance;
        var configB = PaymentConfigManager.Instance;  // same object — no second load
        Console.WriteLine($"configA == configB : {ReferenceEquals(configA, configB)}");
        Console.WriteLine($"Stripe key preview : {configA.StripeApiKey[..7]}***");
        Console.WriteLine($"Max charge limit   : ₹{configA.MaxSingleCharge:N0}");
        Console.WriteLine($"Webhook URL        : {configA.GetWebhookUrl("stripe")}\n");

        // ── FLAVOUR B: Lazy<T> → ExchangeRateCache ────────────────────────────
        Console.WriteLine("── Flavour B: Lazy<T> → ExchangeRateCache ──");
        var cacheA = ExchangeRateCache.Instance;
        var cacheB = ExchangeRateCache.Instance;
        Console.WriteLine($"cacheA == cacheB : {ReferenceEquals(cacheA, cacheB)}");

        decimal usdAmount = 150.00m;
        decimal inrAmount = cacheA.ToInr(usdAmount, "USD");
        Console.WriteLine($"${usdAmount} USD → ₹{inrAmount} (rate: {cacheA.GetRate("USD")})");

        decimal eurAmount = 200.00m;
        decimal inrFromEur = cacheB.ToInr(eurAmount, "EUR");  // same cache instance
        Console.WriteLine($"€{eurAmount} EUR → ₹{inrFromEur} (rate: {cacheB.GetRate("EUR")})\n");

        // ── FLAVOUR C: Static field → PaymentAuditLogger ──────────────────────
        Console.WriteLine("── Flavour C: Static field → PaymentAuditLogger ──");
        var loggerA = PaymentAuditLogger.Instance;
        var loggerB = PaymentAuditLogger.Instance;
        Console.WriteLine($"loggerA == loggerB : {ReferenceEquals(loggerA, loggerB)}");

        // Simulate payment events logged from different services
        loggerA.Log("ORD-001", "CUST-01",  2_500m, "SUCCESS",  "txn_abc123");
        loggerB.Log("ORD-002", "CUST-02", 15_000m, "FAILED",   "txn_def456"); // loggerB = same object
        loggerA.Log("ORD-003", "CUST-01",  8_999m, "REFUNDED", "txn_ghi789");

        Console.WriteLine($"Total audit entries (all in ONE log): {loggerA.GetLog().Count}\n");

        // ── FLAVOUR D: DI-friendly CartSession ────────────────────────────────
        Console.WriteLine("── Flavour D: DI-friendly CartSession ──");
        Console.WriteLine("(Simulating DI by manually creating one instance and sharing it)");

        ICartSession cart = new CartSession();  // DI container does this in production
        Console.WriteLine($"Session ID: {cart.SessionId}");

        // Two different "services" using the SAME cart instance (as DI would provide)
        cart.AddItem(new CartItem("PROD-001", "iPhone 15",      1, 89_990m));
        cart.AddItem(new CartItem("PROD-002", "AirPods Pro",    1, 24_900m));
        cart.AddItem(new CartItem("PROD-001", "iPhone 15",      1, 89_990m)); // qty merge
        cart.AddItem(new CartItem("PROD-003", "MagSafe Charger", 2,  4_900m));

        Console.WriteLine($"\nCart items: {cart.GetItems().Count} lines");
        foreach (var item in cart.GetItems())
            Console.WriteLine($"  {item.ProductName} ×{item.Quantity} = ₹{item.LineTotal:N2}");
        Console.WriteLine($"Cart total : ₹{cart.GetTotal():N2}");

        // Exchange rate conversion at checkout
        var rate   = ExchangeRateCache.Instance;
        var inrTotal = cart.GetTotal();
        Console.WriteLine($"USD equivalent: ${(inrTotal / rate.GetRate("USD")):N2}");

        // Audit the checkout intent
        PaymentAuditLogger.Instance.Log("ORD-004", "CUST-03", inrTotal, "PENDING", "txn_pending");

        Console.WriteLine($"\nFinal audit log count: {PaymentAuditLogger.Instance.GetLog().Count}");
        Console.WriteLine("\n✅ Singleton — understood.");
    }
}
