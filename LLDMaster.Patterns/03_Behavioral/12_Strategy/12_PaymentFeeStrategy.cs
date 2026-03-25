// ═══════════════════════════════════════════════════════════
// Pattern  : Strategy
// Category : Behavioral
// Intent   : Define a family of algorithms, encapsulate each one, and make them interchangeable.
// Domain   : PaymentFeeCalculator — FlatRate / Percentage / Tiered / FreeTier strategies
// Kudvenkat: Video — Strategy Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// Payment gateways charge different transaction fees:
//   Stripe: 2% of amount
//   PayPal: ₹25 flat + 1.5%
//   RazorPay: Free up to ₹1L/month, then 2%
//   Corporate account: ₹10 flat per transaction
//
// Without Strategy: one method with nested if/switch per gateway.
// Adding a new gateway = edit the fee calculator.
// Unit testing one fee rule = must instantiate the whole class.
//
// With Strategy: each fee rule is its own class with ONE responsibility.
// Swap strategy at runtime (by config, by gateway type, by merchant tier).
//
// WHEN TO USE:
//   ✔ Multiple variations of the same algorithm (fee rules, shipping calc, discount calc)
//   ✔ Algorithm must be switchable at runtime
//   ✔ Eliminate conditionals (if/switch) scattered across the codebase
//
// WHEN NOT TO USE:
//   ✘ Only one algorithm exists and never varies — just put it inline
//   ✘ Algorithms share so much state that isolating them is artificial

namespace LLDMaster.Patterns.Behavioral.Strategy;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Strategy)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — if/switch explosion in the fee calculator

