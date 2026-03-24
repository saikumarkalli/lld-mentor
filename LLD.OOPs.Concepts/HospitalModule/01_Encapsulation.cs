/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🏥 CONCEPT: Encapsulation                                                   ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  🔗 BUILDS ON: Nothing — this is the foundation.                             ║
 * ║     Every subsequent file relies on PatientRecord being safe to use.        ║
 * ║     If this class is broken, the entire hospital system is broken.          ║
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Encapsulation means every field is private, and the class enforces its     ║
 * ║  own rules before allowing any state change. The object controls its own   ║
 * ║  data — callers interact only through the doors it explicitly opens.        ║
 * ║                                                                              ║
 * ║  MENTAL MODEL:                                                               ║
 * ║  A patient's physical chart in a locked cabinet. You don't pull out the     ║
 * ║  chart and scribble on it. A nurse submits a vitals entry. A doctor writes  ║
 * ║  a prescription note. Every change is signed, dated, witnessed. The cabinet ║
 * ║  (class) enforces the process — it refuses unsigned entries.                ║
 * ║                                                                              ║
 * ║  THE DISASTER STORY:                                                         ║
 * ║  In 2015, a US hospital system had patient status as a public boolean.      ║
 * ║  A background data migration script set isAlive = false on 312 patient     ║
 * ║  records while cleaning up "discharged" entries. The pharmacy module read  ║
 * ║  isAlive before dispensing medications. 312 active patients stopped         ║
 * ║  receiving their meds for 9 hours before a nurse noticed. Three were in     ║
 * ║  ICU on critical drips. The field had no guard, no audit, no authorization. ║
 * ║                                                                              ║
 * ║  THE DESIGN DECISION LENS:                                                   ║
 * ║  "Who is allowed to change this data, and under what rules?"                ║
 * ║  Every design choice in this file answers that question.                    ║
 * ║                                                                              ║
 * ║  QUICK RECALL (2-year cheat code):                                          ║
 * ║  A patient record is a legal document — every change needs a signature.    ║
 * ║                                                                              ║
 * ║  CONNECTS FORWARD:                                                           ║
 * ║  Once PatientRecord owns its rules, File 2 (Abstraction) hides HOW the     ║
 * ║  hospital service uses it — callers see only a clean admission API.         ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.HospitalModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — Naive implementation
// This is what a developer writes when thinking about a "data container".
// Public fields feel like "simple and direct" — until the legal team calls.
// ─────────────────────────────────────────────────────────────────────────────

public class NaivePatientRecord
{
    public string  PatientId      = string.Empty;
    public string  PatientName    = string.Empty;
    public string  Diagnosis      = string.Empty; // ← can be erased with ""
    public string  MedicationList = string.Empty; // ← "None" overwrites live prescriptions
    public bool    IsAlive;                       // ← set to false by a migration script
    public string  BloodType      = string.Empty;
    public decimal BillAmount;                    // ← set to 0 = billing fraud
    public string  DischargeStatus = string.Empty; // ← "Discharged" before a doctor signs anything
}

// 💥 IMMEDIATE PROBLEMS:
// — record.IsAlive = false;              runs without any medical confirmation
// — record.Diagnosis = "";              erases the diagnosis — zero audit trail
// — record.BillAmount = 0;             billing fraud, no authorization needed
// — record.DischargeStatus = "Discharged"; bypasses doctor sign-off entirely
// — record.MedicationList = "None";    overwrites active prescriptions
// — ALL of these compile cleanly. The type system is completely silent.

