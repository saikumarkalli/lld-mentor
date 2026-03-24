/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏥 CONCEPT: Composition vs Inheritance                                      ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 03_Inheritance.cs + 05_AbstractClass_vs_Interface.cs          ║
 * ║     We know inheritance is for shared identity. Now we ask: "When a service  ║
 * ║     needs audit logging — should it INHERIT an AuditLogger or HOLD one?"    ║
 * ║     The answer is always: HOLD.                                              ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Composition: a class HOLDS a reference to a capability object and          ║
 * ║    delegates to it. PatientCareService holds an IPatientAuditLogger.        ║
 * ║  Inheritance (misused): a class EXTENDS a capability base class.            ║
 * ║    PatientCareService : AuditLogger — now they're tightly welded.           ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A hospital's PatientCareService inherited AuditLogger to "get logging for  ║
 * ║  free." Three months later: "We need to log to a HIPAA-compliant S3 bucket  ║
 * ║  instead of the local DB." The developer realized the log destination was  ║
 * ║  baked into the base class. Changing it required modifying 14 services that ║
 * ║  all inherited AuditLogger. During the 3-week refactor, two services sent   ║
 * ║  audit events to the old destination — a HIPAA compliance violation.        ║
 * ║                                                                              ║
 * ║  THE DESIGN DECISION LENS:                                                   ║
 * ║  "Does this class IS-A logger, or does it USE-A logger?"                    ║
 * ║  PatientCareService is not a logger. It uses a logger. Composition.         ║
 * ║                                                                              ║
 * ║  QUICK RECALL (2-year cheat code):                                          ║
 * ║  Swap the logger from DB to S3 by changing ONE constructor argument.        ║
 * ║  With inheritance you'd need a refactor across 14 services.                 ║
 * ║                                                                              ║
 * ║  CONNECTS FORWARD:                                                           ║
 * ║  File 8 (Dependency Inversion) shows how to wire these composed dependencies║
 * ║  from outside the class — the full DI capstone.                              ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.HospitalModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Naive inheritance-as-reuse
// "AuditLogger has useful methods. I'll extend it to get them for free."
// ─────────────────────────────────────────────────────────────────────────────

public class NaiveAuditLogger
{
    private readonly List<string> _log = [];

    protected void LogAction(string actor, string action, string detail)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {actor} | {action} | {detail}";
        _log.Add(entry);
        Console.WriteLine($"  [AUDIT] {entry}");
    }

    public IReadOnlyList<string> GetLog() => _log.AsReadOnly();
}

public class NaiveFraudDetector
{
    public bool IsSuspicious(string patientId, decimal amount) =>
        amount > 500_000m; // overly simple — stand-in for real logic
}

/// <summary>
/// NAIVE: Inherits AuditLogger to get logging "for free".
/// Also inherits FraudDetector? C# doesn't allow multiple inheritance —
/// so this developer hard-codes a new FraudDetector() inside the class.
/// Two design violations in one class.
/// </summary>
public class NaivePatientCareService : NaiveAuditLogger
{
    // Hard-coded fraud detector — can't be swapped, can't be tested independently
    private readonly NaiveFraudDetector _fraudDetector = new();

    public void AdmitPatient(string patientId, string admittedBy, decimal estimatedCost)
    {
        // Fraud check baked in — can't inject a test double
        if (_fraudDetector.IsSuspicious(patientId, estimatedCost))
        {
            LogAction(admittedBy, "FRAUD_FLAG", $"Suspicious amount: ₹{estimatedCost:N0}");
            return;
        }

        LogAction(admittedBy, "ADMIT", $"Patient {patientId} admitted. Estimated: ₹{estimatedCost:N0}");
        Console.WriteLine($"  Patient {patientId} admitted by {admittedBy}.");
    }

    public void DischargePatient(string patientId, string dischargedBy)
    {
        LogAction(dischargedBy, "DISCHARGE", $"Patient {patientId} discharged.");
        Console.WriteLine($"  Patient {patientId} discharged by {dischargedBy}.");
    }
}

