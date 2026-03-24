/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏥 CONCEPT: Abstract Class vs Interface                                     ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: 03_Inheritance.cs + 04_Polymorphism.cs                        ║
 * ║     We have a staffing hierarchy (HospitalStaff) and a capability interface  ║
 * ║     (ITreatmentProvider). Now we make the RULE EXPLICIT: abstract class =   ║
 * ║     shared identity, interface = pluggable capability.                       ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Abstract class: a partially-built class sharing IDENTITY and state.        ║
 * ║    "A MedicalEquipment is a thing with a serial number and a service date.  ║
 * ║     Subclasses fill in GetReadingUnit() and RequiresCalibration()."         ║
 * ║  Interface: a contract of CAPABILITY with no state and no implementation.   ║
 * ║    "Anything that can respond to an emergency — staff, ambulance, security  ║
 * ║     guard — implements IEmergencyResponder. No shared base class needed."   ║
 * ║                                                                              ║
 * ║  THE PERMANENT RULE:                                                         ║
 * ║    Abstract class = IDENTITY  (HospitalStaff, MedicalEquipment)             ║
 * ║    Interface      = CAPABILITY (ISchedulable, IAuditable, IEmergencyResponder)║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  A hospital ID badge (abstract class) says WHO you are — your name, role,   ║
 * ║  department, hire date. A keycard access permission (interface) says WHAT   ║
 * ║  you can do — enter ICU, dispense drugs, start ambulance. A doctor has both ║
 * ║  a badge AND several keycards. An ambulance has no badge but has keycards.  ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  A hospital built HospitalEntity as a single abstract base with 15 methods: ║
 * ║  billing, scheduling, patient care, reporting, equipment management.        ║
 * ║  LaboratoryEquipment : HospitalEntity compiled fine. Then: the MRI machine  ║
 * ║  "is a HospitalEntity" — it had AdmitPatient(), GetPayslip(), and           ║
 * ║  ScheduleLeave() all throwing NotImplementedException. The framework called ║
 * ║  GetPayslip() on every HospitalEntity during payroll. The MRI machine's     ║
 * ║  exception brought down the payroll service. Eight nurses were not paid.   ║
 * ║                                                                              ║
 * ║  THE DESIGN DECISION LENS:                                                   ║
 * ║  "Is this a TYPE (use abstract class) or a BEHAVIOUR (use interface)?"       ║
 * ║                                                                              ║
 * ║  QUICK RECALL (2-year cheat code):                                          ║
 * ║  An MRI machine IS equipment. It is NOT a staff member. Don't make it one.  ║
 * ║                                                                              ║
 * ║  CONNECTS FORWARD:                                                           ║
 * ║  File 6 (Composition vs Inheritance) shows what happens when you COMPOSE    ║
 * ║  these capabilities — ISchedulable injected into PatientCareService instead ║
 * ║  of PatientCareService inheriting a giant base class.                        ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.HospitalModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Naive implementation
// "Everything in a hospital is a HospitalEntity. I'll put all methods in one
//  abstract base class and inherit from it for everything."
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// NAIVE: One abstract base that tries to be the identity for staff, equipment,
/// ambulances, and billing entities. 15 methods — every subclass throws 80% of them.
/// </summary>
public abstract class NaiveHospitalEntity
{
    public string EntityId   { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;

    // Staff-specific
    public abstract string GetRole();
    public abstract decimal GetPayslip();
    public abstract void ScheduleLeave(DateTime from, DateTime to);
    public abstract void CheckIn(string location);
    public abstract void AdmitPatient(string patientId);
    public abstract void PrescribeMedication(string patientId, string drug);

    // Equipment-specific
    public abstract string GetReadingUnit();
    public abstract bool RequiresCalibration();
    public abstract DateTime GetNextServiceDate();

    // Billing-specific
    public abstract void GenerateInvoice(string invoiceId);
    public abstract decimal GetOutstandingBalance();

    // Scheduling-specific
    public abstract void ScheduleForTime(DateTime slot);
    public abstract bool IsAvailableAt(DateTime slot);

    // Emergency-specific
    public abstract void RespondToEmergency(string roomNumber);
    public abstract int GetResponseTimeMinutes();
}

/// <summary>
/// NAIVE: MRI machine inherits the full 15-method contract.
/// Throws NotImplementedException on 10 of them.
/// </summary>
public class NaiveMriMachine : NaiveHospitalEntity
{
    public override string  GetRole()                              => throw new NotImplementedException("MRI is not staff");
    public override decimal GetPayslip()                          => throw new NotImplementedException("MRI is not staff");
    public override void    ScheduleLeave(DateTime f, DateTime t) => throw new NotImplementedException("MRI cannot take leave");
    public override void    CheckIn(string location)              => throw new NotImplementedException("MRI cannot check in");
    public override void    AdmitPatient(string patientId)        => throw new NotImplementedException("MRI cannot admit patients");
    public override void    PrescribeMedication(string p, string d) => throw new NotImplementedException("MRI cannot prescribe");

