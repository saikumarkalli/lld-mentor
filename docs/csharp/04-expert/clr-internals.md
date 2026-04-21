# CLR Internals

> **⚡ Core Idea (30 seconds):** The CLR (Common Language Runtime) is the engine that executes .NET code. It manages memory (GC), compiles IL to native code (JIT), enforces type safety, loads assemblies, and handles threads and exceptions. Understanding it helps you write code that works *with* the runtime, not against it.

**Domain:** `C#` **Level:** `Expert` **Tags:** `#clr` `#runtime` `#il` `#jit` `#assemblies`

---

## 1. Core Idea

C# code doesn't run directly. The compiler produces **IL (Intermediate Language)** — a CPU-agnostic instruction set. The CLR takes that IL and JIT-compiles it to native machine code at runtime (or ahead of time in .NET Native/NativeAOT). The CLR also owns GC, assembly loading, type safety, thread management, and exception handling.

---

## 2. Deep Explanation

### Compilation Pipeline

```
C# source (.cs)
    ↓ [C# Compiler (Roslyn)]
IL + Metadata (.dll / .exe)    ← "Managed code"
    ↓ [JIT Compiler at runtime]
Native machine code            ← Executed by CPU
```

### Key CLR Subsystems

| Subsystem | Responsibility |
|-----------|---------------|
| **Class Loader** | Loads assemblies and types on demand |
| **JIT Compiler** | Turns IL → native code per-method on first call |
| **GC** | Manages heap memory — Gen 0/1/2, LOH |
| **Thread Pool** | Manages threads for async/parallel work |
| **Exception Handling** | SEH-based structured exception handling |
| **Type System** | Enforces type safety, vtables, interfaces |
| **Security** | Code access security (largely removed in .NET Core) |

### IL — Intermediate Language

IL is a stack-based instruction set. A simple addition:
```csharp
int Add(int a, int b) => a + b;
```
Compiles to IL like:
```
ldarg.1   // push 'a' onto stack
ldarg.2   // push 'b' onto stack
add       // pop two values, push sum
ret       // return top of stack
```

