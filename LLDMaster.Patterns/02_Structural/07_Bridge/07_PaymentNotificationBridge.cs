// ═══════════════════════════════════════════════════════════
// Pattern  : Bridge
// Category : Structural
// Intent   : Decouple an abstraction from its implementation so the two can vary independently.
// Domain   : PaymentProcessor × NotificationChannel — any processor + any channel
// Kudvenkat: Video 18 — Bridge Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// You have 2 dimensions that vary independently:
//   Dimension A — Payment TYPE: OnlinePayment, WalletPayment, EMIPayment
//   Dimension B — Notification CHANNEL: Email, SMS, Push
//
// Without Bridge, you need A × B classes:
//   OnlinePaymentWithEmail, OnlinePaymentWithSMS, OnlinePaymentWithPush
//   WalletPaymentWithEmail, WalletPaymentWithSMS, WalletPaymentWithPush
//   EMIPaymentWithEmail,    EMIPaymentWithSMS,    EMIPaymentWithPush  → 9 classes!
// Add one new type = 3 more classes. Add one new channel = 3 more classes.
//
// With Bridge: A + B classes (3 + 3 = 6), composed at runtime.
// Each side evolves independently.
//
// WHEN TO USE:
//   ✔ Two independent dimensions that must vary without class explosion
//   ✔ You want to switch the "implementation" at runtime
//   ✔ Sharing implementation across multiple abstractions
//
// WHEN NOT TO USE:
//   ✘ Only one dimension varies — simple inheritance or strategy is enough
//   ✘ The two parts are tightly coupled and rarely change independently

namespace LLDMaster.Patterns.Structural.Bridge;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Bridge)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — Cartesian explosion of subclasses

public class NaiveOnlinePaymentWithEmail
{
    public void Process(decimal amount)
    {
        Console.WriteLine($"[Online] Charging {amount:C}");
        Console.WriteLine($"[Email]  Sending payment receipt email");
    }
}
public class NaiveOnlinePaymentWithSms
{
    public void Process(decimal amount)
    {
        Console.WriteLine($"[Online] Charging {amount:C}");
        Console.WriteLine($"[SMS]    Sending payment receipt SMS");
    }
}
// WalletPaymentWithEmail, WalletPaymentWithSms, EMIPayment × 2 ... → explosion

// 💥 Add "WhatsApp" notification → add OnlinePaymentWithWhatsApp, WalletPaymentWithWhatsApp, ...

// ─────────────────────────────────────────────────────────────
// SECTION 2 — BRIDGE (the right way)
// ─────────────────────────────────────────────────────────────

// ── IMPLEMENTATION side (Notification channels) ───────────────

/// <summary>
/// Implementor interface — the notification "bridge" the abstraction will use.
/// C# note: interface preferred over abstract class when no shared state exists.
/// </summary>
public interface INotificationChannel
{
    void Send(string recipient, string subject, string body);
}

public sealed class EmailNotification : INotificationChannel
{
    public void Send(string recipient, string subject, string body)
        => Console.WriteLine($"[EMAIL  → {recipient}] Subject: {subject} | {body}");
}

public sealed class SmsNotification : INotificationChannel
{
    public void Send(string recipient, string subject, string body)
        => Console.WriteLine($"[SMS    → {recipient}] {subject}: {body}");
}

public sealed class PushNotification : INotificationChannel
{
    public void Send(string recipient, string subject, string body)
        => Console.WriteLine($"[PUSH   → {recipient}] 🔔 {subject}: {body}");
}

// Add WhatsApp tomorrow — ZERO changes to the Abstraction side:
public sealed class WhatsAppNotification : INotificationChannel
{
    public void Send(string recipient, string subject, string body)
        => Console.WriteLine($"[WHATSAPP → {recipient}] {subject}: {body}");
}

// ── ABSTRACTION side (Payment types) ─────────────────────────

/// <summary>
/// Abstraction — holds a reference to the Implementor (bridge).
/// Subclasses define WHAT kind of payment; the bridge defines HOW to notify.
/// </summary>
public abstract class PaymentProcessor(INotificationChannel notifier)
{
    // The "bridge" — injected, can be swapped at runtime
    protected readonly INotificationChannel Notifier = notifier;

    /// <summary>Processes the payment and sends confirmation via the injected channel.</summary>
    public abstract PaymentConfirmation Process(string customerId, decimal amount, string currency);
}

public sealed record PaymentConfirmation(bool Success, string TransactionId);