    public override string   GetReadingUnit()       => "Tesla";
    public override bool     RequiresCalibration()  => true;
    public override DateTime GetNextServiceDate()   => DateTime.Now.AddMonths(6);

    public override void    GenerateInvoice(string id)        => throw new NotImplementedException();
    public override decimal GetOutstandingBalance()           => throw new NotImplementedException();
    public override void    ScheduleForTime(DateTime slot)    => Console.WriteLine($"MRI booked at {slot:HH:mm}");
    public override bool    IsAvailableAt(DateTime slot)      => true;
    public override void    RespondToEmergency(string room)   => throw new NotImplementedException("MRI cannot walk to a room");
    public override int     GetResponseTimeMinutes()          => throw new NotImplementedException();
}

// WHY IT FAILS:
// 1. LISKOV VIOLATION: MriMachine "is a" NaiveHospitalEntity — but it can't do 10 of 15
//    things a HospitalEntity promises. The contract is broken.
// 2. PAYROLL BUG: The payroll batch calls GetPayslip() on every HospitalEntity.
//    MRI machine throws NotImplementedException → payroll service crashes.
// 3. ADDING A NEW TYPE IS EXPLOSIVE: Every new entity must implement all 15 methods
//    or throw NotImplementedException — there is no third option.
// 4. TESTING IS IMPOSSIBLE: You can't mock a 15-method abstract class meaningfully.
//    You end up implementing every method in every test double.

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — The clean separation
// THE RULE: Abstract class = IDENTITY (what it IS). Interface = CAPABILITY (what it CAN DO).
// ─────────────────────────────────────────────────────────────────────────────

// ── CAPABILITY INTERFACES ────────────────────────────────────────────────────
// These are "keycards". Any entity can hold any combination of these.

/// <summary>Can occupy a time slot on the hospital schedule.</summary>
public interface ISchedulable
{
    bool IsAvailableAt(DateTime slot);
    void ScheduleForTime(DateTime slot);
    string GetScheduleLabel();
}

/// <summary>Participates in compliance audit trails.</summary>
public interface IAuditable
{
    string GetEntityId();
    string GetEntityType();
    IReadOnlyList<string> GetAuditEvents();
}

/// <summary>Can be dispatched to an emergency.</summary>
public interface IEmergencyResponder
{
    void RespondToEmergency(string roomNumber, string alertDescription);
    int  GetResponseTimeMinutes();
    string GetResponderName();
}

/// <summary>Requires periodic calibration / servicing.</summary>
public interface ICalibrationRequired
{
    bool     RequiresCalibration();
    DateTime GetNextServiceDate();
    void     RecordCalibration(DateTime performedAt, string performedBy);
}

// ── ABSTRACT CLASSES (IDENTITIES) ────────────────────────────────────────────
// These define "what a thing IS" — shared fields, constructors, lifecycle logic.

/// <summary>
/// IDENTITY base for all medical equipment.
/// Has a serial number, purchase date, and service log. That's what makes it "equipment."
/// Capabilities (ISchedulable, ICalibrationRequired) are added via interfaces.
/// </summary>
public abstract class MedicalEquipment
{
    private readonly List<string> _serviceLog = [];

    public string   SerialNumber  { get; }
    public string   ModelName     { get; }
    public DateTime PurchasedAt   { get; }
    public string   Department    { get; }

    protected MedicalEquipment(string serialNumber, string modelName, DateTime purchasedAt, string department)
    {
        SerialNumber = serialNumber;
        ModelName    = modelName;
        PurchasedAt  = purchasedAt;
        Department   = department;
    }

    /// <summary>
    /// What unit does this equipment report readings in? e.g. "Tesla", "BPM", "°C"
    /// </summary>
    public abstract string GetReadingUnit();

    protected void LogService(string entry) => _serviceLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm}] {entry}");
    public IReadOnlyList<string> GetServiceLog() => _serviceLog.AsReadOnly();

    public override string ToString() => $"{ModelName} (S/N: {SerialNumber}) [{Department}]";
}

// ── CONCRETE EQUIPMENT TYPES ──────────────────────────────────────────────────

/// <summary>
/// MRI Scanner — IS MedicalEquipment (identity).
/// CAN BE scheduled (ISchedulable) and requires calibration (ICalibrationRequired).
/// Cannot respond to emergencies. Cannot generate a payslip. Full stop.
/// </summary>
public class MriScanner : MedicalEquipment, ISchedulable, ICalibrationRequired
{
    private DateTime _nextService;
    private DateTime? _nextBooking;

