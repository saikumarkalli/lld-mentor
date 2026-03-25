// ═══════════════════════════════════════════════════════════
// Pattern  : Prototype
// Category : Creational
// Intent   : Create new objects by copying an existing object (prototype) instead of creating from scratch.
// Domain   : Recurring Payment Templates — clone & customise instead of rebuild
// Kudvenkat: Video 14–15 — Prototype Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// A subscription platform has "recurring payment templates":
//   Netflix monthly plan, AWS auto-pay, EMI for a loan.
// Each template shares 80% of the same fields (gateway, currency, retry policy).
// Only amount or billing date changes each cycle.
//
// Without Prototype: re-run the full Builder chain every billing cycle.
// With Prototype: clone the approved template, change only what's different.
//
// WHEN TO USE:
//   ✔ Object creation is expensive (many default fields, validation, DB lookup)
//   ✔ Many objects share the same baseline; only a few fields differ
//   ✔ You want to snapshot + restore an object's state
//
// WHEN NOT TO USE:
//   ✘ Objects have no shared structure — cloning adds no value
//   ✘ Deep copy of complex object graphs is error-prone without clear ownership

namespace LLDMaster.Patterns.Creational.Prototype;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Prototype)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — rebuilding the full payment config every billing cycle

public static class NaiveRecurringBilling
{
    public static void Bill(string customerId, decimal amount, int month)
    {
        // 💥 PROBLEM: same 10 fields re-specified every call
        // Miss one field → billing fails silently (wrong webhook, wrong retry count)
        var config = new
        {
            Gateway       = "stripe",
            Currency      = "INR",
            CustomerId    = customerId,
            Amount        = amount,
            MaxRetries    = 3,
            WebhookUrl    = "https://api.shop.com/webhooks/billing",
            SaveCard      = true,
            Description   = $"Subscription month {month}",
            IdempotencyKey = Guid.NewGuid().ToString("N"),
        };
        Console.WriteLine($"[Naive] Billing month {month}: {config.Description}, {config.Currency}{config.Amount}");
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 2 — PROTOTYPE (the right way)
// ─────────────────────────────────────────────────────────────

/// <summary>Prototype interface — every cloneable payment template must implement this.</summary>
public interface IPaymentTemplate
{
    /// <summary>Shallow clone — returns a new instance with the same field values.</summary>
    IPaymentTemplate ShallowClone();

    /// <summary>Deep clone — returns a completely independent copy, including nested objects.</summary>
    IPaymentTemplate DeepClone();
}

/// <summary>
/// A recurring payment template.
/// Once approved/configured, it is cloned for each billing cycle.
/// Only the fields that change (Amount, BillingDate, IdempotencyKey) are overwritten after cloning.
///
/// C# note: MemberwiseClone() is used for shallow copy — it copies value types and references.
/// For reference-type fields (Metadata dictionary), a deep clone is needed to avoid shared state.
/// </summary>
public sealed class RecurringPaymentTemplate : IPaymentTemplate
{
    public string  CustomerId     { get; set; } = string.Empty;
    public string  SubscriptionId { get; set; } = string.Empty;
    public string  GatewayType    { get; set; } = "stripe";
    public string  Currency       { get; set; } = "INR";
    public decimal Amount         { get; set; }
    public int     MaxRetries     { get; set; } = 3;
    public bool    SaveCard       { get; set; } = true;
    public string  WebhookUrl     { get; set; } = string.Empty;
    public string  IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
    public string  Description    { get; set; } = string.Empty;

    // Reference type — MUST be deep-cloned to avoid templates sharing the same dict
    public Dictionary<string, string> Metadata { get; set; } = [];

    // ── Shallow clone: fast, but Metadata dict is SHARED ──────────────────
    /// <summary>
    /// Shallow clone via <see cref="object.MemberwiseClone"/>.
    /// C# note: MemberwiseClone is protected — we expose it through this interface method.
    /// WARNING: Metadata dictionary is shared. Mutating it on clone affects the original!
    /// </summary>
    public IPaymentTemplate ShallowClone()
    {
        var clone = (RecurringPaymentTemplate)MemberwiseClone();
        // Regenerate idempotency key so each cycle is unique (required by Stripe)
        clone.IdempotencyKey = Guid.NewGuid().ToString("N");
        return clone;
    }

    // ── Deep clone: safe, Metadata is independent ─────────────────────────
    /// <summary>
    /// Deep clone — creates a fully independent copy.
    /// Safe to mutate any field on the clone without affecting the master template.
    /// </summary>
    public IPaymentTemplate DeepClone()
    {
        return new RecurringPaymentTemplate
        {
            CustomerId     = CustomerId,
            SubscriptionId = SubscriptionId,
            GatewayType    = GatewayType,
            Currency       = Currency,
            Amount         = Amount,
            MaxRetries     = MaxRetries,
            SaveCard       = SaveCard,
            WebhookUrl     = WebhookUrl,
            Description    = Description,
            IdempotencyKey = Guid.NewGuid().ToString("N"), // always fresh
            // Deep copy the dictionary — new instance, same key-value pairs
            Metadata       = new Dictionary<string, string>(Metadata),
        };
    }

    public override string ToString()
        => $"Template[{SubscriptionId}] | Customer={CustomerId} | {Currency}{Amount} | " +
           $"Gateway={GatewayType} | Retries={MaxRetries} | Key={IdempotencyKey[..8]}...";
}

// ── Template Registry — stores master templates ───────────────

/// <summary>
/// Registry of approved master templates.
/// Callers clone a template and override only what's different.
/// C# note: Dictionary<string, RecurringPaymentTemplate> acts as a prototype store.
/// </summary>
public sealed class PaymentTemplateRegistry
{
    private readonly Dictionary<string, RecurringPaymentTemplate> _templates = [];

    /// <summary>Registers a master template under a named key.</summary>
    public void Register(string key, RecurringPaymentTemplate template)
        => _templates[key] = template;

    /// <summary>
    /// Returns a deep clone of the named template, ready for customisation.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Template not found.</exception>
    public RecurringPaymentTemplate GetClone(string key)
    {
        if (!_templates.TryGetValue(key, out var template))
            throw new KeyNotFoundException($"Template '{key}' not registered.");

        return (RecurringPaymentTemplate)template.DeepClone();
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// In a billing scheduler (Hangfire / Azure Functions):
//
//   var template = registry.GetClone("netflix_monthly");
//   template.Amount         = currentMonthAmount;   // price can vary
//   template.Description    = $"Netflix - {DateTime.UtcNow:MMMM yyyy}";
//   template.Metadata["invoiceDate"] = DateTime.UtcNow.ToString("O");
//
//   await gatewayFactory.Create(template.GatewayType)
//                        .Charge(template.Amount, template.Currency, template.IdempotencyKey);
//
// The master template is NEVER mutated.
// 1000 subscriptions billing on the same day = 1000 clones of the same template.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class PrototypeDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Prototype Pattern — Recurring Payment Templates ===\n");

        // ── PROBLEM ─────────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: rebuilding full config every billing cycle ──");
        NaiveRecurringBilling.Bill("CUST-01", 499m, 1);
        NaiveRecurringBilling.Bill("CUST-01", 499m, 2);  // same 10 fields re-specified, error-prone
        Console.WriteLine();

        // ── Create master template once ───────────────────────────────────────
        Console.WriteLine("── Setup: create master templates once ──");
        var netflixMaster = new RecurringPaymentTemplate
        {
            CustomerId     = "CUST-01",
            SubscriptionId = "SUB-NETFLIX-001",
            GatewayType    = "stripe",
            Currency       = "INR",
            Amount         = 649m,
            MaxRetries     = 3,
            SaveCard       = true,
            WebhookUrl     = "https://api.shop.com/webhooks/billing",
            Description    = "Netflix Premium",
            Metadata       = new() { ["plan"] = "premium", ["source"] = "web" },
        };
        Console.WriteLine($"Master: {netflixMaster}\n");

        // Register in registry
        var registry = new PaymentTemplateRegistry();
        registry.Register("netflix_monthly", netflixMaster);

        // ── Clone for billing cycles ──────────────────────────────────────────
        Console.WriteLine("── Billing cycle 1 (clone + override description only) ──");
        var cycle1 = registry.GetClone("netflix_monthly");
        cycle1.Description = "Netflix Premium — March 2026";
        Console.WriteLine($"Cycle 1: {cycle1}");

        Console.WriteLine("\n── Billing cycle 2 (price revision + new metadata) ──");
        var cycle2 = registry.GetClone("netflix_monthly");
        cycle2.Amount = 799m;  // price hike
        cycle2.Description = "Netflix Premium — April 2026";
        cycle2.Metadata["priceRevision"] = "true";
        Console.WriteLine($"Cycle 2: {cycle2}");

        // ── Prove master is untouched ─────────────────────────────────────────
        Console.WriteLine($"\nMaster after clones: {netflixMaster}");
        Console.WriteLine($"Master metadata has 'priceRevision': {netflixMaster.Metadata.ContainsKey("priceRevision")}");

        // ── Shallow vs Deep clone gotcha ─────────────────────────────────────
        Console.WriteLine("\n── Shallow clone danger: shared Metadata dict ──");
        var shallow = (RecurringPaymentTemplate)netflixMaster.ShallowClone();
        shallow.Metadata["danger"] = "shared!";  // mutates master's dict too!
        Console.WriteLine($"Master has 'danger' key after shallow clone mutation: {netflixMaster.Metadata.ContainsKey("danger")}");
        Console.WriteLine("Deep clone prevents this — use DeepClone() in production.");

        Console.WriteLine("\n✅ Prototype — understood.");
    }
}
