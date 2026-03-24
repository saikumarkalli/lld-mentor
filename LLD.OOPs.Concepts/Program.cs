using PM = LLD.OOPs.Concepts.PaymentModule;
using BM = LLD.OOPs.Concepts.BankModule;
using HM = LLD.OOPs.Concepts.HospitalModule;

// ══════════════════════════════════════════════════════════════════════════════
// PAYMENT MODULE — 7 concepts, payment domain
// ══════════════════════════════════════════════════════════════════════════════

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║         LLD.OOPs — Payment Module (7 concepts)          ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("  Each demo: WRONG way first, then RIGHT way.");
Console.WriteLine();

PrintSectionHeader(1, 7, "Encapsulation",          "Guard your data — methods are the only doors.");
PM.EncapsulationDemo.Demo();

PrintSectionHeader(2, 7, "Abstraction",             "Hide the HOW. Expose only the WHAT.");
try   { await PM.AbstractionDemo.Demo(); }
catch (Exception ex)
{
    // HTTP call to Stripe/PayPal fails without real API keys — expected in dev.
    Console.WriteLine($"\n  ⚠️  HTTP call failed (expected without API keys): {ex.Message}");
    Console.WriteLine("  The lesson stands: PaymentService never knew it was talking to Stripe or PayPal.");
    Console.WriteLine("✅ Abstraction — understood.");
}

PrintSectionHeader(3, 7, "Inheritance",             "Share common code. Override only what's unique.");
PM.InheritanceDemo.Demo();

PrintSectionHeader(4, 7, "Polymorphism",            "Same call. Different behaviour. Zero if/else.");
PM.PolymorphismDemo.Demo();

PrintSectionHeader(5, 7, "Composition vs Inheritance", "Prefer HAS-A over IS-A when in doubt.");
PM.CompositionDemo.Demo();

PrintSectionHeader(6, 7, "Interface Segregation",   "Don't force classes to implement what they don't need.");
PM.InterfaceSegregationDemo.Demo();

PrintSectionHeader(7, 7, "Dependency Inversion",    "Depend on abstractions, not concrete classes.");
PM.DependencyInversionDemo.Demo();

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║         Payment Module complete. On to Banking.         ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

// ══════════════════════════════════════════════════════════════════════════════
// BANK MODULE — 8 concepts, bank domain, concepts build on each other
// ══════════════════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║         LLD.OOPs — Bank Module (8 concepts)             ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("  Same 8 concepts. One evolving bank codebase.");
Console.WriteLine("  Each file references what the previous one built.");
Console.WriteLine();

PrintSectionHeader(1, 8, "Encapsulation",              "The class is the gatekeeper. No bypassing.");
BM.EncapsulationDemo.Demo();

PrintSectionHeader(2, 8, "Abstraction",                "ATM button hides 60 lines. Show callers nothing extra.");
BM.AbstractionDemo.Demo();

PrintSectionHeader(3, 8, "Inheritance",                "Fix the base once. Every subclass gets it free.");
BM.InheritanceDemo.Demo();

PrintSectionHeader(4, 8, "Polymorphism",               "account.CalculateInterest() — no if/else, ever.");
BM.PolymorphismDemo.Demo();

PrintSectionHeader(5, 8, "Abstract Class vs Interface","Shared CODE → abstract. Shared CONTRACT → interface.");
BM.AbstractVsInterfaceDemo.Demo();

PrintSectionHeader(6, 8, "Composition vs Inheritance", "HAS-A logger. IS-NOT-A logger.");
BM.CompositionDemo.Demo();

PrintSectionHeader(7, 8, "Interface Segregation",      "Fat interface = NotImplementedException waiting to blow.");
BM.InterfaceSegregationDemo.Demo();

PrintSectionHeader(8, 8, "Dependency Inversion",       "Inject abstractions. Test without prod infrastructure.");
BM.DependencyInversionDemo.Demo();

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║       Bank Module complete. On to Hospital.             ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

// ══════════════════════════════════════════════════════════════════════════════
// HOSPITAL MODULE — 8 concepts, hospital & patient management domain
// ══════════════════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║      LLD.OOPs — Hospital Module (8 concepts)            ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("  Patient safety, legal compliance, and NABH standards.");
Console.WriteLine("  Every concept has a disaster story from real hospitals.");
Console.WriteLine();

PrintSectionHeader(1, 8, "Encapsulation",              "Patient chart in a locked cabinet. Signed entries only.");
HM.EncapsulationDemo.Demo();

PrintSectionHeader(2, 8, "Abstraction",                "AdmitPatient() — 80-line workflow behind one clean call.");
HM.AbstractionDemo.Demo();

PrintSectionHeader(3, 8, "Inheritance",                "GPS attendance: fix HospitalStaff.CheckIn() once for all.");
HM.InheritanceDemo.Demo();

PrintSectionHeader(4, 8, "Polymorphism",               "GetShiftSummary(): one call, 4 roles, zero if/else.");
HM.PolymorphismDemo.Demo();

PrintSectionHeader(5, 8, "Abstract Class vs Interface","MRI machine IS equipment. It IS-NOT a staff member.");
HM.AbstractClassVsInterfaceDemo.Demo();

PrintSectionHeader(6, 8, "Composition vs Inheritance", "HIPAA logger swap: 1 constructor arg vs 14-file refactor.");
HM.CompositionVsInheritanceDemo.Demo();

PrintSectionHeader(7, 8, "Interface Segregation",      "Check-in kiosk cannot dispatch ambulances. By design.");
HM.InterfaceSegregationDemo.Demo();

PrintSectionHeader(8, 8, "Dependency Inversion",       "HMS has zero `new` on dependencies. Capstone.");
HM.DependencyInversionDemo.Demo();

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   All 23 OOP demos complete. Now go build something.    ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

// ─────────────────────────────────────────────────────────────────────────────
static void PrintSectionHeader(int number, int total, string concept, string tagline)
{
    Console.WriteLine();
    Console.WriteLine("┌─────────────────────────────────────────────────────┐");
    Console.WriteLine($"│  [{number}/{total}]  {concept,-43}│");
    Console.WriteLine($"│        {tagline,-45}│");
    Console.WriteLine("└─────────────────────────────────────────────────────┘");
}
