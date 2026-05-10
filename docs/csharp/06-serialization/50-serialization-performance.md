# Chapter 50 — Serialization Performance & Large File Handling

> **⚡ Core Idea (30 seconds):** Handling large files (1GB+) or high-throughput APIs requires bypassing standard abstractions. You cannot load full objects or strings into memory. High performance serialization relies on **Streaming** (processing chunks), **`Utf8JsonReader/Writer`** (zero-allocation parsing), and **`System.IO.Pipelines`** (high-performance buffer management).

**Domain:** `C#` **Level:** `Expert` **Tags:** `#performance` `#serialization` `#large-files` `#pipelines`

---

## 1. Core Idea

Think of reading a 5GB file like **drinking from a firehose**. 
- `File.ReadAllText()` puts a giant bucket in front of the hose. The bucket overflows (OutOfMemoryException).
- `StreamReader` is sipping from a cup, refilling it continuously.
- `System.IO.Pipelines` is building an advanced plumbing system that routes the water efficiently across multiple workers without spilling a drop.

---

## 2. Deep Explanation

### 1. The Allocation Problem

Standard JSON/XML serialization parses bytes into strings, strings into dictionaries, and dictionaries into C# objects. Every intermediate step allocates memory on the Heap. The Garbage Collector (GC) has to pause your application to clean up these intermediate objects. Under heavy load, the GC becomes the bottleneck.

### 2. Utf8JsonReader & Utf8JsonWriter (Zero-Allocation)

`System.Text.Json` provides low-level APIs that read directly from UTF-8 byte spans.

- **`Utf8JsonReader`** is a `ref struct`. It scans through a `Span<byte>` and identifies tokens (`StartObject`, `PropertyName`, `Number`). It **does not allocate strings**. If it sees the property `"Age"`, it gives you a `ReadOnlySpan<byte>` pointing exactly to those 3 bytes in the original buffer.
- **`Utf8JsonWriter`** writes UTF-8 bytes directly to a `Stream` or `IBufferWriter<byte>`, bypassing string concatenations.

### 3. System.IO.Pipelines

Introduced in .NET Core 2.1, Pipelines are designed for ultra-high-performance network I/O (it powers Kestrel, the ASP.NET Core web server). 
Standard streams are hard to manage when you read 4KB chunks but a JSON message happens to be split exactly across the 4KB boundary. Pipelines handle buffer management, leasing memory from `ArrayPool`, and stitching chunks together for you.

---

## 3. Code Examples

### Example 1 — Parsing a 10 GB JSON Array
```csharp
// Scenario: [ {"id":1}, {"id":2}, ... 100 million more ... ]
// We cannot deserialize the whole array. We must stream it.

public async IAsyncEnumerable<int> ExtractIdsAsync(Stream jsonStream)
{
    var options = new JsonSerializerOptions { DefaultBufferSize = 8192 };
    
    // IAsyncEnumerable handles the streaming seamlessly
    var users = JsonSerializer.DeserializeAsyncEnumerable<User>(jsonStream, options);
    
    await foreach (var user in users)
    {
        // We only hold one User object in memory at a time!
        if (user is not null) yield return user.Id;
    }
}
```

### Example 2 — Utf8JsonReader for Zero-Allocation Parsing
```csharp
// We want to find the "Total" value in this JSON without allocating any strings/objects
byte[] jsonUtf8Bytes = Encoding.UTF8.GetBytes(@"{ ""Order"": 123, ""Total"": 99.99 }");

public decimal GetTotal(ReadOnlySpan<byte> utf8Json)
{
    var reader = new Utf8JsonReader(utf8Json);
    
    while (reader.Read())
    {
        if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("Total"))
        {
            reader.Read(); // Move to the value
            return reader.GetDecimal(); // Extracts 99.99 directly!
        }
    }
    return 0m;
}
```

### Example 3 — System.IO.Pipelines Basics
```csharp
public async Task ProcessDataAsync(Socket socket)
{
    var pipe = new Pipe();
    Task writing = FillPipeAsync(socket, pipe.Writer);
    Task reading = ReadPipeAsync(pipe.Reader);
    await Task.WhenAll(writing, reading);
}

async Task FillPipeAsync(Socket socket, PipeWriter writer)
{
    while (true)
    {
        // Rent buffer from the pipe
        Memory<byte> memory = writer.GetMemory(512);
        int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
        if (bytesRead == 0) break;
        
        // Tell pipe how much we wrote
        writer.Advance(bytesRead);
        
        // Flush makes data available to the reader
        await writer.FlushAsync();
    }
    await writer.CompleteAsync();
}

async Task ReadPipeAsync(PipeReader reader)
{
    while (true)
    {
        ReadResult result = await reader.ReadAsync();
        ReadOnlySequence<byte> buffer = result.Buffer;
        
        // Process the buffer (e.g., parse lines or messages)
        ProcessMessages(ref buffer);
        
        // Tell pipe how much we consumed and how much we examined
        reader.AdvanceTo(buffer.Start, buffer.End);
        
        if (result.IsCompleted) break;
    }
    await reader.CompleteAsync();
}
```

---

## 4. Interview Questions

1. **How does `JsonSerializer.DeserializeAsyncEnumerable` solve the large JSON array problem?**
   *Instead of materializing the entire JSON array into a `List<T>`, it returns an `IAsyncEnumerable<T>`. It streams the bytes, parses one object at a time, yields it to the caller, and then lets the GC collect it. This keeps memory consumption flat (O(1) memory), allowing you to process infinite streams.*

2. **What is `Utf8JsonReader` and why is it faster than standard deserialization?**
   *It is a high-performance, forward-only, zero-allocation JSON parser. It is a `ref struct` (meaning it lives entirely on the stack). It avoids allocating strings by giving you `ReadOnlySpan<byte>` views into the original byte buffer. It avoids the overhead of Reflection and object instantiation entirely.*

3. **What problem does `System.IO.Pipelines` solve compared to standard Streams?**
   *Buffer management. When reading from a `NetworkStream`, you must manually allocate a byte array, handle the case where a message is split across multiple `Read()` calls, and shift bytes around to form complete messages. Pipelines manages the buffers (using `ArrayPool`), handles the splitting/stitching via `ReadOnlySequence<byte>`, and provides thread-safe producer-consumer synchronization.*

4. **Why should you never serialize directly to a `string` when returning data from an API?**
   *If a user requests 50,000 records, serializing to a `string` will allocate a massive block of memory in the Large Object Heap (LOH), causing severe GC fragmentation. Instead, you should serialize directly to the HTTP Response `Stream`. ASP.NET Core does this automatically when you return objects from a controller.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Buffering massive files before processing
public void ProcessXml(string path)
{
    // ❌ Loads entire XML DOM into memory. Fails on files > 1GB.
    var doc = XDocument.Load(path); 
}
// ✅ FIX: Use XmlReader to stream the file node-by-node.

// MISTAKE 2: Unbounded memory streams
var ms = new MemoryStream();
await file.CopyToAsync(ms); // ❌ 10GB file causes OutOfMemoryException
// ✅ FIX: Process in chunks, or stream directly to disk/network without MemoryStream.
```

---

## Connected Topics

- [Span\<T\>](../04-advanced/34-span-memory.md) — The fundamental type behind zero-allocation parsing
- [Task Parallel Library](../05-expert/43-task-parallel-library.md) — `System.Threading.Channels` (similar producer/consumer model to Pipelines)
- [Performance Tuning](../05-expert/44-performance-tuning.md) — ArrayPool and reducing allocations

---

*Created: May 2026 · Level: Expert*
