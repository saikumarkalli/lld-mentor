/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏥 CONCEPT: Polymorphism                                                    ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 03_Inheritance.cs                                             ║
 * ║     We have HospitalStaff → PhysicianStaff/Nurse/Surgeon/Receptionist.     ║
 * ║     Polymorphism is the DIVIDEND: ShiftManager processes ALL staff types    ║
 * ║     with one call to GetShiftSummary() — zero if/else, zero type checks.   ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  The same method call producing different behaviour depending on the actual ║
 * ║  runtime type of the object. staff.GetShiftSummary() returns a doctor's    ║
 * ║  patient count for a PhysicianStaff and a nurse's vitals count for a Nurse.║
 * ║  One call. Different results. No if-else. Ever.                             ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  A hospital's "End of Shift" button on the nurse station terminal.         ║
 * ║  Pressing it generates a shift report for whoever is logged in —           ║
 * ║  a surgeon gets "3 surgeries, 14 hrs", a nurse gets "27 vitals recorded". ║
 * ║  One button. The system applies the right report template based on the      ║
 * ║  role. The button's code never changes. Ever.                               ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A hospital's reporting system had 40 if-else blocks like:                 ║
 * ║    if (staff is Physiotherapist pt) { report += pt.GetTherapySessions(); } ║
 * ║  When the hospital added a new Radiologist role, the developer updated the  ║
 * ║  HR system and payroll — but missed 3 of the 7 if-else chains in the       ║
 * ║  reporting module. Radiologists showed zero sessions in 2 months of        ║
 * ║  reports. A NABH audit reviewer noticed. Internal investigation launched.  ║
 * ║  Cause: logic that should never have been in the reporting module.          ║
 * ║                                                                              ║
 * ║  THE DESIGN DECISION LENS:                                                   ║
 * ║  "How do we write code that works for ALL types without knowing WHICH type?"║
 * ║                                                                              ║
 * ║  QUICK RECALL (2-year cheat code):                                          ║
 * ║  Zero if-else. New staff type = new class + override. ShiftManager: untouched.║
 * ║                                                                              ║
 * ║  CONNECTS FORWARD:                                                           ║
 * ║  File 5 asks: when does polymorphism need an interface vs an abstract class?║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.HospitalModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Naive implementation
// "I need to handle each staff type differently — if/else is the obvious approach."
// The trap: every new staff type means finding every if/else chain. Everywhere.
// ─────────────────────────────────────────────────────────────────────────────

public class NaiveShiftManager
{
    // ❌ This method grows by 1 else-if for every new staff type. Forever.
    public void GenerateShiftReport(IEnumerable<object> shift)
    {
        Console.WriteLine("   [NAIVE REPORT] Today's Shift:");
        foreach (var member in shift)
        {
            if (member is NaiveDoctor d)
                Console.WriteLine($"   Dr. {d.Name} — patients seen: [unknown, logic missing]");
            else if (member is NaiveNurse n)
                Console.WriteLine($"   Nurse {n.Name} — vitals recorded: [unknown, logic missing]");
            else if (member is NaiveReceptionist r)
                Console.WriteLine($"   Receptionist {r.Name} — appointments: [unknown]");
            // ← Adding Physiotherapist: open THIS file. Add another else-if.
            // ← Adding Radiologist:     open THIS file. Add another else-if.
            // ← Hospital has 18 staff types. This method is 90+ lines of if-else.
            // ← NABH audit: Radiologist sessions missing from 2 months of reports.
            else
                Console.WriteLine($"   Unknown staff type: {member.GetType().Name}");
        }
    }
}

// 💥 IMMEDIATE PROBLEMS:
// — Every new role forces a developer to touch ShiftManager (a CLINICAL system).
// — Every edit to a clinical system = an audit event = compliance review.
// — Missing one else-if = missing data in NABH reports = accreditation risk.
// — ShiftManager KNOWS about PhysicianStaff's patient count.
//   That knowledge belongs to PhysicianStaff, not ShiftManager.

