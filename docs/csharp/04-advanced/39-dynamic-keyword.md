# Chapter 39 — The `dynamic` Keyword

> **⚡ Core Idea (30 seconds):** `dynamic` bypasses compile-time type checking. The type of a `dynamic` variable is resolved at **runtime** using the Dynamic Language Runtime (DLR). This gives flexibility (like Python/JavaScript) but sacrifices IntelliSense, compile-time safety, and performance.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#dynamic` `#dlr` `#runtime` `#interop`

---

## 1. Core Idea

Think of `dynamic` like writing a **blank cheque**. You hand it over with no amount written. If the recipient (runtime) can cash it (find the method), it works. If not, it bounces (throws `RuntimeBinderException`). You get maximum flexibility but zero protection.

---

## 2. Deep Explanation

### How dynamic Works

```csharp
dynamic obj = GetSomething();
obj.Fly();  // No compile-time check. Resolved at RUNTIME.
```

The compiler does NOT verify that `.Fly()` exists. Instead, it emits code that asks the DLR: "Does this object have a method called `Fly`?" If yes → call it. If no → `RuntimeBinderException`.

### dynamic vs object vs var

| Keyword | Type Check | IntelliSense | Type Resolved |
|---------|:----------:|:------------:|:-------------:|
| `var` | ✅ Compile time | ✅ Full | Compile time (inferred) |
| `object` | ✅ Compile time | ❌ Must cast | Compile time (`System.Object`) |
| `dynamic` | ❌ Runtime only | ❌ None | Runtime (DLR) |

### When dynamic Is Justified

1. **COM Interop** (Office automation):
   ```csharp
   dynamic excel = Activator.CreateInstance(Type.GetTypeFromProgID("Excel.Application"));
   excel.Visible = true; // No cast needed, no interop assembly required
   ```

2. **JSON deserialization** (quick prototyping):
   ```csharp
   dynamic json = JsonSerializer.Deserialize<dynamic>(payload);
   string name = json.user.name; // No DTO class needed
   ```

3. **Calling methods on unknown types** (plugin systems):
   ```csharp
   dynamic plugin = LoadPlugin("calculator.dll");
   int result = plugin.Calculate(5, 3);
   ```

### ExpandoObject

```csharp
dynamic person = new ExpandoObject();
person.Name = "Sai";        // Property created at runtime
person.Age = 30;             // Another runtime property
person.Greet = (Action)(() => Console.WriteLine($"Hi, I'm {person.Name}"));
person.Greet();              // "Hi, I'm Sai"
```

---

## 3. Code Examples

### Example 1 — dynamic vs Proper Typing
```csharp
// ❌ Bad: Using dynamic for laziness
dynamic user = GetUser();
Console.WriteLine(user.Name); // No IntelliSense, crashes at runtime if Name doesn't exist

// ✅ Good: Proper typing
User user = GetUser();
Console.WriteLine(user.Name); // IntelliSense, compile-time check, refactoring-safe
```

### Example 2 — Legitimate COM Interop Use
```csharp
// Without dynamic: requires Microsoft.Office.Interop.Excel NuGet + casting everywhere
var excelApp = (Excel.Application)Activator.CreateInstance(
    Type.GetTypeFromProgID("Excel.Application"));
((Excel.Workbook)excelApp.Workbooks.Open("report.xlsx")).Close();

// With dynamic: clean, no interop assembly required
dynamic excelApp = Activator.CreateInstance(
    Type.GetTypeFromProgID("Excel.Application"));
excelApp.Workbooks.Open("report.xlsx").Close(); // ✅ Clean
```

---

## 4. Interview Questions

1. **What is the `dynamic` keyword in C# and how does it differ from `var`?**
   *`var` is compile-time type inference — the compiler determines the exact type and enforces it. `dynamic` defers all type resolution to runtime via the Dynamic Language Runtime (DLR). `var` gives you full IntelliSense and compile-time safety; `dynamic` gives you neither.*

2. **When is `dynamic` justified in production code?**
   *Three legitimate cases: (1) COM interop where strong typing requires heavy interop assemblies, (2) interacting with dynamic languages via the DLR (IronPython), (3) short-lived prototyping with JSON when DTO classes aren't available yet. In all other cases, strong typing is preferred.*

3. **What exception does `dynamic` throw when a member doesn't exist?**
   *`Microsoft.CSharp.RuntimeBinder.RuntimeBinderException`. This is the dynamic equivalent of a compile error — but it happens at runtime, which means it can reach production if not covered by tests.*

4. **What is ExpandoObject?**
   *`ExpandoObject` is a class that lets you add properties and methods at runtime. It implements `IDictionary<string, object>`, so each "property" is actually a key-value pair in a dictionary. It's useful for building dynamic DTOs or configuration objects.*

5. **What is the performance cost of `dynamic`?**
   *The first call to a dynamic member is significantly slower — the DLR must resolve the method, build a call site, and cache it. Subsequent calls to the same member with the same types are cached and faster, but still 10x–100x slower than direct calls due to call site overhead.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using dynamic to avoid learning the type system
dynamic x = 5;
dynamic y = "hello";
dynamic z = x + y; // ❌ RuntimeBinderException! Can't add int + string

// MISTAKE 2: dynamic in public APIs
public dynamic GetUser(int id) { ... } // ❌ Callers have no idea what they get
public User GetUser(int id) { ... }     // ✅ Clear contract

// MISTAKE 3: dynamic defeats refactoring
dynamic user = GetUser();
user.Nmae = "Sai"; // Typo! Compiles fine, crashes at runtime
// Strong typing would catch this at compile time

// GOTCHA: Extension methods don't work on dynamic
dynamic list = new List<int> { 1, 2, 3 };
// list.First(); // ❌ RuntimeBinderException! LINQ extensions can't be resolved dynamically
((IEnumerable<int>)list).First(); // ✅ Cast to concrete type first
```

---

## Connected Topics

- [Reflection](./37-reflection.md) — Alternative to dynamic for runtime type discovery
- [Pattern Matching](./35-pattern-matching.md) — Type patterns (`is string s`) are the safe alternative
- [Generics](../03-intermediate/18-generics.md) — Generics provide compile-time flexibility without runtime cost

---

*Created: May 2026 · Level: Advanced*
