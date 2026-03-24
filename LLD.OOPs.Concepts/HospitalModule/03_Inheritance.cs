/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏥 CONCEPT: Inheritance                                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 02_Abstraction.cs                                             ║
 * ║     We have clean admission service interfaces. Now we model the STAFF      ║
 * ║     that operates those interfaces — different roles that share a common    ║
 * ║     HR identity. Fix CheckIn() once = fixed for all 12 staff types.         ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Inheritance lets a class acquire fields and methods from a parent class,  ║
 * ║  then override only what's unique to it. The parent holds shared code;     ║
 * ║  children define their specializations. One fix in the base propagates to  ║
 * ║  every subclass immediately.                                                 ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  Every hospital staff member, regardless of role, has an employee ID,      ║
 * ║  department, salary, and must follow the same attendance policy. That's    ║
 * ║  the "employee template" — the base class. A surgeon then adds "operating  ║
 * ║  theatre clearance" on top. A nurse adds "nursing unit assignment." Each   ║
 * ║  role fills in its own clause on the standard contract.                     ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  An Indian hospital had Doctor, Nurse, and Receptionist as separate classes ║
 * ║  with copy-pasted CheckIn(). The government mandated GPS-tagged attendance  ║
 * ║  for all hospital staff (to prevent ghost employees). The developer updated ║
 * ║  Doctor.CheckIn(). Forgot Nurse and Receptionist. For 4 months, 31 nurses  ║
 * ║  and 8 receptionists had no geo-tagged attendance. A Labour Department     ║
 * ║  inspection found the gap. Hospital received a ₹15 lakh compliance notice. ║
 * ║                                                                              ║
 * ║  THE DESIGN DECISION LENS:                                                   ║
 * ║  "What is genuinely SHARED code vs what only LOOKS shared?"                 ║
 * ║                                                                              ║
 * ║  QUICK RECALL (2-year cheat code):                                          ║
 * ║  GPS added to base HospitalStaff.CheckIn() = all 12 roles geo-tagged.      ║
 * ║                                                                              ║
 * ║  CONNECTS FORWARD:                                                           ║
 * ║  File 4 (Polymorphism) shows WHY this hierarchy pays off: ShiftManager     ║
 * ║  processes ALL staff types via GetShiftSummary() — zero if/else.            ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.HospitalModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Naive implementation
// "Each role is its own thing. I'll write a class per role."
// Three classes, all with the same 5 fields and 3 methods copy-pasted.
// ─────────────────────────────────────────────────────────────────────────────

public class NaiveDoctor
{
    public string StaffId    = string.Empty;
    public string Name       = string.Empty;
    public string Department = string.Empty;
    public decimal Salary;
    public string Specialization = string.Empty;

    public void   CheckIn()  => Console.WriteLine($"   [DOCTOR] {Name} checked in."); // ← no GPS
    public void   CheckOut() => Console.WriteLine($"   [DOCTOR] {Name} checked out.");
    public string GetPayslip() => $"Dr. {Name}: ₹{Salary:N0}/month";

    public void DiagnosePatient(PatientRecord record) =>
        Console.WriteLine($"   Dr. {Name} diagnosing {record.PatientName}.");
}

public class NaiveNurse
{
    public string  StaffId      = string.Empty;   // ← copy-pasted from NaiveDoctor
    public string  Name         = string.Empty;   // ← copy-pasted
    public string  Department   = string.Empty;   // ← copy-pasted
    public decimal Salary;                         // ← copy-pasted
    public string  NursingUnit  = string.Empty;

    public void   CheckIn()  => Console.WriteLine($"   [NURSE] {Name} checked in.");  // ← no GPS
    public void   CheckOut() => Console.WriteLine($"   [NURSE] {Name} checked out."); // ← copy-pasted
    public string GetPayslip() => $"Nurse {Name}: ₹{Salary:N0}/month";               // ← copy-pasted

    public void RecordVitals(PatientRecord record) =>
        Console.WriteLine($"   Nurse {Name} recording vitals for {record.PatientName}.");
}