// ── Refined Abstractions ───────────────────────────────────────

/// <summary>Online card payment — notifies via injected channel.</summary>
public sealed class OnlineCardPayment(INotificationChannel notifier)
    : PaymentProcessor(notifier)
{
    public override PaymentConfirmation Process(string customerId, decimal amount, string currency)
    {
        Console.WriteLine($"[OnlineCard] Charging {currency}{amount} via card for {customerId}");
        var txnId = $"card_{Guid.NewGuid():N[..8]}";

        // Use the bridge — notification logic is NOT here
        Notifier.Send(customerId, "Payment Successful",
            $"Your card payment of {currency}{amount} is confirmed. TxnId: {txnId}");

        return new PaymentConfirmation(true, txnId);
    }
}

/// <summary>Wallet payment (Paytm / PhonePe style).</summary>
public sealed class WalletPayment(INotificationChannel notifier)
    : PaymentProcessor(notifier)
{
    public override PaymentConfirmation Process(string customerId, decimal amount, string currency)
    {
        Console.WriteLine($"[Wallet] Debiting {currency}{amount} from wallet of {customerId}");
        var txnId = $"wallet_{Guid.NewGuid():N[..8]}";

        Notifier.Send(customerId, "Wallet Debited",
            $"{currency}{amount} debited from your wallet. Balance updated. TxnId: {txnId}");

        return new PaymentConfirmation(true, txnId);
    }
}

/// <summary>EMI payment — deducts one instalment.</summary>
public sealed class EmiPayment(INotificationChannel notifier, int totalInstalmentsRemaining)
    : PaymentProcessor(notifier)
{
    public override PaymentConfirmation Process(string customerId, decimal amount, string currency)
    {
        Console.WriteLine($"[EMI] Charging instalment {currency}{amount} | Remaining: {totalInstalmentsRemaining - 1}");
        var txnId = $"emi_{Guid.NewGuid():N[..8]}";

        Notifier.Send(customerId, "EMI Charged",
            $"EMI of {currency}{amount} charged. {totalInstalmentsRemaining - 1} instalments remaining. TxnId: {txnId}");

        return new PaymentConfirmation(true, txnId);
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// In ASP.NET Core — choose channel from user preference:
//
//   var channel = user.NotificationPreference switch
//   {
//       "email"    => serviceProvider.GetRequiredService<EmailNotification>(),
//       "sms"      => serviceProvider.GetRequiredService<SmsNotification>(),
//       "push"     => serviceProvider.GetRequiredService<PushNotification>(),
//       _          => serviceProvider.GetRequiredService<EmailNotification>(),
//   };
//   var processor = new OnlineCardPayment(channel);
//   processor.Process(userId, amount, "INR");
//
// Switch from Email to WhatsApp for all card payments?
// One line change. Zero changes to OnlineCardPayment.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class BridgeDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Bridge Pattern — Payment × Notification ===\n");

        // ── PROBLEM ─────────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: class explosion without Bridge ──");
        new NaiveOnlinePaymentWithEmail().Process(1000m);
        new NaiveOnlinePaymentWithSms().Process(1000m);
        Console.WriteLine("Adding WhatsApp = add OnlineWithWhatsApp + WalletWithWhatsApp + EMIWithWhatsApp. BAD.\n");

        // ── BRIDGE: mix and match freely ──────────────────────────────────────
        Console.WriteLine("── Bridge: compose payment type × notification channel ──");

        INotificationChannel email   = new EmailNotification();
        INotificationChannel sms     = new SmsNotification();
        INotificationChannel push    = new PushNotification();
        INotificationChannel whatsapp = new WhatsAppNotification();

        // Card + Email
        Console.WriteLine("\n[Card + Email]");
        new OnlineCardPayment(email).Process("user@shop.com", 2500m, "₹");

        // Wallet + SMS
        Console.WriteLine("\n[Wallet + SMS]");
        new WalletPayment(sms).Process("+91-9876543210", 500m, "₹");

        // EMI + Push
        Console.WriteLine("\n[EMI + Push]");
        new EmiPayment(push, totalInstalmentsRemaining: 6).Process("device_token_abc", 1000m, "₹");

        // Card + WhatsApp (new channel — zero changes to OnlineCardPayment)
        Console.WriteLine("\n[Card + WhatsApp — new channel, zero existing code changed]");
        new OnlineCardPayment(whatsapp).Process("+91-9812345678", 3000m, "₹");

        Console.WriteLine("\n✅ Bridge — understood.");
    }
}