You can view IL with `ildasm.exe`, `dotnet-ildasm`, or [sharplab.io](https://sharplab.io/).

### AppDomain vs AssemblyLoadContext

`AppDomain` was the old isolation unit (removed in .NET Core). `AssemblyLoadContext` is the modern equivalent:
- Allows **loading assemblies in isolation** (plugins, hot-reload)
- Can **unload** assemblies (collectible contexts)
- The default `AssemblyLoadContext.Default` loads most assemblies

### Tiered Compilation

The JIT compiles methods in **tiers**:
- **Tier 0**: Fast, unoptimized code on first call
- **Tier 1**: Background recompilation with full optimizations after method is called enough
- **Tier 0+**: Methods with profiling instrumentation

This gives fast startup (Tier 0) + full performance at steady state (Tier 1) — the best of both worlds.

---

## 3. Code Examples

### Basic — Inspecting Assemblies
```csharp
// Load the current assembly's types
var assembly = Assembly.GetExecutingAssembly();
foreach (var type in assembly.GetTypes())
    Console.WriteLine(type.FullName);

// Load a specific assembly
var asm = Assembly.LoadFrom("MyPlugin.dll");
var pluginType = asm.GetType("MyPlugin.EntryPoint");
var instance = Activator.CreateInstance(pluginType!);

// AssemblyLoadContext for isolation
var alc = new AssemblyLoadContext("PluginContext", isCollectible: true);
var loadedAsm = alc.LoadFromAssemblyPath("/plugins/myplugin.dll");
// ... use plugin ...
alc.Unload(); // Unload the assembly and free memory
```

### Real-World — Plugin System with AssemblyLoadContext
```csharp
public class PluginLoader
{
    private readonly Dictionary<string, AssemblyLoadContext> _contexts = new();

    public IPlugin LoadPlugin(string pluginPath)
    {
        var name = Path.GetFileNameWithoutExtension(pluginPath);
        var alc = new AssemblyLoadContext(name, isCollectible: true);
        _contexts[name] = alc;

        var asm = alc.LoadFromAssemblyPath(pluginPath);
        var pluginType = asm.GetTypes()
            .Single(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);

        return (IPlugin)Activator.CreateInstance(pluginType)!;
    }

    public void UnloadPlugin(string name)
    {
        if (_contexts.TryGetValue(name, out var alc))
        {
            _contexts.Remove(name);
            alc.Unload(); // GC can now collect the types
        }
    }
}
```

---

## 4. Interview Questions

1. **What is the difference between C# source code, IL, and native code?**
   *C# source: what you write — high-level, type-safe, human-readable. IL (Intermediate Language): what the C# compiler (Roslyn) produces — a CPU-agnostic bytecode stored in `.dll`/`.exe` files. Native code: what the JIT compiler generates from IL at runtime — actual machine instructions your CPU executes. IL enables platform independence; the JIT specialises it for the current OS and CPU architecture.*

2. **What is the JIT compiler and when does it run?**
   *The JIT (Just-In-Time) compiler converts IL to native machine code **on the first call** of each method. Subsequent calls run the already-compiled native code — there's no re-interpretation. Since .NET introduced Tiered Compilation, the JIT does this in two passes: fast unoptimised code first (Tier 0), then full optimisation in the background once the method is "hot" (Tier 1).*

3. **What is tiered compilation?**
   *Tiered compilation is a two-phase JIT strategy: Tier 0 compiles quickly with minimal optimisation to keep startup fast. After a method is called enough times, a background thread recompiles it with full optimisations (inlining, devirtualisation, SIMD) — Tier 1. Calls after re-JIT use the faster Tier 1 code. This gives you fast startup (Tier 0) and maximum throughput at steady state (Tier 1) without picking one or the other.*

4. **What replaced `AppDomain` in .NET Core and why?**
   *`AssemblyLoadContext` replaced `AppDomain`. In .NET Core, `AppDomain` was stripped down — you can no longer create secondary AppDomains for isolation (the `CreateDomain` method throws). `AssemblyLoadContext` provides assembly isolation and **unloading** support for plugin scenarios: load assemblies into a collectible context, use them, then call `Unload()` to release them. It's more lightweight and precise than AppDomain was.*

5. **What CLR subsystem manages thread scheduling?**
   *The **Thread Pool** (managed by the CLR's thread pool manager). The thread pool maintains a pool of worker threads and I/O completion threads. It uses a **work-stealing scheduler** — idle threads can steal tasks from busy threads' queues. `Task`, `async/await`, `ThreadPool.QueueUserWorkItem`, and parallel operations all run on the thread pool. You rarely create raw `Thread` objects in modern .NET.*

---

## 5. Follow-up Questions

- What is the difference between JIT and AOT (Ahead-of-Time) compilation?
  *(JIT: compile at runtime — flexible but slow startup. AOT: compile at publish time — fast startup, no JIT overhead, but no runtime dynamic features.)*
- How does the CLR ensure type safety?
  *(IL is verified before execution — the verifier checks all type casts, array bounds, etc.)*
- What is `SafeHandle` and how does the CLR handle unmanaged resource tracking?
- What happens when an unhandled exception propagates through `async void`?
  *(It hits the `SynchronizationContext` or the default unhandled exception handler → process may crash)*
- What is NativeAOT and what are its trade-offs?

---

## 6. Edge Cases / Common Mistakes

```csharp
// MISTAKE 1: Confusing IL code execution with C# — IL has different semantics
// Example: Overflow doesn't throw by default in IL (unchecked context)
int.MaxValue + 1; // Wraps to int.MinValue in C# by default
checked { int.MaxValue + 1; } // OverflowException — explicit check

// MISTAKE 2: Assembly.LoadFrom vs Assembly.Load
Assembly.Load("MyLib"); // Searches in known locations (GAC, AppBase)
Assembly.LoadFrom("/custom/path/MyLib.dll"); // Explicit path — isolation gotchas

// MISTAKE 3: Assuming .NET Core has AppDomains for isolation
// AppDomain.CreateDomain() throws on .NET Core — use AssemblyLoadContext instead
```

---

## 7. Real-World Usage

| Scenario | CLR Knowledge Applied |
|----------|----------------------|
| Plugin architectures | `AssemblyLoadContext` for isolated, unloadable plugins |
| Build tools & Roslyn | Source generators run inside the CLR's compilation context |
| AOT deployment | NativeAOT for CLI tools / containers (fast startup, no JIT) |
| Debugging IL | ildasm / sharplab.io to understand compiler transformations |
| Performance profiling | Understanding JIT tiers to know when perf stabilizes |

---

## 8. Depth Levels

| Level | Focus |
|-------|-------|
| **Level 1** | C# → IL → native pipeline, JIT basics |
| **Level 2** | CLR subsystems, AssemblyLoadContext, tiered compilation |
| **Level 3** | IL instruction set, CLR type system vtables, verifier |
| **Level 4** | NativeAOT, dynamic IL generation (Emit), DynamicMethod |

## 🔗 Connected Topics
- [JIT Compilation](./jit-compilation.md) — Deep dive into the JIT tier
- [Garbage Collection](./garbage-collection.md) — GC is a CLR subsystem
- [Reflection](../03-advanced/reflection.md) — Reads the CLR's metadata

*Created: April 2026 · Level: Expert*
