// ═══════════════════════════════════════════════════════════
// Pattern  : Decorator
// Category : Structural
// Intent   : Attach additional responsibilities to an object dynamically without modifying its class.
// Domain   : PaymentProcessor pipeline — logging / retry / fraud-check / audit wrappers
// Kudvenkat: Video 20 — Decorator Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// A raw payment processor just charges the gateway.
// In production you also need: logging, fraud check, retry on failure, audit trail.
// Putting all of this in one class = God object.
// Inheritance approach = LoggingRetryFraudAuditPaymentProcessor — combinatorial explosion.
//
// Decorator wraps the processor, adds ONE responsibility, and passes through the rest.
// Compose the pipeline at startup: Audit(Fraud(Retry(Logging(RealGateway)))).
// Each decorator is independently testable and replaceable.
//
// WHEN TO USE:
//   ✔ Add cross-cutting concerns (logging, caching, retry, security) without subclassing
//   ✔ Multiple combinations of behaviours needed (wrap freely)
//   ✔ Must respect Open/Closed: extend without modifying existing code
//
// WHEN NOT TO USE:
//   ✘ Only one "extra" behaviour — just add it to the class directly
//   ✘ The core logic is tightly coupled to the cross-cutting concern

namespace LLDMaster.Patterns.Structural.Decorator;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Decorator)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — everything crammed into one class

public class NaivePaymentProcessor
{
    public void Charge(decimal amount, string currency, string orderId)
    {
        // 💥 This method does: logging + fraud + retry + actual charge + audit
        // Adding "notification" = edit this method again. Violates Single Responsibility.
        Console.WriteLine($"[LOG] Charging {amount:C}");
        Console.WriteLine($"[FRAUD] Checking {orderId}");
        Console.WriteLine($"[GATEWAY] Charging {amount:C} {currency}");
        Console.WriteLine($"[AUDIT] Recording {orderId}");
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 2 — DECORATOR (the right way)
// ─────────────────────────────────────────────────────────────

// ── Component interface ───────────────────────────────────────

/// <summary>Core payment processor contract.</summary>
public interface IPaymentProcessor
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default);
}

public sealed record PaymentRequest(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string GatewayType
);

public sealed record PaymentResult(
    bool Success,
    string TransactionId,
    string Message,
    TimeSpan Duration
);

// ── Concrete Component — the real gateway call ─────────────────

/// <summary>Real payment processor — talks directly to the payment gateway SDK.</summary>
public sealed class RealPaymentProcessor : IPaymentProcessor
{
    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default)
    {
        // Simulated gateway latency
        await Task.Delay(50, ct);
        Console.WriteLine($"  [GATEWAY] Charged {request.Currency}{request.Amount} | Order={request.OrderId}");
        return new PaymentResult(true, $"txn_{Guid.NewGuid():N[..8]}", "Charge successful", TimeSpan.FromMilliseconds(50));
    }
}

// ── Base Decorator — holds reference to the wrapped processor ──

/// <summary>
/// Base decorator. Subclasses override ChargeAsync, do their work,
/// then call <c>_inner.ChargeAsync</c> to pass through to the next layer.
/// C# note: abstract class here so base decorators don't need to re-implement the passthrough.
/// </summary>
public abstract class PaymentProcessorDecorator(IPaymentProcessor inner) : IPaymentProcessor
{
    protected readonly IPaymentProcessor Inner = inner;

    public abstract Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default);
}

// ── Concrete Decorator 1: Logging ─────────────────────────────

/// <summary>
/// Logs the request and result. Wraps any IPaymentProcessor.
/// In production: inject ILogger&lt;LoggingDecorator&gt; for structured logging.
/// </summary>
public sealed class LoggingDecorator(IPaymentProcessor inner) : PaymentProcessorDecorator(inner)
{
    public override async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default)
    {
        Console.WriteLine($"[LOG ▶] Order={request.OrderId} | Customer={request.CustomerId} | {request.Currency}{request.Amount}");
        var start = DateTime.UtcNow;

        var result = await Inner.ChargeAsync(request, ct);

        var elapsed = DateTime.UtcNow - start;
        Console.WriteLine($"[LOG ◀] TxnId={result.TransactionId} | Success={result.Success} | {elapsed.TotalMilliseconds:F0}ms");
        return result;
    }
}

// ── Concrete Decorator 2: Fraud Check ─────────────────────────

/// <summary>
/// Runs fraud scoring before allowing the charge through.
/// If flagged, short-circuits — inner processor is never called.
/// </summary>
public sealed class FraudCheckDecorator(IPaymentProcessor inner, decimal fraudThreshold = 50_000m)
    : PaymentProcessorDecorator(inner)
{
    public override async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default)
    {
        Console.WriteLine($"[FRAUD ▶] Checking {request.Currency}{request.Amount} for {request.CustomerId}");

        if (request.Amount > fraudThreshold)
        {
            Console.WriteLine($"[FRAUD ✗] BLOCKED — exceeds threshold ₹{fraudThreshold:N0}");
            return new PaymentResult(false, string.Empty, $"Fraud check failed: amount exceeds ₹{fraudThreshold:N0}", TimeSpan.Zero);
        }

        Console.WriteLine($"[FRAUD ✓] Cleared");
        return await Inner.ChargeAsync(request, ct);
    }
}

