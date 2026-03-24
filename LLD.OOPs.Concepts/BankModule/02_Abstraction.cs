/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏦 CONCEPT: Abstraction                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 01_Encapsulation.cs                                           ║
 * ║     BankAccount now owns and guards its data. Abstraction goes one level     ║
 * ║     higher: hiding HOW the banking service enforces its rules, exposing      ║
 * ║     only WHAT it can do. The data is encapsulated; now the behaviour is too. ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Abstraction means showing a simplified interface to the outside world and   ║
 * ║  hiding the implementation details behind it. Callers see 4 clean methods;  ║
 * ║  they have no idea 60 lines of fraud checks, audit logging, and SMS alerts  ║
 * ║  are running underneath. You interact with the WHAT, never the HOW.         ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  An ATM machine. You press "Withdraw ₹5,000". You don't see the network      ║
 * ║  call to the core banking system, the fraud score check, the daily limit     ║
 * ║  validation, or the audit log entry. You see one button. The ATM abstracts  ║
 * ║  all of that complexity behind a single action.                              ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A bank's mobile app team called the BankService's 200-line ProcessPayment() ║
 * ║  method directly — it was public and accessible. When the risk team needed  ║
 * ║  to add a new real-time fraud check, they modified that method. The mobile  ║
 * ║  app's integration tests all passed (they mocked the whole method), but a   ║
 * ║  parameter they assumed was always set was now nullable in one code path.   ║
 * ║  Production went down at 11 PM on a Friday. Cause: no abstraction barrier   ║
 * ║  between the caller and the 200-line implementation.                         ║
 * ║                                                                              ║
 * ║  QUICK RECALL (your 2-year cheat code):                                     ║
 * ║  Show callers the WHAT. Hide the HOW. Change the HOW freely, forever.       ║
 * ║                                                                              ║
 * ║  HOW THIS CONNECTS TO THE NEXT CONCEPT:                                     ║
 * ║  IBankService is an abstraction. File 3 (Inheritance) creates SavingsAccount ║
 * ║  and CurrentAccount sharing a common AccountBase — same idea, applied to   ║
 * ║  data types instead of service behaviour.                                   ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.BankModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — What a beginner naturally writes
// "ProcessWithdrawal does one thing: process a withdrawal." Except it does six.
// Validation, fraud check, limit check, balance update, audit log, SMS — all
// in one method. Every caller is coupled to ALL of this. Untestable. Unmaintainable.
// ─────────────────────────────────────────────────────────────────────────────

public class NaiveBankService
{
    // ❌ One method doing six different jobs
    public void ProcessWithdrawal(string accountId, decimal amount, string customerId)
    {
        // Step 1 — inline validation
        if (amount <= 0 || amount > 100_000)
        {
            Console.WriteLine("   [NAIVE] Invalid amount."); return;
        }

        // Step 2 — inline fraud score check (imagine real HTTP call to fraud API here)
        var fraudScore = GetFraudScore(customerId);  // ← external API call, hardcoded inline
        if (fraudScore > 70)
        {
            Console.WriteLine("   [NAIVE] Fraud check failed."); return;
        }

        // Step 3 — inline daily limit check (hardcoded ₹50,000)
        var todayTotal = GetTodayWithdrawals(accountId);  // ← DB call inline
        if (todayTotal + amount > 50_000)
        {
            Console.WriteLine("   [NAIVE] Daily limit exceeded."); return;
        }

        // Step 4 — inline balance deduction (direct DB mutation inline)
        DeductBalance(accountId, amount);   // ← DAO call hardcoded inline

        // Step 5 — inline audit log (hardcoded to a specific audit DB table)
        WriteAuditLog(accountId, amount);   // ← different DB call, also inline

        // Step 6 — inline SMS notification (hardcoded to one SMS vendor)
        SendSms(customerId, $"Withdrawn ₹{amount:N0}");  // ← SMS vendor hardcoded

        Console.WriteLine("   [NAIVE] Withdrawal done.");
    }

