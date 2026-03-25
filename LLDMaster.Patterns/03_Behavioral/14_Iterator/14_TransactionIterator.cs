// ═══════════════════════════════════════════════════════════
// Pattern  : Iterator
// Category : Behavioral
// Intent   : Provide a way to sequentially access elements without exposing the underlying collection.
// Domain   : TransactionHistory — paginated, filtered traversal of payment records
// Kudvenkat: Video — Iterator Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// WHY THIS PATTERN?
// ─────────────────────────────────────────────────────────────
// A payment system's transaction history can have thousands of records.
// The UI wants paginated access: "give me 20 at a time, let me go next/prev".
// The reporting service wants filtered traversal: "only failed transactions".
// The audit service wants reverse chronological order.
//
// Without Iterator: expose the raw List<Transaction> to every caller.
// Caller controls traversal → leaks internals, breaks when you switch to DB-paged queries.
//
// With Iterator: callers just do foreach or call MoveNext()/Current.
// Internally it could be a list, a DB cursor, or a stream — callers never know.
//
// WHEN TO USE:
//   ✔ Need multiple ways to traverse the same collection (forward, reverse, filtered)
//   ✔ Want to hide the storage mechanism (List today, DB cursor tomorrow)
//   ✔ Support uniform iteration via foreach across custom collections
//
// WHEN NOT TO USE:
//   ✘ Collection is a plain List<T> — use LINQ directly, no custom iterator needed
//   ✘ Single-pass traversal with no complex logic — IEnumerable<T> is sufficient

namespace LLDMaster.Patterns.Behavioral.Iterator;

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (before Iterator)
// ─────────────────────────────────────────────────────────────

// ❌ BEFORE — raw collection exposed to callers

public class NaiveTransactionRepository
{
    // 💥 Internal List exposed — caller can mutate it, sort it, delete from it
    // 💥 Switch to DB-paged queries tomorrow = break every caller
    public List<NaiveTransaction> GetAll() => _data;
    private readonly List<NaiveTransaction> _data =
    [
        new("TXN-001", 1000m, "SUCCESS"),
        new("TXN-002", 500m,  "FAILED"),
        new("TXN-003", 2000m, "SUCCESS"),
    ];
}
public record NaiveTransaction(string Id, decimal Amount, string Status);

// ─────────────────────────────────────────────────────────────
// SECTION 2 — ITERATOR (the right way)
// ─────────────────────────────────────────────────────────────

// ── Domain model ──────────────────────────────────────────────

/// <summary>A single payment transaction record.</summary>
public sealed record Transaction(
    string   TransactionId,
    string   OrderId,
    string   CustomerId,
    decimal  Amount,
    string   Currency,
    string   Status,        // SUCCESS | FAILED | PENDING | REFUNDED
    DateTime ProcessedAt
);

// ── C# approach: IEnumerable<T> + yield return ────────────────
// C# note: The Iterator pattern in C# is built into the language via IEnumerable<T>/IEnumerator<T>.
// yield return lets you write iterators without implementing IEnumerator manually.
// Custom IEnumerator is needed ONLY when you require stateful navigation (HasPrev, Reset to page N).

/// <summary>
/// Transaction history store — exposes multiple traversal strategies
/// without exposing the underlying storage.
/// </summary>
public sealed class TransactionHistory
{
    private readonly List<Transaction> _transactions;

    public TransactionHistory(IEnumerable<Transaction> transactions)
        => _transactions = transactions.ToList();

    // ── Standard forward iterator (via IEnumerable + yield) ───────────────

    /// <summary>Forward chronological traversal — standard foreach.</summary>
    public IEnumerable<Transaction> All()
    {
        foreach (var txn in _transactions)
            yield return txn;
    }

    /// <summary>Reverse chronological (most recent first).</summary>
    public IEnumerable<Transaction> AllReverse()
    {
        for (int i = _transactions.Count - 1; i >= 0; i--)
            yield return _transactions[i];
    }

    /// <summary>Only failed transactions — filtered iterator.</summary>
    public IEnumerable<Transaction> Failed()
        => _transactions.Where(t => t.Status == "FAILED");

    /// <summary>Only successful transactions above a threshold (e.g., audit high-value).</summary>
    public IEnumerable<Transaction> HighValue(decimal threshold)
        => _transactions.Where(t => t.Status == "SUCCESS" && t.Amount >= threshold);

    /// <summary>Paginated iterator — page is 1-based.</summary>
    public IEnumerable<Transaction> Page(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _transactions.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}

// ── Custom stateful iterator: paginated cursor ────────────────

/// <summary>
/// Paginated cursor — supports HasNext, MoveNext, Prev navigation.
/// Useful when the UI needs to know "is there a next page?" without loading it.
/// C# note: Implements IEnumerator&lt;T&gt; manually for full control.
/// </summary>
public sealed class PaginatedTransactionCursor : IEnumerator<IReadOnlyList<Transaction>>
{
    private readonly IReadOnlyList<Transaction> _all;
    private readonly int _pageSize;
    private int _pageIndex = -1; // -1 = before first page