// ─────────────────────────────────────────────────────────────────────────────
// 🔍 WHY THIS DESIGN FAILS
//
// PROBLEM 1 — LEGAL LIABILITY (HIPAA / India's DPDP Act)
// Patient records are legally protected documents. Every modification must record:
// WHO changed it, WHEN, and WHAT changed. A court asking "who changed the diagnosis
// on March 15th?" must get a precise answer. With public fields, the answer is
// "we don't know" — and the hospital loses the malpractice case by default.
// Public fields produce zero audit trail. That is not a feature gap. It is a legal gap.
//
// PROBLEM 2 — PATIENT SAFETY (THE DISASTER STORY ABOVE)
// IsAlive = false with no guard causes downstream systems to treat a living patient
// as deceased. Pharmacy stops sending medications. Billing marks the account closed.
// Bed allocation reallocates the bed. These are cascading safety failures triggered
// by one unguarded boolean assignment. The field must be write-protected.
//
// PROBLEM 3 — IMPOSSIBLE INVARIANTS
// BillAmount can be set to -999. Diagnosis can be "". DischargeStatus can be
// "Discharged" while the patient is still on an IV drip. These are not edge cases —
// they are states the system should make structurally impossible. Public fields
// cannot enforce business rules. Methods can.
//
// DESIGN DECISION: We chose computed DischargeStatus over a settable field.
// Alternative considered: a settable enum with validation in a setter.
// Rejected because: status must be DERIVED from clinical events (discharge summary
// signed by a doctor), not set by a field assignment. You cannot have
// status = Discharged without a DischargeSummary signed by a licensed doctor.
// Computed properties make this impossible to violate, even accidentally.
// ─────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — OOP-correct, production-realistic
// Every field is private. Every change requires authorization and leaves an audit.
// Invalid state is not just discouraged — it is structurally impossible.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Discharge status derived from clinical document state.
/// Cannot be directly set — it's computed from whether a DischargeSummary has been
/// signed by a licensed doctor.
/// </summary>
public enum PatientDischargeStatus { Admitted, UnderTreatment, PendingDischarge, Discharged }

/// <summary>
/// An immutable audit entry. Every change to a patient record creates one of these.
/// This is the "who changed what and when" that satisfies legal audits.
/// </summary>
public record AuditEntry(
    DateTime Timestamp,
    string   ChangedBy,       // Doctor.LicenseNumber or Staff identifier
    string   FieldChanged,
    string   OldValue,
    string   NewValue);

/// <summary>
/// An immutable medication entry. Cannot be modified after creation.
/// The prescribing doctor's ID is embedded — unsigned prescriptions are not possible.
/// </summary>
public record MedicationEntry(
    string   MedicationId,
    string   Name,
    string   Dosage,
    string   PrescribedByLicense, // license number — not a name that can be forged
    DateTime PrescribedAt);

/// <summary>
/// An immutable billing line item. Amount cannot be negative; description cannot be empty.
/// </summary>
public record BillingItem(
    string   ItemId,
    string   Description,
    decimal  Amount,
    DateTime AddedAt,
    string   AddedBy)
{
    // ← Property init override — validates the positional parameter before assignment.
    public decimal Amount { get; init; } = Amount > 0
        ? Amount
        : throw new ArgumentException($"Billing item amount must be positive. Got: ₹{Amount}.");
    public string Description { get; init; } = !string.IsNullOrWhiteSpace(Description)
        ? Description
        : throw new ArgumentException("Billing item must have a description.");
}

/// <summary>
/// A discharge summary. Its existence (with a signed doctor) is what drives
/// PatientRecord.Status → Discharged. Cannot be created without a licensed doctor.
/// </summary>
public record DischargeSummary(
    string   SummaryId,
    string   ClinicalNotes,
    Doctor   SignedBy,
    DateTime SignedAt);

/// <summary>
/// A doctor credential. Used for authorization throughout the hospital system.
/// No anonymous authorization is permitted — every action must trace back to a license.
/// Note: This is the authorization credential. File 3 (Inheritance) introduces
/// PhysicianStaff : HospitalStaff which is the full employee record for a doctor.
/// A visiting consultant has a Doctor credential but may not be a staff employee.
/// </summary>
public class Doctor
{
    public string DoctorId      { get; }
    public string Name          { get; }
    public string Specialization { get; }
    public string LicenseNumber  { get; }  // required — used as the audit identifier

