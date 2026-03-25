// ═══════════════════════════════════════════════════════════
// Pattern  : Abstract Factory
// Category : Creational
// Intent   : Provide an interface for creating families of related objects without specifying concrete classes.
// Domain   : Payment Provider Families — Domestic vs International
// Kudvenkat: Video 10 — Abstract Factory Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// A payment system has FAMILIES of related objects that must work together:
//   Domestic family  → DomesticGateway  + DomesticFraudChecker  + DomesticInvoice
//   International    → IntlGateway      + IntlFraudChecker       + IntlInvoice
//
// Factory Method creates ONE product. Abstract Factory creates a FAMILY.
// The rule: objects in a family are designed to work together.
// Mixing families (DomesticGateway + IntlFraudChecker) causes subtle bugs
// e.g., different currency formats, regex patterns, tax rules.
//
// WHEN TO USE:
//   ✔ Your system has multiple "themes" or "environments" (domestic/intl, dev/prod)
//   ✔ Objects must be used together — mixing families would be a bug
//   ✔ You want to swap the entire family in one place (config change → all objects change)
//
// WHEN NOT TO USE:
//   ✘ Products are independent — use Factory Method per product instead
//   ✘ Only one family exists now and a second is not planned — YAGNI

namespace LLDMaster.Patterns.Creational.AbstractFactory;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Abstract Factory)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — objects are created ad-hoc, families can be accidentally mixed

public class NaiveOrderProcessor
{
    public void Process(bool isDomestic, decimal amount)
    {
        // 💥 PROBLEM: nothing stops a developer from mixing
        //    DomesticGateway with InternationalFraudChecker
        //    → wrong currency regex, wrong tax logic, wrong invoice format

        if (isDomestic)
        {
            var gw    = new NaiveDomesticGateway();
            var fraud = new NaiveDomesticFraud();   // must match gateway family
            gw.Charge(amount);
            fraud.Check(amount);
        }
        else
        {
            var gw    = new NaiveIntlGateway();
            var fraud = new NaiveIntlFraud();
            gw.Charge(amount);
            fraud.Check(amount);
        }
    }
}

public class NaiveDomesticGateway { public void Charge(decimal a) => Console.WriteLine($"[Dom-GW]   ₹{a}"); }
public class NaiveDomesticFraud   { public void Check(decimal a)  => Console.WriteLine($"[Dom-Fraud] ₹{a} ok"); }
public class NaiveIntlGateway     { public void Charge(decimal a) => Console.WriteLine($"[Intl-GW]  ${a}"); }
public class NaiveIntlFraud       { public void Check(decimal a)  => Console.WriteLine($"[Intl-Fraud] ${a} ok"); }

// ─────────────────────────────────────────────────────────────
// SECTION 2 — ABSTRACT FACTORY (the right way)
// ─────────────────────────────────────────────────────────────

// ── Abstract Products ──────────────────────────────────────────

/// <summary>Gateway product — every family must implement this.</summary>
public interface IPaymentGateway
{
    PaymentConfirmation Charge(decimal amount, string currency);
}

/// <summary>Fraud checker product — tied to its gateway family.</summary>
public interface IFraudChecker
{
    FraudResult Evaluate(decimal amount, string customerId);
}

/// <summary>Invoice generator product — format differs by family.</summary>
public interface IInvoiceGenerator
{
    string Generate(string orderId, decimal amount);
}

public sealed record PaymentConfirmation(string TransactionId, string Status);
public sealed record FraudResult(bool Approved, string Reason);

// ── Abstract Factory ───────────────────────────────────────────

/// <summary>
/// The factory contract. Each implementation returns a coherent, compatible family.
/// C# note: interface preferred over abstract class here — no shared state needed.
/// </summary>
public interface IPaymentProviderFactory
{
    IPaymentGateway   CreateGateway();
    IFraudChecker     CreateFraudChecker();
    IInvoiceGenerator CreateInvoiceGenerator();
}

// ── Concrete Family 1: Domestic (INR, RazorPay-style) ─────────

public sealed class DomesticGateway : IPaymentGateway
{
    public PaymentConfirmation Charge(decimal amount, string currency)
    {
        Console.WriteLine($"[Domestic-GW] Charging ₹{amount} via RazorPay (UPI/NEFT/Card)");
        return new PaymentConfirmation($"dom_{Guid.NewGuid():N}", "SUCCESS");
    }
}

public sealed class DomesticFraudChecker : IFraudChecker
{
    public FraudResult Evaluate(decimal amount, string customerId)
    {
        // Domestic rule: flag orders above ₹1,00,000 for manual review
        var flagged = amount > 100_000m;
        Console.WriteLine($"[Domestic-Fraud] ₹{amount} — flagged={flagged}");
        return new FraudResult(!flagged, flagged ? "Exceeds domestic threshold" : "OK");
    }
}

public sealed class DomesticInvoiceGenerator : IInvoiceGenerator
{
    public string Generate(string orderId, decimal amount)
    {
        // GST invoice format
        var invoice = $"[GST Invoice] Order={orderId} | Amt=₹{amount} | GSTIN=27AABCU9603R1ZX";
        Console.WriteLine(invoice);
        return invoice;
    }
}

