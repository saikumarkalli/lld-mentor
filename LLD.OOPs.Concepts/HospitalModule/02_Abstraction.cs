/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏥 CONCEPT: Abstraction                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 01_Encapsulation.cs                                           ║
 * ║     PatientRecord now owns and enforces its own data rules. Abstraction     ║
 * ║     goes one level up: hiding HOW the admission service orchestrates all    ║
 * ║     those rules — callers see only a clean 3-method contract.               ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Abstraction means showing a simplified interface to the outside world and  ║
 * ║  hiding the implementation details behind it. The 80-line AdmitPatient()   ║
 * ║  becomes invisible — callers press one button, the system handles the rest. ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  Hospital reception desk. You say "I need to admit this patient."           ║
 * ║  You don't manage the bed allocation system, call the insurance API,        ║
 * ║  run the fraud check, or configure the SMS notification. Reception does     ║
 * ║  all of that invisibly. You filled one form. That's the abstraction.        ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A hospital's mobile app team called AdmitPatient() directly — it was 90   ║
 * ║  lines of mixed concerns. When the insurance team switched to a new API,   ║
 * ║  they changed a parameter in the insurance call. The mobile app sent the   ║
 * ║  old parameter name. The new API silently ignored unknown fields.           ║
 * ║  For 6 weeks, every mobile-admitted patient had zero insurance coverage    ║
 * ║  processed. 843 patients were billed the full amount instead of insured.   ║
 * ║  Cause: no abstraction boundary. Callers were coupled to implementation.   ║
 * ║                                                                              ║
 * ║  THE DESIGN DECISION LENS:                                                   ║
 * ║  "What does a caller NEED to know vs what should be HIDDEN?"               ║
 * ║                                                                              ║
 * ║  QUICK RECALL (2-year cheat code):                                          ║
 * ║  Callers know Admit/Discharge/Transfer. Nothing else. Ever.                ║
 * ║                                                                              ║
 * ║  CONNECTS FORWARD:                                                           ║
 * ║  IBedManagement, IInsuranceVerifier are abstractions. File 3 (Inheritance) ║
 * ║  creates staff types that ALSO hide complexity behind a shared contract.    ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.HospitalModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Naive implementation
// "AdmitPatient does one thing — it admits a patient." Except it does seven.
// Every sub-system is inline. Every caller is coupled to every implementation.
// ─────────────────────────────────────────────────────────────────────────────

public class NaiveHospitalOpsService
{
    // ❌ This one method has 7 reasons to change. One for each system it touches.
    public PatientRecord? AdmitPatient(string patientName, string diagnosis, string insuranceId)
    {
        // Step 1 — validate inline
        if (string.IsNullOrWhiteSpace(patientName)) return null;

        // Step 2 — check bed availability (SQL inline)
        var availableBed = GetAvailableBed_SqlInline();  // ← hardcoded SQL
        if (availableBed == null) { Console.WriteLine("   [NAIVE] No beds available."); return null; }

        // Step 3 — verify insurance (HTTP call to Ayushman Bharat API inline)
        var insuranceApproved = VerifyInsurance_HttpInline(insuranceId);  // ← hardcoded API
        if (!insuranceApproved) { Console.WriteLine("   [NAIVE] Insurance rejected."); return null; }

        // Step 4 — create patient record (business logic inline)
        var record  = new PatientRecord(Guid.NewGuid().ToString("N")[..8], patientName, "Unknown");
        var doctor  = new Doctor("SYS", "Duty Doctor", "General", "SYS-001");
        record.UpdateDiagnosis(diagnosis, doctor);

        // Step 5 — assign bed (another SQL update inline)
        AssignBed_SqlInline(availableBed, record.PatientId);  // ← hardcoded SQL

        // Step 6 — send SMS to family (Twilio inline)
        SendSms_TwilioInline(patientName);  // ← hardcoded Twilio

        // Step 7 — write to audit system (yet another DB call inline)
        WriteAdmissionAudit_DbInline(record.PatientId);  // ← hardcoded audit DB

        Console.WriteLine($"   [NAIVE] Admitted: {record.PatientName}");
        return record;
    }

