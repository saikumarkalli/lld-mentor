// ═══════════════════════════════════════════════════════════
// Pattern  : Template Method
// Category : Behavioral
// Intent   : Define the skeleton of an algorithm in a base class; let subclasses override specific steps.
// Domain   : PaymentReportGenerator — daily / monthly / reconciliation reports
// Kudvenkat: Video — Template Method Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// Every payment report follows the same steps:
//   1. FetchData()         — query the DB for transactions
//   2. FilterData()        — apply date/status filters
//   3. AggregateMetrics()  — sum, count, group
//   4. FormatReport()      — CSV / JSON / PDF
//   5. DeliverReport()     — email / S3 / portal download
//
// Steps 1, 5 are IDENTICAL for every report.
// Steps 2, 3, 4 VARY per report type (daily vs monthly vs reconciliation).
//
// Without Template Method: duplicate steps 1 and 5 in every report class.
// With Template Method: base class owns the algorithm skeleton.
//   Subclasses ONLY override the parts that differ.
//
// WHEN TO USE:
//   ✔ Multiple classes share the same algorithm with minor variations
//   ✔ You want to prevent subclasses from changing the algorithm ORDER
//   ✔ "Hooks" — optional overrideable steps (e.g., AddWatermark)
//
// WHEN NOT TO USE:
//   ✘ Only one implementation exists — just write it directly
//   ✘ Algorithm varies completely between classes — Template Method forces artificial sharing

namespace LLDMaster.Patterns.Behavioral.TemplateMethod;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Template Method)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — duplicated FetchData and DeliverReport in every class

public class NaiveDailyReport
{
    public void Generate()
    {
        Console.WriteLine("[Naive] Connecting to DB...");       // duplicated in every report
        Console.WriteLine("[Naive] Fetching today's data...");  // varies
        Console.WriteLine("[Naive] Formatting as CSV...");      // varies
        Console.WriteLine("[Naive] Sending email...");          // duplicated in every report
    }
}

public class NaiveMonthlyReport
{
    public void Generate()
    {
        Console.WriteLine("[Naive] Connecting to DB...");       // SAME — copy-pasted
        Console.WriteLine("[Naive] Fetching monthly data...");
        Console.WriteLine("[Naive] Formatting as PDF...");
        Console.WriteLine("[Naive] Sending email...");          // SAME — copy-pasted
    }
}
// 💥 Change email delivery (e.g., add BCC) = edit every report class. Classic DRY violation.

// ─────────────────────────────────────────────────────────────
// SECTION 2 — TEMPLATE METHOD (the right way)
// ─────────────────────────────────────────────────────────────

// ── Shared domain model ───────────────────────────────────────

public sealed record Transaction(
    string  TransactionId,
    string  OrderId,
    string  Status,
    decimal Amount,
    DateTime ProcessedAt
);

public sealed record ReportData(
    string        ReportName,
    List<Transaction> Transactions,
    decimal       TotalAmount,
    int           SuccessCount,
    int           FailureCount,
    DateTime      GeneratedAt
);

// ── Abstract base — the algorithm skeleton ────────────────────

/// <summary>
/// Defines the payment report generation algorithm.
/// Subclasses override specific steps; the Run() orchestration is sealed.
///
/// C# note:
///   - Run() is sealed → subclasses CANNOT change the algorithm order
///   - abstract methods  → MUST be overridden
///   - virtual methods   → CAN be overridden (hooks with defaults)
///   - protected         → visible to subclasses but not to callers
/// </summary>
public abstract class PaymentReportGenerator
{
    // ── Template Method — sealed to protect the algorithm order ───────────

    /// <summary>
    /// Runs the complete report generation pipeline.
    /// This is the Template Method — the algorithm skeleton.
    /// </summary>
    // C# note: 'sealed' on a non-virtual method is redundant — 'public void' + abstract class
    // already prevents callers from skipping steps. Made non-virtual intentionally.
    public void Run()
    {
        Console.WriteLine($"\n[Report] Starting: {ReportName}");

        var rawTransactions = FetchData();
        var filtered        = FilterData(rawTransactions);
        var reportData      = AggregateMetrics(filtered);
        var formatted       = FormatReport(reportData);

        // Hook — optional pre-delivery step
        OnBeforeDeliver(reportData);

        DeliverReport(formatted, reportData);

        Console.WriteLine($"[Report] Completed: {ReportName}");
    }

    // ── Abstract steps — MUST be overridden by each report type ──────────

