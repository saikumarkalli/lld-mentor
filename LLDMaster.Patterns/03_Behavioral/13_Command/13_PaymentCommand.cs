// ═══════════════════════════════════════════════════════════
// Pattern  : Command
// Category : Behavioral
// Intent   : Encapsulate a request as an object, allowing parameterization, queuing, and undo.
// Domain   : PaymentCommand — ProcessPayment with Refund (undo) + command history
// Kudvenkat: Video — Command Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// In a payment admin panel, an operator can:
//   - Process a payment
//   - Refund it (undo the payment)
//   - Queue multiple charges to run at midnight
//   - Replay failed transactions from an audit log
//
// Without Command: service methods are called directly.
// You can't queue them, can't undo them, can't log and replay them.
//
// Command turns EVERY OPERATION into an object with Execute() + Undo().
// Objects can be: stored in a queue, serialised to a DB, replayed, undone.
//
// WHEN TO USE:
//   ✔ Need undo/redo functionality (refund is the undo of a payment)
//   ✔ Need to queue, schedule, or retry operations
//   ✔ Need an audit log that can be replayed
//   ✔ Macro operations (batch multiple commands into one)
//
// WHEN NOT TO USE:
//   ✘ Simple one-time calls with no undo requirement
//   ✘ Command overhead (object per call) is too expensive for high-frequency ops

namespace LLDMaster.Patterns.Behavioral.Command;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Command)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — no way to undo, queue, or replay

public class NaivePaymentAdmin
{
    public void ChargeCustomer(string customerId, decimal amount)
        => Console.WriteLine($"[Naive] Charged ₹{amount} from {customerId}");

    // 💥 How do you refund this? Call a separate Refund() method manually.
    // 💥 How do you queue 100 charges for batch midnight processing?
    // 💥 How do you replay a failed charge from yesterday's log?
    // Answer: you can't — the operation is gone once the method returns.
}

// ─────────────────────────────────────────────────────────────
// SECTION 2 — COMMAND (the right way)
// ─────────────────────────────────────────────────────────────

// ── Command interface ──────────────────────────────────────────

/// <summary>
/// Command interface — every payment operation implements this.
/// C# note: async Execute/Undo because real payment calls are I/O bound.
/// </summary>
public interface IPaymentCommand
{
    string CommandId  { get; }
    string Description { get; }
    Task ExecuteAsync(CancellationToken ct = default);
    Task UndoAsync(CancellationToken ct = default);
}

// ── Receiver — the actual payment processing logic ─────────────

/// <summary>The payment gateway receiver — does the real work.</summary>
public sealed class PaymentGateway
{
    private readonly Dictionary<string, decimal> _transactions = [];

    public async Task<string> ChargeAsync(string customerId, decimal amount, string currency, CancellationToken ct)
    {
        await Task.Delay(30, ct); // simulate gateway latency
        var txnId = $"txn_{Guid.NewGuid():N[..8]}";
        _transactions[txnId] = amount;
        Console.WriteLine($"  [Gateway] CHARGED ₹{amount} from {customerId} | TxnId={txnId}");
        return txnId;
    }

    public async Task RefundAsync(string transactionId, decimal amount, CancellationToken ct)
    {
        await Task.Delay(20, ct);
        if (_transactions.Remove(transactionId))
            Console.WriteLine($"  [Gateway] REFUNDED ₹{amount} | TxnId={transactionId}");
        else
            Console.WriteLine($"  [Gateway] REFUND FAILED — transaction {transactionId} not found");
    }

    public async Task ApplyDiscountAsync(string orderId, decimal discountAmount, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        Console.WriteLine($"  [Gateway] DISCOUNT ₹{discountAmount} applied to Order={orderId}");
    }

    public async Task ReverseDiscountAsync(string orderId, decimal discountAmount, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        Console.WriteLine($"  [Gateway] DISCOUNT REVERSED ₹{discountAmount} on Order={orderId}");
    }
}

// ── Concrete Command 1: ProcessPaymentCommand ──────────────────

/// <summary>
/// Encapsulates a single payment charge.
/// Undo = refund the transaction.
/// </summary>
public sealed class ProcessPaymentCommand : IPaymentCommand
{
    private readonly PaymentGateway _gateway;
    private readonly string _customerId;
    private readonly decimal _amount;
    private readonly string _currency;
    private string _executedTransactionId = string.Empty;

    public string CommandId    { get; } = Guid.NewGuid().ToString("N")[..8];
    public string Description  { get; }

    public ProcessPaymentCommand(PaymentGateway gateway, string customerId, decimal amount, string currency = "₹")
    {
        _gateway    = gateway;
        _customerId = customerId;
        _amount     = amount;
        _currency   = currency;
        Description = $"Charge ₹{amount} from {customerId}";
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        Console.WriteLine($"[CMD Execute] {Description}");
        _executedTransactionId = await _gateway.ChargeAsync(_customerId, _amount, _currency, ct);
    }

    public async Task UndoAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_executedTransactionId))
        {
            Console.WriteLine($"[CMD Undo] Cannot undo — command was not executed.");
            return;
        }
        Console.WriteLine($"[CMD Undo] Refunding {Description}");
        await _gateway.RefundAsync(_executedTransactionId, _amount, ct);
    }
}

// ── Concrete Command 2: ApplyDiscountCommand ───────────────────