// ─────────────────────────────────────────────────────────────────────────────
// 🔍 WHY THIS DESIGN FAILS
//
// PROBLEM 1 — OPEN/CLOSED PRINCIPLE PREVIEW (you'll formalise this in SOLID Phase 2)
// ShiftManager should be CLOSED for modification when new staff types are added.
// Every else-if violates this. Each one risks breaking existing staff report logic.
// A bug fix for Physiotherapist reporting can accidentally break Doctor reporting
// if both are in the same chain. In a hospital, broken clinical reports are not
// inconvenient — they're liability.
//
// PROBLEM 2 — KNOWLEDGE IN THE WRONG CLASS
// ShiftManager knows that PhysicianStaff has a patient count.
// It knows that Surgeon has surgery hours.
// This domain knowledge belongs to those classes, not to the reporting manager.
// If PhysicianStaff renames a field, ShiftManager breaks. Wrong place to put this.
//
// PROBLEM 3 — TYPE-CHECKING IS A DESIGN SMELL
// 'is PhysicianStaff' in business logic means the logic is coupled to a concrete type.
// The moment you add a new subclass, you must find every 'is XxxStaff' check
// in the entire codebase. In a large hospital system, that's dozens of files.
//
// DESIGN DECISION: We chose virtual method override over the Visitor pattern.
// Alternative considered: Visitor pattern (Design Patterns, covered in Phase 3).
// Rejected here because: Visitor adds complexity that's premature for this team.
// Virtual override is simpler, achieves the same zero-if-else result, and is
// the idiomatic C# approach for this use case.
// ─────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — OOP-correct polymorphism
// GetShiftSummary() is already virtual on HospitalStaff (File 3).
// Each subclass overrides it. ShiftManager calls one method. Zero if/else.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Capability: can provide medical treatment to a patient.
/// Both PhysicianStaff and Physiotherapist treat patients — but they're not the
/// same type of employee. An interface is the ONLY way to group them.
/// (File 5 explores this distinction in depth.)
/// </summary>
public interface ITreatmentProvider
{
    void TreatPatient(PatientRecord patient);
    string GetTreatmentType();
}

/// <summary>
/// Physiotherapist — a new staff type added AFTER ShiftManager was written.
/// ShiftManager: ZERO changes. Zero compliance paperwork. This is the payoff.
/// </summary>
public class Physiotherapist : HospitalStaff, ITreatmentProvider
{
    public string TherapySpecialization { get; }
    private int _sessionsConducted;
    private int _patientsRecovered;

    public Physiotherapist(string staffId, string name, string department, decimal salary,
                           string therapySpecialization)
        : base(staffId, name, department, salary)
    {
        TherapySpecialization = therapySpecialization;
    }

    public override string GetRole()        => "Physiotherapist";
    public override AccessLevel GetAccessLevel() => AccessLevel.Medical;

    public override bool CanPerform(MedicalAction action) =>
        action is MedicalAction.RecordVitals or MedicalAction.AdministerMedication;

    public void TreatPatient(PatientRecord patient)
    {
        _sessionsConducted++;
        Console.WriteLine($"   [PHYSIO] {Name} ({TherapySpecialization}) conducting session #{_sessionsConducted} with {patient.PatientName}.");
    }

    public string GetTreatmentType() => $"Physiotherapy ({TherapySpecialization})";

    public void MarkPatientRecovered()
    {
        _patientsRecovered++;
        Console.WriteLine($"   [PHYSIO] {Name}: patient recovery marked. Total recovered: {_patientsRecovered}.");
    }

    // ← Just add this override. ShiftManager needs zero changes.
    public override string GetShiftSummary() =>
        $"Physiotherapist {Name} ({TherapySpecialization}) — {_sessionsConducted} sessions conducted, {_patientsRecovered} patients recovered";
}