// WHY IT FAILS:
// 1. NaivePatientCareService IS-NOT-A logger. It USES-A logger. Inheritance is wrong.
// 2. To swap logging to S3/Kafka/HIPAA store: must modify the base class.
//    All 14 services inheriting it are affected — regression risk is enormous.
// 3. NaiveFraudDetector is hard-coded with `new` — untestable. You can't inject
//    a "never-flag" test double to test the happy admission path in isolation.
// 4. C# single-inheritance limit: if you needed a second parent (e.g., NotificationBase),
//    you're stuck. Composition has no such limit.

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — Composition: hold capabilities, inject them, swap at will
// ─────────────────────────────────────────────────────────────────────────────

// ── CAPABILITY INTERFACES ────────────────────────────────────────────────────

/// <summary>Write-only append-log for compliance audit events.</summary>
public interface IPatientAuditLogger
{
    void LogAdmission(string patientId,  string actor, decimal estimatedCost);
    void LogDischarge(string patientId,  string actor);
    void LogFraudFlag(string patientId,  string actor, string reason);
    void LogMedication(string patientId, string actor, string drug);
}

/// <summary>Patient repository — read/write operations on patient records.</summary>
public interface IPatientRepository
{
    PatientRecord? FindById(string patientId);
    void           Save(PatientRecord record);
    bool           Exists(string patientId);
}

/// <summary>Outbound notifications to patients and their next-of-kin.</summary>
public interface IPatientNotifier
{
    void NotifyAdmission(string  patientId, string message);
    void NotifyDischarge(string  patientId, string message);
    void NotifyMedication(string patientId, string drug, string instructions);
}

/// <summary>Flags suspicious billing patterns for review.</summary>
public interface IBillingFraudDetector
{
    bool IsSuspicious(string patientId, decimal amount);
    string GetFlagReason(string patientId, decimal amount);
}

// ── CONCRETE IMPLEMENTATIONS (swappable) ─────────────────────────────────────

/// <summary>Logs to the local console — used in development.</summary>
public sealed class ConsolePatientAuditLogger : IPatientAuditLogger
{
    public void LogAdmission(string p, string actor, decimal cost) =>
        Console.WriteLine($"  [AUDIT:ADMIT]    {p} by {actor} | ₹{cost:N0}");
    public void LogDischarge(string p, string actor) =>
        Console.WriteLine($"  [AUDIT:DISCHARGE] {p} by {actor}");
    public void LogFraudFlag(string p, string actor, string reason) =>
        Console.WriteLine($"  [AUDIT:FRAUD]    {p} flagged by {actor} | {reason}");
    public void LogMedication(string p, string actor, string drug) =>
        Console.WriteLine($"  [AUDIT:MED]      {p} | {drug} by {actor}");
}

/// <summary>
/// In-memory audit logger — captures all events for testing and assertion.
/// Swap this in during unit tests to verify exactly what was logged.
/// </summary>
public sealed class InMemoryPatientAuditLogger : IPatientAuditLogger
{
    private readonly List<string> _events = [];
    public IReadOnlyList<string> Events => _events.AsReadOnly();

    public void LogAdmission(string p, string actor, decimal cost) =>
        _events.Add($"ADMIT:{p}|{actor}|{cost}");
    public void LogDischarge(string p, string actor) =>
        _events.Add($"DISCHARGE:{p}|{actor}");
    public void LogFraudFlag(string p, string actor, string reason) =>
        _events.Add($"FRAUD:{p}|{actor}|{reason}");
    public void LogMedication(string p, string actor, string drug) =>
        _events.Add($"MED:{p}|{actor}|{drug}");
}

/// <summary>Silently discards all notifications — used in tests.</summary>
public sealed class NullPatientNotifier : IPatientNotifier
{
    public void NotifyAdmission(string p,  string m) { }
    public void NotifyDischarge(string p,  string m) { }
    public void NotifyMedication(string p, string d, string i) { }
}