public class NaiveReceptionist
{
    public string  StaffId    = string.Empty;   // ← copy-pasted from NaiveDoctor and NaiveNurse
    public string  Name       = string.Empty;   // ← copy-pasted
    public string  Department = string.Empty;   // ← copy-pasted
    public decimal Salary;                       // ← copy-pasted

    public void   CheckIn()  => Console.WriteLine($"   [RECEPT] {Name} checked in.");  // ← no GPS
    public void   CheckOut() => Console.WriteLine($"   [RECEPT] {Name} checked out."); // ← copy-pasted
    public string GetPayslip() => $"{Name}: ₹{Salary:N0}/month";                       // ← copy-pasted
}

// 💥 IMMEDIATE PROBLEMS:
// — GPS mandate issued. Developer adds gps param to NaiveDoctor.CheckIn(GpsCoordinates gps).
//   Forgets NaiveNurse. Forgets NaiveReceptionist.
//   ₹15 lakh compliance notice. (See disaster story above.)
// — 12 staff types exist in a real hospital. Fix = 12 places.
// — Without a common base, you can never write: List<Staff> todaysShift = [...all staff...]
//   Polymorphism (File 4) is blocked before it can start.

// ─────────────────────────────────────────────────────────────────────────────
// 🔍 WHY THIS DESIGN FAILS
//
// PROBLEM 1 — CHANGE AMPLIFICATION
// HR rules, payslip format, compliance logging, GPS tagging — any change to
// "what all staff do" requires N edits across N files. N = 12 in this hospital.
// In a system where every file edit generates an audit event (hospital software),
// 12 unnecessary edits = 12 audit entries = 12 compliance reviews. One base class
// edit generates 1 entry and satisfies all 12 roles immediately.
//
// PROBLEM 2 — WRONG MODELLING (the deeper issue)
// Doctor and Nurse aren't just SIMILAR — they ARE HospitalStaff.
// "Doctor IS-A HospitalStaff" is a true real-world statement.
// When code doesn't reflect truth, developers build wrong mental models.
// Wrong mental models lead to wrong design decisions in future files.
// Inheritance isn't just about code reuse — it's about modelling reality.
//
// PROBLEM 3 — POLYMORPHISM BLOCKED
// Without a common base type, List<HospitalStaff> is impossible.
// ShiftManager must maintain List<NaiveDoctor>, List<NaiveNurse>, List<NaiveReceptionist>
// separately — and process them with separate loops and separate if-else.
// File 4 (Polymorphism) shows how this hierarchy unlocks zero-if-else processing.
//
// DESIGN DECISION: We chose an ABSTRACT CLASS over an interface here.
// WHY abstract class and NOT interface?
// — HospitalStaff has shared FIELDS (StaffId, Name, Salary, Department).
//   Interfaces cannot hold field state. Abstract class can.
// — CheckIn(), GetPayslip() are IDENTICAL across all staff types. Write once.
//   An interface would force every subclass to re-implement the same logic.
// — CanPerform() and GetRole() MUST be overridden — abstract forces this.
// Rule: shared CODE → abstract class. Shared CONTRACT only → interface (File 5).
// ─────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — OOP-correct inheritance hierarchy
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>GPS coordinates for compliance-required geo-tagged attendance.</summary>
public record GpsCoordinates(double Latitude, double Longitude, DateTime Timestamp);

/// <summary>Governs what clinical areas a staff member can access.</summary>
public enum AccessLevel { Standard, Administrative, Medical, Surgical }

/// <summary>Actions that require specific authorization levels.</summary>
public enum MedicalAction { Diagnose, Prescribe, PerformSurgery, AdministerMedication, RecordVitals, ScheduleAppointment }

/// <summary>Recorded vitals — immutable snapshot taken at a point in time.</summary>
public record VitalSigns(decimal TemperatureC, int PulseRate, int SystolicBp, int DiastolicBp, int SpO2Percent, DateTime RecordedAt);