    protected abstract string ReportName { get; }

    /// <summary>Fetches raw transactions from the data source.</summary>
    protected abstract List<Transaction> FetchData();

    /// <summary>Filters transactions relevant to this report type.</summary>
    protected abstract List<Transaction> FilterData(List<Transaction> transactions);

    /// <summary>Aggregates totals, counts, and group-by logic.</summary>
    protected abstract ReportData AggregateMetrics(List<Transaction> transactions);

    /// <summary>Formats the report data into a deliverable string (CSV, JSON, PDF).</summary>
    protected abstract string FormatReport(ReportData data);

    // ── Concrete step — SAME for ALL report types ─────────────────────────

    /// <summary>Delivers the formatted report (email + S3 upload). Same for all reports.</summary>
    private void DeliverReport(string formattedReport, ReportData data)
    {
        // In production: inject IEmailService, IS3Service
        Console.WriteLine($"  [Deliver] Uploading to S3: reports/{data.ReportName.Replace(" ", "_")}.csv");
        Console.WriteLine($"  [Deliver] Emailing to finance@shop.com | Subject: {data.ReportName}");
        Console.WriteLine($"  [Deliver] Report preview: {formattedReport[..Math.Min(80, formattedReport.Length)]}...");
    }

    // ── Hook — optional override ───────────────────────────────────────────

    /// <summary>
    /// Hook: override to add custom behaviour before delivery (e.g., add digital watermark).
    /// Default: no-op.
    /// </summary>
    protected virtual void OnBeforeDeliver(ReportData data) { }
}

// ── Concrete Report 1: Daily Transactions ────────────────────

/// <summary>Daily CSV report — all of today's transactions.</summary>
public sealed class DailyTransactionReport(IEnumerable<Transaction> allTransactions)
    : PaymentReportGenerator
{
    protected override string ReportName => $"Daily Report {DateTime.UtcNow:yyyy-MM-dd}";

    protected override List<Transaction> FetchData()
    {
        Console.WriteLine("  [Fetch] Querying DB: today's transactions");
        return allTransactions.ToList();
    }

    protected override List<Transaction> FilterData(List<Transaction> transactions)
    {
        var today = DateTime.UtcNow.Date;
        var filtered = transactions.Where(t => t.ProcessedAt.Date == today).ToList();
        Console.WriteLine($"  [Filter] Today: {filtered.Count} transactions");
        return filtered;
    }

    protected override ReportData AggregateMetrics(List<Transaction> transactions)
    {
        return new ReportData(
            ReportName,
            transactions,
            TotalAmount   : transactions.Where(t => t.Status == "SUCCESS").Sum(t => t.Amount),
            SuccessCount  : transactions.Count(t => t.Status == "SUCCESS"),
            FailureCount  : transactions.Count(t => t.Status == "FAILED"),
            GeneratedAt   : DateTime.UtcNow);
    }

    protected override string FormatReport(ReportData data)
    {
        // CSV format
        var lines = data.Transactions.Select(t =>
            $"{t.TransactionId},{t.OrderId},{t.Status},{t.Amount},{t.ProcessedAt:O}");
        return $"TransactionId,OrderId,Status,Amount,ProcessedAt\n{string.Join("\n", lines)}";
    }
}

// ── Concrete Report 2: Monthly Revenue Summary ───────────────

/// <summary>Monthly PDF-style summary — aggregated by day.</summary>
public sealed class MonthlyRevenueReport(IEnumerable<Transaction> allTransactions, int year, int month)
    : PaymentReportGenerator
{
    protected override string ReportName => $"Monthly Revenue {year}-{month:D2}";

    protected override List<Transaction> FetchData()
    {
        Console.WriteLine($"  [Fetch] Querying DB: {year}-{month:D2} all transactions");
        return allTransactions.ToList();
    }

    protected override List<Transaction> FilterData(List<Transaction> transactions)
    {
        var filtered = transactions
            .Where(t => t.ProcessedAt.Year == year && t.ProcessedAt.Month == month)
            .ToList();
        Console.WriteLine($"  [Filter] Month {month}: {filtered.Count} transactions");
        return filtered;
    }

    protected override ReportData AggregateMetrics(List<Transaction> transactions)
    {
        return new ReportData(
            ReportName,
            transactions,
            TotalAmount  : transactions.Where(t => t.Status == "SUCCESS").Sum(t => t.Amount),
            SuccessCount : transactions.Count(t => t.Status == "SUCCESS"),
            FailureCount : transactions.Count(t => t.Status == "FAILED"),
            GeneratedAt  : DateTime.UtcNow);
    }

    protected override string FormatReport(ReportData data)
    {
        // JSON-style summary
        return $"{{\"report\":\"{data.ReportName}\",\"total\":₹{data.TotalAmount:N2}," +
               $"\"success\":{data.SuccessCount},\"failure\":{data.FailureCount}}}";
    }

    // Hook override — add watermark for CFO monthly reports
    protected override void OnBeforeDeliver(ReportData data)
        => Console.WriteLine($"  [Hook] Adding CONFIDENTIAL watermark to {data.ReportName}");
}

