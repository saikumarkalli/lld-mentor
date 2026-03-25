// ═══════════════════════════════════════════════════════════
// Pattern  : Facade
// Category : Structural
// Intent   : Provide a simplified interface to a complex subsystem.
// Domain   : CheckoutFacade — inventory + payment + invoice + email in one PlaceOrder()
// Kudvenkat: Video 21 — Facade Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// Placing an order involves 5 subsystems:
//   1. InventoryService — check & reserve stock
//   2. PaymentService   — charge the gateway
//   3. InvoiceService   — generate PDF invoice
//   4. EmailService     — send confirmation email
//   5. LoyaltyService   — award points
//
// Without Facade, the controller/API endpoint must orchestrate all 5 systems
// and know their exact method signatures — 50 lines of plumbing per endpoint.
// Add a new step (analytics event)? Touch every endpoint that places an order.
//
// Facade provides ONE method: PlaceOrder(dto) → OrderConfirmation.
// The controller stays thin. The orchestration is in one place.
//
// WHEN TO USE:
//   ✔ Simplifying a complex subsystem for common use cases
//   ✔ Defining a single entry point (e.g., checkout, refund, cancellation)
//   ✔ Layered architecture: Facade = the "application service layer"
//
// WHEN NOT TO USE:
//   ✘ All callers need full control over individual steps — don't hide them
//   ✘ Subsystems are already simple — Facade adds no value

namespace LLDMaster.Patterns.Structural.Facade;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Facade)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — controller directly orchestrates all subsystems

public class NaiveOrderController
{
    // 💥 Controller knows about ALL subsystems — tightly coupled
    // 💥 50 lines of orchestration in the controller
    // 💥 Adding "loyalty points" step = edit every controller that places orders
    public string PlaceOrder(string productId, int qty, decimal amount, string customerId, string gatewayType)
    {
        // Step 1: check inventory
        Console.WriteLine($"[Naive] Checking inventory for {productId}...");
        // Step 2: reserve
        Console.WriteLine($"[Naive] Reserving {qty} units...");
        // Step 3: charge
        Console.WriteLine($"[Naive] Charging ₹{amount} via {gatewayType}...");
        // Step 4: invoice
        Console.WriteLine($"[Naive] Generating invoice...");
        // Step 5: email
        Console.WriteLine($"[Naive] Sending email to {customerId}...");
        return "ORD-001";
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 2 — FACADE (the right way)
// ─────────────────────────────────────────────────────────────

// ── Subsystem 1: Inventory ─────────────────────────────────────

/// <summary>Manages product stock levels.</summary>
public sealed class InventoryService
{
    private readonly Dictionary<string, int> _stock = new()
    {
        ["PROD-iPhone"]  = 10,
        ["PROD-MacBook"]  = 5,
        ["PROD-AirPods"] = 0,  // out of stock
    };

    /// <summary>Returns true if the requested quantity is available.</summary>
    public bool IsInStock(string productId, int quantity)
    {
        var available = _stock.GetValueOrDefault(productId, 0);
        Console.WriteLine($"  [Inventory] {productId}: {available} in stock, requested={quantity}");
        return available >= quantity;
    }

    /// <summary>Deducts stock. Call only after payment succeeds.</summary>
    public void ReserveStock(string productId, int quantity)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) - quantity;
        Console.WriteLine($"  [Inventory] Reserved {quantity}x {productId}. Remaining: {_stock[productId]}");
    }
}

// ── Subsystem 2: Payment ───────────────────────────────────────

public sealed record ChargeResult(bool Success, string TransactionId, string Message);

/// <summary>Processes payment charges via the configured gateway.</summary>
public sealed class PaymentService
{
    public ChargeResult Charge(decimal amount, string currency, string orderId, string gatewayType)
    {
        Console.WriteLine($"  [Payment] Charging {currency}{amount:N2} via {gatewayType} | OrderId={orderId}");
        var txnId = $"{gatewayType}_txn_{Guid.NewGuid():N[..8]}";
        return new ChargeResult(true, txnId, "Charge successful");
    }

    public void Refund(string transactionId, decimal amount)
        => Console.WriteLine($"  [Payment] Refunding {amount:C} for {transactionId}");
}

// ── Subsystem 3: Invoice ───────────────────────────────────────

/// <summary>Generates and persists invoice documents.</summary>
public sealed class InvoiceService
{
    public string GenerateInvoice(string orderId, string customerId, decimal amount)
    {
        var invoiceNo = $"INV-{DateTime.UtcNow:yyyyMMdd}-{orderId}";
        Console.WriteLine($"  [Invoice] Generated {invoiceNo} for {customerId} | ₹{amount:N2}");
        return invoiceNo;
    }
}

// ── Subsystem 4: Email ─────────────────────────────────────────

/// <summary>Sends transactional emails.</summary>
public sealed class EmailService
{
    public void SendOrderConfirmation(string to, string orderId, string invoiceNo, decimal amount)
        => Console.WriteLine($"  [Email → {to}] Order {orderId} confirmed | Invoice: {invoiceNo} | ₹{amount:N2}");

    public void SendOrderFailure(string to, string orderId, string reason)
        => Console.WriteLine($"  [Email → {to}] Order {orderId} FAILED: {reason}");
}

// ── Subsystem 5: Loyalty ───────────────────────────────────────