/// <summary>
/// Processes shift data for ALL staff types.
/// Notice: zero knowledge of PhysicianStaff, Nurse, Surgeon, or Physiotherapist.
/// GetShiftSummary() dispatches to the right override at runtime.
/// This method will NEVER be touched again when new staff types are added.
/// </summary>
public class ShiftManager
{
    /// <summary>
    /// Generates a shift report for any collection of staff.
    /// New staff type = create class + override GetShiftSummary(). This method: unchanged.
    /// </summary>
    public void GenerateShiftReport(IEnumerable<HospitalStaff> shift, DateTime shiftDate)
    {
        Console.WriteLine($"\n   ── Shift Report: {shiftDate:dd-MMM-yyyy} ─────────────────────");
        foreach (var staff in shift)
        {
            // ← This one call dispatches to the correct override at runtime.
            //   CLR looks up the actual type and calls the right GetShiftSummary().
            //   No if-else. No type checks. No coupling to concrete types.
            Console.WriteLine($"   {staff.GetShiftSummary()}");
        }
        Console.WriteLine($"   Total staff on duty: {shift.Count()}");
        Console.WriteLine("   ─────────────────────────────────────────────────────────");
    }

    public void PrintAccessMatrix(IEnumerable<HospitalStaff> staff)
    {
        Console.WriteLine("\n   ── Access Matrix ──────────────────────────────────────────");
        Console.WriteLine($"   {"Role",-18} {"Access",-16} {"Prescribe",-12} {"Surgery",-10} {"Vitals"}");
        foreach (var s in staff)
        {
            Console.WriteLine($"   {s.GetRole(),-18} {s.GetAccessLevel(),-16} " +
                              $"{s.CanPerform(MedicalAction.Prescribe),-12} " +
                              $"{s.CanPerform(MedicalAction.PerformSurgery),-10} " +
                              $"{s.CanPerform(MedicalAction.RecordVitals)}");
        }
        Console.WriteLine("   ──────────────────────────────────────────────────────────");
    }
}

/// <summary>
/// Treatment coordinator. Uses ITreatmentProvider — zero knowledge of concrete types.
/// A physiotherapist and a doctor can both participate in a treatment round.
/// </summary>
public class TreatmentCoordinator
{
    /// <summary>
    /// Runs a treatment round across all providers. Zero type-checking. Ever.
    /// Adding OccupationalTherapist = create class implementing ITreatmentProvider.
    /// This method: untouched.
    /// </summary>
    public void RunTreatmentRound(IEnumerable<ITreatmentProvider> providers, PatientRecord patient)
    {
        Console.WriteLine($"\n   ── Treatment Round: {patient.PatientName} ─────────────────────");
        foreach (var provider in providers)
        {
            Console.WriteLine($"   Treatment type: {provider.GetTreatmentType()}");
            provider.TreatPatient(patient);
        }
        Console.WriteLine("   ─────────────────────────────────────────────────────────");
    }
}

/// <summary>Alert severity levels for emergency alerts.</summary>
public enum AlertSeverity { Informational, Warning, Critical, CodeBlue }

/// <summary>
/// Demonstrates compile-time polymorphism (method overloading).
/// Same method name — compiler picks the right overload based on argument types.
/// </summary>
public class EmergencyAlert
{
    private readonly List<string> _alertLog = [];

    /// <summary>Overload 1 — basic alert to all staff on duty.</summary>
    public void RaiseAlert(string message)
    {
        var entry = $"[ALERT-INFO] {message}";
        _alertLog.Add(entry);
        Console.WriteLine($"   {entry}");
    }

    /// <summary>Overload 2 — alert with severity level (compiler picks this when severity provided).</summary>
    public void RaiseAlert(string message, AlertSeverity severity)
    {
        var icon  = severity == AlertSeverity.CodeBlue ? "🔵" :
                    severity == AlertSeverity.Critical  ? "🔴" : "🟡";
        var entry = $"[{icon} {severity.ToString().ToUpper()}] {message}";
        _alertLog.Add(entry);
        Console.WriteLine($"   {entry}");
    }

    /// <summary>Overload 3 — targeted alert to a specific staff member.</summary>
    public void RaiseAlert(string message, AlertSeverity severity, HospitalStaff notifyStaff)
    {
        RaiseAlert(message, severity);
        Console.WriteLine($"   ↳ Directly paged: {notifyStaff.GetRole()} {notifyStaff.Name}");
    }