// ── Concrete Report 3: Reconciliation Report ─────────────────

/// <summary>Reconciliation — only failed/pending transactions for ops team.</summary>
public sealed class ReconciliationReport(IEnumerable<Transaction> allTransactions)
    : PaymentReportGenerator
{
    protected override string ReportName => $"Reconciliation {DateTime.UtcNow:yyyy-MM-dd}";

    protected override List<Transaction> FetchData()
    {
        Console.WriteLine("  [Fetch] Querying DB: failed + pending transactions");
        return allTransactions.ToList();
    }

    protected override List<Transaction> FilterData(List<Transaction> transactions)
    {
        var filtered = transactions.Where(t => t.Status is "FAILED" or "PENDING").ToList();
        Console.WriteLine($"  [Filter] Needs attention: {filtered.Count} transactions");
        return filtered;
    }

    protected override ReportData AggregateMetrics(List<Transaction> transactions)
    {
        return new ReportData(
            ReportName,
            transactions,
            TotalAmount  : transactions.Sum(t => t.Amount),
            SuccessCount : 0,
            FailureCount : transactions.Count,
            GeneratedAt  : DateTime.UtcNow);
    }

    protected override string FormatReport(ReportData data)
    {
        var lines = data.Transactions.Select(t => $"INVESTIGATE: {t.TransactionId} | {t.Status} | ₹{t.Amount}");
        return string.Join("\n", lines);
    }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// Scheduled via Hangfire / Azure Functions:
//   RecurringJob.AddOrUpdate("daily-report",
//       () => new DailyTransactionReport(dbTransactions).Run(),
//       Cron.Daily(hour: 23, minute: 59));
//
//   RecurringJob.AddOrUpdate("monthly-report",
//       () => new MonthlyRevenueReport(dbTransactions, DateTime.UtcNow.Year, DateTime.UtcNow.Month).Run(),
//       Cron.Monthly());
//
// Adding a new report type (e.g., ChargebackReport):
//   Extend PaymentReportGenerator, implement 4 methods, register the job.
//   Run() algorithm never changes. Delivery logic never duplicated.

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class TemplateMethodDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Template Method Pattern — Payment Reports ===\n");

        // ── PROBLEM ──────────────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: duplicated FetchData + DeliverReport everywhere ──");
        new NaiveDailyReport().Generate();
        new NaiveMonthlyReport().Generate();
        Console.WriteLine("Change email delivery = edit both classes. BAD.\n");

        var transactions = SeedTransactions();

        // ── Daily report ──────────────────────────────────────────────────
        Console.WriteLine("── Daily Transaction Report ──");
        new DailyTransactionReport(transactions).Run();

        // ── Monthly report (with hook) ─────────────────────────────────────
        Console.WriteLine("\n── Monthly Revenue Report (with watermark hook) ──");
        new MonthlyRevenueReport(transactions, DateTime.UtcNow.Year, DateTime.UtcNow.Month).Run();

        // ── Reconciliation report ─────────────────────────────────────────
        Console.WriteLine("\n── Reconciliation Report ──");
        new ReconciliationReport(transactions).Run();

        Console.WriteLine("\n✅ Template Method — understood.");
    }

    private static List<Transaction> SeedTransactions()
    {
        var now = DateTime.UtcNow;
        return
        [
            new("TXN-001", "ORD-001", "SUCCESS",  1_200m, now.AddHours(-2)),
            new("TXN-002", "ORD-002", "FAILED",     500m, now.AddHours(-1)),
            new("TXN-003", "ORD-003", "SUCCESS",  8_999m, now.AddMinutes(-30)),
            new("TXN-004", "ORD-004", "PENDING",  2_500m, now.AddMinutes(-10)),
            new("TXN-005", "ORD-005", "SUCCESS", 12_000m, now.AddMinutes(-5)),
        ];
    }
}
