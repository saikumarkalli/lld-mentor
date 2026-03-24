/*
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  🧠 CONCEPT: Polymorphism                                                    ║
 * ╠══════════════════════════════════════════════════════════════════════════════╣
 * ║                                                                              ║
 * ║  WHAT IS IT?                                                                 ║
 * ║  Polymorphism means "many forms". One variable (or method call) behaves      ║
 * ║  differently depending on the actual type at runtime. You call the same      ║
 * ║  method name — each type decides how to respond. Also covers method          ║
 * ║  overloading: same name, different parameters, at compile time.              ║
 * ║                                                                              ║
 * ║  MENTAL MODEL (the analogy to remember forever):                             ║
 * ║  A "Make Sound" command sent to animals. You call MakeSound() on a Dog, Cat, ║
 * ║  and Duck. Dog barks, Cat meows, Duck quacks. You wrote MakeSound() once.    ║
 * ║  Each animal decided what that means for it. You didn't write                ║
 * ║  if (animal is Dog) Bark() else if (animal is Cat) Meow() ...               ║
 * ║                                                                              ║
 * ║  THE PRODUCTION HORROR STORY (why this matters):                             ║
 * ║  A payment processor had a 200-line switch statement routing payment types.  ║
 * ║  Every time a new payment method launched (5 in 2 years), a developer had   ║
 * ║  to open this file, add a case, and re-deploy the entire service. Once, two ║
 * ║  developers opened it simultaneously. Their changes conflicted. The merge    ║
 * ║  silently dropped UPI processing for 4 hours on a sale day.                 ║
 * ║                                                                              ║
 * ║  QUICK RECALL (read this in 2 years and instantly remember):                 ║
 * ║  Replace if/else type-checking with polymorphic dispatch — each class knows  ║
 * ║  how to handle itself. Adding a new type = add one class, touch zero others. ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 */

namespace LLD.OOPs.Concepts.PaymentModule;

// ─────────────────────────────────────────────────────────────────────────────
// ❌ BEFORE — How a beginner naturally writes this
// One big switch/if-else that routes to the right logic per payment type.
// It works perfectly — until you need to add a 4th payment method.
// Then this file gets touched. And a 5th. And a 6th. Forever.
// ─────────────────────────────────────────────────────────────────────────────

public class BadPaymentProcessor
{
    // ❌ This method must be edited every time a new payment type is added
    public void Process(string type, decimal amount, string token)
    {
        if (type == "creditcard")
        {
            // ❌ CreditCard-specific logic inline here
            Console.WriteLine($"   Validating card expiry for token {token}...");
            Console.WriteLine($"   Charging card: ₹{amount}");
        }
        else if (type == "upi")
        {
            // ❌ UPI-specific logic inline here
            Console.WriteLine($"   Verifying VPA {token}...");
            Console.WriteLine($"   Sending UPI push: ₹{amount}");
        }
        else if (type == "wallet")
        {
            // ❌ Wallet-specific logic inline here
            Console.WriteLine($"   Checking wallet balance...");
            Console.WriteLine($"   Debiting wallet: ₹{amount}");
        }
        // ❌ Adding CryptoPayment = open this file, add another else-if, re-test everything
        // ❌ Two devs adding types simultaneously = merge conflict in this one method
    }
}

// 💥 WHAT GOES WRONG:
// 5 payment types → 5 cases in this method (and growing).
// Bug in the routing logic = affects ALL payment types.
// You can never add a new payment type WITHOUT touching PaymentProcessor.
// This violates the Open/Closed Principle: the class is never "closed for modification".


// ─────────────────────────────────────────────────────────────────────────────
// ✅ AFTER — The OOP-correct way
// ProcessAll() has zero if/else. It calls payment.Validate() and payment.Execute()
// on whatever Payment object it receives — each object handles itself.
// Adding CryptoPayment = write one new class, zero changes to ProcessAll().
// ─────────────────────────────────────────────────────────────────────────────

// ✅ These classes extend Payment from 03_Inheritance.cs.
//    Each one IS-A Payment, so ProcessAll() can hold any of them.
//    (Redefined here lightly for standalone readability.)

public abstract class PaymentBase
{
    public decimal Amount    { get; protected set; }
    public string  PaymentId { get; } = Guid.NewGuid().ToString("N")[..6].ToUpper();

    protected PaymentBase(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive.");
        Amount = amount;
    }

    // ✅ Every subclass promises it can do these two things.
    //    The processor calls them without knowing which subclass it holds.
    public abstract bool Validate();
    public abstract void Execute();
}

public class CreditCardPayment2 : PaymentBase
{
    private readonly DateTime _expiry;
    public CreditCardPayment2(decimal amount, DateTime expiry) : base(amount) { _expiry = expiry; }

