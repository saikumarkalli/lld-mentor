/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏥 CONCEPT: Dependency Inversion Principle (DIP) — CAPSTONE                 ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: All 7 previous files.                                         ║
 * ║     Encapsulation gave us safe data. Abstraction gave us clean contracts.   ║
 * ║     Inheritance gave us shared identity. Polymorphism gave us open code.    ║
 * ║     Abstract/Interface gave us the right tool for each job. Composition     ║
 * ║     let us swap parts. ISP kept interfaces narrow. DIP wires it all.        ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  High-level modules should NOT depend on low-level modules. Both should     ║
 * ║  depend on abstractions.                                                     ║
 * ║  In code: HospitalManagementSystem receives its dependencies via constructor║
 * ║  injection. It never calls `new DatabasePatientRepo()` internally.          ║
 * ║  The CALLER decides what implementations to wire. The class stays testable, ║
 * ║  portable, and NABH-compliant.                                               ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  A hospital director doesn't hire a specific nurse by walking to nursing    ║
 * ║  school and picking one. They call HR and say "I need a nurse who can do    ║
 * ║  ICU shifts." HR (the composition root) finds the right person and assigns  ║
 * ║  them. The director works with a contract (interface), not a person.        ║
 * ║  If the nurse quits, HR assigns a replacement. The director never changes.  ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  NaiveHospitalManagementSystem had `new SqlPatientDatabase()` inside it.   ║
 * ║  When the hospital decided to move to a cloud EHR system, the developer     ║
 * ║  had to find every `new SqlPatientDatabase()` across 22 files and replace  ║
 * ║  them with `new CloudEhrAdapter()`. Six files were missed. For 11 days,    ║
 * ║  patient records written via the new UI were invisible to the old billing  ║
 * ║  module (still using SqlPatientDatabase). Three patients were billed ₹0.   ║
 * ║                                                                              ║
 * ║  THE DESIGN DECISION LENS:                                                   ║
 * ║  "If I want to test this class with a fake database, can I do it without   ║
 * ║   modifying the class?" If no → it violates DIP. Fix: inject the dependency.║
 * ║                                                                              ║
 * ║  QUICK RECALL (2-year cheat code):                                          ║
 * ║  `new` inside a class = a dependency nail. Constructor injection = a plug.  ║
 * ║  Plugs let you swap. Nails require a hammer.                                ║
 * ║                                                                              ║
 * ║  CAPSTONE — What you can now do:                                             ║
 * ║  Run the full patient journey: admit → prescribe → lab → blood → discharge ║
 * ║  with THREE different wiring configurations without touching HMS code.      ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.HospitalModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Hard-coded dependencies inside the high-level module
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Concrete SQL database — hard to swap, impossible to test without a DB.</summary>
public sealed class NaiveSqlPatientDatabase
{
    public void SavePatient(string id, string name) =>
        Console.WriteLine($"  [SQL] INSERT patient ({id}, {name})");
    public string? FindPatient(string id) =>
        id.StartsWith("P-") ? $"Patient:{id}" : null;
}

/// <summary>Concrete SMTP notifier — sends real emails in tests, can't be silenced.</summary>
public sealed class NaiveSmtpNotifier
{
    public void Send(string patientId, string message) =>
        Console.WriteLine($"  [SMTP→{patientId}] {message}");
}

/// <summary>
/// NAIVE: High-level HMS creates its own low-level dependencies.
/// To test: you need a real SQL server. To swap notifier: you edit this class.
/// To add HIPAA audit logging: you edit this class. Every "add feature" = edit class.
/// </summary>
public sealed class NaiveHospitalManagementSystem
{
    // Hard-coded — nailed in. Cannot be swapped.
    private readonly NaiveSqlPatientDatabase _db       = new();
    private readonly NaiveSmtpNotifier       _notifier = new();

    public void AdmitPatient(string patientId, string name)
    {
        _db.SavePatient(patientId, name);
        _notifier.Send(patientId, $"Welcome to Sunrise Hospital, {name}.");
        Console.WriteLine($"  [HMS-NAIVE] Patient {name} admitted.");
    }
}

// WHY IT FAILS:
// 1. UNTESTABLE: every test hits a real SQL server. CI fails without DB connectivity.
// 2. CLOUD MIGRATION: changing to CloudEhrAdapter requires editing NaiveHMS directly.
//    It was missed in 6 of 22 files → ₹0 billing incidents.
// 3. OPEN/CLOSED: every new feature (audit, fraud detection) modifies this class.
//    It grows to 600 lines and breaks on every sprint.
// 4. PARALLEL WORK: Frontend team can't mock HMS for UI testing — it's a monolith.

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — HospitalManagementSystem depends on abstractions only
// All dependencies injected. HMS never calls `new` on anything external.
// ─────────────────────────────────────────────────────────────────────────────