    public IReadOnlyList<string> GetAlertLog() => _alertLog.AsReadOnly();
}

// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class PolymorphismDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  POLYMORPHISM DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — NaiveShiftManager with if/else chains:");
        var naiveManager = new NaiveShiftManager();
        naiveManager.GenerateShiftReport([
            new NaiveDoctor   { Name = "Ravi" },
            new NaiveNurse    { Name = "Sunita" },
            new NaiveReceptionist { Name = "Anita" }
        ]);
        Console.WriteLine("   ^ Adding Physiotherapist = open ShiftManager, add else-if.");
        Console.WriteLine("   ^ NABH audit: Radiologist sessions missing from 2 months of reports.\n");

        // ── AFTER ── Runtime Polymorphism ────────────────────────────────────
        Console.WriteLine("✅ AFTER — ShiftManager with zero if/else:\n");

        var credential = new Doctor("D001", "Ravi Kumar", "Cardiology", "MCI-2017-44210");
        var physician  = new PhysicianStaff("S001", "Ravi Kumar",  "Cardiology",  1_20_000m, "Cardiology",     "MCI-2017-44210");
        var nurse      = new Nurse         ("S002", "Sunita Das",   "ICU",          45_000m, "ICU",             "B.Sc Nursing");
        var surgeon    = new Surgeon       ("S004", "Deepak Verma", "Surgery",    2_00_000m, "Cardiac Surgery", "MCI-2010-11200", "OT-3");
        var physio     = new Physiotherapist("S005", "Lakshmi Rao", "Rehab",       55_000m, "Musculoskeletal");

        var patient = new PatientRecord("P010", "Arun Mehta", "O+");
        patient.UpdateDiagnosis("Post-surgical cardiac recovery — physiotherapy indicated", credential);

        // Simulate some activity
        physician.DiagnosePatient(patient, credential);
        physician.PrescribeMedication(patient, "Metoprolol", "50mg BD", credential);
        surgeon.PerformSurgery(patient, "Stent placement", 2.5);
        var vitals = new VitalSigns(37.1m, 72, 120, 80, 98, DateTime.Now);
        nurse.RecordVitals(patient, vitals);
        physio.TreatPatient(patient);
        physio.MarkPatientRecovered();

        HospitalStaff[] shift = [physician, nurse, surgeon, physio];
        var shiftMgr = new ShiftManager();
        shiftMgr.GenerateShiftReport(shift, DateTime.Today);
        shiftMgr.PrintAccessMatrix(shift);

        Console.WriteLine("\n   Physiotherapist was added AFTER ShiftManager was written.");
        Console.WriteLine("   ShiftManager.GenerateShiftReport(): ZERO changes. Zero compliance paperwork.");

        // ── Treatment round — ITreatmentProvider polymorphism ─────────────
        Console.WriteLine("\n✅ AFTER — ITreatmentProvider: treatment round, zero type checking:");

        var coordinator = new TreatmentCoordinator();
        ITreatmentProvider[] providers =
        [
            physio,   // Physiotherapist implements ITreatmentProvider
            // A PhysicianStaff implementing ITreatmentProvider would be added here
            // (shown in File 5 where we explore interface + class combos)
        ];
        coordinator.RunTreatmentRound(providers, patient);

        // ── Method overloading — compile-time polymorphism ─────────────────
        Console.WriteLine("\n✅ AFTER — EmergencyAlert overloads (compile-time polymorphism):");
        var alertSystem = new EmergencyAlert();
        alertSystem.RaiseAlert("Blood bank: O- units critically low.");
        alertSystem.RaiseAlert("Patient P010 cardiac monitor alarm.", AlertSeverity.Critical);
        alertSystem.RaiseAlert("Code Blue — ICU Bed 3.", AlertSeverity.CodeBlue, surgeon);
        Console.WriteLine($"   Alerts raised: {alertSystem.GetAlertLog().Count}");
        Console.WriteLine("   Same method name 'RaiseAlert'. Compiler picks the overload. No if/else.");

        Console.WriteLine("\n✅ Polymorphism — understood.");
    }
}
