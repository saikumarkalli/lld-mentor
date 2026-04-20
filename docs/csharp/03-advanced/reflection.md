# Reflection & Attributes

> **⚡ Core Idea (30 seconds):** Reflection lets you **inspect and invoke types, methods, and properties at runtime** — code that reads and manipulates other code. Attributes are metadata annotations you attach to types/members that reflection (and frameworks) can read. Together, they power serializers, ORMs, DI containers, and test frameworks.

**Domain:** `C#` **Level:** `Advanced` **Tags:** `#reflection` `#attributes` `#metadata` `#runtime` `#performance`

---

## 1. Core Idea

Every compiled .NET type carries metadata about itself (its methods, properties, constructors). Reflection is the API to read that metadata and even invoke things dynamically. Attributes are how you embed custom metadata into the type system.

Analogy: Reflection is like looking up an employee in the company directory and calling their desk. It works, but it's slower than calling someone you already know directly.

---

## 2. Deep Explanation

### The Type Object

Every .NET type has a `Type` object (its metadata descriptor):

```csharp
Type type = typeof(Order);             // Compile-time — preferred
Type type2 = order.GetType();          // Runtime — actual type of instance
Type type3 = Type.GetType("MyApp.Order"); // From string (slow, fragile)

// Inspect:
type.Name         // "Order"
type.FullName     // "MyApp.Domain.Order"
type.IsClass      // true
type.GetProperties()  // PropertyInfo[]
type.GetMethods()     // MethodInfo[]
```

### Invoking Members Dynamically

```csharp
var obj = Activator.CreateInstance(type);            // Create instance
var prop = type.GetProperty("Name");
prop.SetValue(obj, "Alice");                         // Set property value
prop.GetValue(obj);                                  // Get property value

var method = type.GetMethod("Process");
method.Invoke(obj, new object[] { arg1, arg2 });     // Invoke method
```

### Attributes — Custom Metadata

```csharp
// Define a custom attribute
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class MaxLengthAttribute : System.Attribute
{
    public int Length { get; }
    public MaxLengthAttribute(int length) => Length = length;
}

// Apply it
public class UserDto
{
    [MaxLength(50)]
    public string Name { get; set; } = "";

    [MaxLength(200)]
    public string Email { get; set; } = "";
}

// Read it at runtime via reflection
public static void Validate<T>(T obj)
{
    foreach (var prop in typeof(T).GetProperties())
    {
        var attr = prop.GetCustomAttribute<MaxLengthAttribute>();
        if (attr is null) continue;

        var value = prop.GetValue(obj)?.ToString() ?? "";
        if (value.Length > attr.Length)
            throw new ValidationException($"{prop.Name} exceeds max length {attr.Length}");
    }
}
```

### Performance Profile

Reflection is **10–100x slower** than direct calls because:
1. No JIT optimization (calls not inlined or devirtualized)
2. Type safety checks happen at runtime, not compile time
3. Boxing of value types for `object` arguments

For hot paths, use **compiled expressions** or **source generators**:

```csharp
// Slow — reflection per call
prop.GetValue(obj);

// Fast — compile a Func<T, object> once, reuse many times
var getter = (Func<Order, string>)Delegate.CreateDelegate(
    typeof(Func<Order, string>), prop.GetGetMethod()!);
var value = getter(order); // Near-native speed after first compilation
```

---

## 3. Code Examples

### Basic — Building a Simple Mapper
```csharp
public static TDest MapProperties<TSource, TDest>(TSource source)
    where TDest : new()
{
    var dest = new TDest();
    var sourceProps = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var destProps = typeof(TDest).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .ToDictionary(p => p.Name);

    foreach (var srcProp in sourceProps)
    {
        if (destProps.TryGetValue(srcProp.Name, out var destProp)
            && destProp.CanWrite
            && destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
        {
            destProp.SetValue(dest, srcProp.GetValue(source));
        }
    }
    return dest;
}

// Usage
var dto = MapProperties<Order, OrderDto>(order);
```