    // 💥 IMMEDIATE PROBLEMS:
    // — Change Twilio to AWS SNS → edit AdmitPatient().
    // — Change insurance API → edit AdmitPatient().
    // — Change bed algorithm → edit AdmitPatient().
    // — To test "does admitting create a PatientRecord?" you need: live SQL, live Twilio,
    //   live insurance API. A dev can't run tests from home. CI takes 45 min.
    // — This method throws HttpRequestException, SqlException — callers now know internals.

    private string? GetAvailableBed_SqlInline()   => "BED-007";
    private bool    VerifyInsurance_HttpInline(string id) => !string.IsNullOrEmpty(id);
    private void    AssignBed_SqlInline(string bed, string pid)  =>
        Console.WriteLine($"   [SQL] Assigned {bed} to patient {pid}.");
    private void    SendSms_TwilioInline(string name) =>
        Console.WriteLine($"   [TWILIO] SMS sent: {name} admitted.");
    private void    WriteAdmissionAudit_DbInline(string pid) =>
        Console.WriteLine($"   [AUDIT-DB] Admission logged for {pid}.");
}

// ─────────────────────────────────────────────────────────────────────────────
// 🔍 WHY THIS DESIGN FAILS
//
// PROBLEM 1 — ONE METHOD, SEVEN REASONS TO BREAK
// Single Responsibility: a method should have one reason to change.
// AdmitPatient() changes when: SQL schema changes, insurance API changes,
// SMS vendor changes, bed algorithm changes, audit format changes, etc.
// Every change to any subsystem requires touching clinical admission logic.
// In a hospital system, every edit to a clinical method is an audit event.
// Unnecessary edits generate unnecessary compliance paperwork.
//
// PROBLEM 2 — UNTESTABLE SAFETY-CRITICAL CODE
// NABH hospital accreditation requires documented, reproducible test evidence
// for all clinical workflows. "Admit patient" is a clinical workflow.
// A method that requires live Stripe + live SMS + live SQL cannot be tested
// in isolation. The test coverage gap is an accreditation risk.
//
// PROBLEM 3 — KNOWLEDGE LEAK TO CALLERS
// The method throws HttpRequestException (from insurance API) and SqlException.
// Every caller must catch these implementation-specific exceptions.
// If you swap from SQL to MongoDB, all callers must now handle different exceptions.
// Abstraction should shield callers from implementation changes entirely.
//
// DESIGN DECISION: We chose layered abstraction — a public interface, an abstract
// base for shared orchestration logic, and concrete implementations.
// Alternative considered: a flat service class with virtual methods.
// Rejected because: virtual methods are still coupled in one class hierarchy.
// The interface + composition approach (used in File 6 fully) lets us swap
// sub-components (insurance verifier, bed management) independently.
// In a hospital, being able to swap the insurance provider without touching
// the admission logic is not a luxury — it's a contractual requirement.
// ─────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — OOP-correct, production-realistic
// IHospitalAdmissionService is the public contract — 3 methods, nothing else.
// The 7 sub-systems are hidden behind their own interfaces.
// Swap insurance verifier in 1 line. Test admission with all fakes in 2ms.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// The public contract for hospital admission. Callers only ever see this.
/// They don't know about beds, insurance APIs, or SMS. They call Admit.
/// </summary>
public interface IHospitalAdmissionService
{
    /// <returns>The created PatientRecord, or null if admission was rejected.</returns>
    PatientRecord? Admit(string patientName, string bloodType, string insuranceId, Doctor admittingDoctor);
    void Discharge(PatientRecord record, string clinicalNotes, Doctor dischargingDoctor);
    void Transfer(PatientRecord record, string targetWard);
}

/// <summary>
/// Bed management abstraction. In-memory for tests; SQL-backed in production.
/// The admission service never knows which is running.
/// </summary>
public interface IBedManagement
{
    string? GetAvailableBed(string ward = "General");
    void    AssignBed(string bedId, string patientId);
    void    ReleaseBed(string bedId);
    int     GetAvailableCount();
}

/// <summary>
/// Insurance verification abstraction.
/// Swap Ayushman Bharat → Star Health → CGHS by changing one constructor argument.
/// </summary>
public interface IInsuranceVerifier
{
    bool    VerifyCoverage(string insuranceId);
    decimal GetApprovedAmount(string insuranceId, string procedureCode);
}