/// <summary>Logs notifications to console — used in staging/production.</summary>
public sealed class ConsolePatientNotifier : IPatientNotifier
{
    public void NotifyAdmission(string p, string msg)         => Console.WriteLine($"  [SMS] {p}: {msg}");
    public void NotifyDischarge(string p, string msg)         => Console.WriteLine($"  [SMS] {p}: {msg}");
    public void NotifyMedication(string p, string d, string i)=> Console.WriteLine($"  [SMS] {p}: {d} — {i}");
}

/// <summary>Flags anything over ₹5L as suspicious — real rules would use ML.</summary>
public sealed class ThresholdBillingFraudDetector : IBillingFraudDetector
{
    private readonly decimal _threshold;
    public ThresholdBillingFraudDetector(decimal threshold = 500_000m) => _threshold = threshold;

    public bool   IsSuspicious(string p, decimal amount)      => amount > _threshold;
    public string GetFlagReason(string p, decimal amount)      => $"Amount ₹{amount:N0} exceeds threshold ₹{_threshold:N0}";
}

/// <summary>Never flags anything — safe for happy-path unit tests.</summary>
public sealed class NullBillingFraudDetector : IBillingFraudDetector
{
    public bool   IsSuspicious(string p, decimal amount) => false;
    public string GetFlagReason(string p, decimal amount) => string.Empty;
}

/// <summary>Simple in-memory patient store.</summary>
public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly Dictionary<string, PatientRecord> _store = [];

    public PatientRecord? FindById(string id) => _store.GetValueOrDefault(id);
    public void           Save(PatientRecord r) => _store[r.PatientId] = r;
    public bool           Exists(string id)    => _store.ContainsKey(id);
}

// ── THE COMPOSED SERVICE ──────────────────────────────────────────────────────

/// <summary>
/// PatientCareService HOLDS its capabilities via constructor injection.
/// It does not inherit from anything except object.
/// To swap the audit logger from console → HIPAA S3: change one constructor argument.
/// </summary>
public sealed class PatientCareService
{
    private readonly IPatientRepository    _repo;
    private readonly IPatientAuditLogger   _audit;
    private readonly IPatientNotifier      _notifier;
    private readonly IBillingFraudDetector _fraud;

    public PatientCareService(
        IPatientRepository    repo,
        IPatientAuditLogger   audit,
        IPatientNotifier      notifier,
        IBillingFraudDetector fraud)
    {
        _repo     = repo;
        _audit    = audit;
        _notifier = notifier;
        _fraud    = fraud;
    }

    /// <summary>
    /// Admits a new patient. Checks fraud, saves record, notifies, logs audit.
    /// All four behaviours are swappable. None are inherited.
    /// </summary>
    public bool AdmitPatient(string patientId, string patientName, string admittedBy, decimal estimatedCost)
    {
        if (_fraud.IsSuspicious(patientId, estimatedCost))
        {
            _audit.LogFraudFlag(patientId, admittedBy, _fraud.GetFlagReason(patientId, estimatedCost));
            Console.WriteLine($"  ⚠️  Admission for {patientId} flagged for review.");
            return false;
        }

        if (_repo.Exists(patientId))
        {
            Console.WriteLine($"  ⚠️  Patient {patientId} already admitted.");
            return false;
        }

        var record = new PatientRecord(patientId, patientName, "O+");
        _repo.Save(record);
        _audit.LogAdmission(patientId, admittedBy, estimatedCost);
        _notifier.NotifyAdmission(patientId, $"You have been admitted to Sunrise Hospital. Ref: {patientId}");
        Console.WriteLine($"  ✓ Patient {patientId} ({patientName}) admitted by {admittedBy}.");
        return true;
    }