    public PaginatedTransactionCursor(IReadOnlyList<Transaction> transactions, int pageSize)
    {
        _all      = transactions;
        _pageSize = pageSize;
    }

    public IReadOnlyList<Transaction> Current
    {
        get
        {
            if (_pageIndex < 0) throw new InvalidOperationException("Call MoveNext() first.");
            return _all.Skip(_pageIndex * _pageSize).Take(_pageSize).ToList();
        }
    }

    object System.Collections.IEnumerator.Current => Current;

    public int TotalPages => (int)Math.Ceiling((double)_all.Count / _pageSize);
    public int CurrentPage => _pageIndex + 1;
    public bool HasNext    => _pageIndex < TotalPages - 1;
    public bool HasPrev    => _pageIndex > 0;

    public bool MoveNext()
    {
        if (!HasNext) return false;
        _pageIndex++;
        return true;
    }

    public bool MovePrev()
    {
        if (!HasPrev) return false;
        _pageIndex--;
        return true;
    }

    public void Reset() => _pageIndex = -1;
    public void Dispose() { /* no unmanaged resources */ }
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — REAL-WORLD USAGE
// ─────────────────────────────────────────────────────────────
// In a real payment system:
//   - TransactionHistory wraps a Dapper query or EF DbSet
//   - Page() maps to OFFSET / FETCH NEXT in SQL
//   - Failed() maps to WHERE Status = 'FAILED'
//   - Callers never touch the SQL; they just call Page(2, 20)
//
// With async (EF Core):
//   public async IAsyncEnumerable<Transaction> AllAsync([EnumeratorCancellation] CancellationToken ct)
//   {
//       await foreach (var txn in _dbContext.Transactions.AsAsyncEnumerable().WithCancellation(ct))
//           yield return txn;
//   }
// Caller: await foreach (var txn in history.AllAsync(ct)) { ... }

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO
// ─────────────────────────────────────────────────────────────

public static class IteratorDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Iterator Pattern — Transaction History ===\n");

        var history = new TransactionHistory(SeedTransactions());

        // ── All forward ────────────────────────────────────────────────────
        Console.WriteLine("── All transactions (chronological) ──");
        foreach (var txn in history.All())
            Print(txn);

        // ── Reverse ───────────────────────────────────────────────────────
        Console.WriteLine("\n── Reverse (most recent first) ──");
        foreach (var txn in history.AllReverse())
            Print(txn);

        // ── Filtered: failed ──────────────────────────────────────────────
        Console.WriteLine("\n── Failed transactions only ──");
        foreach (var txn in history.Failed())
            Print(txn);

        // ── Filtered: high value ──────────────────────────────────────────
        Console.WriteLine("\n── High-value success (≥ ₹5,000) ──");
        foreach (var txn in history.HighValue(5_000m))
            Print(txn);

        // ── Paginated ─────────────────────────────────────────────────────
        Console.WriteLine("\n── Paginated (page 1 of 3 records/page) ──");
        Console.WriteLine("Page 1:");
        foreach (var txn in history.Page(1, 3)) Print(txn);
        Console.WriteLine("Page 2:");
        foreach (var txn in history.Page(2, 3)) Print(txn);

        // ── Paginated cursor ──────────────────────────────────────────────
        Console.WriteLine("\n── Paginated Cursor (UI navigation) ──");
        var allTxns = history.All().ToList();
        var cursor  = new PaginatedTransactionCursor(allTxns, pageSize: 2);

        while (cursor.MoveNext())
        {
            Console.WriteLine($"  -- Page {cursor.CurrentPage}/{cursor.TotalPages} (HasNext={cursor.HasNext}) --");
            foreach (var txn in cursor.Current) Print(txn);
        }

        Console.WriteLine("\n✅ Iterator — understood.");
    }

    private static void Print(Transaction t)
        => Console.WriteLine($"  {t.TransactionId} | {t.OrderId} | ₹{t.Amount,8:N2} | {t.Status,-8} | {t.ProcessedAt:HH:mm:ss}");

    private static IEnumerable<Transaction> SeedTransactions()
    {
        var now = DateTime.UtcNow;
        return
        [
            new("TXN-001", "ORD-001", "CUST-01",  1_200m, "₹", "SUCCESS", now.AddHours(-5)),
            new("TXN-002", "ORD-002", "CUST-02",    500m, "₹", "FAILED",  now.AddHours(-4)),
            new("TXN-003", "ORD-003", "CUST-01",  8_999m, "₹", "SUCCESS", now.AddHours(-3)),
            new("TXN-004", "ORD-004", "CUST-03",  2_500m, "₹", "REFUNDED",now.AddHours(-2)),
            new("TXN-005", "ORD-005", "CUST-04", 12_000m, "₹", "SUCCESS", now.AddHours(-1)),
            new("TXN-006", "ORD-006", "CUST-02",    750m, "₹", "FAILED",  now.AddMinutes(-30)),
            new("TXN-007", "ORD-007", "CUST-05",  3_400m, "₹", "SUCCESS", now.AddMinutes(-10)),
        ];
    }
}