/// <summary>
/// Applies a discount to an order.
/// Undo = reverse the discount.
/// </summary>
public sealed class ApplyDiscountCommand : IPaymentCommand
{
    private readonly PaymentGateway _gateway;
    private readonly string _orderId;
    private readonly decimal _discountAmount;

    public string CommandId    { get; } = Guid.NewGuid().ToString("N")[..8];
    public string Description  { get; }

    public ApplyDiscountCommand(PaymentGateway gateway, string orderId, decimal discountAmount)
    {
        _gateway        = gateway;
        _orderId        = orderId;
        _discountAmount = discountAmount;
        Description     = $"Apply ₹{discountAmount} discount on {orderId}";
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        Console.WriteLine($"[CMD Execute] {Description}");
        await _gateway.ApplyDiscountAsync(_orderId, _discountAmount, ct);
    }

    public async Task UndoAsync(CancellationToken ct = default)
    {
        Console.WriteLine($"[CMD Undo] Reversing: {Description}");
        await _gateway.ReverseDiscountAsync(_orderId, _discountAmount, ct);
    }
}

// ── Invoker — CommandProcessor with history ────────────────────

/// <summary>
/// Executes commands and maintains history for undo support.
/// C# note: Stack&lt;T&gt; is natural for undo — LIFO (last executed = first undone).
/// </summary>
public sealed class PaymentCommandProcessor
{
    private readonly Stack<IPaymentCommand> _history = new();
    private readonly Queue<IPaymentCommand> _pendingQueue = new();

    /// <summary>Executes a command immediately and adds it to undo history.</summary>
    public async Task ExecuteAsync(IPaymentCommand command, CancellationToken ct = default)
    {
        await command.ExecuteAsync(ct);
        _history.Push(command);
        Console.WriteLine($"  [Processor] History size: {_history.Count}");
    }

    /// <summary>Undoes the last executed command (LIFO).</summary>
    public async Task UndoLastAsync(CancellationToken ct = default)
    {
        if (_history.TryPop(out var last))
        {
            Console.WriteLine($"\n[Processor] Undoing: {last.Description}");
            await last.UndoAsync(ct);
        }
        else
        {
            Console.WriteLine("[Processor] Nothing to undo.");
        }
    }

    /// <summary>Enqueues a command for later batch execution.</summary>
    public void Enqueue(IPaymentCommand command)
    {
        _pendingQueue.Enqueue(command);
        Console.WriteLine($"  [Processor] Queued: {command.Description}");
    }

    /// <summary>Runs all queued commands (e.g., midnight batch).</summary>
    public async Task ExecuteQueueAsync(CancellationToken ct = default)
    {
        Console.WriteLine($"\n[Processor] Running batch queue ({_pendingQueue.Count} commands)...");
        while (_pendingQueue.TryDequeue(out var cmd))
            await ExecuteAsync(cmd, ct);
        Console.WriteLine("[Processor] Batch complete.");
    }

    public IReadOnlyCollection<IPaymentCommand> History => _history;
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// Saga pattern (distributed transactions):
//   Each step in the saga is a Command.
//   If step 3 fails, undo steps 2 and 1 in reverse order.
//   ProcessPaymentCommand, ReserveInventoryCommand, SendEmailCommand
//   → all undoable → saga handles compensating transactions.
//
// Scheduled retry:
//   Serialize the command to DB. Dequeue and re-execute on failure.
//   Command holds all data needed to re-execute (idempotency key).
//
// Admin audit UI:
//   Show command history → operator clicks "Undo Last 3" → calls UndoLastAsync() 3 times.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class CommandDemo
{
    public static async Task Run()
    {
        Console.WriteLine("=== Command Pattern — Payment Operations ===\n");

        // ── PROBLEM ─────────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: no undo, no queue, no replay ──");
        new NaivePaymentAdmin().ChargeCustomer("CUST-01", 1000m);
        Console.WriteLine("Cannot refund this. Cannot queue it. Cannot replay it. BAD.\n");

        // ── COMMAND: execute + undo ────────────────────────────────────────────
        Console.WriteLine("── Execute + Undo ──");
        var gateway   = new PaymentGateway();
        var processor = new PaymentCommandProcessor();

        var chargeCmd    = new ProcessPaymentCommand(gateway, "CUST-01", 2_500m);
        var discountCmd  = new ApplyDiscountCommand(gateway, "ORD-001", 200m);

        await processor.ExecuteAsync(chargeCmd);
        await processor.ExecuteAsync(discountCmd);

        // Undo last two (LIFO)
        Console.WriteLine();
        await processor.UndoLastAsync(); // undoes discount
        await processor.UndoLastAsync(); // undoes charge (refund)
        await processor.UndoLastAsync(); // nothing left

        // ── COMMAND: batch queue (midnight processing) ─────────────────────────
        Console.WriteLine("\n── Batch queue (midnight billing) ──");
        var batchProcessor = new PaymentCommandProcessor();
        batchProcessor.Enqueue(new ProcessPaymentCommand(gateway, "CUST-02", 999m));
        batchProcessor.Enqueue(new ProcessPaymentCommand(gateway, "CUST-03", 1_299m));
        batchProcessor.Enqueue(new ApplyDiscountCommand(gateway, "ORD-002", 100m));
        await batchProcessor.ExecuteQueueAsync();

        Console.WriteLine("\n✅ Command — understood.");
    }
}
