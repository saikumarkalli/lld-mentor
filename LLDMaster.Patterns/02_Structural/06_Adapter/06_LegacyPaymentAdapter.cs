// ═══════════════════════════════════════════════════════════
// Pattern  : Adapter
// Category : Structural
// Intent   : Convert the interface of a class into another interface that clients expect.
// Domain   : LegacyPaymentAdapter — old bank XML API → modern IPaymentGateway
// Kudvenkat: Video 17 — Adapter Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// Your company acquires a bank's legacy payment system (10 years old).
// It has an XML-based SOAP API with methods like:
//   DebitAccount(string xmlPayload) → string xmlResponse
//
// Your modern checkout pipeline expects:
//   IPaymentGateway.Charge(decimal amount, string currency)
//
// You CANNOT change the legacy bank system (it's a vendor black box).
// You CANNOT change your pipeline (other gateways depend on IPaymentGateway).
//
// The Adapter wraps the old system and makes it speak the new language.
//
// WHEN TO USE:
//   ✔ Integrating a third-party / legacy system you cannot modify
//   ✔ Reusing existing class whose interface doesn't match what you need
//   ✔ Progressive migration: wrap old system, replace internals later
//
// WHEN NOT TO USE:
//   ✘ You own both interfaces — just change one of them directly

namespace LLDMaster.Patterns.Structural.Adapter;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Adapter)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — calling code must know about the legacy API directly

public class LegacyBankPaymentSystem
{
    // Old SOAP/XML-based method — can't change this (vendor code)
    public string DebitAccount(string xmlPayload)
    {
        Console.WriteLine($"[LegacyBank] Processing XML: {xmlPayload}");
        return "<response><status>SUCCESS</status><ref>BANK_REF_001</ref></response>";
    }

    public string ReverseDebit(string xmlReversePayload)
    {
        Console.WriteLine($"[LegacyBank] Reversing XML: {xmlReversePayload}");
        return "<response><status>REVERSED</status><ref>BANK_REV_001</ref></response>";
    }
}

// 💥 PROBLEM: every payment service must know XML format, build payloads, parse XML responses
// 💥 Adding a new field? Update XML building everywhere.
// 💥 Moving to JSON API later? Update everywhere again.

public class NaiveCheckoutService
{
    private readonly LegacyBankPaymentSystem _bank = new();

