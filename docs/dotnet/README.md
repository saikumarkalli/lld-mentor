# .NET & ASP.NET Core — Knowledge Hub

> Framework internals, hosting model, DI, Middleware, and REST API patterns.

## Topics

### Beginner
- [x] [CLR (Common Language Runtime) Deep Dive](./01-beginner/clr-deep-dive.md) — Architecture, Memory Management, GC internals, and JIT compilation.
- [x] [Async/Await Deep Dive](./01-beginner/async-await-deep-dive.md) — State machines, SynchronizationContext, Task Parallel Library, and common anti-patterns.
- [ ] [.NET vs .NET Framework vs .NET Standard](./01-beginner/dotnet-versions.md) — Historical context, differences, and migration paths.
- [ ] [Project types: Web API, Worker Service, Console](./01-beginner/project-types.md) — Choosing the right hosting model for your application.
- [ ] [appsettings.json & Configuration](./01-beginner/configuration.md) — Options pattern, environment variables, and secure secrets management.
- [ ] [Startup / Program.cs structure](./01-beginner/program-structure.md) — WebApplicationBuilder, host configuration, and the entry point execution flow.

### Intermediate
- [x] [Dependency Injection Deep Dive](./01-beginner/di-deep-dive.md) — Service lifetimes, captive dependencies, scopes, and advanced DI patterns.
- [x] [Middleware Pipeline Deep Dive](./01-beginner/middleware-deep-dive.md) — Request processing, custom middleware creation, ordering, and terminal delegates.
- [x] [Routing, Controllers & Actions Deep Dive](./02-intermediate/routing-controllers-deep-dive.md) — Endpoint routing, attribute routing, model binding, and action results.
- [ ] [Model Binding & Validation](./02-intermediate/model-binding.md) — FluentValidation, binding sources, and custom model binders.
- [ ] [Filters (Action, Exception, Authorization)](./02-intermediate/filters.md) — The MVC filter pipeline and cross-cutting concerns.
- [ ] [HttpClient & IHttpClientFactory](./02-intermediate/httpclient.md) — Socket exhaustion prevention, Polly retries, and named clients.
- [ ] [Background Services (IHostedService)](./02-intermediate/background-services.md) — Long-running workers, task execution, and DI scope management.
- [ ] [Entity Framework Core — Core Concepts](./02-intermediate/efcore-basics.md) — DbContext, change tracking, migrations, and basic querying.

### Advanced
- [ ] [EF Core — Performance & Pitfalls](./03-advanced/efcore-advanced.md) — Query compilation, split queries, N+1 problems, and explicit loading.
- [ ] [Caching (IMemoryCache, IDistributedCache)](./03-advanced/caching.md) — Cache invalidation strategies, Redis integration, and memory limits.
- [ ] [Authentication & Authorization (JWT, OAuth2)](./03-advanced/auth.md) — Token validation, claims-based authorization, and custom policies.
- [ ] [Custom Middleware & Error Handling](./03-advanced/error-handling.md) — Global exception handling, problem details, and standardized API responses.
- [ ] [gRPC in .NET](./03-advanced/grpc.md) — Protobuf contracts, performance benefits, and bi-directional streaming.
- [ ] [SignalR (Real-time)](./03-advanced/signalr.md) — WebSockets, hub contexts, scaling with Redis backplane, and client connections.
- [ ] [.NET Performance Profiling](./03-advanced/performance.md) — Diagnostic tools, EventCounters, GC tuning, and reducing allocations.

---

## 🔗 Links to LLD Code
- Design patterns used in .NET apps: [`LLDMaster.Patterns/`](../../LLDMaster.Patterns/)