### Real-World — DI Container (simplified)
```csharp
public class SimpleContainer
{
    private readonly Dictionary<Type, Type> _registrations = new();

    public void Register<TInterface, TImplementation>()
        => _registrations[typeof(TInterface)] = typeof(TImplementation);

    public T Resolve<T>() => (T)Resolve(typeof(T));

    private object Resolve(Type type)
    {
        if (_registrations.TryGetValue(type, out var implType))
            type = implType;

        // Find the greediest constructor
        var ctor = type.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        // Recursively resolve each parameter
        var args = ctor.GetParameters()
            .Select(p => Resolve(p.ParameterType))
            .ToArray();

        return Activator.CreateInstance(type, args)!;
    }
}
```

---

## 4. Interview Questions

1. **What is reflection and when would you use it?**
2. **What is the performance cost of reflection? How can you mitigate it?**
3. **What is an attribute in C#? How do you create and read a custom attribute?**
4. **What is `typeof(T)` vs `obj.GetType()` — when does each matter?**
5. **What frameworks use reflection heavily?**

---

## 5. Follow-up Questions

- What is the difference between `GetProperty("Name")` and `GetProperty("Name", BindingFlags.NonPublic | BindingFlags.Instance)`?
  *(Default only returns public members. BindingFlags lets you access private/internal ones)*
- How can you cache reflection results to avoid performance penalties?
  *(Store `PropertyInfo[]`, `MethodInfo` in `static readonly` fields)*
- What are **source generators** (C# 9+) and how do they replace runtime reflection?
  *(Code generators that run at compile time — produce strongly-typed code, zero reflection overhead)*
- What is `Expression<Func<T, TResult>>` and how does it relate to reflection?
  *(Expression trees let you compile a reflective getter into a delegate — near-native speed)*

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Reflection in hot loops — devastating performance
for (int i = 0; i < 1_000_000; i++)
{
    var prop = typeof(Order).GetProperty("Total"); // ❌ GetProperty every iteration!
    prop.SetValue(order, i);
}
// FIX: Cache PropertyInfo
var prop = typeof(Order).GetProperty("Total"); // Once
for (int i = 0; i < 1_000_000; i++)
    prop!.SetValue(order, (decimal)i); // Reuse

// MISTAKE 2: Assuming private members can always be accessed
var field = type.GetField("_secret", BindingFlags.NonPublic | BindingFlags.Instance);
// Works in full-trust, but might be restricted in AOT/trimmed apps

// MISTAKE 3: Forgetting that reflection breaks with AOT/trimming
// .NET 7+ AOT and Linker trims unused code — reflectively-accessed members may be trimmed
// Fix: Use [DynamicallyAccessedMembers] attribute or source generators
```

---

## 7. Real-World Usage

| Scenario | Reflection / Attributes |
|----------|------------------------|
| `System.Text.Json` | Reads `[JsonPropertyName]`, `[JsonIgnore]` via attributes |
| Entity Framework | Maps `[Key]`, `[Column]`, `[Required]` to DB schema |
| FluentValidation | Inspects properties to build validators |
| ASP.NET Core | Routes, model binding, authorization via attributes |
| xUnit / NUnit | Finds `[Fact]` / `[Test]` methods via reflection |
| AutoMapper | Maps properties by name convention via reflection |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | typeof, GetType, GetProperties, GetMethods |
| **Level 2** | Custom attributes, Activator.CreateInstance, BindingFlags |
| **Level 3** | Performance mitigation, compiled expressions, caching PropertyInfo |
| **Level 4** | Source generators, DynamicallyAccessedMembers, AOT-safe patterns |

## 🔗 Connected Topics
- [Generics](../02-intermediate/generics.md) — Generic reflection: `MakeGenericType()` |
- [Attributes in ASP.NET](../../dotnet/02-intermediate/filters.md) — Built on reflection
- [Garbage Collection](../04-expert/garbage-collection.md) — Reflection generates IL that avoids GC; source generators do it at compile time

*Created: April 2026 · Level: Advanced*
