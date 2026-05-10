# Chapter 49 — XML Basics & LINQ to XML

> **⚡ Core Idea (30 seconds):** XML is older and heavier than JSON, but still heavily used in enterprise integrations, SOAP APIs, configuration files, and document formats (like `.docx`). In .NET, `XDocument` (LINQ to XML) is the modern, expressive way to create and query XML, replacing the old, clunky `XmlDocument`.

**Domain:** `C#` **Level:** `Expert` **Tags:** `#xml` `#linq-to-xml` `#serialization` `#soap`

---

## 1. Core Idea

Think of JSON as a **post-it note** — lightweight, unstructured, great for quick messages between servers. Think of XML as a **legal contract** — verbose, strictly structured with schemas (XSD), and explicitly defining namespaces to avoid conflicts. You wouldn't use a post-it note for a mortgage agreement, which is why banks, governments, and enterprise systems still rely on XML.

---

## 2. Deep Explanation

### The Three Ways to Handle XML in .NET

1. **`XmlSerializer` (Object Mapping)**
   *The classic way to map C# classes directly to XML elements.*
   Use when you have a strict contract (XSD) and want to work with strongly typed C# objects.

2. **`XDocument` / LINQ to XML (DOM Parsing)**
   *The modern, functional way to query and manipulate XML in memory.*
   Use when you need to extract specific data from an XML file, transform XML shapes, or generate XML dynamically without creating C# classes. (Replaces `XmlDocument`).

3. **`XmlReader` / `XmlWriter` (Streaming)**
   *The forward-only, non-cached way to process XML byte by byte.*
   Use for massive files (e.g., a 5GB data dump) where loading an `XDocument` into memory would cause an `OutOfMemoryException`.

### Namespaces (`xmlns`)

The biggest pain point in XML is namespaces. `<User>` in system A might mean something different than `<User>` in system B. XML uses namespaces to prevent collisions.

```xml
<ns1:Order xmlns:ns1="http://shop.com/schema">
    <ns1:Id>123</ns1:Id>
</ns1:Order>
```

When querying this document, you CANNOT just search for `"Id"`. You must search for `"{http://shop.com/schema}Id"`.

---

## 3. Code Examples

### Example 1 — XmlSerializer
```csharp
// Define the model with attributes mapping to XML structure
[XmlRoot("CustomerData")]
public class Customer
{
    [XmlAttribute("id")] public int Id { get; set; }
    [XmlElement("FullName")] public string Name { get; set; }
    [XmlIgnore] public string InternalSecret { get; set; }
}

// Serialize
var customer = new Customer { Id = 1, Name = "Sai" };
var serializer = new XmlSerializer(typeof(Customer));

using var writer = new StringWriter();
serializer.Serialize(writer, customer);
// Output: <CustomerData id="1"><FullName>Sai</FullName></CustomerData>

// Deserialize
using var reader = new StringReader(writer.ToString());
var parsed = (Customer)serializer.Deserialize(reader);
```

### Example 2 — XDocument (LINQ to XML)
```csharp
// Functional construction (no classes needed!)
var doc = new XDocument(
    new XElement("Orders",
        new XElement("Order", new XAttribute("id", 1), new XElement("Total", 99.99)),
        new XElement("Order", new XAttribute("id", 2), new XElement("Total", 149.50))
    )
);

// Querying with LINQ
var highValueOrders = doc.Descendants("Order")
    .Where(x => (decimal)x.Element("Total") > 100)
    .Select(x => (int)x.Attribute("id"))
    .ToList(); // Returns [2]
```

### Example 3 — Handling Namespaces in XDocument
```csharp
var xml = @"<root xmlns='http://api.com/v1'><user>Sai</user></root>";
var doc = XDocument.Parse(xml);

// ❌ Fails: "user" in no namespace doesn't exist!
var user1 = doc.Descendants("user").FirstOrDefault(); 

// ✅ Correct: You must combine the namespace and the element name
XNamespace ns = "http://api.com/v1";
var user2 = doc.Descendants(ns + "user").FirstOrDefault(); 
```

---

## 4. Interview Questions

1. **What is the difference between `XmlDocument` and `XDocument`?**
   *`XmlDocument` is the legacy W3C DOM API from .NET 1.0. It is clunky, mutable, and hard to use. `XDocument` (LINQ to XML) was introduced in .NET 3.5. It is built for functional construction, integrates perfectly with LINQ for querying, and is the standard way to work with XML DOMs in modern .NET.*

2. **How do you parse a 10 GB XML file without crashing the server?**
   *You cannot use `XDocument.Load()` or `XmlSerializer`, as both will load the entire 10 GB structure into RAM. You must use `XmlReader`, which streams the file progressively. You move the reader forward node by node (`reader.Read()`), process the element, and discard it, keeping the memory footprint flat.*

3. **Why is `XmlSerializer` considered slow to start?**
   *When you first instantiate `new XmlSerializer(typeof(MyClass))`, the CLR dynamically emits and compiles a temporary assembly at runtime to handle the serialization logic. This compilation takes time. You should always cache and reuse your `XmlSerializer` instances.*

4. **Why is my LINQ to XML query returning null even though I can see the element in the file?**
   *99% of the time, this is a namespace issue. If the XML document defines an `xmlns="http://..."` at the root, ALL child elements inherit that namespace. You cannot query `.Element("Name")`. You must define an `XNamespace` and query `.Element(ns + "Name")`.*

5. **What is an XML attribute vs an element?**
   *An element is a structural node (`<User>Sai</User>`). An attribute is metadata attached to the opening tag (`<User id="1">`). In C#, you map them using `[XmlElement]` and `[XmlAttribute]`. Best practice usually prefers elements for data (extensible) and attributes for metadata/IDs.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Creating XmlSerializer in a loop (Massive Memory Leak!)
public void Process(List<string> xmls)
{
    foreach (var xml in xmls)
    {
        // ❌ BAD: Recompiles dynamic assembly every loop! Leaks memory!
        var serializer = new XmlSerializer(typeof(MyType), new XmlRootAttribute("Root")); 
    }
}
// ✅ FIX: Cache the serializer instance statically.

// MISTAKE 2: Trying to cast missing elements in XDocument
decimal total = (decimal)orderElement.Element("Total"); 
// ❌ If <Total> is missing, Element() returns null. Casting null to decimal throws!

// ✅ FIX: Cast to nullable type. It returns null instead of throwing.
decimal total = (decimal?)orderElement.Element("Total") ?? 0m;
```

---

## 🔗 Connected Topics

- [JSON Serialization](./48-json-serialization.md) — The modern alternative for APIs
- [File I/O](./47-file-io.md) — Streaming large XML files using `XmlReader` over `FileStream`
- [LINQ](../03-intermediate/21-linq.md) — Used heavily with `XDocument`

---

*Created: May 2026 · Level: Expert*