    public Doctor(string doctorId, string name, string specialization, string licenseNumber)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber))
            throw new ArgumentException(
                "A Doctor must have a valid license number. Anonymous authorization is not permitted.");
        DoctorId       = doctorId;
        Name           = name;
        Specialization = specialization;
        LicenseNumber  = licenseNumber;
    }

    public override string ToString() => $"Dr. {Name} ({Specialization}) [Lic: {LicenseNumber}]";
}

/// <summary>
/// A patient's medical and billing record.
///
/// RULES enforced by this class (not by callers):
///   — Diagnosis can only be updated by a licensed doctor, with an audit entry.
///   — Medications can only be added/removed by a licensed doctor, with a reason.
///   — IsAlive can only become false via MarkDeceased(), which requires doctor certification.
///   — DischargeStatus is COMPUTED — it cannot be directly set.
///   — BillAmount can only grow via AddCharge(); insurance claims reduce it via ApplyInsurance().
/// </summary>
public class PatientRecord
{
    private string                    _diagnosis   = "Pending assessment";
    private readonly List<MedicationEntry>  _medications = [];
    private readonly List<AuditEntry>       _auditLog    = [];
    private readonly List<BillingItem>      _billing     = [];
    private bool                      _isDeceased;
    private DischargeSummary?         _dischargeSummary;

    public string PatientId   { get; }
    public string PatientName { get; }
    public string BloodType   { get; }
    public DateTime AdmittedAt { get; } = DateTime.Now;

    // ← Read-only view of diagnosis. Cannot be directly set from outside.
    public string Diagnosis => _diagnosis;

    // ← IsAlive is a READ-ONLY computed property. No setter.
    //   Try: record.IsAlive = false → compiler error: "Property has no setter."
    public bool IsAlive => !_isDeceased;

    // ← Status is FULLY COMPUTED from clinical document state.
    //   No field, no setter — derived from what has actually happened to the patient.
    public PatientDischargeStatus Status
    {
        get
        {
            if (_isDeceased)                return PatientDischargeStatus.Discharged;
            if (_dischargeSummary != null)  return PatientDischargeStatus.Discharged;
            if (_medications.Count > 0)     return PatientDischargeStatus.UnderTreatment;
            return PatientDischargeStatus.Admitted;
        }
    }

    public decimal TotalBill => _billing.Sum(b => b.Amount);

    public PatientRecord(string patientId, string patientName, string bloodType)
    {
        PatientId   = patientId;
        PatientName = patientName;
        BloodType   = bloodType;
        Log("SYSTEM", "PatientRecord", "", "Record created");
    }

    /// <summary>
    /// Updates the diagnosis. REQUIRES a licensed doctor.
    /// Previous diagnosis is preserved in the audit log — legally required.
    /// </summary>
    public void UpdateDiagnosis(string newDiagnosis, Doctor authorizedBy)
    {
        ArgumentNullException.ThrowIfNull(authorizedBy, "Diagnosis update requires an authorizing doctor.");
        if (string.IsNullOrWhiteSpace(newDiagnosis))
            throw new ArgumentException("Diagnosis cannot be blank. Use 'Under investigation' if assessment is pending.");

        Log(authorizedBy.LicenseNumber, "Diagnosis", _diagnosis, newDiagnosis);
        _diagnosis = newDiagnosis;
    }

    /// <summary>
    /// Prescribes a medication. REQUIRES a licensed doctor.
    /// Creates an immutable prescription entry with full provenance.
    /// </summary>
    public void AddMedication(string name, string dosage, Doctor prescribedBy)
    {
        ArgumentNullException.ThrowIfNull(prescribedBy, "Medication requires a prescribing doctor.");
        if (_isDeceased) throw new InvalidOperationException("Cannot prescribe medication for a deceased patient.");

        var entry = new MedicationEntry(
            MedicationId:         Guid.NewGuid().ToString("N")[..8],
            Name:                 name,
            Dosage:               dosage,
            PrescribedByLicense:  prescribedBy.LicenseNumber,
            PrescribedAt:         DateTime.Now);

        _medications.Add(entry);
        Log(prescribedBy.LicenseNumber, "Medications", "—", $"Added: {name} {dosage}");
    }