/// <summary>
/// Patient notification abstraction. SMS today, push notification tomorrow.
/// The service doesn't know. Doesn't need to know.
/// </summary>
public interface IAdmissionNotifier
{
    void NotifyAdmission(string patientName, string wardInfo);
    void NotifyDischarge(string patientName);
}

// ── Abstract base: shared orchestration helpers ───────────────────────────────

/// <summary>
/// Base for all admission service implementations.
/// Protected helpers are invisible to callers (not on IHospitalAdmissionService).
/// Subclasses orchestrate; they don't implement sub-systems.
/// </summary>
public abstract class HospitalAdmissionBase : IHospitalAdmissionService
{
    protected readonly IBedManagement      _beds;
    protected readonly IInsuranceVerifier  _insurance;
    protected readonly IAdmissionNotifier  _notifier;

    protected HospitalAdmissionBase(
        IBedManagement     beds,
        IInsuranceVerifier insurance,
        IAdmissionNotifier notifier)
    {
        _beds      = beds;
        _insurance = insurance;
        _notifier  = notifier;
    }

    public abstract PatientRecord? Admit(string patientName, string bloodType, string insuranceId, Doctor admittingDoctor);
    public abstract void Discharge(PatientRecord record, string clinicalNotes, Doctor dischargingDoctor);
    public abstract void Transfer(PatientRecord record, string targetWard);

    /// <summary>
    /// Protected validation — shared by all concrete implementations.
    /// Not on the public interface; callers never call this directly.
    /// </summary>
    protected bool ValidateAdmissionPrerequisites(string patientName, string insuranceId, out string? rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(patientName)) { rejectionReason = "Patient name is required."; return false; }
        if (_beds.GetAvailableBed() is null)         { rejectionReason = "No beds available in General ward."; return false; }
        if (!_insurance.VerifyCoverage(insuranceId)) { rejectionReason = "Insurance coverage could not be verified."; return false; }
        rejectionReason = null;
        return true;
    }
}

/// <summary>
/// The standard hospital admission service.
/// Notice how clean Admit() is — 8 lines of business logic, no infrastructure code.
/// The caller sees exactly this level of detail: no more, no less.
/// </summary>
public class RetailAdmissionService : HospitalAdmissionBase
{
    public RetailAdmissionService(IBedManagement beds, IInsuranceVerifier insurance, IAdmissionNotifier notifier)
        : base(beds, insurance, notifier) { }

    public override PatientRecord? Admit(string patientName, string bloodType, string insuranceId, Doctor admittingDoctor)
    {
        if (!ValidateAdmissionPrerequisites(patientName, insuranceId, out var reason))
        {
            Console.WriteLine($"   [ADMISSION] Rejected: {reason}");
            return null;
        }

        var bedId  = _beds.GetAvailableBed()!;
        var record = new PatientRecord(Guid.NewGuid().ToString("N")[..8], patientName, bloodType);

        _beds.AssignBed(bedId, record.PatientId);
        _notifier.NotifyAdmission(patientName, $"Ward: General, Bed: {bedId}");
        Console.WriteLine($"   [ADMISSION] {patientName} admitted. Bed: {bedId}. Record: {record.PatientId}");
        return record;
    }

    public override void Discharge(PatientRecord record, string clinicalNotes, Doctor dischargingDoctor)
    {
        record.SignDischargeSummary(clinicalNotes, dischargingDoctor);
        _notifier.NotifyDischarge(record.PatientName);
        Console.WriteLine($"   [DISCHARGE] {record.PatientName} discharged. Status: {record.Status}");
    }

    public override void Transfer(PatientRecord record, string targetWard) =>
        Console.WriteLine($"   [TRANSFER] {record.PatientName} → {targetWard}");
}

// ── Concrete implementations ──────────────────────────────────────────────────

/// <summary>In-memory bed management for development and tests. No SQL required.</summary>
public class InMemoryBedManagement : IBedManagement
{
    private readonly Dictionary<string, string?> _beds;

    public InMemoryBedManagement(int bedCount = 5)
    {
        _beds = Enumerable.Range(1, bedCount)
                          .ToDictionary(i => $"BED-{i:D3}", _ => (string?)null);
    }

    public string?  GetAvailableBed(string ward = "General") =>
        _beds.FirstOrDefault(b => b.Value == null).Key;