// ── HMS-SPECIFIC ABSTRACTIONS ─────────────────────────────────────────────────
// These live at the high-level module boundary. Low-level implementations
// implement these contracts — not the other way around.

/// <summary>
/// Central patient record store. Any storage backend implements this.
/// HMS depends on this contract, never on SqlPatientDatabase or CloudEhrAdapter.
/// </summary>
public interface IHmsPatientStore
{
    void          Upsert(PatientRecord record);
    PatientRecord? FindById(string patientId);
    IReadOnlyList<PatientRecord> GetAllActive();
}

/// <summary>
/// Outbound communication gateway.
/// Implemented by: SmtpGateway, SmsGateway, NullGateway (tests).
/// </summary>
public interface IHmsCommunicationGateway
{
    void SendAdmissionNotice(string patientId, string patientName);
    void SendDischargeNotice(string patientId, string patientName);
    void SendMedicationReminder(string patientId, string drug, string instructions);
    void SendEmergencyAlert(string patientId, string alert);
}

/// <summary>
/// HIPAA/NABH-compliant event log.
/// Implementation details (S3, local DB, cloud sink) are hidden from HMS.
/// </summary>
public interface IHmsAuditSink
{
    void Record(string eventType, string actor, string entityId, string detail);
    IReadOnlyList<string> GetRecentEvents(int count);
}

/// <summary>
/// Billing engine — calculates, records, and finalises invoices.
/// HMS delegates billing logic here; it never computes amounts itself.
/// </summary>
public interface IHmsBillingEngine
{
    void   AddCharge(string patientId, string description, decimal amount);
    decimal GetTotal(string patientId);
    void   Finalise(string patientId);
}

// ── CONCRETE IMPLEMENTATIONS ──────────────────────────────────────────────────

/// <summary>Cloud EHR store — swapped in for the new platform migration.</summary>
public sealed class InMemoryHmsPatientStore : IHmsPatientStore
{
    private readonly Dictionary<string, PatientRecord> _store = [];

    public void           Upsert(PatientRecord r)   => _store[r.PatientId] = r;
    public PatientRecord? FindById(string id)         => _store.GetValueOrDefault(id);
    public IReadOnlyList<PatientRecord> GetAllActive() => _store.Values.ToList().AsReadOnly();
}

/// <summary>Console gateway — used in development and integration tests.</summary>
public sealed class ConsoleHmsCommunicationGateway : IHmsCommunicationGateway
{
    public void SendAdmissionNotice(string id, string name)       => Console.WriteLine($"  [MSG→{id}] Welcome, {name}! Bed assigned.");
    public void SendDischargeNotice(string id, string name)       => Console.WriteLine($"  [MSG→{id}] {name} — discharge summary sent.");
    public void SendMedicationReminder(string id, string d, string i) => Console.WriteLine($"  [MSG→{id}] Reminder: {d} — {i}");
    public void SendEmergencyAlert(string id, string alert)        => Console.WriteLine($"  [ALERT→{id}] ⚠️  {alert}");
}

/// <summary>Null gateway — silently discards all messages in unit tests.</summary>
public sealed class NullHmsCommunicationGateway : IHmsCommunicationGateway
{
    public void SendAdmissionNotice(string id, string name)       { }
    public void SendDischargeNotice(string id, string name)       { }
    public void SendMedicationReminder(string id, string d, string i) { }
    public void SendEmergencyAlert(string id, string alert)        { }
}

/// <summary>In-memory audit sink — captures events for test assertions.</summary>
public sealed class InMemoryHmsAuditSink : IHmsAuditSink
{
    private readonly List<string> _events = [];

    public void Record(string type, string actor, string entity, string detail)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {type} | {actor} | {entity} | {detail}";
        _events.Add(entry);
    }

    public IReadOnlyList<string> GetRecentEvents(int count) =>
        _events.TakeLast(count).ToList().AsReadOnly();
}

/// <summary>Console audit sink — prints all events to stdout (staging/dev).</summary>
public sealed class ConsoleHmsAuditSink : IHmsAuditSink
{
    private readonly List<string> _events = [];

    public void Record(string type, string actor, string entity, string detail)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {type,-18} | {actor,-20} | {entity,-12} | {detail}";
        _events.Add(entry);
        Console.WriteLine($"  [AUDIT] {entry}");
    }

    public IReadOnlyList<string> GetRecentEvents(int count) =>
        _events.TakeLast(count).ToList().AsReadOnly();
}

