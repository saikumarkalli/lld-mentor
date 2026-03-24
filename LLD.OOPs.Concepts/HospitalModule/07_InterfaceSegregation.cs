/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏥 CONCEPT: Interface Segregation Principle (ISP)                           ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 05_AbstractClass_vs_Interface.cs + 06_CompositionVsInheritance║
 * ║     We know interfaces define capability. ISP says: keep each interface     ║
 * ║     NARROW. A blood bank module should not be forced to implement ambulance  ║
 * ║     dispatch just because both live in "IHospitalSystem".                   ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  No client should be forced to depend on methods it does not use.           ║
 * ║  A fat interface (IHospitalSystem with 15 methods) forces every consumer    ║
 * ║  to implement or stub all 15 — even those it never calls.                   ║
 * ║  Split it into focused interfaces: IPatientAdmission, IPrescriptionService, ║
 * ║  ILabService, IAmbulanceService, IBloodBankService, IRegulatoryReporting.   ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  Hospital departments each have a dedicated helpline.                        ║
 * ║  You don't call the switchboard and navigate 15 options to ask the lab for  ║
 * ║  a blood type. You call the lab directly. Each department exposes exactly   ║
 * ║  the services it provides — nothing more.                                   ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A hospital used IHospitalSystem with 15 methods as the contract for all    ║
 * ║  integrations. A startup built a mobile check-in app that only needed       ║
 * ║  AdmitPatient() and GetQueueStatus(). They were forced to implement all 15  ║
 * ║  methods including DispatchAmbulance() and RunLabTest(). Their stub         ║
 * ║  implementations of DispatchAmbulance silently returned success = true.     ║
 * ║  A test scenario accidentally triggered a real ambulance dispatch via the  ║
 * ║  check-in app in staging. (The staging DB was pointed at production.)       ║
 * ║                                                                              ║
 * ║  THE DESIGN DECISION LENS:                                                   ║
 * ║  "Can I remove this method from the interface without breaking consumers    ║
 * ║   who don't care about it?" If yes → the interface is too fat.              ║
 * ║                                                                              ║
 * ║  QUICK RECALL (2-year cheat code):                                          ║
 * ║  Lab module implements ILabService only. It compiles without knowing        ║
 * ║  ambulances exist.                                                           ║
 * ║                                                                              ║
 * ║  CONNECTS FORWARD:                                                           ║
 * ║  File 8 (Dependency Inversion) wires these narrow interfaces together via   ║
 * ║  a composition root — each dependency injected from outside.                ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.HospitalModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Fat interface
// One interface, 15 methods. Every consumer implements all 15 or throws.
// ─────────────────────────────────────────────────────────────────────────────

public interface INaiveHospitalSystem
{
    // Patient admission
    void   AdmitPatient(string patientId, string name);
    void   DischargePatient(string patientId);
    string GetQueueStatus();

    // Prescription
    void PrescribeMedication(string patientId, string drug, string dosage);
    bool VerifyDrugInteraction(string drug1, string drug2);

    // Lab
    string OrderLabTest(string patientId, string testType);
    string GetLabResult(string testId);

    // Ambulance
    bool   DispatchAmbulance(string address, string emergencyType);
    string GetAmbulanceLocation(string ambulanceId);

    // Blood bank
    bool   RequestBloodUnit(string bloodType, int units);
    int    GetBloodInventory(string bloodType);

    // Regulatory
    void   SubmitNABHReport(string reportType, DateTime period);
    string GetComplianceStatus();
    void   RecordAdverseEvent(string patientId, string description);
    void   ArchiveRecords(DateTime olderThan);
}

/// <summary>
/// NAIVE: Mobile check-in app only needs AdmitPatient + GetQueueStatus.
/// Forced to implement all 15 methods. DispatchAmbulance silently returns true.
/// </summary>
public class NaiveMobileCheckInApp : INaiveHospitalSystem
{
    public void   AdmitPatient(string id, string name) => Console.WriteLine($"  Check-in: {name}");
    public void   DischargePatient(string id)          => Console.WriteLine($"  Discharge: {id}");
    public string GetQueueStatus()                     => "Queue: 3 patients waiting";

