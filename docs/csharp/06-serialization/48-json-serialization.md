# Chapter 48 — JSON Serialization

> **⚡ Core Idea (30 seconds):** Serialization converts C# objects into JSON text; deserialization converts JSON text back into C# objects. In modern .NET, use the built-in `System.Text.Json` instead of the legacy `Newtonsoft.Json`. It is much faster, uses fewer allocations via `Span<T>`, and supports Source Generators for Ahead-Of-Time (AOT) compilation.

**Domain:** `C#` **Level:** `Expert` **Tags:** `#json` `#serialization` `#system-text-json` `#performance`

---

## 1. Core Idea

Think of serialization like **packing a house for moving**. You can't fit a whole house into a truck. You have to break the furniture down (serialize) into flat boxes (JSON strings), put them in the truck (network/disk), and reassemble them (deserialize) at the destination.

---

## 2. Deep Explanation

### System.Text.Json (STJ) vs Newtonsoft.Json

`Newtonsoft.Json` was the king of .NET JSON for a decade. It is highly flexible but allocates a lot of memory.
`System.Text.Json` (introduced in .NET Core 3.0) was built from the ground up for performance. It uses `Span<T>` and `Utf8JsonReader/Writer` to parse JSON directly from byte streams, heavily reducing Garbage Collection pressure.

**When to still use Newtonsoft:**
- You have deeply complex, dynamic JSON.
- You rely on missing features (though STJ has mostly caught up by .NET 8).
- Legacy codebases.

### The Source Generator (C# 9+)

Normally, JSON serializers use Reflection at runtime to inspect your properties. Reflection is slow and defeats AOT compilation (because AOT removes unused code, and reflection hides what is used).

STJ Source Generators generate the serialization code at **compile time**.

```csharp
[JsonSerializable(typeof(User))]
internal partial class UserContext : JsonSerializerContext { }

// Usage: Zero reflection, AOT compatible!
string json = JsonSerializer.Serialize(user, UserContext.Default.User);
```

### Important Attributes

| Attribute | Purpose |
|-----------|---------|
| `[JsonPropertyName("id")]` | Changes the JSON key name. |
| `[JsonIgnore]` | Excludes a property from serialization. |
| `[JsonInclude]` | Includes non-public properties. |
| `[JsonConstructor]` | Tells the deserializer which constructor to use for immutable types. |
| `[JsonDerivedType]` | (.NET 7+) Used for polymorphic serialization (base classes/interfaces). |

---

## 3. Code Examples

### Example 1 — Basic Serialization & Options
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    [JsonIgnore] public string PasswordHash { get; set; } // Never serialize secrets!
}

// ✅ Good: Reusing options
var options = new JsonSerializerOptions 
{ 
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // "Id" -> "id"
    WriteIndented = true,                              // Pretty print
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Omit nulls
};

var user = new User { Id = 1, Name = "Sai" };

// Serialize
string json = JsonSerializer.Serialize(user, options);

// Deserialize
User parsed = JsonSerializer.Deserialize<User>(json, options);
```

### Example 2 — Immutable Types / Records
```csharp
// System.Text.Json handles C# 9 records naturally
public record Product(int Id, string Name);

// For classes without a parameterless constructor, use [JsonConstructor]
public class Order
{
    public int Id { get; }
    
    [JsonConstructor]
    public Order(int id) => Id = id; 
}
```

### Example 3 — Polymorphic Serialization (.NET 7+)
```csharp
// Problem: If you serialize a List<Animal>, how do you know if it's a Dog or Cat when deserializing?
[JsonDerivedType(typeof(Dog), typeDiscriminator: "dog")]
[JsonDerivedType(typeof(Cat), typeDiscriminator: "cat")]
public abstract class Animal { public string Name { get; set; } }

public class Dog : Animal { public bool Barks { get; set; } }
public class Cat : Animal { public bool Purrs { get; set; } }

// JSON output will include: "$type": "dog"
```

---

## 4. Interview Questions

1. **Why did Microsoft introduce `System.Text.Json` when `Newtonsoft.Json` already existed?**
   *Performance and memory efficiency. `Newtonsoft` uses `string` and `char[]` heavily, causing massive GC pressure in high-throughput APIs. `System.Text.Json` was built using modern C# features like `Span<T>`, `Memory<T>`, and `ref struct`, allowing it to read and write JSON directly over UTF-8 byte streams with near-zero allocations.*

2. **How does the JSON Source Generator improve performance?**
   *Standard JSON serialization uses Reflection at runtime to discover properties, which is slow. The Source Generator analyzes your classes at compile-time and generates the exact C# code needed to serialize/deserialize them. This eliminates Reflection overhead, improves startup time, and makes the code compatible with Native AOT compilation.*

3. **How do you handle polymorphic deserialization in `System.Text.Json`?**
   *Before .NET 7, you had to write a custom `JsonConverter`. In .NET 7+, you use the `[JsonDerivedType]` attribute on the base class/interface. You provide the derived type and a `typeDiscriminator` string. The serializer will add a `$type` field to the JSON so it knows which concrete class to instantiate when deserializing.*

4. **Is it better to serialize to a `string` or directly to a `Stream`?**
   *For small objects, `string` is fine. For large objects or web APIs, always serialize directly to a `Stream` (using `JsonSerializer.SerializeAsync`). Serializing to a string forces the entire JSON payload to be allocated in RAM as a giant string object. Streaming writes chunks to the network/disk progressively, keeping memory flat.*

5. **How do you change property casing (e.g., PascalCase to camelCase)?**
   *Pass a `JsonSerializerOptions` object to the Serialize method, setting `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`. By default, STJ uses exact casing (PascalCase in C# remains PascalCase in JSON), unlike Newtonsoft which defaulted to camelCase.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Recreating JsonSerializerOptions on every request
public string ToJson(User u)
{
    // ❌ BAD: Creates a new options instance and wipes the internal reflection cache!
    // This will cause a massive memory leak and CPU spike under load.
    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    return JsonSerializer.Serialize(u, options);
}

// ✅ FIX: Make it static
private static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

// MISTAKE 2: Trying to serialize cyclic references (e.g. Entity Framework navigation properties)
public class Parent { public List<Child> Children { get; set; } }
public class Child { public Parent Parent { get; set; } }
// ❌ JsonException: A possible object cycle was detected.
// ✅ FIX: Set ReferenceHandler = ReferenceHandler.Preserve in options, or use [JsonIgnore] on the back-reference.
```

---

## 🔗 Connected Topics

- [File I/O & Streams](./47-file-io.md) — Serializing directly to streams
- [Span\<T\>](../04-advanced/34-span-memory.md) — The technology powering STJ's performance
- [Reflection](../04-advanced/37-reflection.md) — What the Source Generator replaces

---

*Created: May 2026 · Level: Expert*