/// <summary>
/// The base class for ALL hospital staff.
///
/// SHARED CODE lives here:
///   — CheckIn(GpsCoordinates) — GPS-compliant attendance for ALL roles
///   — GetPayslip() — salary calculation formula shared by all
///   — StaffId, Name, Department, Salary, JoiningDate — universal HR fields
///
/// ENFORCED CONTRACTS (must override):
///   — GetRole() — each subclass must declare its human-readable role name
///   — CanPerform() — each subclass defines what it can legally do
///
/// Note on naming: 'PhysicianStaff' is used for the doctor employee type to
/// distinguish it from 'Doctor' (the credential class defined in File 1).
/// File 1's Doctor = authorization reference (a visiting consultant has one).
/// PhysicianStaff = the full employment record (with HR, payroll, shifts).
/// </summary>
public abstract class HospitalStaff
{
    private DateTime? _checkInTime;
    private readonly List<string> _attendanceLog = [];

    public string   StaffId     { get; }
    public string   Name        { get; }
    public string   Department  { get; }
    public decimal  Salary      { get; protected set; }
    public DateTime JoiningDate { get; }
    public bool     IsOnDuty    => _checkInTime.HasValue;

    protected HospitalStaff(string staffId, string name, string department, decimal salary)
    {
        StaffId     = staffId;
        Name        = name;
        Department  = department;
        Salary      = salary;
        JoiningDate = DateTime.Today;
    }

    // ── Concrete shared methods — written ONCE, correct for ALL staff types ──

    /// <summary>
    /// GPS-tagged check-in. One method. All 12 staff roles. One compliance fix reaches all.
    /// This is the method the Labour Department checks. It lives here, once.
    /// </summary>
    public void CheckIn(GpsCoordinates location)
    {
        if (IsOnDuty) throw new InvalidOperationException($"{Name} is already checked in.");
        _checkInTime = DateTime.Now;
        var entry = $"CHECK-IN | {Name} ({GetRole()}) | GPS: {location.Latitude:F4},{location.Longitude:F4} | {_checkInTime:HH:mm:ss}";
        _attendanceLog.Add(entry);
        Console.WriteLine($"   [ATTENDANCE] {entry}");
    }

    /// <summary>Calculates shift hours and logs checkout. Identical for all roles.</summary>
    public void CheckOut()
    {
        if (!IsOnDuty) throw new InvalidOperationException($"{Name} has not checked in.");
        var hours = (DateTime.Now - _checkInTime!.Value).TotalMinutes;
        var entry = $"CHECK-OUT | {Name} ({GetRole()}) | Shift: {hours:F0} min";
        _attendanceLog.Add(entry);
        _checkInTime = null;
        Console.WriteLine($"   [ATTENDANCE] {entry}");
    }

    /// <summary>Payslip format — shared formula applies to all staff types.</summary>
    public string GetPayslip(DateTime month)
    {
        decimal pf  = Salary * 0.12m;
        decimal esi = Salary * 0.0175m;
        decimal net = Salary - pf - esi;
        return $"[PAYSLIP] {GetRole()}: {Name} | Gross: ₹{Salary:N0} | PF: ₹{pf:N0} | ESI: ₹{esi:N0} | Net: ₹{net:N0} | {month:MMM yyyy}";
    }

    public IReadOnlyList<string> GetAttendanceLog() => _attendanceLog.AsReadOnly();

    // ── Abstract — every subclass MUST define these ───────────────────────────

    /// <summary>Human-readable role name. Used in reports, UI, and audit logs.</summary>
    public abstract string GetRole();

    /// <summary>
    /// Whether this staff member is authorized to perform a specific medical action.
    /// Prevents a receptionist from writing prescriptions. Enforced structurally.
    /// </summary>
    public abstract bool CanPerform(MedicalAction action);

    // ── Virtual — default behaviour provided; subclasses override if needed ───

    /// <summary>Shift report line. Subclasses override in File 4 (Polymorphism demo).</summary>
    public virtual string GetShiftSummary() =>
        $"{GetRole()}: {Name} — standard shift";