    // EVERYTHING BELOW IS FORCED — not relevant to a check-in app
    public void   PrescribeMedication(string p, string d, string dos) => throw new NotImplementedException();
    public bool   VerifyDrugInteraction(string d1, string d2)         => throw new NotImplementedException();
    public string OrderLabTest(string p, string t)                    => throw new NotImplementedException();
    public string GetLabResult(string testId)                         => throw new NotImplementedException();

    // ← THIS IS THE DISASTER: returns true silently — staging hit production
    public bool   DispatchAmbulance(string addr, string type)         => true;
    public string GetAmbulanceLocation(string id)                     => throw new NotImplementedException();
    public bool   RequestBloodUnit(string bt, int units)              => throw new NotImplementedException();
    public int    GetBloodInventory(string bt)                        => throw new NotImplementedException();
    public void   SubmitNABHReport(string t, DateTime p)              => throw new NotImplementedException();
    public string GetComplianceStatus()                               => throw new NotImplementedException();
    public void   RecordAdverseEvent(string p, string d)              => throw new NotImplementedException();
    public void   ArchiveRecords(DateTime older)                      => throw new NotImplementedException();
}

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — Segregated interfaces: one interface per department/capability
// ─────────────────────────────────────────────────────────────────────────────

// ── FOCUSED INTERFACES ────────────────────────────────────────────────────────

/// <summary>Patient admission desk operations only.</summary>
public interface IPatientAdmission
{
    void   AdmitPatient(string patientId, string name, string admittedBy);
    void   DischargePatient(string patientId, string dischargedBy);
    string GetQueueStatus();
    int    GetCurrentCensus();
}

/// <summary>Prescription and medication management only.</summary>
public interface IPrescriptionService
{
    void PrescribeMedication(string patientId, string drug, string dosage, string prescribedBy);
    bool VerifyDrugInteraction(string drug1, string drug2);
    IReadOnlyList<string> GetActivePrescriptions(string patientId);
}

/// <summary>Laboratory test ordering and result retrieval only.</summary>
public interface ILabService
{
    string OrderLabTest(string patientId, string testType, string orderedBy);
    string GetLabResult(string testId);
    bool   IsResultReady(string testId);
}

/// <summary>Ambulance dispatch and tracking only.</summary>
public interface IAmbulanceService
{
    string DispatchAmbulance(string address, string emergencyType, string dispatchedBy);
    string GetAmbulanceLocation(string ambulanceId);
    bool   IsAmbulanceAvailable();
}

/// <summary>Blood bank inventory and request management only.</summary>
public interface IBloodBankService
{
    bool RequestBloodUnit(string bloodType, int units, string requestedBy);
    int  GetBloodInventory(string bloodType);
    bool IsCompatible(string donorType, string recipientType);
}

/// <summary>NABH/regulatory reporting and compliance only.</summary>
public interface IRegulatoryReporting
{
    void   SubmitNABHReport(string reportType, DateTime forPeriod);
    string GetComplianceStatus();
    void   RecordAdverseEvent(string patientId, string description, string reportedBy);
    void   ArchiveRecords(DateTime olderThan);
}

// ── CONCRETE IMPLEMENTATIONS ──────────────────────────────────────────────────
// Each class implements ONLY what it genuinely provides.

public sealed class AdmissionDesk : IPatientAdmission
{
    private readonly List<string> _queue = [];
    private int _census;

    public void   AdmitPatient(string id, string name, string by) { _queue.Add(id); _census++; Console.WriteLine($"  ✓ Admitted: {name} ({id}) by {by}"); }
    public void   DischargePatient(string id, string by)          { _queue.Remove(id); _census--; Console.WriteLine($"  ✓ Discharged: {id} by {by}"); }
    public string GetQueueStatus()                                => $"Queue: {_queue.Count} waiting";
    public int    GetCurrentCensus()                              => _census;
}

public sealed class PharmacyService : IPrescriptionService
{
    private readonly Dictionary<string, List<string>> _prescriptions = [];

    public void PrescribeMedication(string p, string drug, string dosage, string by)
    {
        if (!_prescriptions.ContainsKey(p)) _prescriptions[p] = [];
        _prescriptions[p].Add($"{drug} {dosage}");
        Console.WriteLine($"  💊 Prescribed {drug} ({dosage}) for {p} by {by}");
    }

