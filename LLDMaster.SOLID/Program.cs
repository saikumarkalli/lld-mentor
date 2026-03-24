using LLDMaster.SOLID.PaymentModule;
using LLDMaster.SOLID.PaymentModule.SRP;
using LLDMaster.SOLID.PaymentModule.OCP;
using LLDMaster.SOLID.PaymentModule.LSP;
using LLDMaster.SOLID.PaymentModule.ISP;
using LLDMaster.SOLID.PaymentModule.DIP;

// ═══════════════════════════════════════════════════════════════
// SOLID Principles — Payment Module
// Run each demo in sequence: each one shows the violation,
// then the fix, and ends with a pointer to the next principle.
// ═══════════════════════════════════════════════════════════════

SolidOverviewDemo.Demo();

Console.WriteLine("\n" + new string('═', 52));

SingleResponsibilityDemo.Demo();

Console.WriteLine("\n" + new string('═', 52));

OpenClosedDemo.Demo();

Console.WriteLine("\n" + new string('═', 52));

LiskovSubstitutionDemo.Demo();

Console.WriteLine("\n" + new string('═', 52));

InterfaceSegregationDemo.Demo();

Console.WriteLine("\n" + new string('═', 52));

DependencyInversionDemo.Demo();

Console.WriteLine("\n" + new string('═', 52));
Console.WriteLine("\n✅ All 5 SOLID principles — understood.");
Console.WriteLine("→ S+O+L+I+D = One idea: Easy to change, hard to break.\n");
