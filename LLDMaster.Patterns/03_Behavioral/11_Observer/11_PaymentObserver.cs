// ═══════════════════════════════════════════════════════════
// Pattern  : Observer
// Category : Behavioral
// Intent   : Define a one-to-many dependency so that when one object changes state, all dependents are notified.
// Domain   : PaymentProcessed event → Warehouse + Email + Analytics + Loyalty
// Kudvenkat: Video — Observer Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// When payment succeeds, multiple systems need to react:
//   - Warehouse must ship the order
//   - Email must send the receipt
//   - Analytics must record revenue
//   - Loyalty must award points
//
// Without Observer: PaymentService directly calls WarehouseService, EmailService, etc.
// Adding Analytics = edit PaymentService. That's a payment class knowing about analytics. WRONG.
//
// With Observer: PaymentService fires an event. Subscribers react independently.
// Add/remove subscribers without touching PaymentService.
//
// WHEN TO USE:
//   ✔ One event must trigger multiple independent reactions
//   ✔ Publishers should be decoupled from subscribers
//   ✔ Subscribers can be added/removed at runtime
//
// WHEN NOT TO USE:
//   ✘ The chain is complex with ordering dependencies — use a pipeline/saga instead
//   ✘ Observer adds overhead when only one subscriber exists

namespace LLDMaster.Patterns.Behavioral.Observer;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Observer)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — PaymentService knows about every downstream system

public class NaivePaymentService
{
    // 💥 PaymentService is coupled to Warehouse, Email, Analytics, Loyalty
    // 💥 Adding "Fraud Analytics" step = edit this class
    // 💥 Testing PaymentService requires mocking 4 dependencies
    public void ProcessPayment(string orderId, decimal amount)
    {
        Console.WriteLine($"[Naive] Payment processed: {orderId}");
        Console.WriteLine($"[Naive] Notifying warehouse...");
        Console.WriteLine($"[Naive] Sending email...");
        Console.WriteLine($"[Naive] Recording analytics...");
        Console.WriteLine($"[Naive] Awarding loyalty...");
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 2 — OBSERVER (the right way)
// ─────────────────────────────────────────────────────────────

// ── Event data (the "notification" passed to all observers) ───

/// <summary>
/// Immutable event raised when a payment is successfully processed.
/// C# note: record provides structural equality and a clean ToString() for logging.
/// </summary>
public sealed record PaymentProcessedEvent(
    string  OrderId,
    string  CustomerId,
    string  CustomerEmail,
    decimal Amount,
    string  Currency,
    string  TransactionId,
    DateTime ProcessedAt
);

// ── Observer interface ─────────────────────────────────────────

/// <summary>
/// Observer contract. Each subscriber implements this.
/// C# note: async version allows I/O-bound subscribers (email send, DB write) without blocking.
/// </summary>
public interface IPaymentObserver
{
    string Name { get; }
    Task OnPaymentProcessedAsync(PaymentProcessedEvent @event, CancellationToken ct = default);
}

// ── Subject (Observable) ───────────────────────────────────────

/// <summary>
/// The payment processor that publishes events.
/// It holds a list of observers and notifies them all on success.
/// C# note: using List + interface allows runtime subscribe/unsubscribe.
/// For production, prefer MediatR, MassTransit, or Azure Service Bus for full decoupling.
/// </summary>
public sealed class PaymentService
{
    private readonly List<IPaymentObserver> _observers = [];

    /// <summary>Subscribes an observer to payment events.</summary>
    public void Subscribe(IPaymentObserver observer)
    {
        _observers.Add(observer);
        Console.WriteLine($"[PaymentService] Subscribed: {observer.Name}");
    }

    /// <summary>Unsubscribes an observer (e.g., when a feature is disabled).</summary>
    public void Unsubscribe(IPaymentObserver observer)
    {
        _observers.Remove(observer);
        Console.WriteLine($"[PaymentService] Unsubscribed: {observer.Name}");
    }

    /// <summary>Processes payment and notifies all observers on success.</summary>
    public async Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default)
    {
        Console.WriteLine($"\n[PaymentService] Processing ₹{request.Amount} for Order={request.OrderId}");

        // Simulate gateway call
        await Task.Delay(30, ct);
        var txnId = $"txn_{Guid.NewGuid():N[..8]}";
        var result = new PaymentResult(true, txnId, "Success");

        if (result.Success)
        {
            var @event = new PaymentProcessedEvent(
                request.OrderId, request.CustomerId, request.CustomerEmail,
                request.Amount, request.Currency, txnId, DateTime.UtcNow);

            await NotifyAllAsync(@event, ct);
        }

        return result;
    }

    // Notify all observers — fire in parallel for performance
    private async Task NotifyAllAsync(PaymentProcessedEvent @event, CancellationToken ct)
    {
        var tasks = _observers.Select(o => SafeNotifyAsync(o, @event, ct));
        await Task.WhenAll(tasks);
    }

    private static async Task SafeNotifyAsync(IPaymentObserver observer, PaymentProcessedEvent @event, CancellationToken ct)
    {
        try
        {
            await observer.OnPaymentProcessedAsync(@event, ct);
        }
        catch (Exception ex)
        {
            // Never let one failed observer break the payment
            Console.WriteLine($"[PaymentService] Observer '{observer.Name}' failed: {ex.Message}");
        }
    }
}

public sealed record PaymentRequest(
    string OrderId, string CustomerId, string CustomerEmail,
    decimal Amount, string Currency);

public sealed record PaymentResult(bool Success, string TransactionId, string Message);

// ── Concrete Observers ─────────────────────────────────────────

/// <summary>Triggers warehouse to pick and pack the order.</summary>
public sealed class WarehouseObserver : IPaymentObserver
{
    public string Name => "WarehouseObserver";