    public void AssignBed(string bedId, string patientId)
    {
        _beds[bedId] = patientId;
        Console.WriteLine($"   [BED-MGR] Assigned {bedId} to patient {patientId}.");
    }

    public void ReleaseBed(string bedId)
    {
        _beds[bedId] = null;
        Console.WriteLine($"   [BED-MGR] Released {bedId}.");
    }

    public int GetAvailableCount() => _beds.Count(b => b.Value == null);
}

/// <summary>
/// Mock insurance verifier for tests. Returns predictable results.
/// The "shouldApprove" flag lets tests simulate rejection scenarios.
/// </summary>
public class MockInsuranceVerifier : IInsuranceVerifier
{
    private readonly bool _shouldApprove;

    public MockInsuranceVerifier(bool shouldApprove = true) => _shouldApprove = shouldApprove;

    public bool    VerifyCoverage(string insuranceId)
    {
        Console.WriteLine($"   [INSURANCE-MOCK] Checking coverage for {insuranceId}: {(_shouldApprove ? "approved" : "rejected")}.");
        return _shouldApprove;
    }

    public decimal GetApprovedAmount(string insuranceId, string procedureCode) => _shouldApprove ? 50_000m : 0;
}

/// <summary>Console notifier for development — prints instead of sending SMS.</summary>
public class ConsoleAdmissionNotifier : IAdmissionNotifier
{
    public void NotifyAdmission(string name, string ward) =>
        Console.WriteLine($"   [NOTIFY] 📱 Family notified: {name} admitted. {ward}.");
    public void NotifyDischarge(string name) =>
        Console.WriteLine($"   [NOTIFY] 📱 Family notified: {name} discharged.");
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
        Console.WriteLine("❌ BEFORE — NaiveHospitalOpsService (7 jobs in 1 method):");
        var naive = new NaiveHospitalOpsService();
        naive.AdmitPatient("Arjun Kapoor", "Fever, suspected typhoid", "AYUSH-88712");
        Console.WriteLine("   ↑ SQL, HTTP, Twilio, audit DB — all inline. 7 reasons to break.");
        Console.WriteLine("   To test admission logic: need live SQL + live insurance API + live Twilio.\n");

        // ── AFTER ── Standard wiring ──────────────────────────────────────
        Console.WriteLine("✅ AFTER — IHospitalAdmissionService: caller sees 3 methods only.\n");

        var admittingDoc = new Doctor("D001", "Vijay Kumar", "General Medicine", "MCI-2018-55210");
        var dischargeDoc = new Doctor("D002", "Meena Pillai", "Internal Medicine", "MCI-2016-33090");

        IHospitalAdmissionService service = new RetailAdmissionService(
            beds:      new InMemoryBedManagement(bedCount: 3),
            insurance: new MockInsuranceVerifier(shouldApprove: true),
            notifier:  new ConsoleAdmissionNotifier()
        );

        var record = service.Admit("Kavitha Reddy", "O+", "AYUSH-55109", admittingDoc);
        admittingDoc.ToString(); // doctor object used in File 1's UpdateDiagnosis
        record?.UpdateDiagnosis("Viral fever — observation required", admittingDoc);
        record?.AddMedication("Paracetamol", "500mg TID", admittingDoc);
        Console.WriteLine($"   Record: {record}");

        Console.WriteLine("\n   Discharge:");
        if (record != null)
            service.Discharge(record, "Fever resolved. Patient stable. Oral medications prescribed for 5 days.", dischargeDoc);

        Console.WriteLine("\n   Test scenario — insurance rejection (1 constructor arg change):");
        IHospitalAdmissionService rejectService = new RetailAdmissionService(
            beds:      new InMemoryBedManagement(),
            insurance: new MockInsuranceVerifier(shouldApprove: false),  // ← 1 line changed
            notifier:  new ConsoleAdmissionNotifier()
        );
        rejectService.Admit("Test Patient", "A+", "INVALID-ID", admittingDoc);

        Console.WriteLine("\n   RetailAdmissionService.Admit() is 8 lines.");
        Console.WriteLine("   Caller knows nothing about beds, insurance, or notifications.");

        Console.WriteLine("\n✅ Abstraction — understood.");
    }
}