    public MriScanner(string serialNumber, string department)
        : base(serialNumber, "Siemens MAGNETOM Altea 1.5T", DateTime.Now.AddYears(-2), department)
    {
        _nextService = DateTime.Now.AddMonths(6);
    }

    public override string GetReadingUnit() => "Tesla (field strength)";

    // ISchedulable
    public bool   IsAvailableAt(DateTime slot)  => _nextBooking == null || _nextBooking.Value.Date != slot.Date;
    public void   ScheduleForTime(DateTime slot) { _nextBooking = slot; LogService($"Scheduled scan at {slot:HH:mm}"); }
    public string GetScheduleLabel()             => $"MRI — {ModelName}";

    // ICalibrationRequired
    public bool     RequiresCalibration()   => DateTime.Now >= _nextService;
    public DateTime GetNextServiceDate()    => _nextService;
    public void     RecordCalibration(DateTime performedAt, string by)
    {
        _nextService = performedAt.AddMonths(6);
        LogService($"Calibrated by {by}. Next due: {_nextService:yyyy-MM-dd}");
    }
}

/// <summary>
/// Lab centrifuge — IS MedicalEquipment.
/// CAN BE scheduled and calibrated.
/// </summary>
public class LaboratoryEquipment : MedicalEquipment, ISchedulable, ICalibrationRequired
{
    private DateTime _nextService;

    public LaboratoryEquipment(string serialNumber, string modelName, string department)
        : base(serialNumber, modelName, DateTime.Now.AddYears(-1), department)
    {
        _nextService = DateTime.Now.AddMonths(3);
    }

    public override string GetReadingUnit() => "RPM";

    public bool   IsAvailableAt(DateTime slot)   => true;
    public void   ScheduleForTime(DateTime slot)  => LogService($"Lab run scheduled: {slot:HH:mm}");
    public string GetScheduleLabel()              => $"Lab — {ModelName}";

    public bool     RequiresCalibration()          => DateTime.Now >= _nextService;
    public DateTime GetNextServiceDate()           => _nextService;
    public void     RecordCalibration(DateTime at, string by)
    {
        _nextService = at.AddMonths(3);
        LogService($"Calibrated by {by}");
    }
}

// ── STAFF + EMERGENCY RESPONDER ───────────────────────────────────────────────
// Staff types (already defined in File 3) can also implement IEmergencyResponder.
// Here we show SecurityStaff — not a PhysicianStaff, not a Nurse, but IS HospitalStaff.

/// <summary>
/// Security guard — IS HospitalStaff (inherits employee identity).
/// CAN respond to emergencies (IEmergencyResponder).
/// Cannot prescribe or treat patients.
/// </summary>
public class SecurityStaff : HospitalStaff, IEmergencyResponder
{
    private readonly string _assignedZone;
    private readonly List<string> _incidentLog = [];

    public SecurityStaff(string employeeId, string name, string department, decimal salary, string zone)
        : base(employeeId, name, department, salary)
    {
        _assignedZone = zone;
    }

    public override string  GetRole()              => "Security Officer";
    public override bool    CanPerform(MedicalAction action) =>
        action is MedicalAction.ScheduleAppointment;

    // IEmergencyResponder
    public void   RespondToEmergency(string room, string alert)
    {
        _incidentLog.Add($"[{DateTime.Now:HH:mm}] Responded to {room}: {alert}");
        Console.WriteLine($"  🔒 Security [{Name}] responding to {room} — {alert}");
    }
    public int    GetResponseTimeMinutes() => 2;
    public string GetResponderName()       => $"Security: {Name} (Zone {_assignedZone})";
}

/// <summary>
/// Ambulance — IS NOT HospitalStaff, IS NOT MedicalEquipment.
/// It's a vehicle. But it CAN respond to emergencies and CAN be scheduled.
/// Interfaces let it participate in both dispatch lists and schedule grids.
/// </summary>
public class HospitalAmbulance : IEmergencyResponder, ISchedulable, IAuditable
{
    private readonly string       _vehicleId;
    private readonly string       _driverName;
    private readonly List<string> _auditEvents = [];
    private DateTime?             _nextBooking;

    public HospitalAmbulance(string vehicleId, string driverName)
    {
        _vehicleId  = vehicleId;
        _driverName = driverName;
    }

    // IEmergencyResponder
    public void RespondToEmergency(string room, string alert)
    {
        var entry = $"[{DateTime.Now:HH:mm}] Dispatched to {room}: {alert}";
        _auditEvents.Add(entry);
        Console.WriteLine($"  🚑 Ambulance [{_vehicleId}] en route to {room}");
    }
    public int    GetResponseTimeMinutes() => 8;
    public string GetResponderName()       => $"Ambulance {_vehicleId} (Driver: {_driverName})";