/// <summary>Concrete factory — creates the entire Domestic family.</summary>
public sealed class DomesticPaymentProviderFactory : IPaymentProviderFactory
{
    public IPaymentGateway   CreateGateway()          => new DomesticGateway();
    public IFraudChecker     CreateFraudChecker()     => new DomesticFraudChecker();
    public IInvoiceGenerator CreateInvoiceGenerator() => new DomesticInvoiceGenerator();
}

// ── Concrete Family 2: International (USD/EUR, Stripe-style) ──

public sealed class InternationalGateway : IPaymentGateway
{
    public PaymentConfirmation Charge(decimal amount, string currency)
    {
        Console.WriteLine($"[Intl-GW] Charging {currency}{amount} via Stripe (3DS2)");
        return new PaymentConfirmation($"intl_{Guid.NewGuid():N}", "SUCCESS");
    }
}

public sealed class InternationalFraudChecker : IFraudChecker
{
    public FraudResult Evaluate(decimal amount, string customerId)
    {
        // International rule: flag if >$5000 and new customer
        var flagged = amount > 5000m;
        Console.WriteLine($"[Intl-Fraud] ${amount} — flagged={flagged}");
        return new FraudResult(!flagged, flagged ? "Exceeds international threshold" : "OK");
    }
}

public sealed class InternationalInvoiceGenerator : IInvoiceGenerator
{
    public string Generate(string orderId, decimal amount)
    {
        // VAT invoice format
        var invoice = $"[VAT Invoice] Order={orderId} | Amt=${amount} | VAT=20%";
        Console.WriteLine(invoice);
        return invoice;
    }
}

/// <summary>Concrete factory — creates the entire International family.</summary>
public sealed class InternationalPaymentProviderFactory : IPaymentProviderFactory
{
    public IPaymentGateway   CreateGateway()          => new InternationalGateway();
    public IFraudChecker     CreateFraudChecker()     => new InternationalFraudChecker();
    public IInvoiceGenerator CreateInvoiceGenerator() => new InternationalInvoiceGenerator();
}

// ── Consumer — uses only the abstract factory, never concrete types ──

/// <summary>
/// Processes an order using whatever family the factory produces.
/// To switch to International: inject InternationalPaymentProviderFactory.
/// This class has ZERO knowledge of Domestic vs International logic.
/// </summary>
public sealed class OrderPaymentProcessor(IPaymentProviderFactory factory)
{
    public void ProcessOrder(string orderId, decimal amount, string currency, string customerId)
    {
        Console.WriteLine($"\n--- Processing order {orderId} ---");

        var gateway   = factory.CreateGateway();
        var fraud     = factory.CreateFraudChecker();
        var invoicer  = factory.CreateInvoiceGenerator();

        var fraudResult = fraud.Evaluate(amount, customerId);
        if (!fraudResult.Approved)
        {
            Console.WriteLine($"BLOCKED: {fraudResult.Reason}");
            return;
        }

        var confirmation = gateway.Charge(amount, currency);
        var invoice      = invoicer.Generate(orderId, amount);

        Console.WriteLine($"Payment confirmed: {confirmation.TransactionId}");
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// In ASP.NET Core — swap family via config:
//
//   var isInternational = config["Payment:Mode"] == "international";
//   IPaymentProviderFactory factory = isInternational
//       ? new InternationalPaymentProviderFactory()
//       : new DomesticPaymentProviderFactory();
//   builder.Services.AddSingleton(factory);
//   builder.Services.AddScoped<OrderPaymentProcessor>();
//
// ONE config change switches the entire payment family.
// No controller, service, or handler needs to change.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class AbstractFactoryDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Abstract Factory Pattern — Payment Provider Families ===\n");

        // ── PROBLEM ─────────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: ad-hoc creation, families can be mixed ──");
        new NaiveOrderProcessor().Process(true,  750.00m);
        new NaiveOrderProcessor().Process(false, 200.00m);
        Console.WriteLine("Nothing stops NaiveDomesticGateway + NaiveIntlFraud being paired. BAD.\n");

        // ── DOMESTIC FAMILY ──────────────────────────────────────────────────
        Console.WriteLine("── Domestic Family ──");
        var domesticProcessor = new OrderPaymentProcessor(new DomesticPaymentProviderFactory());
        domesticProcessor.ProcessOrder("ORD-D001", 45_000m, "INR", "CUST-01");
        domesticProcessor.ProcessOrder("ORD-D002", 1_50_000m, "INR", "CUST-02"); // flagged

        // ── INTERNATIONAL FAMILY ─────────────────────────────────────────────
        Console.WriteLine("\n── International Family ──");
        var intlProcessor = new OrderPaymentProcessor(new InternationalPaymentProviderFactory());
        intlProcessor.ProcessOrder("ORD-I001", 3_500m, "USD", "CUST-03");
        intlProcessor.ProcessOrder("ORD-I002", 6_000m, "USD", "CUST-04"); // flagged

        Console.WriteLine("\n✅ Abstract Factory — understood.");
    }
}