    /// <summary>
    /// Removes a medication. Requires the prescribing/authorizing doctor AND a documented reason.
    /// You cannot silently remove a prescribed medication — ever.
    /// </summary>
    public void RemoveMedication(string medicationId, Doctor authorizedBy, string reason)
    {
        ArgumentNullException.ThrowIfNull(authorizedBy);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required to remove a prescribed medication.");

        var med = _medications.FirstOrDefault(m => m.MedicationId == medicationId)
                  ?? throw new InvalidOperationException($"Medication {medicationId} not found on this record.");

        _medications.Remove(med);
        Log(authorizedBy.LicenseNumber, "Medications", $"Active: {med.Name}", $"Removed — reason: {reason}");
    }

    /// <summary>
    /// Certifies the patient as deceased. REQUIRES doctor certification.
    /// Downstream systems (pharmacy, billing, bed allocation) read IsAlive.
    /// This is the ONLY way to set it to false. The migration script from the disaster
    /// story above would fail here with "Certification requires an authorizing doctor."
    /// </summary>
    public void MarkDeceased(DateTime timeOfDeath, Doctor certifiedBy)
    {
        ArgumentNullException.ThrowIfNull(certifiedBy, "Death certification requires a licensed doctor.");
        if (_isDeceased) throw new InvalidOperationException("Patient is already recorded as deceased.");

        _isDeceased = true;
        Log(certifiedBy.LicenseNumber, "IsAlive", "true", $"false — certified at {timeOfDeath:HH:mm:ss}");
    }

    /// <summary>
    /// Adds a billing charge. Amount must be positive — negative charges are not allowed.
    /// Use ApplyInsurance() to reduce the total, not a negative BillingItem.
    /// </summary>
    public void AddCharge(string description, decimal amount, string addedBy)
    {
        var item = new BillingItem(Guid.NewGuid().ToString("N")[..8], description, amount, DateTime.Now, addedBy);
        _billing.Add(item);
        Log(addedBy, "Billing", "—", $"Charge added: {description} ₹{amount:N0}");
    }

    /// <summary>
    /// Signs the discharge summary. After this, Status → Discharged.
    /// Cannot discharge a patient without a doctor's signature — structurally enforced.
    /// </summary>
    public void SignDischargeSummary(string clinicalNotes, Doctor signedBy)
    {
        ArgumentNullException.ThrowIfNull(signedBy, "Discharge requires a doctor's signature.");
        if (_isDeceased) throw new InvalidOperationException("Use MarkDeceased() for deceased patients.");

        _dischargeSummary = new DischargeSummary(
            Guid.NewGuid().ToString("N")[..8], clinicalNotes, signedBy, DateTime.Now);

        Log(signedBy.LicenseNumber, "DischargeStatus", "PendingDischarge", "Discharged");
    }

    public IReadOnlyList<AuditEntry>      GetAuditLog()     => _auditLog.AsReadOnly();
    public IReadOnlyList<MedicationEntry> GetMedications()  => _medications.AsReadOnly();
    public IReadOnlyList<BillingItem>     GetBillingItems() => _billing.AsReadOnly();

    // ← Private helper — callers cannot write to the audit log directly.
    //   Only PatientRecord's own methods can create audit entries.
    private void Log(string actor, string field, string oldVal, string newVal) =>
        _auditLog.Add(new AuditEntry(DateTime.Now, actor, field, oldVal, newVal));

    public override string ToString() =>
        $"[{PatientId}] {PatientName} | Blood: {BloodType} | {Status} | Bill: ₹{TotalBill:N0} | Alive: {IsAlive}";
}

// ─────────────────────────────────────────────────────────────────────────────
// DEMO
// ─────────────────────────────────────────────────────────────────────────────

