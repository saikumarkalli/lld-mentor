# Chapter 47 — File I/O & Streams

> **⚡ Core Idea (30 seconds):** A `Stream` is an abstraction for a sequence of bytes. It allows you to read or write data incrementally (chunk by chunk) instead of loading the entire file into memory at once. `FileStream` connects a stream to a file, while `StreamReader/Writer` translates those raw bytes into strings.

**Domain:** `C#` **Level:** `Expert` **Tags:** `#io` `#streams` `#files` `#performance`

---

## 1. Core Idea

Think of reading a book. 
- **`File.ReadAllText()`** is like trying to memorize the entire book in one glance before reading it. If the book has 10,000 pages, your brain (RAM) will crash (`OutOfMemoryException`).
- **`StreamReader`** is like reading the book page by page. It doesn't matter if the book has 10,000 pages or 10 million pages — you only ever hold one page in your brain at a time.

This is the foundation of: **parsing multi-gigabyte logs, streaming HTTP responses, handling file uploads safely, and keeping server memory footprints flat.**

---

## 2. Deep Explanation

### The Stream Hierarchy

`System.IO.Stream` is an abstract base class.
- **`FileStream`** reads/writes bytes to a file on disk.
- **`MemoryStream`** reads/writes bytes to a byte array in RAM (useful for mocking or buffering).
- **`NetworkStream`** reads/writes bytes over a TCP/IP socket.

Streams only understand `byte[]` and `Span<byte>`. They know nothing about text encodings, JSON, or lines.

### Readers and Writers (The Decorators)

To work with text, you wrap the Stream in a Reader/Writer:
- **`StreamReader`** / **`StreamWriter`**: Translates bytes to characters/strings using an Encoding (like UTF-8).
- **`BinaryReader`** / **`BinaryWriter`**: Translates bytes to primitive types (e.g., reads 4 bytes and returns an `int`).

### Async I/O (The Golden Rule)

Disk drives and networks are orders of magnitude slower than CPUs.
- Sync I/O (`.Read()`, `.ReadLine()`): Blocks the thread while waiting for the disk head to move.
- Async I/O (`.ReadAsync()`, `.ReadLineAsync()`): Hands the operation to the OS driver and releases the thread. **Always use Async I/O in web servers.**

---

## 3. Code Examples

### Example 1 — Reading a massive file without crashing
```csharp
// ❌ Bad: Loads entire file into memory. Will OOM on a 5GB log file.
public string[] ReadLogs(string path)
{
    return File.ReadAllLines(path); 
}

// ✅ Good: Streams line by line. Memory footprint stays < 1MB.
public async IAsyncEnumerable<string> ReadLogsAsync(string path)
{
    // bufferSize: 4096 is default. FileOptions.Asynchronous is critical for true async OS I/O!
    await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, 
                                                FileShare.Read, bufferSize: 4096, 
                                                FileOptions.Asynchronous);
    using var reader = new StreamReader(fileStream);
    
    string? line;
    while ((line = await reader.ReadLineAsync()) != null)
    {
        yield return line;
    }
}
```

### Example 2 — Streaming an Uploaded File to Disk
```csharp
// ❌ Bad: Copies the entire file into a MemoryStream first
public async Task SaveFileBad(IFormFile file, string path)
{
    using var ms = new MemoryStream();
    await file.CopyToAsync(ms); // Loads full file into RAM!
    await File.WriteAllBytesAsync(path, ms.ToArray());
}

// ✅ Good: Streams directly from network to disk
public async Task SaveFileGood(IFormFile file, string path)
{
    await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, 
                                                FileShare.None, 4096, FileOptions.Asynchronous);
    // Connects the HTTP input stream directly to the File output stream
    await file.CopyToAsync(fileStream); 
}
```

---

## 4. Interview Questions

1. **What is the difference between `Stream`, `FileStream`, and `StreamReader`?**
   *`Stream` is the abstract base class for reading/writing raw bytes incrementally. `FileStream` is a concrete implementation that connects a stream to a file on disk. `StreamReader` is a decorator that wraps a stream, handles text encoding (like UTF-8), and allows you to read those bytes as C# strings (e.g., `ReadLine()`).*

2. **Why should you use `FileOptions.Asynchronous` when creating a FileStream?**
   *If you call `ReadAsync()` on a FileStream created WITHOUT `FileOptions.Asynchronous`, it actually executes synchronously! It blocks a ThreadPool thread while waiting for the disk. Adding the flag tells the Windows kernel to use asynchronous Overlapped I/O, allowing the thread to do other work while the disk spins.*

3. **How do you read a 10 GB log file in a server with only 2 GB of RAM?**
   *Never use `File.ReadAllText()` or `File.ReadAllLines()`. Instead, open a `FileStream`, wrap it in a `StreamReader`, and read it chunk-by-chunk using `ReadLineAsync()` inside a `while` loop (or return an `IAsyncEnumerable<string>`). This keeps the memory footprint flat, allocating only the string for the current line.*

4. **What does `Stream.CopyToAsync()` do?**
   *It reads bytes from a source stream and writes them to a destination stream using an internal buffer (default 80KB). It is the most efficient way to move data from a network socket to a file (or vice versa) because it streams the data in chunks without ever loading the entire payload into memory.*

5. **Why do we use the `using` statement with Streams?**
   *Streams hold unmanaged OS resources (like file handles or network sockets). If you don't dispose them, the file remains locked (you can't delete or move it), and you leak system resources until the GC runs the finalizer. `using` guarantees `Dispose()` is called even if an exception occurs.*

---

## 5. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Forgetting to rewind a MemoryStream
var ms = new MemoryStream();
await JsonSerializer.SerializeAsync(ms, myObject);
// ms.Position is now at the END of the stream!
var text = new StreamReader(ms).ReadToEnd(); // Returns "" (empty string)
// FIX:
ms.Position = 0; 
var text = new StreamReader(ms).ReadToEnd(); // Works

// MISTAKE 2: Leaving files locked
var stream = File.OpenRead("data.txt");
// An exception happens here...
// The file is locked until the GC cleans up 'stream'
// FIX: Always use 'using var stream = ...'

// MISTAKE 3: Closing the StreamReader closes the underlying Stream
var ms = new MemoryStream();
using (var writer = new StreamWriter(ms)) { writer.Write("Hello"); }
// ms is now closed/disposed! Calling ms.ToArray() will throw ObjectDisposedException
// FIX: leaveOpen parameter
using (var writer = new StreamWriter(ms, Encoding.UTF8, 1024, leaveOpen: true)) { }
```

---

## 🔗 Connected Topics

- [Memory Management / IDisposable](../04-advanced/33-memory-management.md) — Why streams must be disposed
- [Async / Await](../04-advanced/27-async-await.md) — Asynchronous I/O mechanics
- [Iterators](../03-intermediate/24-iterators.md) — Combining `IAsyncEnumerable` with file streams

---

*Created: May 2026 · Level: Expert*