    // ✅ CreditCard decides what Validate means for itself
    public override bool Validate()
    {
        if (_expiry < DateTime.UtcNow)
        {
            Console.WriteLine($"   [{PaymentId}] Card expired — skipping.");
            return false;
        }
        return true;
    }

    public override void Execute() =>
        Console.WriteLine($"   [{PaymentId}] CreditCard charged: ₹{Amount}");
}

public class UpiPayment2 : PaymentBase
{
    private readonly string _vpa;
    public UpiPayment2(decimal amount, string vpa) : base(amount) { _vpa = vpa; }

    // ✅ UPI decides what Validate means for itself
    public override bool Validate()
    {
        if (!_vpa.Contains('@'))
        {
            Console.WriteLine($"   [{PaymentId}] Invalid VPA '{_vpa}' — skipping.");
            return false;
        }
        return true;
    }

    public override void Execute() =>
        Console.WriteLine($"   [{PaymentId}] UPI push to {_vpa}: ₹{Amount}");
}

public class WalletPayment2 : PaymentBase
{
    private readonly decimal _balance;
    public WalletPayment2(decimal amount, decimal balance) : base(amount) { _balance = balance; }

    public override bool Validate()
    {
        if (_balance < Amount)
        {
            Console.WriteLine($"   [{PaymentId}] Insufficient wallet balance (₹{_balance}) for ₹{Amount} — skipping.");
            return false;
        }
        return true;
    }

    public override void Execute() =>
        Console.WriteLine($"   [{PaymentId}] Wallet debited: ₹{Amount}");
}

// ✅ Adding CryptoPayment tomorrow: write this class, touch nothing else.
public class CryptoPayment : PaymentBase
{
    private readonly string _walletAddress;
    public CryptoPayment(decimal amount, string walletAddress) : base(amount) { _walletAddress = walletAddress; }

    public override bool Validate() => _walletAddress.StartsWith("0x") && _walletAddress.Length == 42;

    public override void Execute() =>
        Console.WriteLine($"   [{PaymentId}] Crypto transfer to {_walletAddress[..6]}...: ₹{Amount} equivalent");
}

// ✅ The processor: not a single if/else or switch/case
public class PaymentProcessor
{
    // ✅ Accepts ANY collection of Payment objects — doesn't care which concrete type
    public void ProcessAll(IEnumerable<PaymentBase> payments)
    {
        foreach (var payment in payments)
        {
            // ✅ .Validate() and .Execute() dispatch to the correct class at runtime.
            //    This is runtime polymorphism (also called dynamic dispatch).
            if (payment.Validate())
                payment.Execute();
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// BONUS: Method Overloading (compile-time polymorphism)
// Same method name, different parameter signatures — compiler picks the right one.
// ─────────────────────────────────────────────────────────────────────────────

public class ReceiptGenerator
{
    // ✅ Overload 1 — print to console only
    public void GenerateReceipt(PaymentBase payment)
    {
        Console.WriteLine($"\n   --- RECEIPT ---");
        Console.WriteLine($"   Payment ID : {payment.PaymentId}");
        Console.WriteLine($"   Amount     : ₹{payment.Amount}");
        Console.WriteLine($"   Type       : {payment.GetType().Name}");
        Console.WriteLine($"   ---------------");
    }

    // ✅ Overload 2 — same name, additional email parameter
    //    Compiler picks this one when you pass a string as the second argument.
    public void GenerateReceipt(PaymentBase payment, string email)
    {
        GenerateReceipt(payment);  // Reuse overload 1 for the console print
        Console.WriteLine($"   Receipt emailed to: {email}");
    }
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

        Console.WriteLine("❌ BEFORE — if/else routing. Adding CryptoPayment means editing the processor.\n");

        Console.WriteLine("✅ AFTER — Runtime Polymorphism (ProcessAll):\n");

        var payments = new List<PaymentBase>
        {
            new CreditCardPayment2(1500m, DateTime.UtcNow.AddYears(2)),    // valid
            new UpiPayment2(500m, "sai@okicici"),                           // valid
            new WalletPayment2(200m, balance: 100m),                        // will fail — low balance
            new CryptoPayment(999m, "0x742d35Cc6634C0532925a3b8D4C9E7e1234567890"), // valid
        };

        var processor = new PaymentProcessor();
        processor.ProcessAll(payments);  // ✅ No if/else — polymorphism handles dispatch

        Console.WriteLine("\n✅ AFTER — Method Overloading (compile-time polymorphism):\n");

        var generator = new ReceiptGenerator();
        var validPayment = new UpiPayment2(750m, "demo@okaxis");
        validPayment.Validate();
        validPayment.Execute();

        generator.GenerateReceipt(validPayment);                         // Overload 1
        generator.GenerateReceipt(validPayment, "sai@example.com");      // Overload 2

        Console.WriteLine("\n✅ Polymorphism — understood.");
    }
}
