// ═══════════════════════════════════════════════════════
// THE 5 SOLID PRINCIPLES — ONE IDEA, FIVE ANGLES
// ═══════════════════════════════════════════════════════
//
// Most tutorials teach SOLID as 5 separate rules.
// That's why they feel disconnected.
// They're NOT separate. They're one idea:
//
//   "YOUR CODE SHOULD BE EASY TO CHANGE WITHOUT BREAKING THINGS"
//
// Each principle attacks a different reason code becomes hard to change:
//
// S — Single Responsibility  → Hard to change because ONE class does TOO MUCH
//                               "If you need to change 3 things to fix 1 bug, SRP is violated"
//
// O — Open/Closed            → Hard to change because adding features BREAKS existing code
//                               "If adding PayPal means editing PaymentProcessor, OCP is violated"
//
// L — Liskov Substitution    → Hard to change because SWAPPING implementations breaks callers
//                               "If replacing StripeGateway with PayPalGateway crashes, LSP is violated"
//
// I — Interface Segregation  → Hard to change because interfaces are TOO FAT
//                               "If implementing an interface forces NotImplementedException, ISP is violated"
//
// D — Dependency Inversion   → Hard to change because classes are HARDWIRED to concretions
//                               "If testing requires a real Stripe API key, DIP is violated"
//
// ───────────────────────────────────────────────────────
// THE PAYMENT MODULE CONNECTION:
// ───────────────────────────────────────────────────────
// In your OOP PaymentModule you wrote:
//   - PaymentTransaction (Encapsulation)
//   - IPaymentGateway (Abstraction)
//   - CreditCardPayment, UpiPayment (Inheritance)
//   - PaymentProcessor.ProcessAll() (Polymorphism)
//   - GoodPaymentService_WithDIP with injected dependencies (Composition + DIP preview)
//
// SOLID doesn't replace any of that.
// SOLID gives NAMES and RULES to the instincts you developed.
// Every OOP refactor you did was SOLID in disguise.
//
// File 01: Your PaymentService from OOP had SRP violations — here's why
// File 02: Your PaymentProcessor had an OCP violation — here's the fix
// File 03: Your IPaymentGateway had an LSP risk — here's how to prevent it
// File 04: Your IPaymentService was fat — ISP says split it (you previewed this)
// File 05: Your DI setup was almost right — DIP completes it
// ───────────────────────────────────────────────────────
//
// HOW TO READ SOLID VIOLATIONS IN A PR:
// When reviewing code, ask these 5 questions:
// S: "How many reasons does this class have to change?"  (> 1 = violation)
// O: "Does adding a new payment type require editing existing classes?" (yes = violation)
// L: "Can I swap any implementation without the caller knowing?" (no = violation)
// I: "Does any implementor throw NotImplementedException?" (yes = violation)
// D: "Does any class use the `new` keyword on a dependency?" (yes = violation)

namespace LLDMaster.SOLID.PaymentModule;

public static class SolidOverviewDemo
{
    public static void Demo()
    {
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║  SOLID Principles — Payment Module        ║");
        Console.WriteLine("║  One idea: Easy to change, hard to break  ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("S — Single Responsibility : One class, one reason to change");
        Console.WriteLine("O — Open/Closed           : Add features without editing existing code");
        Console.WriteLine("L — Liskov Substitution   : Swap implementations safely");
        Console.WriteLine("I — Interface Segregation : No forced NotImplementedException");
        Console.WriteLine("D — Dependency Inversion  : Depend on abstractions, never concretions");
        Console.WriteLine();
        Console.WriteLine("Each file below is your OOP code, evolved.");
    }
}