    public virtual AccessLevel GetAccessLevel() => AccessLevel.Standard;

    public override string ToString() =>
        $"[{StaffId}] {GetRole()}: {Name} | {Department} | ₹{Salary:N0}/month | {(IsOnDuty ? "On Duty" : "Off Duty")}";
}

// ─────────────────────────────────────────────────────────────────────────────
// Concrete staff types — each defines ONLY what's unique to that role
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A doctor who is a hospital employee (as opposed to Doctor — the credential/auth class).
/// Inherits all HR mechanics from HospitalStaff; adds clinical authorization.
/// PhysicianStaff : HospitalStaff — Level 2.
/// </summary>
public class PhysicianStaff : HospitalStaff, IEmergencyResponder
{
    public string Specialization   { get; }
    public string LicenseNumber    { get; }   // links to the Doctor credential from File 1
    public int    MaxDailyPatients { get; }
    private int   _patientsSeen;
    private int   _prescriptionsWritten;

    public PhysicianStaff(string staffId, string name, string department, decimal salary,
                          string specialization, string licenseNumber, int maxDailyPatients = 20)
        : base(staffId, name, department, salary)
    {
        Specialization   = specialization;
        LicenseNumber    = licenseNumber;
        MaxDailyPatients = maxDailyPatients;
    }

    public override string GetRole()        => "Doctor";
    public override AccessLevel GetAccessLevel() => AccessLevel.Medical;

    // ← Only doctors can diagnose or prescribe
    public override bool CanPerform(MedicalAction action) =>
        action is MedicalAction.Diagnose or MedicalAction.Prescribe or MedicalAction.RecordVitals;

    public void DiagnosePatient(PatientRecord record, Doctor credential)
    {
        _patientsSeen++;
        Console.WriteLine($"   Dr. {Name} ({Specialization}) examining {record.PatientName}. [#{_patientsSeen}]");
    }

    public void PrescribeMedication(PatientRecord record, string medication, string dosage, Doctor credential)
    {
        record.AddMedication(medication, dosage, credential);
        _prescriptionsWritten++;
        Console.WriteLine($"   Dr. {Name} prescribed {medication} {dosage} for {record.PatientName}.");
    }

    public override string GetShiftSummary() =>
        $"Dr. {Name} ({Specialization}) — {_patientsSeen} patients seen, {_prescriptionsWritten} prescriptions written";

    // IEmergencyResponder — a doctor CAN respond to code blue / emergencies
    public void RespondToEmergency(string room, string alert)
        => Console.WriteLine($"  🩺 Dr. {Name} ({Specialization}) responding to {room} — {alert}");
    public int    GetResponseTimeMinutes() => 3;
    public string GetResponderName()       => $"Dr. {Name} ({Specialization})";
}

/// <summary>
/// Nursing staff. Can administer medications and record vitals.
/// CANNOT diagnose or prescribe — CanPerform() enforces this structurally.
/// Nurse : HospitalStaff — Level 2.
/// </summary>
public class Nurse : HospitalStaff, IEmergencyResponder
{
    public string NursingUnit        { get; }
    public string CertificationLevel { get; }   // e.g. "GNM", "B.Sc Nursing"
    private int   _vitalsRecorded;
    private int   _medicationsAdministered;

    public Nurse(string staffId, string name, string department, decimal salary,
                 string nursingUnit, string certificationLevel)
        : base(staffId, name, department, salary)
    {
        NursingUnit        = nursingUnit;
        CertificationLevel = certificationLevel;
    }

    public override string GetRole()        => "Nurse";
    public override AccessLevel GetAccessLevel() => AccessLevel.Medical;

    // ← Nurses can record vitals and administer (not prescribe)
    public override bool CanPerform(MedicalAction action) =>
        action is MedicalAction.RecordVitals or MedicalAction.AdministerMedication;