    /// <summary>Logs a medication entry for an admitted patient.</summary>
    public void RecordMedication(string patientId, string drug, Doctor credential)
    {
        var record = _repo.FindById(patientId);
        if (record is null)
        {
            Console.WriteLine($"  ⚠️  Patient {patientId} not found.");
            return;
        }

        record.AddMedication(drug, "1 tablet twice daily", credential);
        _repo.Save(record);
        _audit.LogMedication(patientId, credential.Name, drug);
        _notifier.NotifyMedication(patientId, drug, "1 tablet twice daily after meals");
    }

    /// <summary>Discharges an admitted patient.</summary>
    public void DischargePatient(string patientId, string dischargedBy, Doctor signingDoctor)
    {
        var record = _repo.FindById(patientId);
        if (record is null) { Console.WriteLine($"  ⚠️  Patient {patientId} not found."); return; }

        record.SignDischargeSummary("Recovered — follow-up in 2 weeks.", signingDoctor);
        _repo.Save(record);
        _audit.LogDischarge(patientId, dischargedBy);
        _notifier.NotifyDischarge(patientId, "You have been discharged. Please collect your discharge summary.");
        Console.WriteLine($"  ✓ Patient {patientId} discharged by {dischargedBy}.");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🎬 DEMO
// ─────────────────────────────────────────────────────────────────────────────
public static class CompositionVsInheritanceDemo
{
    public static void Demo()
    {
        Console.WriteLine("=== Composition vs Inheritance: Patient Care Service ===\n");

        var doctorCredential = new Doctor("D-303", "Dr. Arjun Mehta", "Internal Medicine", "MCI-AM-303");

        // ── Scenario 1: Production wiring (console logger + real notifier) ──
        Console.WriteLine("── Production Wiring ──");
        var prodService = new PatientCareService(
            repo:     new InMemoryPatientRepository(),
            audit:    new ConsolePatientAuditLogger(),
            notifier: new ConsolePatientNotifier(),
            fraud:    new ThresholdBillingFraudDetector(threshold: 500_000m)
        );

        prodService.AdmitPatient("P-1001", "Meena Joshi", "Staff-Desk", 85_000m);
        prodService.RecordMedication("P-1001", "Metformin 500mg", doctorCredential);
        prodService.DischargePatient("P-1001", "Staff-Desk", doctorCredential);

        // ── Scenario 2: Fraud flag triggers ──
        Console.WriteLine("\n── Fraud Flag Scenario ──");
        var fraudService = new PatientCareService(
            repo:     new InMemoryPatientRepository(),
            audit:    new ConsolePatientAuditLogger(),
            notifier: new NullPatientNotifier(),
            fraud:    new ThresholdBillingFraudDetector(threshold: 500_000m)
        );
        fraudService.AdmitPatient("P-1002", "Suspicious Corp", "Staff-Desk", 950_000m);

        // ── Scenario 3: Test wiring — in-memory logger, null notifier ──
        Console.WriteLine("\n── Test Wiring (unit test simulation) ──");
        var testAudit = new InMemoryPatientAuditLogger();
        var testService = new PatientCareService(
            repo:     new InMemoryPatientRepository(),
            audit:    testAudit,
            notifier: new NullPatientNotifier(),   // ← silent in tests
            fraud:    new NullBillingFraudDetector() // ← never flags
        );

        testService.AdmitPatient("P-TEST-01", "Test Patient", "TestStaff", 50_000m);
        testService.RecordMedication("P-TEST-01", "TestDrug", doctorCredential);

        Console.WriteLine($"\n  Test audit captured {testAudit.Events.Count} events:");
        foreach (var ev in testAudit.Events)
            Console.WriteLine($"    {ev}");

        Console.WriteLine("\n  KEY INSIGHT:");
        Console.WriteLine("  Swapping audit logger from Console → HIPAA S3: change ONE constructor arg.");
        Console.WriteLine("  With inheritance: refactor 14 service classes. With composition: 1 line.");
    }
}