    // ISchedulable
    public bool   IsAvailableAt(DateTime slot)   => _nextBooking == null || _nextBooking.Value != slot;
    public void   ScheduleForTime(DateTime slot)  { _nextBooking = slot; _auditEvents.Add($"Scheduled: {slot:HH:mm}"); }
    public string GetScheduleLabel()              => $"Ambulance {_vehicleId}";

    // IAuditable
    public string                 GetEntityId()    => _vehicleId;
    public string                 GetEntityType()  => "HospitalAmbulance";
    public IReadOnlyList<string>  GetAuditEvents() => _auditEvents.AsReadOnly();
}

// ── THE KEY DEMO PATTERN ──────────────────────────────────────────────────────
// A single List<IEmergencyResponder> holds TOTALLY unrelated types.
// Doctor (staff), SecurityStaff (staff), HospitalAmbulance (vehicle) — one interface binds them.

// ─────────────────────────────────────────────────────────────────────────────
// 🎬 DEMO
// ─────────────────────────────────────────────────────────────────────────────
public static class AbstractClassVsInterfaceDemo
{
    public static void Demo()
    {
        Console.WriteLine("=== Abstract Class vs Interface: Hospital Entities ===\n");

        // 1. Equipment uses abstract class identity + interface capabilities
        Console.WriteLine("── Equipment scheduling & calibration ──");
        var mri  = new MriScanner("MRI-2023-001", "Radiology");
        var lab  = new LaboratoryEquipment("LAB-CB-007", "Heraeus Multifuge X3R", "Pathology");

        mri.ScheduleForTime(DateTime.Today.AddHours(9));
        Console.WriteLine($"  MRI available tomorrow? {mri.IsAvailableAt(DateTime.Today.AddDays(1))}");
        Console.WriteLine($"  MRI needs calibration?  {mri.RequiresCalibration()}");

        lab.ScheduleForTime(DateTime.Today.AddHours(11));
        Console.WriteLine($"  Lab reading unit: {lab.GetReadingUnit()}");

        // 2. Emergency dispatch list — totally unrelated types, one interface
        Console.WriteLine("\n── Emergency Response Dispatch ──");
        Console.WriteLine("  🚨 Code Blue — Room 407. Dispatching all responders...\n");

        // PhysicianStaff, SecurityStaff, HospitalAmbulance — all IEmergencyResponder
        IReadOnlyList<IEmergencyResponder> responders =
        [
            new PhysicianStaff("D-101", "Dr. Kavya Reddy",    "Cardiology", 250_000m, "Cardiology", "MCI-KR-101"),
            new Nurse(         "N-055", "Nurse Priya Sharma",  "ICU",        65_000m,  "ICU", "B.Sc Nursing"),
            new SecurityStaff( "S-009", "Ramesh Kumar",        "Security",   35_000m,  "Wing-B"),
            new HospitalAmbulance("AMB-KA-03", "Suresh Nair"),
        ];

        foreach (var r in responders)
        {
            r.RespondToEmergency("407", "Cardiac arrest — patient unresponsive");
            Console.WriteLine($"    ETA: {r.GetResponseTimeMinutes()} min — {r.GetResponderName()}");
        }

        // 3. ISchedulable works for both equipment AND ambulance
        Console.WriteLine("\n── Unified Schedule Board ──");
        IReadOnlyList<ISchedulable> scheduleables =
        [
            mri,
            lab,
            new HospitalAmbulance("AMB-KA-04", "Vijay Kumar"),
        ];

        var slot = DateTime.Today.AddHours(14);
        foreach (var s in scheduleables)
        {
            if (s.IsAvailableAt(slot))
            {
                s.ScheduleForTime(slot);
                Console.WriteLine($"  ✓ {s.GetScheduleLabel()} booked at {slot:HH:mm}");
            }
        }

        // 4. Audit — only entities that implement IAuditable are included
        Console.WriteLine("\n── Audit-Eligible Entities ──");
        var ambulance = new HospitalAmbulance("AMB-KA-03", "Suresh Nair");
        ambulance.RespondToEmergency("407", "Test alert");
        ambulance.ScheduleForTime(DateTime.Today.AddHours(16));

        IReadOnlyList<IAuditable> auditables = [ambulance];
        foreach (var a in auditables)
        {
            Console.WriteLine($"  Entity [{a.GetEntityType()}:{a.GetEntityId()}]");
            foreach (var ev in a.GetAuditEvents())
                Console.WriteLine($"    {ev}");
        }

        Console.WriteLine("\n  RULE CONFIRMED:");
        Console.WriteLine("  Abstract class = IDENTITY  (MedicalEquipment, HospitalStaff)");
        Console.WriteLine("  Interface      = CAPABILITY (IEmergencyResponder, ISchedulable, IAuditable)");
        Console.WriteLine("  An ambulance has no base class — just capabilities. That's correct.");
    }
}