    public bool VerifyDrugInteraction(string d1, string d2)
    {
        // Simplified: Warfarin + Aspirin is a known interaction
        var pair = $"{d1.ToLower()}+{d2.ToLower()}";
        var known = new HashSet<string> { "warfarin+aspirin", "aspirin+warfarin" };
        var dangerous = known.Contains(pair);
        Console.WriteLine($"  🔬 Interaction check [{d1} + {d2}]: {(dangerous ? "⚠️ DANGEROUS" : "✓ Safe")}");
        return dangerous;
    }

    public IReadOnlyList<string> GetActivePrescriptions(string p) =>
        _prescriptions.TryGetValue(p, out var list) ? list.AsReadOnly() : [];
}

public sealed class LabDepartment : ILabService
{
    private readonly Dictionary<string, (string Result, bool Ready)> _tests = [];

    public string OrderLabTest(string p, string type, string by)
    {
        var testId = $"LAB-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        _tests[testId] = ("Pending", false);
        Console.WriteLine($"  🧪 Lab test ordered: {type} for {p} → {testId} by {by}");
        // Simulate instant result for demo
        _tests[testId] = ($"{type}: Normal range", true);
        return testId;
    }

    public string GetLabResult(string testId) =>
        _tests.TryGetValue(testId, out var r) ? r.Result : "Test not found";

    public bool IsResultReady(string testId) =>
        _tests.TryGetValue(testId, out var r) && r.Ready;
}

public sealed class AmbulanceDispatchCenter : IAmbulanceService
{
    private bool _available = true;

    public string DispatchAmbulance(string address, string type, string by)
    {
        if (!_available) return "No ambulance available";
        _available = false;
        var id = "AMB-KA-01";
        Console.WriteLine($"  🚑 Ambulance {id} dispatched to {address} — {type} (by {by})");
        return id;
    }

    public string GetAmbulanceLocation(string id)  => "NH-44, 3km from hospital";
    public bool   IsAmbulanceAvailable()            => _available;
}

public sealed class BloodBank : IBloodBankService
{
    private readonly Dictionary<string, int> _inventory = new()
    {
        ["A+"] = 12, ["A-"] = 4, ["B+"] = 8, ["B-"] = 2,
        ["AB+"] = 5, ["AB-"] = 1, ["O+"] = 15, ["O-"] = 6
    };

    public bool RequestBloodUnit(string bt, int units, string by)
    {
        if (!_inventory.TryGetValue(bt, out var stock) || stock < units)
        {
            Console.WriteLine($"  🩸 Blood request FAILED: {units}u {bt} — insufficient stock ({stock} available)");
            return false;
        }
        _inventory[bt] -= units;
        Console.WriteLine($"  🩸 Blood issued: {units}u {bt} to {by}. Remaining: {_inventory[bt]}u");
        return true;
    }

    public int  GetBloodInventory(string bt) => _inventory.GetValueOrDefault(bt);
    public bool IsCompatible(string donor, string recipient) =>
        donor == "O-" || donor == recipient; // simplified
}

public sealed class NABHComplianceOffice : IRegulatoryReporting
{
    private readonly List<string> _adverseEvents = [];
    private int _reportCount;

    public void   SubmitNABHReport(string type, DateTime period)    { _reportCount++; Console.WriteLine($"  📋 NABH Report #{_reportCount} submitted: {type} for {period:yyyy-MM}"); }
    public string GetComplianceStatus()                              => _reportCount > 0 ? "COMPLIANT" : "PENDING FIRST REPORT";
    public void   RecordAdverseEvent(string p, string desc, string by) { _adverseEvents.Add($"{p}:{desc}"); Console.WriteLine($"  ⚠️  Adverse event recorded for {p}: {desc} (by {by})"); }
    public void   ArchiveRecords(DateTime older)                     => Console.WriteLine($"  🗄️  Records before {older:yyyy-MM-dd} archived.");
}

// ── CONSUMER — uses ONLY what it needs ───────────────────────────────────────