    public async Task OnPaymentProcessedAsync(PaymentProcessedEvent @event, CancellationToken ct = default)
    {
        await Task.Delay(10, ct); // simulate async DB write
        Console.WriteLine($"  [Warehouse] Pick-pack initiated for Order={@event.OrderId} | Txn={@event.TransactionId}");
    }
}

/// <summary>Sends the payment receipt email.</summary>
public sealed class EmailNotificationObserver : IPaymentObserver
{
    public string Name => "EmailNotificationObserver";

    public async Task OnPaymentProcessedAsync(PaymentProcessedEvent @event, CancellationToken ct = default)
    {
        await Task.Delay(20, ct); // simulate SMTP call
        Console.WriteLine($"  [Email → {@event.CustomerEmail}] Receipt for ₹{@event.Amount} | OrderId={@event.OrderId}");
    }
}

/// <summary>Records revenue metrics in the analytics platform.</summary>
public sealed class AnalyticsObserver : IPaymentObserver
{
    public string Name => "AnalyticsObserver";
    private decimal _totalRevenue;

    public async Task OnPaymentProcessedAsync(PaymentProcessedEvent @event, CancellationToken ct = default)
    {
        await Task.Delay(5, ct);
        _totalRevenue += @event.Amount;
        Console.WriteLine($"  [Analytics] Revenue +₹{@event.Amount} | Total today: ₹{_totalRevenue:N2}");
    }
}

/// <summary>Awards loyalty points for the purchase.</summary>
public sealed class LoyaltyObserver : IPaymentObserver
{
    public string Name => "LoyaltyObserver";

    public async Task OnPaymentProcessedAsync(PaymentProcessedEvent @event, CancellationToken ct = default)
    {
        await Task.Delay(5, ct);
        var points = (int)(@event.Amount / 100);
        Console.WriteLine($"  [Loyalty] +{points} pts → Customer={@event.CustomerId}");
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// Production approaches (ordered by scale):
//
// 1. In-process (MediatR):
//    await mediator.Publish(new PaymentProcessedEvent(...));
//    Each INotificationHandler<PaymentProcessedEvent> is an observer.
//
// 2. Message bus (RabbitMQ / Azure Service Bus):
//    PaymentService publishes a message to "payment.processed" topic.
//    Warehouse, Email, Analytics each have their own queue subscription.
//    Fully decoupled — separate deployable services.
//
// 3. C# events (in-process, tight coupling):
//    public event EventHandler<PaymentProcessedEvent>? PaymentProcessed;
//    Simpler, but no async, no error isolation per subscriber.
//
// The interface-based approach shown here sits between 1 and 3.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class ObserverDemo
{
    public static async Task Run()
    {
        Console.WriteLine("=== Observer Pattern — Payment Events ===\n");

        // ── PROBLEM ─────────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: PaymentService coupled to all downstream systems ──");
        new NaivePaymentService().ProcessPayment("ORD-000", 1000m);
        Console.WriteLine("Adding 'FraudAnalytics' step = edit NaivePaymentService. BAD.\n");

        // ── OBSERVER ─────────────────────────────────────────────────────────
        Console.WriteLine("── Observer: subscribe independently ──");
        var service   = new PaymentService();
        var warehouse = new WarehouseObserver();
        var analytics = new AnalyticsObserver();

        service.Subscribe(warehouse);
        service.Subscribe(new EmailNotificationObserver());
        service.Subscribe(analytics);
        service.Subscribe(new LoyaltyObserver());
        Console.WriteLine();

        // ── Payment 1 ─────────────────────────────────────────────────────────
        var req1 = new PaymentRequest("ORD-001", "CUST-01", "user@shop.com", 2_500m, "₹");
        var res1 = await service.ProcessAsync(req1);
        Console.WriteLine($"Result: {res1.Message}\n");

        // ── Payment 2 ─────────────────────────────────────────────────────────
        var req2 = new PaymentRequest("ORD-002", "CUST-02", "user2@shop.com", 5_000m, "₹");
        var res2 = await service.ProcessAsync(req2);
        Console.WriteLine($"Result: {res2.Message}\n");

        // ── Runtime unsubscribe (e.g., disable warehouse during maintenance) ──
        Console.WriteLine("── Unsubscribe Warehouse (maintenance window) ──");
        service.Unsubscribe(warehouse);

        var req3 = new PaymentRequest("ORD-003", "CUST-01", "user@shop.com", 1_200m, "₹");
        await service.ProcessAsync(req3);
        Console.WriteLine("Warehouse NOT notified for ORD-003 (unsubscribed).");

        Console.WriteLine("\n✅ Observer — understood.");
    }
}