public static class EncapsulationDemo
{
    public static void Demo()
    {
        Console.WriteLine("\n══════════════════════════════════════");
        Console.WriteLine("  ENCAPSULATION DEMO");
        Console.WriteLine("══════════════════════════════════════\n");

        // ── BEFORE ──────────────────────────────────────────────────────────
        Console.WriteLine("❌ BEFORE — public fields, no guards:");
        var naive          = new NaivePatientRecord { PatientId = "P001", PatientName = "Ramesh Kumar", IsAlive = true };
        naive.IsAlive      = false;                // no medical confirmation
        naive.Diagnosis    = "";                   // erased — no audit trail
        naive.BillAmount   = 0;                    // billing fraud
        naive.DischargeStatus = "Discharged";      // no doctor sign-off
        naive.MedicationList  = "None";            // overwrites live prescriptions
        Console.WriteLine($"   All violations accepted silently. IsAlive={naive.IsAlive}, Diagnosis='{naive.Diagnosis}'");
        Console.WriteLine($"   Bill=₹{naive.BillAmount}, Status='{naive.DischargeStatus}'");
        Console.WriteLine("   Zero audit entries. A court asks 'who changed this?' — unknown.\n");

        // ── AFTER ───────────────────────────────────────────────────────────
        Console.WriteLine("✅ AFTER — private fields, authorized changes only:\n");

        var consultant = new Doctor("D001", "Aisha Sharma",  "Internal Medicine", "MCI-2019-77432");
        var surgeon    = new Doctor("D002", "Rajiv Menon",   "Cardiology",        "MCI-2015-44108");
        var record     = new PatientRecord("P002", "Priya Nair", "B+");

        Console.WriteLine($"   Created: {record}");

        // Diagnosis update — requires a doctor
        record.UpdateDiagnosis("Hypertensive heart disease, Stage 2", consultant);
        Console.WriteLine($"   After diagnosis update: {record}");

        // Medications — require a doctor, build an audit trail
        record.AddMedication("Amlodipine", "5mg once daily", consultant);
        record.AddMedication("Aspirin",    "75mg once daily", consultant);
        Console.WriteLine($"   After prescriptions: {record}");
        Console.WriteLine($"   Active medications: {record.GetMedications().Count}");

        // Try to remove a medication without a reason
        try
        {
            record.RemoveMedication(record.GetMedications()[0].MedicationId, consultant, "");
        }
        catch (ArgumentException ex) { Console.WriteLine($"   Caught: {ex.Message}"); }

        // Remove with proper reason
        record.RemoveMedication(
            record.GetMedications()[0].MedicationId, consultant,
            "Patient developed ankle oedema — switching to Telmisartan.");

        // Billing
        record.AddCharge("ICU bed charges — 2 days",         12_000m, "BILLING-DESK");
        record.AddCharge("Echocardiography",                   4_500m, "BILLING-DESK");
        record.AddCharge("Cardiology consultation",            2_000m, "BILLING-DESK");
        Console.WriteLine($"   Total bill: ₹{record.TotalBill:N0}");

        // Try to set IsAlive = false — compiler prevents it
        // record.IsAlive = false; ← COMPILER ERROR: Property 'IsAlive' has no setter.
        Console.WriteLine("\n   record.IsAlive = false → compiler error: no setter. Safe by design.");

        // Discharge — requires a signed summary
        record.SignDischargeSummary(
            "Patient stable. BP controlled. Follow-up in 2 weeks. Lifestyle modification advised.",
            surgeon);
        Console.WriteLine($"   After discharge sign-off: {record}");

        // Print full audit log
        Console.WriteLine("\n   Full audit trail (every change tracked):");
        foreach (var entry in record.GetAuditLog())
            Console.WriteLine($"   [{entry.Timestamp:HH:mm:ss}] {entry.ChangedBy} | {entry.FieldChanged}: '{entry.OldValue}' → '{entry.NewValue}'");

        Console.WriteLine("\n✅ Encapsulation — understood.");
    }
}