/// <summary>In-memory billing engine — sufficient for demos and tests.</summary>
public sealed class InMemoryHmsBillingEngine : IHmsBillingEngine
{
    private readonly Dictionary<string, List<(string Desc, decimal Amt)>> _charges = [];

    public void AddCharge(string patientId, string description, decimal amount)
    {
        if (!_charges.ContainsKey(patientId)) _charges[patientId] = [];
        _charges[patientId].Add((description, amount));
        Console.WriteLine($"  [BILL] {patientId}: +₹{amount:N0} ({description})");
    }

    public decimal GetTotal(string patientId) =>
        _charges.TryGetValue(patientId, out var list) ? list.Sum(x => x.Amt) : 0;

    public void Finalise(string patientId)
    {
        var total = GetTotal(patientId);
        Console.WriteLine($"  [BILL] {patientId}: INVOICE FINALISED — Total ₹{total:N0}");
    }
}

// ── HIGH-LEVEL MODULE ─────────────────────────────────────────────────────────

/// <summary>
/// HospitalManagementSystem — the capstone orchestrator.
/// It depends on ZERO concrete classes. All 4 dependencies are injected.
/// Swap store → no code change here. Swap audit sink → no code change here.
/// Test with fakes → no code change here.
/// </summary>
public sealed class HospitalManagementSystem
{
    private readonly IHmsPatientStore          _store;
    private readonly IHmsCommunicationGateway  _comm;
    private readonly IHmsAuditSink             _audit;
    private readonly IHmsBillingEngine         _billing;

    public HospitalManagementSystem(
        IHmsPatientStore         store,
        IHmsCommunicationGateway comm,
        IHmsAuditSink            audit,
        IHmsBillingEngine        billing)
    {
        _store   = store;
        _comm    = comm;
        _audit   = audit;
        _billing = billing;
    }

    /// <summary>Full admission flow: record → notify → audit → initial charge.</summary>
    public PatientRecord AdmitPatient(string patientId, string name, string admittedBy, decimal estimatedCost)
    {
        var record = new PatientRecord(patientId, name, "O+");
        _store.Upsert(record);
        _billing.AddCharge(patientId, "Admission fee", estimatedCost);
        _comm.SendAdmissionNotice(patientId, name);
        _audit.Record("ADMIT", admittedBy, patientId, $"Estimated cost: ₹{estimatedCost:N0}");
        Console.WriteLine($"  ✓ Admitted: {name} ({patientId})");
        return record;
    }

    /// <summary>Prescribe medication: update record → notify patient → audit.</summary>
    public void PrescribeMedication(string patientId, string drug, string dosage,
                                     Doctor credential, decimal drugCost)
    {
        var record = _store.FindById(patientId);
        if (record is null) { Console.WriteLine($"  ⚠️  {patientId} not found."); return; }

        record.AddMedication(drug, dosage, credential);
        _store.Upsert(record);
        _billing.AddCharge(patientId, $"Medication: {drug}", drugCost);
        _comm.SendMedicationReminder(patientId, drug, dosage);
        _audit.Record("PRESCRIBE", credential.Name, patientId, $"{drug} {dosage}");
        Console.WriteLine($"  ✓ Prescribed: {drug} for {patientId}");
    }

    /// <summary>Emergency alert: notify immediately, no billing, urgent audit.</summary>
    public void RaiseEmergency(string patientId, string alertDescription, string raisedBy)
    {
        _comm.SendEmergencyAlert(patientId, alertDescription);
        _audit.Record("EMERGENCY", raisedBy, patientId, alertDescription);
        Console.WriteLine($"  🚨 Emergency raised for {patientId}: {alertDescription}");
    }

    /// <summary>Full discharge: update record → finalise bill → notify → audit.</summary>
    public void DischargePatient(string patientId, string dischargedBy,
                                  Doctor signingDoctor,
                                  string diagnosis, string followUpInstructions)
    {
        var record = _store.FindById(patientId);
        if (record is null) { Console.WriteLine($"  ⚠️  {patientId} not found."); return; }

        record.SignDischargeSummary($"{diagnosis}. {followUpInstructions}", signingDoctor);
        _store.Upsert(record);
        _billing.Finalise(patientId);
        _comm.SendDischargeNotice(patientId, record.PatientName);
        _audit.Record("DISCHARGE", dischargedBy, patientId, $"Diagnosis: {diagnosis}");
        Console.WriteLine($"  ✓ Discharged: {record.PatientName} ({patientId})");
    }

