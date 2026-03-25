// ═══════════════════════════════════════════════════════════
// Pattern  : Composite
// Category : Structural
// Intent   : Compose objects into tree structures to represent part-whole hierarchies.
// Domain   : PaymentBundle — split payments, group charges, mixed wallets
// Kudvenkat: Video 19 — Composite Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// A customer can pay with multiple sources:
//   - Pay ₹500 from wallet + ₹1000 from card = ₹1500 total
//   - Or a "subscription bundle": Netflix + Prime + Hotstar billed together
//   - Or an "order split": pay item by item or all-at-once
//
// The client code should NOT know whether it's dealing with one payment or a bundle.
// Composite lets you treat a single PaymentItem and a PaymentBundle EXACTLY the same way:
//   both expose ProcessAll() → the client calls it once regardless.
//
// WHEN TO USE:
//   ✔ Tree / hierarchical structure (part-whole)
//   ✔ Client should treat leaf and composite uniformly
//   ✔ Recursive structure: a bundle can contain other bundles
//
// WHEN NOT TO USE:
//   ✘ Flat list only — a simple List<T> is enough
//   ✘ Children types are very different — uniform interface becomes forced and leaky

namespace LLDMaster.Patterns.Structural.Composite;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Composite)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — client must check "is it a single or a bundle?" everywhere

public class NaivePaymentProcessor
{
    public void Process(object payment)
    {
        // 💥 PROBLEM: type-checking everywhere, can't nest bundles inside bundles
        if (payment is NaiveSinglePayment single)
        {
            Console.WriteLine($"[Single] Pay {single.Amount:C}");
        }
        else if (payment is List<NaiveSinglePayment> bundle)
        {
            foreach (var p in bundle)
                Console.WriteLine($"[Bundle item] Pay {p.Amount:C}");
        }
        // what about bundle of bundles? → need another else if. Never ending.
    }
}
public record NaiveSinglePayment(decimal Amount, string Method);

// ─────────────────────────────────────────────────────────────
// SECTION 2 — COMPOSITE (the right way)
// ─────────────────────────────────────────────────────────────

// ── Component — the uniform interface ─────────────────────────

/// <summary>
/// Component interface — both leaf (SinglePayment) and composite (PaymentBundle) implement this.
/// C# note: interface keeps it lightweight; use abstract class if shared behaviour exists.
/// </summary>
public interface IPaymentComponent
{
    string Name { get; }
    decimal TotalAmount { get; }
    PaymentSummary Process();
    void Display(int indent = 0);
}

public sealed record PaymentSummary(string Name, decimal Amount, bool Success, List<PaymentSummary> Children);

// ── Leaf — a single indivisible payment ───────────────────────

/// <summary>
/// Leaf node — represents a single payment charge.
/// Cannot contain children; just executes and returns.
/// </summary>
public sealed class SinglePayment : IPaymentComponent
{
    public string  Name        { get; }
    public decimal TotalAmount { get; }
    public string  GatewayType { get; }

    public SinglePayment(string name, decimal amount, string gatewayType)
        => (Name, TotalAmount, GatewayType) = (name, amount, gatewayType);

    public PaymentSummary Process()
    {
        // Simulate gateway call
        Console.WriteLine($"{new string(' ', 4)}[{GatewayType}] Processing: {Name} — ₹{TotalAmount:N2}");
        return new PaymentSummary(Name, TotalAmount, Success: true, Children: []);
    }

    public void Display(int indent = 0)
        => Console.WriteLine($"{new string(' ', indent)}• {Name} [{GatewayType}] ₹{TotalAmount:N2}");
}

// ── Composite — a bundle containing other components ──────────

/// <summary>
/// Composite node — can hold any mix of <see cref="SinglePayment"/> and other <see cref="PaymentBundle"/> items.
/// ProcessAll() recurses through the tree — client code is the same for any depth.
/// </summary>
public sealed class PaymentBundle : IPaymentComponent
{
    private readonly List<IPaymentComponent> _children = [];