    public void RecordVitals(PatientRecord record, VitalSigns vitals)
    {
        _vitalsRecorded++;
        Console.WriteLine($"   Nurse {Name} recorded vitals for {record.PatientName}: " +
                          $"Temp {vitals.TemperatureC}°C, BP {vitals.SystolicBp}/{vitals.DiastolicBp}, SpO2 {vitals.SpO2Percent}%.");
    }

    public void AdministerMedication(PatientRecord record, string medicationId)
    {
        if (!record.GetMedications().Any(m => m.MedicationId == medicationId))
            throw new InvalidOperationException("Cannot administer medication not on the patient's active prescription.");
        _medicationsAdministered++;
        Console.WriteLine($"   Nurse {Name} administered medication {medicationId} to {record.PatientName}.");
    }

    public override string GetShiftSummary() =>
        $"Nurse {Name} ({NursingUnit}) — {_vitalsRecorded} vitals recorded, {_medicationsAdministered} medications administered";

    // IEmergencyResponder — nurses are first responders in most hospital emergencies
    public void RespondToEmergency(string room, string alert)
        => Console.WriteLine($"  🩹 Nurse {Name} ({NursingUnit}) responding to {room} — {alert}");
    public int    GetResponseTimeMinutes() => 1;
    public string GetResponderName()       => $"Nurse {Name} ({CertificationLevel})";
}

/// <summary>
/// Administrative staff. Handles scheduling and registration.
/// CANNOT perform any clinical actions — enforced by CanPerform().
/// Receptionist : HospitalStaff — Level 2.
/// </summary>
public class Receptionist : HospitalStaff
{
    private int _appointmentsBooked;
    private int _patientsRegistered;

    public Receptionist(string staffId, string name, string department, decimal salary)
        : base(staffId, name, department, salary) { }

    public override string GetRole()        => "Receptionist";
    public override AccessLevel GetAccessLevel() => AccessLevel.Administrative;

    // ← Receptionists handle scheduling only. No clinical actions permitted.
    public override bool CanPerform(MedicalAction action) =>
        action == MedicalAction.ScheduleAppointment;

    public void ScheduleAppointment(string patientName, PhysicianStaff doctor, DateTime slot)
    {
        _appointmentsBooked++;
        Console.WriteLine($"   [{Name}] Appointment #{_appointmentsBooked} booked: {patientName} with Dr. {doctor.Name} at {slot:HH:mm dd-MMM}.");
    }

    public PatientRecord RegisterNewPatient(string name, string bloodType)
    {
        _patientsRegistered++;
        Console.WriteLine($"   [{Name}] Registered new patient: {name}.");
        return new PatientRecord(Guid.NewGuid().ToString("N")[..8], name, bloodType);
    }

    public override string GetShiftSummary() =>
        $"Receptionist {Name} — {_appointmentsBooked} appointments booked, {_patientsRegistered} new registrations";
}

/// <summary>
/// A surgeon. Extends PhysicianStaff — this is a 3-level hierarchy:
/// Surgeon → PhysicianStaff → HospitalStaff.
/// Adds surgical clearance and theatre management on top of physician capabilities.
/// </summary>
public class Surgeon : PhysicianStaff
{
    public string SurgerySpecialization { get; }
    public string OperatingTheatreId    { get; }
    private int   _surgeriesPerformed;
    private double _surgeryHoursTotal;

    public Surgeon(string staffId, string name, string department, decimal salary,
                   string surgerySpecialization, string licenseNumber, string theatreId)
        : base(staffId, name, department, salary, surgerySpecialization, licenseNumber)
    {
        SurgerySpecialization = surgerySpecialization;
        OperatingTheatreId    = theatreId;
    }

    public override string GetRole()        => "Surgeon";
    public override AccessLevel GetAccessLevel() => AccessLevel.Surgical;

    // ← Surgeons can do everything a physician can, plus surgery
    public override bool CanPerform(MedicalAction action) =>
        action == MedicalAction.PerformSurgery || base.CanPerform(action);

