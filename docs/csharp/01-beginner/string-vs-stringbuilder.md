# String vs StringBuilder

> **⚡ Core Idea (30 seconds):** `string` in C# is **immutable** — every modification creates a new object on the heap. `StringBuilder` is a **mutable buffer** that builds strings without allocating intermediates. For concatenation inside loops, StringBuilder is the only correct choice.

**Domain:** `C#` **Level:** `Beginner` **Tags:** `#string` `#memory` `#performance` `#stringbuilder`

---

## 1. Core Idea

`string` is a reference type, but it acts like a value type because it's **immutable**. When you write `str += "world"`, you're not modifying the original string — you're creating a brand new string object and discarding the old one.

Imagine building a sentence by crumpling the entire paper and rewriting it every time you add a word. That's `string` in a loop. `StringBuilder` is the notepad where you just write the next word.

---

## 2. Deep Explanation

### How String Immutability Works

Every `string` is stored as a **read-only sequence of chars** on the heap. The `string` class has no mutable operations. Every method (`ToUpper`, `Replace`, `Trim`) returns a **new string**.

```csharp
string s = "Hello";
s += " World"; // CLR creates a NEW string "Hello World" on the heap
               // "Hello" becomes unreachable → eligible for GC
```

For `n` concatenations: **O(n²) allocations** and **O(n²) total characters copied**.

### String Interning

The CLR maintains a **string intern pool** — a dictionary of unique string literals. Identical string literals in code point to the **same object**.

```csharp
string a = "hello";
string b = "hello";
Console.WriteLine(ReferenceEquals(a, b)); // True — same interned object

string c = new string("hello".ToCharArray()); // Forces new allocation
Console.WriteLine(ReferenceEquals(a, c)); // False
```

You can explicitly intern: `string.Intern(value)` — useful for caching frequently repeated strings (e.g., XML attribute names).

### StringBuilder Internals

`StringBuilder` wraps a `char[]` array. It starts with a default capacity (16 chars) and **doubles** the buffer when it fills up (amortized O(1) appends). Final `.ToString()` creates a single string from the buffer.

```
StringBuilder("Hello"):
[H][e][l][l][o][ ][ ][ ][ ][ ][ ][ ][ ][ ][ ][ ]  ← 16-char buffer

After .Append(" World"):
[H][e][l][l][o][ ][W][o][r][l][d][ ][ ][ ][ ][ ]  ← same buffer, no allocation
```

### When to Use What

| Scenario | Use |
|----------|-----|
| Single concatenation | `string` with `+` (compiler optimizes to `string.Concat`) |
| Few concatenations (< 4–5) | `$"..."` string interpolation or `string.Concat` |
| Loop concatenations | `StringBuilder` |
| Massive string building | `StringBuilder` with initial capacity set |
| Read-heavy, no mutation | `string` (immutable = thread-safe) |
| High-performance, zero-alloc | `Span<char>` / `stackalloc` |

---

## 3. Code Examples

### Example 1 — Basic: The Loop Problem
```csharp
// BAD: O(n²) allocations — 10,000 strings created and GC'd
string result = "";
for (int i = 0; i < 10_000; i++)
{
    result += i.ToString(); // New string object on every iteration!
}

// GOOD: O(n) — one buffer, one final string
var sb = new StringBuilder(capacity: 60_000); // Pre-allocate if size is known
for (int i = 0; i < 10_000; i++)
{
    sb.Append(i);
}
string result2 = sb.ToString(); // Single allocation
```

### Example 2 — Real-World: Building SQL / HTML Dynamically
```csharp
// Generating a dynamic SQL query (though parameterized queries are preferred for safety)
public string BuildInsertQuery(List<User> users)
{
    if (users.Count == 0) return string.Empty;

    var sb = new StringBuilder("INSERT INTO Users (Name, Email) VALUES ");

    for (int i = 0; i < users.Count; i++)
    {
        sb.Append($"('{users[i].Name}', '{users[i].Email}')");
        if (i < users.Count - 1)
            sb.Append(", ");
    }

    sb.Append(';');
    return sb.ToString();
}

// Or: building an HTML report
public string GenerateHtmlReport(IEnumerable<OrderSummary> orders)
{
    var sb = new StringBuilder(1024); // Reasonable starting capacity
    sb.AppendLine("<table>");
    sb.AppendLine("<tr><th>Order ID</th><th>Total</th></tr>");

    foreach (var order in orders)
    {
        sb.Append("<tr><td>").Append(order.Id)
          .Append("</td><td>").Append(order.Total)
          .AppendLine("</td></tr>");
    }

    sb.AppendLine("</table>");
    return sb.ToString();
}
```