public class NaiveFeeCalculator
{
    // 💥 Every new gateway = edit this method
    // 💥 Unit testing RazorPay's free-tier logic requires instantiating everything
    public decimal Calculate(string gatewayType, decimal amount)
    {
        return gatewayType switch
        {
            "stripe"   => amount * 0.02m,
            "paypal"   => 25m + (amount * 0.015m),
            "razorpay" => amount <= 100_000m ? 0m : amount * 0.02m,
            "corporate"=> 10m,
            _          => throw new NotSupportedException(gatewayType),
        };
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 2 — STRATEGY (the right way)
// ─────────────────────────────────────────────────────────────

// ── Strategy interface ─────────────────────────────────────────

/// <summary>
/// Encapsulates one fee calculation algorithm.
/// C# note: interface is preferred over abstract class here — no shared state.
/// </summary>
public interface IPaymentFeeStrategy
{
    string Name { get; }

    /// <summary>Calculates the gateway fee for the given transaction amount.</summary>
    FeeResult Calculate(decimal amount, string currency);
}

public sealed record FeeResult(decimal Fee, string Breakdown);

// ── Concrete Strategies ────────────────────────────────────────

/// <summary>Flat fee per transaction (e.g., bank transfers, corporate accounts).</summary>
public sealed class FlatFeeStrategy(decimal flatFee) : IPaymentFeeStrategy
{
    public string Name => $"FlatFee(₹{flatFee})";

    public FeeResult Calculate(decimal amount, string currency)
    {
        Console.WriteLine($"  [FlatFee] ₹{flatFee} regardless of amount");
        return new FeeResult(flatFee, $"Flat ₹{flatFee}");
    }
}

/// <summary>Percentage of transaction amount (e.g., Stripe 2%).</summary>
public sealed class PercentageFeeStrategy(decimal percentageRate) : IPaymentFeeStrategy
{
    public string Name => $"Percentage({percentageRate * 100:F1}%)";

    public FeeResult Calculate(decimal amount, string currency)
    {
        var fee = Math.Round(amount * percentageRate, 2);
        Console.WriteLine($"  [Percentage] {percentageRate * 100:F1}% of ₹{amount} = ₹{fee}");
        return new FeeResult(fee, $"{percentageRate * 100:F1}% of ₹{amount}");
    }
}

/// <summary>Flat fee plus percentage (e.g., PayPal ₹25 + 1.5%).</summary>
public sealed class FlatPlusPercentageFeeStrategy(decimal flatFee, decimal percentageRate) : IPaymentFeeStrategy
{
    public string Name => $"FlatPlusPercentage(₹{flatFee} + {percentageRate * 100:F1}%)";

    public FeeResult Calculate(decimal amount, string currency)
    {
        var percentagePart = Math.Round(amount * percentageRate, 2);
        var total = flatFee + percentagePart;
        Console.WriteLine($"  [FlatPlusPercentage] ₹{flatFee} + {percentageRate * 100:F1}% of ₹{amount} = ₹{total}");
        return new FeeResult(total, $"₹{flatFee} + ₹{percentagePart}");
    }
}

/// <summary>
/// Free up to a monthly threshold, then percentage.
/// (RazorPay-style: free tier up to ₹1L/month.)
/// </summary>
public sealed class FreeTierFeeStrategy(decimal freeUpTo, decimal percentageAboveFree) : IPaymentFeeStrategy
{
    public string Name => $"FreeTier(free ≤ ₹{freeUpTo:N0}, then {percentageAboveFree * 100:F1}%)";

    private decimal _monthlyVolume; // tracks running volume for the month

    public FeeResult Calculate(decimal amount, string currency)
    {
        _monthlyVolume += amount;

        if (_monthlyVolume <= freeUpTo)
        {
            Console.WriteLine($"  [FreeTier] ₹{amount} | Monthly volume ₹{_monthlyVolume} ≤ ₹{freeUpTo} → FREE");
            return new FeeResult(0m, "Within free tier");
        }

        var fee = Math.Round(amount * percentageAboveFree, 2);
        Console.WriteLine($"  [FreeTier] Monthly volume ₹{_monthlyVolume} exceeds ₹{freeUpTo} → {percentageAboveFree * 100:F1}% fee = ₹{fee}");
        return new FeeResult(fee, $"{percentageAboveFree * 100:F1}% of ₹{amount} (above free tier)");
    }

    /// <summary>Resets the monthly volume counter (call on 1st of each month).</summary>
    public void ResetMonthlyVolume() => _monthlyVolume = 0;
}

/// <summary>
/// Tiered pricing — fee rate drops as volume increases (bulk discount).
/// E.g., enterprise gateways that reward high-volume merchants.
/// </summary>
public sealed class TieredFeeStrategy : IPaymentFeeStrategy
{
    // Tiers: (minAmount, rate) — first matching tier wins
    private static readonly (decimal Threshold, decimal Rate)[] Tiers =
    [
        (50_000m, 0.010m),  // ≥ ₹50,000 → 1%
        (10_000m, 0.015m),  // ≥ ₹10,000 → 1.5%
        (0m,      0.020m),  // any amount → 2%
    ];

    public string Name => "Tiered(2% / 1.5% / 1%)";

    public FeeResult Calculate(decimal amount, string currency)
    {
        var (threshold, rate) = Tiers.First(t => amount >= t.Threshold);
        var fee = Math.Round(amount * rate, 2);
        Console.WriteLine($"  [Tiered] ₹{amount} → tier {rate * 100:F1}% = ₹{fee}");
        return new FeeResult(fee, $"Tiered {rate * 100:F1}% (threshold ≥ ₹{threshold:N0})");
    }
}

// ── Context — uses the strategy ────────────────────────────────

/// <summary>
/// Fee calculator context. Strategy is injected and can be swapped at runtime.
/// C# note: strategy is a property setter so it can be changed between calls
/// (e.g., switch strategy mid-session for a VIP customer upgrade).
/// </summary>
public sealed class PaymentFeeCalculator(IPaymentFeeStrategy strategy)
{
    private IPaymentFeeStrategy _strategy = strategy;

    /// <summary>Switches the fee strategy at runtime (e.g., VIP merchant upgrade).</summary>
    public void SetStrategy(IPaymentFeeStrategy newStrategy)
    {
        Console.WriteLine($"  [FeeCalc] Strategy changed: {_strategy.Name} → {newStrategy.Name}");
        _strategy = newStrategy;
    }

    /// <summary>Calculates the gateway fee and returns the total charged to the customer.</summary>
    public (decimal NetAmount, decimal Fee, string Breakdown) CalculateTotal(decimal amount, string currency = "₹")
    {
        var result = _strategy.Calculate(amount, currency);
        var total  = amount + result.Fee;
        Console.WriteLine($"  [FeeCalc] Net=₹{amount} + Fee=₹{result.Fee} = Total=₹{total} | {result.Breakdown}");
        return (amount, result.Fee, result.Breakdown);
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// In ASP.NET Core — select strategy based on merchant tier / gateway:
//
//   IPaymentFeeStrategy strategy = merchant.Tier switch
//   {
//       MerchantTier.Startup    => new FreeTierFeeStrategy(100_000m, 0.02m),
//       MerchantTier.Growth     => new PercentageFeeStrategy(0.015m),
//       MerchantTier.Enterprise => new TieredFeeStrategy(),
//       MerchantTier.Corporate  => new FlatFeeStrategy(10m),
//       _                       => new PercentageFeeStrategy(0.02m),
//   };
//   var calculator = new PaymentFeeCalculator(strategy);
//   var (net, fee, _) = calculator.CalculateTotal(orderAmount);

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class StrategyDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Strategy Pattern — Payment Fee Calculation ===\n");

        // ── PROBLEM ─────────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: if/switch in NaiveFeeCalculator ──");
        var naive = new NaiveFeeCalculator();
        Console.WriteLine($"Stripe ₹1000 fee: ₹{naive.Calculate("stripe", 1000m)}");
        Console.WriteLine($"PayPal ₹1000 fee: ₹{naive.Calculate("paypal", 1000m)}");
        Console.WriteLine("Add CryptoPay gateway = edit NaiveFeeCalculator. BAD.\n");

        // ── STRATEGY: each algorithm isolated ─────────────────────────────────
        Console.WriteLine("── FlatFee (Corporate: ₹10/txn) ──");
        var calc = new PaymentFeeCalculator(new FlatFeeStrategy(10m));
        calc.CalculateTotal(500m);
        calc.CalculateTotal(50_000m);

        Console.WriteLine("\n── Percentage (Stripe: 2%) ──");
        calc.SetStrategy(new PercentageFeeStrategy(0.02m));
        calc.CalculateTotal(2_500m);

        Console.WriteLine("\n── FlatPlusPercentage (PayPal: ₹25 + 1.5%) ──");
        calc.SetStrategy(new FlatPlusPercentageFeeStrategy(25m, 0.015m));
        calc.CalculateTotal(2_500m);

        Console.WriteLine("\n── FreeTier (RazorPay: free ≤ ₹1L) ──");
        var freeTier = new FreeTierFeeStrategy(100_000m, 0.02m);
        calc.SetStrategy(freeTier);
        calc.CalculateTotal(40_000m);  // free
        calc.CalculateTotal(40_000m);  // free (total 80K)
        calc.CalculateTotal(30_000m);  // exceeds 1L → charged

        Console.WriteLine("\n── Tiered (Enterprise: 2%/1.5%/1%) ──");
        calc.SetStrategy(new TieredFeeStrategy());
        calc.CalculateTotal(5_000m);   // 2%
        calc.CalculateTotal(15_000m);  // 1.5%
        calc.CalculateTotal(75_000m);  // 1%

        Console.WriteLine("\n✅ Strategy — understood.");
    }
}