/// <summary>
/// Check-in kiosk — uses ONLY IPatientAdmission.
/// Has no knowledge that ambulances, labs, or blood banks exist.
/// It cannot accidentally dispatch an ambulance.
/// </summary>
public sealed class CheckInKiosk
{
    private readonly IPatientAdmission _admission;
    public CheckInKiosk(IPatientAdmission admission) => _admission = admission;

    public void CheckIn(string patientId, string name)
    {
        _admission.AdmitPatient(patientId, name, "Self-Service Kiosk");
        Console.WriteLine($"  🖥️  Kiosk: token issued. {_admission.GetQueueStatus()}");
    }
}

/// <summary>
/// Compliance dashboard — uses ONLY IRegulatoryReporting + ILabService.
/// Has no knowledge that admissions or prescriptions exist.
/// </summary>
public sealed class ComplianceDashboard
{
    private readonly IRegulatoryReporting _reg;
    private readonly ILabService          _lab;

    public ComplianceDashboard(IRegulatoryReporting reg, ILabService lab)
    {
        _reg = reg;
        _lab = lab;
    }

    public void RunMonthlyClose(DateTime period)
    {
        _reg.SubmitNABHReport("Monthly Morbidity", period);
        Console.WriteLine($"  📊 Compliance: {_reg.GetComplianceStatus()}");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 🎬 DEMO
// ─────────────────────────────────────────────────────────────────────────────
public static class InterfaceSegregationDemo
{
    public static void Demo()
    {
        Console.WriteLine("=== Interface Segregation: Hospital Departments ===\n");

        var admission  = new AdmissionDesk();
        var pharmacy   = new PharmacyService();
        var lab        = new LabDepartment();
        var ambulance  = new AmbulanceDispatchCenter();
        var bloodBank  = new BloodBank();
        var compliance = new NABHComplianceOffice();

        // ── Patient journey using only relevant department interfaces ──
        Console.WriteLine("── Patient Journey ──");
        admission.AdmitPatient("P-2001", "Ravi Shankar", "Desk-Officer-1");

        pharmacy.VerifyDrugInteraction("Warfarin", "Aspirin");
        pharmacy.PrescribeMedication("P-2001", "Metoprolol", "25mg OD", "Dr. Kavya");

        var testId = lab.OrderLabTest("P-2001", "Complete Blood Count", "Dr. Kavya");
        Console.WriteLine($"  Result ready: {lab.IsResultReady(testId)} → {lab.GetLabResult(testId)}");

        bloodBank.GetBloodInventory("O+");
        bloodBank.RequestBloodUnit("B+", 2, "Surgical Team");

        Console.WriteLine($"\n  Census: {admission.GetCurrentCensus()} patient(s)");

        // ── Emergency dispatch ──
        Console.WriteLine("\n── Emergency Dispatch ──");
        if (ambulance.IsAmbulanceAvailable())
            ambulance.DispatchAmbulance("42 MG Road, Bengaluru", "RTA trauma", "Control Room");

        // ── Check-in kiosk: only knows about IPatientAdmission ──
        Console.WriteLine("\n── Self-Service Kiosk (knows ONLY IPatientAdmission) ──");
        var kiosk = new CheckInKiosk(admission);
        kiosk.CheckIn("P-2002", "Sunita Rao");

        // ── Compliance dashboard: only knows about IRegulatoryReporting + ILabService ──
        Console.WriteLine("\n── Compliance Dashboard (knows ONLY IRegulatoryReporting + ILabService) ──");
        compliance.RecordAdverseEvent("P-2001", "Mild allergic reaction to contrast dye", "Dr. Kavya");
        var dashboard = new ComplianceDashboard(compliance, lab);
        dashboard.RunMonthlyClose(DateTime.Today);

        Console.WriteLine("\n  KEY INSIGHT:");
        Console.WriteLine("  CheckInKiosk cannot dispatch an ambulance — it literally has no method for it.");
        Console.WriteLine("  ComplianceDashboard cannot prescribe medication — it doesn't implement IPrescriptionService.");
        Console.WriteLine("  Narrow interfaces = safe consumers. Fat interface = accidental ambulance dispatch.");
    }
}