### Example 3 — Modern: String.Create for Zero-Copy Building (Advanced)
```csharp
// .NET 6+ — most performant for known-length strings
string result = string.Create(count * 3, numbers, (span, state) =>
{
    int pos = 0;
    foreach (int n in state)
    {
        n.TryFormat(span[pos..], out int written);
        pos += written;
        span[pos++] = ',';
    }
});
```

---

## 4. Interview Questions

1. **Why is `string` immutable in C#? What are the benefits?**
2. **What is the performance difference between `string +=` and `StringBuilder.Append` in a loop?**
3. **What is string interning? When would you use `string.Intern()`?**
4. **Is `string` a value type or reference type? Why does it behave like a value type?**
5. **When is `StringBuilder` NOT the right choice?**

---

## 5. Follow-up Questions

> 🎯 *Interviewer Mindset: These reveal whether you understand the CLR or just the surface API.*

- The compiler optimizes `"a" + "b" + "c"` into `string.Concat("a", "b", "c")` — how many allocations does this cost?
  *(Just 1 — `string.Concat` spans the inputs into one allocation)*
- What is `string.IsInterned()` and when would you use it?
- How does `string` comparison work? What's the difference between `==`, `.Equals()`, and `ReferenceEquals()`?
  ```csharp
  string a = "hello";
  string b = new string("hello".ToCharArray());
  a == b;              // True  — value equality (content)
  a.Equals(b);         // True  — value equality
  ReferenceEquals(a,b);// False — different heap objects
  ```
- How does `string.Format` vs `$""` interpolation differ in allocation behavior?
- In what scenario would `Span<char>` beat `StringBuilder`?
- Does `StringBuilder` have a maximum capacity?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Using StringBuilder for just 2-3 concatenations — slower due to object overhead!
var sb = new StringBuilder();
sb.Append("Hello");
sb.Append(" World");
string result = sb.ToString(); // For this, just use "Hello" + " World" or $"Hello World"

// MISTAKE 2: Not setting initial capacity when you know the expected size
// StringBuilder doubles buffer = extra allocations
var sb2 = new StringBuilder(users.Count * 50); // Much better

// MISTAKE 3: String comparison with == for culture-sensitive scenarios
string input = "café";
string stored = "cafe\u0301"; // Decomposed form
bool equal = input == stored; // False! Same visual, different bytes
// Use: string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase)

// MISTAKE 4: Forgetting strings are thread-safe (immutable) but StringBuilder is NOT
// Don't share a StringBuilder across threads without locking

// MISTAKE 5: Substring in tight loops — creates many string allocations
// Use AsSpan() instead:
ReadOnlySpan<char> sub = myString.AsSpan(5, 10); // Zero allocation slice
```

---

## 7. Real-World Usage

| Scenario | What to Use |
|----------|------------|
| Logging messages with interpolation | `$""` (compiler optimizes with `DefaultInterpolatedStringHandler`) |
| Building JSON/XML manually | `StringBuilder` |
| Template engines | `StringBuilder` + pre-allocated capacity |
| Config key lookups | Interned strings |
| CSV parsing | `Span<char>` over raw stream data |
| `HttpClient` query strings | `QueryHelpers.AddQueryString` or StringBuilder |

---

## 8. Depth Levels

| Level | What You Should Know |
|-------|---------------------|
| **Level 1** | `string` is immutable, StringBuilder for loops |
| **Level 2** | String interning, capacity pre-allocation, `string.Concat` vs `+` |
| **Level 3** | String format IL output, interpolation optimizations in .NET 6+ |
| **Level 4** | `Span<char>`, `string.Create()`, `SearchValues<char>`, rope data structures |

---

## 🔗 Connected Topics

- [Value vs Reference Types](./value-vs-reference-types.md) — Why string is a reference type but behaves like value type
- [Span & Memory](../03-advanced/span-memory.md) — Zero-allocation string operations
- [Memory Management](../03-advanced/memory-management.md) — GC impact of string allocations
- [Collections Overview](./collections-overview.md) — char[] vs string storage

---

*Created: April 2026 · Level: Beginner*
