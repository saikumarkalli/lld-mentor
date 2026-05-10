# .NET & ASP.NET Core — Knowledge Hub

> A structured, progressive learning path to master .NET framework internals, API development, performance, and enterprise architecture. Designed to be read in order — from Foundation to Solution Architect.

**Legend:** [x] Complete · [ ] Planned

---

## 📁 Folder Structure

```
docs/dotnet/
├── 01-beginner/        Part I: Foundation & Application Structure
├── 02-intermediate/    Part II: Building APIs & Request Flow
├── 03-advanced/        Part III: Data Access & Performance
└── 04-expert/          Part IV: Enterprise Systems & Architecture
```

---

## Part I: Beginner — Foundation & Application Structure
#### 📂 `01-beginner/`

> Before building an API, understand how the framework boots up, manages memory, executes code, and wires up dependencies.

- [ ] [01-dotnet-versions.md](./01-beginner/01-dotnet-versions.md) — .NET vs .NET Framework vs .NET Standard.
- [x] [02-clr-deep-dive.md](./01-beginner/02-clr-deep-dive.md) — Architecture, Memory Management, GC internals, and JIT compilation.
- [ ] [03-project-types.md](./01-beginner/03-project-types.md) — Choosing Web API vs Worker Service vs gRPC vs Console.
- [ ] [04-program-structure.md](./01-beginner/04-program-structure.md) — Host Builder, Kestrel, and the entry point execution flow.
- [ ] [05-configuration.md](./01-beginner/05-configuration.md) — appsettings.json, environment variables, and the Options pattern.
- [x] [06-di-deep-dive.md](./01-beginner/06-di-deep-dive.md) — Dependency Injection internals, scopes, and captive dependencies.
- [ ] [07-logging-and-serilog.md](./01-beginner/07-logging-and-serilog.md) — Structured logging, sinks, and ILogger.
- [x] [08-async-await-deep-dive.md](./01-beginner/08-async-await-deep-dive.md) — State machines, SynchronizationContext, and Task Parallel Library.

---

## Part II: Intermediate — Building APIs & Request Flow
#### 📂 `02-intermediate/`

> Understanding how an HTTP request enters the application, gets routed, validated, and processed before hitting the database.

- [x] [09-middleware-deep-dive.md](./02-intermediate/09-middleware-deep-dive.md) — Request processing, custom middleware, ordering, and terminal delegates.
- [x] [10-routing-controllers-deep-dive.md](./02-intermediate/10-routing-controllers-deep-dive.md) — Endpoint routing, attribute routing, and action results.
- [ ] [11-minimal-apis.md](./02-intermediate/11-minimal-apis.md) — Modern, high-performance API endpoints vs Controllers.
- [ ] [12-model-binding-validation.md](./02-intermediate/12-model-binding-validation.md) — FluentValidation, binding sources, and custom model binders.
- [ ] [13-filters.md](./02-intermediate/13-filters.md) — The MVC filter pipeline: Action, Authorization, and Result filters.
- [ ] [14-http-request-lifecycle.md](./02-intermediate/14-http-request-lifecycle.md) — Massive visual deep dive from Kestrel down to the DB.
- [ ] [15-httpclient-factory.md](./02-intermediate/15-httpclient-factory.md) — Socket exhaustion prevention, Polly retries, and named clients.
- [ ] [16-global-exception-handling.md](./02-intermediate/16-global-exception-handling.md) — .NET 8 `IExceptionHandler`, ProblemDetails, and standardized API responses.

---

## Part III: Advanced — Data Access & Performance
#### 📂 `03-advanced/`

> Connecting to databases, optimizing queries, caching data, and handling long-running background tasks.

- [ ] [17-efcore-basics.md](./03-advanced/17-efcore-basics.md) — DbContext, change tracking, migrations, and basic querying.
- [ ] [18-dapper-and-micro-orms.md](./03-advanced/18-dapper-and-micro-orms.md) — High-performance raw SQL querying for read-heavy operations.
- [ ] [19-efcore-advanced.md](./03-advanced/19-efcore-advanced.md) — Query compilation, split queries, N+1 problems, and explicit loading.
- [ ] [20-caching.md](./03-advanced/20-caching.md) — In-memory caching, distributed caching with Redis, and invalidation strategies.
- [ ] [21-background-services.md](./03-advanced/21-background-services.md) — `IHostedService`, long-running workers, and task execution.
- [x] [22-tpl-deep-dive.md](./03-advanced/22-tpl-deep-dive.md) — Parallel.ForEach, ThreadPool, Channels, and Concurrency Primitives.

---

## Part IV: Expert / Solution Architect — Enterprise Systems
#### 📂 `04-expert/`

> Security, real-time communication, microservices architecture, and deep system observability.

- [ ] [23-auth-jwt-oauth.md](./04-expert/23-auth-jwt-oauth.md) — Token validation, claims-based authorization, and custom policies.
- [ ] [24-signalr.md](./04-expert/24-signalr.md) — WebSockets, hub contexts, scaling with Redis backplane, and client connections.
- [ ] [25-grpc.md](./04-expert/25-grpc.md) — Protobuf contracts, performance benefits, and bi-directional streaming.
- [ ] [26-clean-architecture-ddd.md](./04-expert/26-clean-architecture-ddd.md) — Domain-Driven Design, CQRS with MediatR, and project structuring.
- [ ] [27-testing-webapplicationfactory.md](./04-expert/27-testing-webapplicationfactory.md) — Integration testing APIs with Testcontainers.
- [ ] [28-messaging-masstransit.md](./04-expert/28-messaging-masstransit.md) — RabbitMQ, Service Bus, and Pub/Sub event-driven patterns.
- [ ] [29-observability-opentelemetry.md](./04-expert/29-observability-opentelemetry.md) — Distributed tracing, structured logging via ETW, Grafana, and Jaeger.
- [ ] [30-performance-profiling.md](./04-expert/30-performance-profiling.md) — Diagnostic tools (`dotnet-trace`, `dotnet-dump`), GC tuning, and reducing allocations.

---

## 🔗 Links to LLD Code
- Design patterns used in .NET apps: [`LLDMaster.Patterns/`](../../LLDMaster.Patterns/)