    // 💥 WHAT BREAKS:
    // — To add email notification alongside SMS: edit this 60-line method.
    // — To swap fraud vendor from ClearScore to Experian: edit this method.
    // — To test "does balance reduce correctly": you need a real fraud API, real SMS
    //   gateway, real audit DB. You cannot test the balance logic in isolation. Ever.
    // — Every new feature (UPI daily limit, international limit) is another
    //   hardcoded if-block appended to this method. It grows to 200 lines.

    private int  GetFraudScore(string cId)         => 30;  // stub
    private decimal GetTodayWithdrawals(string aId) => 10_000m; // stub
    private void DeductBalance(string aId, decimal amt) =>
        Console.WriteLine($"   [DB] Deducted ₹{amt:N0} from {aId}.");
    private void WriteAuditLog(string aId, decimal amt) =>
        Console.WriteLine($"   [AUDIT-DB] Logged ₹{amt:N0} withdrawal for {aId}.");
    private void SendSms(string cId, string msg) =>
        Console.WriteLine($"   [SMS-VENDOR] Sent to {cId}: {msg}");
}

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — OOP-correct abstraction
// IBankService is the public contract — 4 methods, nothing else.
// BankServiceBase hides the messy protected helpers. Callers never see them.
// RetailBankService is the concrete implementation. To add EmailNotifier,
// you override NotifyCustomer() in a subclass — zero caller impact.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// The public contract for the bank service layer.
/// Callers only ever interact through this interface — they're shielded from
/// all implementation details. Swap RetailBankService for IslamicBankService
/// and zero callers need to change.
/// </summary>
public interface IBankService
{
    void    Deposit(string accountId, decimal amount);
    void    Withdraw(string accountId, decimal amount, string customerId);
    void    Transfer(string fromId, string toId, decimal amount);
    decimal GetBalance(string accountId);
}

/// <summary>
/// Abstract base that owns the shared, protected helpers.
/// These are deliberately NOT on the interface — callers should never call them.
/// Concrete subclasses use them internally and override them when behaviour differs.
/// </summary>
public abstract class BankServiceBase : IBankService
{
    // ── Public API — what callers see ───────────────────────────────────────
    public abstract void    Deposit(string accountId, decimal amount);
    public abstract void    Withdraw(string accountId, decimal amount, string customerId);
    public abstract void    Transfer(string fromId, string toId, decimal amount);
    public abstract decimal GetBalance(string accountId);

    // ── Protected helpers — hidden from callers, shared by all subclasses ───

    /// <summary>
    /// Returns a fraud risk score 0–100. Override to swap vendors (ClearScore → Experian).
    /// Callers never know this method exists.
    /// </summary>
    protected virtual int GetFraudScore(string customerId)
    {
        // In production: HTTP call to fraud scoring API
        Console.WriteLine($"   [FRAUD-API] Scoring customer {customerId}...");
        return 25;  // simulated low-risk score
    }

    /// <summary>
    /// Writes an immutable audit record. Override to swap databases.
    /// Change from SQL to Cosmos DB here — zero callers affected.
    /// </summary>
    protected virtual void LogAudit(string accountId, string action, decimal amount)
    {
        Console.WriteLine($"   [AUDIT] {action} ₹{amount:N0} | Account: {accountId} | {DateTime.Now:HH:mm:ss}");
    }

    /// <summary>
    /// Sends a customer notification. Override NotifyCustomer to swap SMS → Email → Push.
    /// RetailBankService uses SMS today. Override it in MobileBankService for push.
    /// </summary>
    protected virtual void NotifyCustomer(string customerId, string message)
    {
        Console.WriteLine($"   [SMS] → {customerId}: {message}");
    }

    /// <summary>Returns true if the withdrawal would exceed the daily ₹1,00,000 limit.</summary>
    protected virtual bool ExceedsDailyLimit(string accountId, decimal requestedAmount)
    {
        decimal todayTotal = 20_000m; // simulated — real impl queries DB
        return (todayTotal + requestedAmount) > 1_00_000m;
    }
}

