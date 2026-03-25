// ═══════════════════════════════════════════════════════════
// Pattern  : Singleton
// Category : Creational
// Intent   : Ensure a class has only one instance and provide a global point of access to it.
// Domain   : CartSessionManager — one cart session per application lifetime
// Kudvenkat: Video 2–7 — Singleton Design Pattern
// ═══════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────
// SECTION 1 — THE PROBLEM (no Singleton yet)
// ─────────────────────────────────────────────────────────────
// Imagine every service just does: new CartSessionManager()
// Each call creates a NEW instance → cart state is lost between calls.
//
// BUG SCENARIO:
//   var cart1 = new NaiveCartSessionManager();
//   cart1.AddItem("Laptop");
//
//   var cart2 = new NaiveCartSessionManager();   // different object!
//   cart2.GetItems();                            // returns empty — Laptop is gone
//
// Without Singleton, two parts of the app can hold DIFFERENT cart states
// simultaneously, leading to silent data loss.

namespace LLDMaster.Patterns.Creational.Singleton;

// ─────────────────────────────────────────────────────────────
// SECTION 2 — NAIVE (broken) version — shows the problem
// ─────────────────────────────────────────────────────────────

/// <summary>
/// NAIVE — do NOT use. Shows the problem: each caller gets its own instance.
/// </summary>
public class NaiveCartSessionManager
{
    private readonly List<string> _items = [];

    public void AddItem(string productName) => _items.Add(productName);
    public IReadOnlyList<string> GetItems() => _items.AsReadOnly();
}

// ─────────────────────────────────────────────────────────────
// SECTION 3 — SINGLETON IMPLEMENTATIONS (4 flavours, C# specific)
// ─────────────────────────────────────────────────────────────

// ── 3A. Classic lock-based (thread-safe, .NET Framework era) ─────────────────

/// <summary>
/// Thread-safe Singleton using double-checked locking.
/// C# note: <c>volatile</c> prevents the CPU from reordering instructions
/// and ensures the null-check reads a fully constructed object.
/// </summary>
public sealed class CartSessionManagerV1
{
    // C# best practice: volatile + double-checked locking for thread safety
    private static volatile CartSessionManagerV1? _instance;
    private static readonly object _lock = new();

    private readonly List<string> _items = [];

    // Private constructor blocks external instantiation
    private CartSessionManagerV1() { }

    /// <summary>Gets the single application-wide cart session instance.</summary>
    public static CartSessionManagerV1 Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    // Double-check: another thread may have created it while we waited
                    _instance ??= new CartSessionManagerV1();
                }
            }
            return _instance;
        }
    }

    public void AddItem(string productName) => _items.Add(productName);
    public IReadOnlyList<string> GetItems() => _items.AsReadOnly();
    public void Clear() => _items.Clear();
}

// ── 3B. Lazy<T> — idiomatic modern C# ───────────────────────────────────────

/// <summary>
/// Thread-safe Singleton using <see cref="Lazy{T}"/>.
/// C# note: <c>Lazy&lt;T&gt;</c> with default LazyThreadSafetyMode.ExecutionAndPublication
/// gives you thread-safety + lazy init with zero boilerplate.
/// This is the PREFERRED approach in .NET 8+ for most cases.
/// </summary>
public sealed class CartSessionManagerV2
{
    // Lazy<T> handles thread safety and deferred construction automatically
    private static readonly Lazy<CartSessionManagerV2> _lazy =
        new(() => new CartSessionManagerV2());

    private readonly List<string> _items = [];

    private CartSessionManagerV2() { }

    /// <summary>Gets the single application-wide cart session instance.</summary>
    public static CartSessionManagerV2 Instance => _lazy.Value;

    public void AddItem(string productName) => _items.Add(productName);
    public IReadOnlyList<string> GetItems() => _items.AsReadOnly();
    public void Clear() => _items.Clear();
}

// ── 3C. Static field initialiser — simplest, always eager ───────────────────

/// <summary>
/// Eager Singleton: instance is created when the class is first accessed.
/// C# note: The CLR guarantees static field initialisation is thread-safe.
/// Use when startup cost of creation is negligible.
/// </summary>
public sealed class CartSessionManagerV3
{
    // CLR initialises this exactly once, thread-safely, at class-load time
    private static readonly CartSessionManagerV3 _instance = new();