/// <summary>Awards loyalty points on successful purchases.</summary>
public sealed class LoyaltyService
{
    // 1 point per ₹100 spent
    public int AwardPoints(string customerId, decimal amount)
    {
        var points = (int)(amount / 100);
        Console.WriteLine($"  [Loyalty] Awarded {points} pts to {customerId} (₹{amount:N2} / 100)");
        return points;
    }
}

// ── Facade — the single entry point ───────────────────────────

/// <summary>
/// The Checkout Facade orchestrates all subsystems.
/// The controller only knows about CheckoutFacade — not the 5 services behind it.
/// Adding a new step (Analytics, Warehouse notification) = change ONLY this class.
/// </summary>
public sealed class CheckoutFacade(
    InventoryService inventory,
    PaymentService   payment,
    InvoiceService   invoice,
    EmailService     email,
    LoyaltyService   loyalty)
{
    public OrderConfirmation PlaceOrder(PlaceOrderRequest request)
    {
        Console.WriteLine($"\n[Facade] Starting checkout for Order={request.OrderId}");

        // Step 1: Check inventory
        if (!inventory.IsInStock(request.ProductId, request.Quantity))
        {
            email.SendOrderFailure(request.CustomerEmail, request.OrderId, "Out of stock");
            return OrderConfirmation.Failure(request.OrderId, "Out of stock");
        }

        // Step 2: Charge payment
        var charge = payment.Charge(request.Amount, "₹", request.OrderId, request.GatewayType);
        if (!charge.Success)
        {
            email.SendOrderFailure(request.CustomerEmail, request.OrderId, charge.Message);
            return OrderConfirmation.Failure(request.OrderId, charge.Message);
        }

        // Step 3: Reserve stock (after payment, not before — avoid phantom reservations)
        inventory.ReserveStock(request.ProductId, request.Quantity);

        // Step 4: Generate invoice
        var invoiceNo = invoice.GenerateInvoice(request.OrderId, request.CustomerId, request.Amount);

        // Step 5: Send confirmation email
        email.SendOrderConfirmation(request.CustomerEmail, request.OrderId, invoiceNo, request.Amount);

        // Step 6: Award loyalty points
        var points = loyalty.AwardPoints(request.CustomerId, request.Amount);

        Console.WriteLine($"[Facade] Checkout complete for Order={request.OrderId}");
        return OrderConfirmation.Succeed(request.OrderId, charge.TransactionId, invoiceNo, points);
    }

    /// <summary>Cancels an order and initiates refund.</summary>
    public void CancelOrder(string orderId, string transactionId, decimal amount, string customerEmail)
    {
        Console.WriteLine($"\n[Facade] Cancelling Order={orderId}");
        payment.Refund(transactionId, amount);
        email.SendOrderFailure(customerEmail, orderId, "Order cancelled by customer");
    }
}

// ── DTOs ───────────────────────────────────────────────────────

public sealed record PlaceOrderRequest(
    string OrderId,
    string CustomerId,
    string CustomerEmail,
    string ProductId,
    int    Quantity,
    decimal Amount,
    string GatewayType
);

public sealed record OrderConfirmation(
    bool   Success,
    string OrderId,
    string TransactionId,
    string InvoiceNumber,
    int    LoyaltyPointsAwarded,
    string Message
)
{
    public static OrderConfirmation Succeed(string orderId, string txnId, string invoiceNo, int points)
        => new(true, orderId, txnId, invoiceNo, points, "Order placed successfully");

    public static OrderConfirmation Failure(string orderId, string reason)
        => new(false, orderId, string.Empty, string.Empty, 0, reason);
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// In ASP.NET Core DI:
//   builder.Services.AddScoped<InventoryService>();
//   builder.Services.AddScoped<PaymentService>();
//   builder.Services.AddScoped<InvoiceService>();
//   builder.Services.AddScoped<EmailService>();
//   builder.Services.AddScoped<LoyaltyService>();
//   builder.Services.AddScoped<CheckoutFacade>();
//
// Controller:
//   [HttpPost("checkout")]
//   public async Task<IActionResult> Checkout(
//       [FromBody] PlaceOrderRequest req,
//       [FromServices] CheckoutFacade facade)
//   {
//       var result = facade.PlaceOrder(req);
//       return result.Success ? Ok(result) : BadRequest(result.Message);
//   }
// Controller is 5 lines. All complexity is in the Facade.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class FacadeDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Facade Pattern — Checkout ===\n");

        var facade = new CheckoutFacade(
            new InventoryService(),
            new PaymentService(),
            new InvoiceService(),
            new EmailService(),
            new LoyaltyService());

        // ── Success case ─────────────────────────────────────────────────────
        Console.WriteLine("── Successful order ──");
        var result1 = facade.PlaceOrder(new PlaceOrderRequest(
            "ORD-101", "CUST-01", "user@shop.com",
            "PROD-iPhone", Quantity: 1, Amount: 89_990m, "stripe"));
        Console.WriteLine($"Result: {result1}\n");

        // ── Out of stock ──────────────────────────────────────────────────────
        Console.WriteLine("── Out of stock order ──");
        var result2 = facade.PlaceOrder(new PlaceOrderRequest(
            "ORD-102", "CUST-02", "user2@shop.com",
            "PROD-AirPods", Quantity: 1, Amount: 12_000m, "razorpay"));
        Console.WriteLine($"Result: {result2}\n");

        // ── Cancellation ──────────────────────────────────────────────────────
        Console.WriteLine("── Cancel order 101 ──");
        facade.CancelOrder("ORD-101", result1.TransactionId, 89_990m, "user@shop.com");

        Console.WriteLine("\n✅ Facade — understood.");
    }
}