    public string  Name        { get; }
    public string  Description { get; }

    // TotalAmount is always the sum of children — computed, never stored
    public decimal TotalAmount => _children.Sum(c => c.TotalAmount);

    public PaymentBundle(string name, string description = "")
        => (Name, Description) = (name, description);

    /// <summary>Adds a payment item (leaf or another bundle) to this bundle.</summary>
    public PaymentBundle Add(IPaymentComponent component)
    {
        _children.Add(component);
        return this; // fluent API
    }

    /// <summary>Removes an item from the bundle.</summary>
    public void Remove(IPaymentComponent component) => _children.Remove(component);

    public PaymentSummary Process()
    {
        Console.WriteLine($"{new string(' ', 2)}[Bundle] Processing: {Name} (₹{TotalAmount:N2} total)");
        var childSummaries = _children.Select(c => c.Process()).ToList();
        var allSuccess = childSummaries.All(s => s.Success);
        return new PaymentSummary(Name, TotalAmount, allSuccess, childSummaries);
    }

    public void Display(int indent = 0)
    {
        Console.WriteLine($"{new string(' ', indent)}📦 {Name} — ₹{TotalAmount:N2}");
        foreach (var child in _children)
            child.Display(indent + 4);
    }
}

// ── Client — uses IPaymentComponent, unaware of leaf vs composite ──

/// <summary>
/// Checkout service — works with any IPaymentComponent.
/// Single payment? Bundle? Nested bundle? Doesn't matter — same call.
/// </summary>
public sealed class CheckoutService
{
    public void Checkout(IPaymentComponent payment)
    {
        Console.WriteLine($"\n=== Checkout: {payment.Name} | Total: ₹{payment.TotalAmount:N2} ===");
        payment.Display();
        Console.WriteLine("\nProcessing:");
        var summary = payment.Process();
        Console.WriteLine($"Result: {summary.Name} — Success={summary.Success}, Amount=₹{summary.Amount:N2}");
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// Subscription management:
//   var familyPlan = new PaymentBundle("Family Plan - March 2026")
//       .Add(new SinglePayment("Netflix",  649m, "stripe"))
//       .Add(new SinglePayment("Amazon Prime", 299m, "stripe"))
//       .Add(new SinglePayment("Hotstar",  299m, "razorpay"));
//
// Split payment (wallet + card):
//   var splitPayment = new PaymentBundle("Order ORD-500 Split Pay")
//       .Add(new SinglePayment("Wallet debit",   500m, "paytm_wallet"))
//       .Add(new SinglePayment("Card debit",    1200m, "stripe"));
//
// checkout.Checkout(splitPayment); — same line regardless of structure

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class CompositeDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Composite Pattern — Payment Bundle ===\n");

        var checkout = new CheckoutService();

        // ── Leaf: single payment ─────────────────────────────────────────────
        var single = new SinglePayment("iPhone 15 - Card Payment", 89_990m, "stripe");
        checkout.Checkout(single);

        // ── Composite: split payment (wallet + card) ──────────────────────────
        var splitPayment = new PaymentBundle("Order ORD-501 — Split Pay")
            .Add(new SinglePayment("Paytm Wallet",  1_000m, "paytm_wallet"))
            .Add(new SinglePayment("HDFC Credit Card", 3_000m, "razorpay"));
        checkout.Checkout(splitPayment);

        // ── Nested composite: subscription bundle inside an order ─────────────
        var subscriptions = new PaymentBundle("Annual Subscriptions")
            .Add(new SinglePayment("Netflix",      7_788m, "stripe"))   // 649×12
            .Add(new SinglePayment("Amazon Prime", 1_499m, "stripe"));

        var yearEndBundle = new PaymentBundle("Year-End Payment Bundle")
            .Add(new SinglePayment("MacBook Pro",    1_99_990m, "stripe"))
            .Add(subscriptions);  // bundle inside bundle — Composite power!

        checkout.Checkout(yearEndBundle);

        Console.WriteLine("\n✅ Composite — understood.");
    }
}