    public IReadOnlyList<PatientRecord> GetActiveCensus() => _store.GetAllActive();
}

// ─────────────────────────────────────────────────────────────────────────────
// 🎬 DEMO — Three wiring configurations, same HMS class
// ─────────────────────────────────────────────────────────────────────────────
public static class DependencyInversionDemo
{
    public static void Demo()
    {
        Console.WriteLine("=== Dependency Inversion: Hospital Management System (Capstone) ===\n");

        // Doctor credential (used for medication auth and discharge signing)
        var doctorCred = new Doctor("D-001", "Dr. Priya Nair", "Cardiology", "MCI-PN-001");

        // ════════════════════════════════════════════════════════════════
        // CONFIGURATION 1 — Production: console output, console audit
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine("┌─ Config 1: Production wiring ─────────────────────────────────┐");
        var hms = new HospitalManagementSystem(
            store:   new InMemoryHmsPatientStore(),
            comm:    new ConsoleHmsCommunicationGateway(),
            audit:   new ConsoleHmsAuditSink(),
            billing: new InMemoryHmsBillingEngine()
        );

        // Full patient journey
        hms.AdmitPatient("P-3001", "Anjali Singh", "Desk-Officer-2", 45_000m);
        hms.PrescribeMedication("P-3001", "Amlodipine",   "5mg OD",  doctorCred, 350m);
        hms.PrescribeMedication("P-3001", "Atorvastatin", "10mg OD", doctorCred, 420m);
        hms.RaiseEmergency("P-3001", "Blood pressure spike — 190/120", "Nurse Kavitha");
        hms.DischargePatient("P-3001", "Desk-Officer-2", doctorCred, "Hypertension", "Follow-up in 2 weeks");

        Console.WriteLine($"└─ Census after discharge: {hms.GetActiveCensus().Count} active\n");

        // ════════════════════════════════════════════════════════════════
        // CONFIGURATION 2 — Testing: null comms, in-memory audit, verify events
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine("┌─ Config 2: Test wiring (null gateway, capturable audit) ───────┐");
        var testAudit   = new InMemoryHmsAuditSink();
        var testBilling = new InMemoryHmsBillingEngine();
        var testHms = new HospitalManagementSystem(
            store:   new InMemoryHmsPatientStore(),
            comm:    new NullHmsCommunicationGateway(), // ← silent
            audit:   testAudit,
            billing: testBilling
        );

        testHms.AdmitPatient("P-TEST-01", "Test Patient Alpha", "TestStaff", 30_000m);
        testHms.PrescribeMedication("P-TEST-01", "TestDrug X", "1 tab OD", doctorCred, 100m);
        testHms.DischargePatient("P-TEST-01", "TestStaff", doctorCred, "Recovered", "None");

        Console.WriteLine($"\n  Audit events captured ({testAudit.GetRecentEvents(10).Count}):");
        foreach (var ev in testAudit.GetRecentEvents(10))
            Console.WriteLine($"    {ev}");

        Console.WriteLine($"\n  Billing total for P-TEST-01: ₹{testBilling.GetTotal("P-TEST-01"):N0}");
        Console.WriteLine("└────────────────────────────────────────────────────────────────\n");

        // ════════════════════════════════════════════════════════════════
        // CONFIGURATION 3 — Same HMS, two patients concurrently
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine("┌─ Config 3: Shared store, two concurrent patients ───────────────┐");
        var sharedStore = new InMemoryHmsPatientStore();
        var multiHms = new HospitalManagementSystem(
            store:   sharedStore,
            comm:    new ConsoleHmsCommunicationGateway(),
            audit:   new ConsoleHmsAuditSink(),
            billing: new InMemoryHmsBillingEngine()
        );

        multiHms.AdmitPatient("P-4001", "Ramesh Iyer",   "Desk-A", 60_000m);
        multiHms.AdmitPatient("P-4002", "Sunita Kulkarni", "Desk-B", 40_000m);

        Console.WriteLine($"\n  Active census: {multiHms.GetActiveCensus().Count} patients");
        foreach (var r in multiHms.GetActiveCensus())
            Console.WriteLine($"    {r.PatientId}: {r.PatientName}");

        Console.WriteLine("└────────────────────────────────────────────────────────────────\n");

        Console.WriteLine("  CAPSTONE CONFIRMED:");
        Console.WriteLine("  HospitalManagementSystem contains ZERO `new` calls on dependencies.");
        Console.WriteLine("  Swap store/comm/audit/billing → zero edits to HospitalManagementSystem.");
        Console.WriteLine("  This is the dividend of all 8 OOP principles working together.");
    }
}