    public void PerformSurgery(PatientRecord patient, string procedureName, double estimatedHours)
    {
        _surgeriesPerformed++;
        _surgeryHoursTotal += estimatedHours;
        Console.WriteLine($"   Surgeon {Name} performing {procedureName} on {patient.PatientName} " +
                          $"in {OperatingTheatreId}. Est. {estimatedHours:F1} hrs. [Surgery #{_surgeriesPerformed}]");
    }

    public override string GetShiftSummary() =>
        $"Surgeon {Name} ({SurgerySpecialization}) — {_surgeriesPerformed} surgeries, {_surgeryHoursTotal:F1} hrs in theatre";
}

// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class InheritanceDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  INHERITANCE DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        var location = new GpsCoordinates(12.9716, 77.5946, DateTime.Now);

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — three separate classes with copy-paste:");
        var naiveDoc   = new NaiveDoctor   { StaffId = "S001", Name = "Ravi Kumar", Salary = 80_000m };
        var naiveNurse = new NaiveNurse    { StaffId = "S002", Name = "Sunita Das",  Salary = 35_000m };
        var naiveRecep = new NaiveReceptionist { StaffId = "S003", Name = "Anita Roy", Salary = 20_000m };

        naiveDoc.CheckIn();    // no GPS
        naiveNurse.CheckIn();  // no GPS
        naiveRecep.CheckIn();  // no GPS

        Console.WriteLine("   GPS mandate issued. Developer adds GPS to Doctor.CheckIn() only.");
        Console.WriteLine("   Nurse and Receptionist still have no GPS. ₹15 lakh compliance notice.");
        Console.WriteLine($"   And 3 separate payslips with 3 separate formulas:\n   {naiveDoc.GetPayslip()}\n   {naiveNurse.GetPayslip()}\n   {naiveRecep.GetPayslip()}\n");

        // ── AFTER ───────────────────────────────────────────────────────────
        Console.WriteLine("✅ AFTER — HospitalStaff base. GPS added once → all 4 types covered:\n");

        var credential = new Doctor("D001", "Ravi Kumar", "Cardiology", "MCI-2017-44210");
        var physician  = new PhysicianStaff("S001", "Ravi Kumar",  "Cardiology", 1_20_000m, "Cardiology", "MCI-2017-44210");
        var nurse      = new Nurse         ("S002", "Sunita Das",   "ICU",        45_000m,  "ICU",      "B.Sc Nursing");
        var receptionist = new Receptionist("S003", "Anita Roy",    "OPD",        25_000m);
        var surgeon    = new Surgeon       ("S004", "Deepak Verma", "Surgery",    2_00_000m, "Cardiac Surgery", "MCI-2010-11200", "OT-3");

        Console.WriteLine("   All 4 staff types check in with GPS (one base method):");
        HospitalStaff[] todaysShift = [physician, nurse, receptionist, surgeon];
        foreach (var staff in todaysShift)
            staff.CheckIn(location);

        Console.WriteLine("\n   Payslip — one formula, all types:");
        foreach (var staff in todaysShift)
            Console.WriteLine($"   {staff.GetPayslip(DateTime.Today)}");

        Console.WriteLine("\n   Access levels:");
        foreach (var staff in todaysShift)
            Console.WriteLine($"   {staff.GetRole(),-15} | Access: {staff.GetAccessLevel(),-15} | CanPrescribe: {staff.CanPerform(MedicalAction.Prescribe)}");

        Console.WriteLine("\n   Surgeon is 3 levels deep: Surgeon → PhysicianStaff → HospitalStaff:");
        var patient = new PatientRecord("P001", "Mohan Lal", "AB+");
        patient.UpdateDiagnosis("Coronary artery disease — surgical intervention indicated", credential);
        surgeon.PerformSurgery(patient, "CABG (Coronary Artery Bypass Graft)", estimatedHours: 4.5);

        Console.WriteLine("\n   Government GPS update tomorrow? Change 1 line in HospitalStaff.CheckIn().");
        Console.WriteLine("   All 4 types — and any future types — get it for free.");

        Console.WriteLine("\n✅ Inheritance — understood.");
    }
}