    private readonly List<string> _items = [];

    private CartSessionManagerV3() { }

    /// <summary>Gets the single application-wide cart session instance.</summary>
    public static CartSessionManagerV3 Instance => _instance;

    public void AddItem(string productName) => _items.Add(productName);
    public IReadOnlyList<string> GetItems() => _items.AsReadOnly();
    public void Clear() => _items.Clear();
}

// ── 3D. DI-friendly Singleton — the ASP.NET Core way ────────────────────────

/// <summary>
/// DI-Friendly Singleton: registered as <c>services.AddSingleton&lt;ICartSession, CartSession&gt;()</c>.
/// C# note: In ASP.NET Core, prefer DI over the static Instance pattern — it is
/// testable, replaceable, and plays well with the service lifetime model.
/// The constructor is PUBLIC so the DI container can call it.
/// </summary>
public interface ICartSession
{
    void AddItem(string productName);
    IReadOnlyList<string> GetItems();
    void Clear();
}

/// <summary>
/// Concrete implementation registered as Singleton in the DI container.
/// No static members needed — the container owns the lifetime.
/// </summary>
public sealed class CartSession : ICartSession
{
    private readonly List<string> _items = [];

    // C# primary constructor — clean, no boilerplate needed here
    public CartSession() { }

    public void AddItem(string productName) => _items.Add(productName);
    public IReadOnlyList<string> GetItems() => _items.AsReadOnly();
    public void Clear() => _items.Clear();
}

// ─────────────────────────────────────────────────────────────
// SECTION 4 — DEMO (runnable, no test framework needed)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Demonstrates each Singleton variant. Call <see cref="Run"/> from Program.cs.
/// </summary>
public static class SingletonDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Singleton Pattern Demo ===\n");

        // ── PROBLEM demonstration ────────────────────────────────────────────
        Console.WriteLine("── PROBLEM: Naive (no Singleton) ──");
        var naive1 = new NaiveCartSessionManager();
        naive1.AddItem("Laptop");

        var naive2 = new NaiveCartSessionManager(); // different object!
        Console.WriteLine($"naive2 items: {naive2.GetItems().Count}"); // 0 — bug!
        Console.WriteLine("Laptop added to naive1 is LOST in naive2.\n");

        // ── V1: Double-checked locking ────────────────────────────────────────
        Console.WriteLine("── V1: Double-checked locking ──");
        var v1a = CartSessionManagerV1.Instance;
        var v1b = CartSessionManagerV1.Instance;
        v1a.AddItem("Headphones");
        Console.WriteLine($"v1a == v1b : {ReferenceEquals(v1a, v1b)}");  // True
        Console.WriteLine($"v1b items  : {string.Join(", ", v1b.GetItems())}\n");
        v1a.Clear();

        // ── V2: Lazy<T> (preferred in .NET 8) ───────────────────────────────
        Console.WriteLine("── V2: Lazy<T> (preferred) ──");
        var v2a = CartSessionManagerV2.Instance;
        var v2b = CartSessionManagerV2.Instance;
        v2a.AddItem("Mechanical Keyboard");
        Console.WriteLine($"v2a == v2b : {ReferenceEquals(v2a, v2b)}");  // True
        Console.WriteLine($"v2b items  : {string.Join(", ", v2b.GetItems())}\n");
        v2a.Clear();

        // ── V3: Static field (eager) ─────────────────────────────────────────
        Console.WriteLine("── V3: Static field initialiser (eager) ──");
        var v3a = CartSessionManagerV3.Instance;
        var v3b = CartSessionManagerV3.Instance;
        v3a.AddItem("USB-C Hub");
        Console.WriteLine($"v3a == v3b : {ReferenceEquals(v3a, v3b)}");  // True
        Console.WriteLine($"v3b items  : {string.Join(", ", v3b.GetItems())}\n");
        v3a.Clear();

        // ── V4: DI-friendly (show the concept; no real container here) ───────
        Console.WriteLine("── V4: DI-friendly (ASP.NET Core style) ──");
        Console.WriteLine("In Startup: services.AddSingleton<ICartSession, CartSession>();");
        Console.WriteLine("The DI container ensures exactly one CartSession lives per app.\n");

        Console.WriteLine("=== Demo complete ===");
    }
}