    public void Pay(decimal amount, string currency, string orderId)
    {
        // Must build XML manually — coupled to vendor format
        var xml = $"<debit><amount>{amount}</amount><currency>{currency}</currency><ref>{orderId}</ref></debit>";
        var response = _bank.DebitAccount(xml);
        // Must parse XML response manually
        var success = response.Contains("<status>SUCCESS</status>");
        Console.WriteLine($"[Naive] Payment {(success ? "OK" : "FAILED")}");
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 2 — ADAPTER (the right way)
// ─────────────────────────────────────────────────────────────

// ── Target interface (what the modern system expects) ─────────

/// <summary>Modern payment gateway interface — all new gateways implement this.</summary>
public interface IPaymentGateway
{
    PaymentResult Charge(decimal amount, string currency, string referenceId);
    PaymentResult Refund(string transactionId, decimal amount);
}

public sealed record PaymentResult(bool Success, string TransactionId, string Message);

// ── Adaptee — the legacy system (unchanged) ───────────────────
// LegacyBankPaymentSystem defined above — we do NOT touch it.

// ── Adapter — wraps legacy, exposes IPaymentGateway ──────────

/// <summary>
/// Adapts <see cref="LegacyBankPaymentSystem"/> to <see cref="IPaymentGateway"/>.
///
/// C# note: Object Adapter (uses composition, not inheritance) is preferred in C#
/// because it works with sealed classes and doesn't require the adaptee to be inheritable.
/// Class Adapter (inheritance) is only possible if the adaptee is not sealed.
/// </summary>
public sealed class LegacyBankPaymentAdapter : IPaymentGateway
{
    // Composition: holds a reference to the adaptee
    private readonly LegacyBankPaymentSystem _legacyBank;

    public LegacyBankPaymentAdapter(LegacyBankPaymentSystem legacyBank)
        => _legacyBank = legacyBank;

    /// <summary>
    /// Translates modern Charge() call → legacy XML DebitAccount().
    /// All XML construction and parsing is isolated here.
    /// </summary>
    public PaymentResult Charge(decimal amount, string currency, string referenceId)
    {
        // Translate: modern params → legacy XML format
        var xmlPayload = BuildDebitXml(amount, currency, referenceId);

        var xmlResponse = _legacyBank.DebitAccount(xmlPayload);

        // Translate: legacy XML response → modern PaymentResult
        return ParseResponse(xmlResponse);
    }

    /// <summary>Translates modern Refund() call → legacy ReverseDebit().</summary>
    public PaymentResult Refund(string transactionId, decimal amount)
    {
        var xmlPayload = $"<reverse><ref>{transactionId}</ref><amount>{amount}</amount></reverse>";
        var xmlResponse = _legacyBank.ReverseDebit(xmlPayload);
        return ParseResponse(xmlResponse);
    }

    // ── Private translation helpers (isolation zone) ──────────

    private static string BuildDebitXml(decimal amount, string currency, string referenceId)
        => $"<debit><amount>{amount}</amount><currency>{currency}</currency><ref>{referenceId}</ref></debit>";

    private static PaymentResult ParseResponse(string xml)
    {
        // Real code: use XmlDocument or System.Xml.Linq
        var success = xml.Contains("<status>SUCCESS</status>") || xml.Contains("<status>REVERSED</status>");
        var txnId   = ExtractXmlValue(xml, "ref");
        var status  = ExtractXmlValue(xml, "status");
        return new PaymentResult(success, txnId, status);
    }

    private static string ExtractXmlValue(string xml, string tag)
    {
        var start = xml.IndexOf($"<{tag}>") + tag.Length + 2;
        var end   = xml.IndexOf($"</{tag}>");
        return start >= 0 && end > start ? xml[start..end] : string.Empty;
    }
}

// ── Modern checkout pipeline uses only IPaymentGateway ────────

/// <summary>
/// Modern checkout — completely unaware that the bank uses legacy XML.
/// Swap LegacyBankPaymentAdapter for StripeGateway = zero changes here.
/// </summary>
public sealed class ModernCheckoutService(IPaymentGateway gateway)
{
    public PaymentResult ProcessPayment(decimal amount, string currency, string orderId)
    {
        Console.WriteLine($"[ModernCheckout] Charging {amount:C} {currency} for order {orderId}");
        return gateway.Charge(amount, currency, orderId);
    }

    public PaymentResult RefundPayment(string transactionId, decimal amount)
    {
        Console.WriteLine($"[ModernCheckout] Refunding {amount:C} for txn {transactionId}");
        return gateway.Refund(transactionId, amount);
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// In ASP.NET Core DI:
//
//   builder.Services.AddSingleton<LegacyBankPaymentSystem>();
//   builder.Services.AddSingleton<IPaymentGateway, LegacyBankPaymentAdapter>();
//   builder.Services.AddScoped<ModernCheckoutService>();
//
// Later, when the bank upgrades their API:
//   Replace LegacyBankPaymentAdapter with NewBankGateway.
//   ModernCheckoutService is untouched. Controllers are untouched.
// This is the power of the Adapter: isolate change in one class.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class AdapterDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Adapter Pattern — Legacy Bank Payment ===\n");

        // ── PROBLEM ─────────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: Naive service coupled to legacy XML API ──");
        new NaiveCheckoutService().Pay(1500m, "INR", "ORD-001");
        Console.WriteLine("Every service must know XML format. Adding JSON API breaks everything.\n");

        // ── ADAPTER ──────────────────────────────────────────────────────────
        Console.WriteLine("── Adapter: ModernCheckoutService talks IPaymentGateway ──");
        var legacyBank = new LegacyBankPaymentSystem();
        var adapter    = new LegacyBankPaymentAdapter(legacyBank);
        var checkout   = new ModernCheckoutService(adapter);

        var chargeResult = checkout.ProcessPayment(2500m, "INR", "ORD-002");
        Console.WriteLine($"Result: Success={chargeResult.Success}, TxnId={chargeResult.TransactionId}\n");

        var refundResult = checkout.RefundPayment(chargeResult.TransactionId, 2500m);
        Console.WriteLine($"Refund: Success={refundResult.Success}, Status={refundResult.Message}");

        Console.WriteLine("\n✅ Adapter — understood.");
    }
}