// ── Concrete Decorator 3: Retry ────────────────────────────────

/// <summary>
/// Retries on transient failures with exponential backoff.
/// C# note: exponential backoff = 2^attempt × 100ms (100ms, 200ms, 400ms).
/// </summary>
public sealed class RetryDecorator(IPaymentProcessor inner, int maxRetries = 3) : PaymentProcessorDecorator(inner)
{
    public override async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Console.WriteLine($"[RETRY] Attempt {attempt}/{maxRetries}");
            var result = await Inner.ChargeAsync(request, ct);

            if (result.Success) return result;

            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));
                Console.WriteLine($"[RETRY] Failed, waiting {delay.TotalMilliseconds:F0}ms...");
                await Task.Delay(delay, ct);
            }
        }

        return new PaymentResult(false, string.Empty, $"All {maxRetries} attempts failed", TimeSpan.Zero);
    }
}

// ── Concrete Decorator 4: Audit Trail ─────────────────────────

/// <summary>
/// Persists an immutable audit record after every charge attempt.
/// In production: write to an append-only audit table / event store.
/// </summary>
public sealed class AuditDecorator(IPaymentProcessor inner) : PaymentProcessorDecorator(inner)
{
    private readonly List<AuditEntry> _auditLog = []; // in-memory for demo; use DB in prod

    public IReadOnlyList<AuditEntry> AuditLog => _auditLog;

    public override async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var result = await Inner.ChargeAsync(request, ct);

        _auditLog.Add(new AuditEntry(
            Timestamp   : DateTime.UtcNow,
            OrderId     : request.OrderId,
            CustomerId  : request.CustomerId,
            Amount      : request.Amount,
            Currency    : request.Currency,
            TransactionId: result.TransactionId,
            Success     : result.Success,
            Message     : result.Message
        ));
        Console.WriteLine($"[AUDIT] Recorded: {request.OrderId} → {result.TransactionId} Success={result.Success}");
        return result;
    }
}

public sealed record AuditEntry(
    DateTime Timestamp, string OrderId, string CustomerId,
    decimal Amount, string Currency, string TransactionId, bool Success, string Message
);

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// In ASP.NET Core DI (using Scrutor for decorator registration):
//
//   builder.Services.AddScoped<IPaymentProcessor, RealPaymentProcessor>();
//   builder.Services.Decorate<IPaymentProcessor, AuditDecorator>();
//   builder.Services.Decorate<IPaymentProcessor, RetryDecorator>();
//   builder.Services.Decorate<IPaymentProcessor, FraudCheckDecorator>();
//   builder.Services.Decorate<IPaymentProcessor, LoggingDecorator>();
//
// Or manually in Program.cs:
//   IPaymentProcessor processor =
//       new LoggingDecorator(
//           new FraudCheckDecorator(
//               new RetryDecorator(
//                   new AuditDecorator(
//                       new RealPaymentProcessor()))));

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class DecoratorDemo
{
    public static async Task Run()
    {
        Console.WriteLine("=== Decorator Pattern — Payment Pipeline ===\n");

        // ── Build the pipeline: Log → FraudCheck → Retry → Audit → RealGateway ─
        var auditDecorator = new AuditDecorator(new RealPaymentProcessor());

        IPaymentProcessor pipeline =
            new LoggingDecorator(
                new FraudCheckDecorator(
                    new RetryDecorator(auditDecorator, maxRetries: 2),
                    fraudThreshold: 10_000m));

        // ── Normal payment ────────────────────────────────────────────────────
        Console.WriteLine("── Normal payment (₹2,500) ──");
        var req1 = new PaymentRequest("ORD-001", "CUST-01", 2_500m, "₹", "stripe");
        var res1 = await pipeline.ChargeAsync(req1);
        Console.WriteLine($"Final: {res1.Message}\n");

        // ── Fraud-blocked payment ─────────────────────────────────────────────
        Console.WriteLine("── Fraud check blocks ₹15,000 payment ──");
        var req2 = new PaymentRequest("ORD-002", "CUST-02", 15_000m, "₹", "stripe");
        var res2 = await pipeline.ChargeAsync(req2);
        Console.WriteLine($"Final: {res2.Message}\n");

        // ── Audit log ─────────────────────────────────────────────────────────
        Console.WriteLine("── Audit log ──");
        foreach (var entry in auditDecorator.AuditLog)
            Console.WriteLine($"  {entry.Timestamp:HH:mm:ss} | {entry.OrderId} | ₹{entry.Amount} | Success={entry.Success}");

        Console.WriteLine("\n✅ Decorator — understood.");
    }
}