/// <summary>
/// Concrete retail banking implementation.
/// Notice how clean this is — each method reads like a business rule, not tech.
/// Swap SMS for email: override NotifyCustomer(). Nothing else changes.
/// </summary>
public class RetailBankService : BankServiceBase
{
    private readonly Dictionary<string, decimal> _balances = new()
    {
        ["ACC001"] = 50_000m,
        ["ACC002"] = 20_000m,
    };

    public override void Deposit(string accountId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Deposit must be positive.");
        _balances[accountId] = _balances.GetValueOrDefault(accountId) + amount;
        LogAudit(accountId, "DEPOSIT", amount);
        NotifyCustomer(accountId, $"₹{amount:N0} deposited. Balance: ₹{_balances[accountId]:N0}.");
    }

    public override void Withdraw(string accountId, decimal amount, string customerId)
    {
        if (amount <= 0) throw new ArgumentException("Withdrawal must be positive.");

        var fraudScore = GetFraudScore(customerId);   // hidden helper — callers never call this
        if (fraudScore > 70) { Console.WriteLine("   Withdrawal blocked: high fraud risk."); return; }

        if (ExceedsDailyLimit(accountId, amount)) { Console.WriteLine("   Withdrawal blocked: daily limit exceeded."); return; }

        if (!_balances.TryGetValue(accountId, out var bal) || bal < amount)
        {
            Console.WriteLine("   Withdrawal blocked: insufficient balance."); return;
        }

        _balances[accountId] -= amount;
        LogAudit(accountId, "WITHDRAWAL", amount);
        NotifyCustomer(customerId, $"₹{amount:N0} withdrawn. Balance: ₹{_balances[accountId]:N0}.");
    }

    public override void Transfer(string fromId, string toId, decimal amount)
    {
        Withdraw(fromId, amount, fromId);
        Deposit(toId, amount);
        LogAudit(fromId, $"TRANSFER → {toId}", amount);
    }

    public override decimal GetBalance(string accountId) =>
        _balances.GetValueOrDefault(accountId, 0);
}

/// <summary>
/// A subclass that swaps SMS for email — only ONE method changes.
/// RetailBankService, Transfer, Withdraw, fraud check — all inherited unchanged.
/// THIS is why the abstraction exists.
/// </summary>
public class EmailNotificationBankService : RetailBankService
{
    // ← Override just the notification. Everything else is inherited.
    protected override void NotifyCustomer(string customerId, string message)
    {
        Console.WriteLine($"   [EMAIL] → {customerId}@bank.com: {message}");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class AbstractionDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  ABSTRACTION DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — NaiveBankService: one method, six jobs:");
        var naive = new NaiveBankService();
        naive.ProcessWithdrawal("ACC001", 5_000m, "CUST001");
        Console.WriteLine("   ↑ Fraud check, daily limit, deduction, audit, SMS — all inline.");
        Console.WriteLine("   To test balance logic alone: impossible. To swap SMS vendor: edit this method.\n");

        // ── AFTER ────────────────────────────────────────────────────────────
        Console.WriteLine("✅ AFTER — IBankService: caller sees only 4 methods.\n");

        Console.WriteLine("   Using RetailBankService (SMS notifications):");
        IBankService smsService = new RetailBankService();
        smsService.Deposit("ACC001", 10_000m);
        smsService.Withdraw("ACC001", 5_000m, "CUST001");
        Console.WriteLine($"   Balance: ₹{smsService.GetBalance("ACC001"):N0}\n");

        Console.WriteLine("   Swapping to EmailNotificationBankService — caller code UNCHANGED:");
        IBankService emailService = new EmailNotificationBankService();
        emailService.Deposit("ACC001", 10_000m);    // ← identical call
        emailService.Withdraw("ACC001", 5_000m, "CUST001");  // ← identical call
        Console.WriteLine("   ^ NotifyCustomer() swapped. Fraud check, audit, limits — inherited unchanged.");

        Console.WriteLine("\n   The caller (this Demo method) never saw GetFraudScore(),");
        Console.WriteLine("   ExceedsDailyLimit(), LogAudit(), or NotifyCustomer().");
        Console.WriteLine("   That's abstraction: you interact with the WHAT, never the HOW.");

        Console.WriteLine("\n✅ Abstraction — understood.");
    }
}
