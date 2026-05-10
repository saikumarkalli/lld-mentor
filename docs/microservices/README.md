# Microservices & Distributed Systems — Knowledge Hub

> A structured, progressive learning path to master distributed architecture, communication patterns, resilience, and operational concerns. Designed to be read in order — from Foundation to Solution Architect.

**Legend:** [ ] Planned · [x] Complete

---

## 📁 Folder Structure

```
docs/microservices/
├── 01-beginner/        Part I: Foundation
├── 02-intermediate/    Part II: Core Communication & Resilience
├── 03-advanced/        Part III: Advanced Data & Transactions
└── 04-scenarios/       Part IV: Real-World Scenarios
```

---

## Part I: Foundation
#### 📂 `01-beginner/`

> The "Why" and "How" of splitting up applications. Understand the core trade-offs before writing any code.

- [ ] [01-monolith-vs-microservices.md](./01-beginner/01-monolith-vs-microservices.md) — Trade-offs, scalability, and when NOT to use microservices.
- [ ] [02-service-boundaries-ddd.md](./01-beginner/02-service-boundaries-ddd.md) — Bounded Contexts, Domain-Driven Design, and how to split the monolith.
- [ ] [03-api-styles-rest-grpc-graphql.md](./01-beginner/03-api-styles-rest-grpc-graphql.md) — Protocols, data fetching, and when to use which style.
- [ ] [04-docker-and-containers.md](./01-beginner/04-docker-and-containers.md) — Containerization basics, isolation, and why microservices rely on Docker.

---

## Part II: Core Communication & Resilience
#### 📂 `02-intermediate/`

> How isolated services securely talk to each other, discover each other, and survive inevitable network failures.

- [ ] [05-api-gateway-pattern.md](./02-intermediate/05-api-gateway-pattern.md) — Backend-For-Frontend (BFF), routing, rate limiting, and SSL termination.
- [ ] [06-service-discovery-registry.md](./02-intermediate/06-service-discovery-registry.md) — Client-side vs Server-side discovery, Consul, and Kubernetes DNS.
- [ ] [07-sync-vs-async-communication.md](./02-intermediate/07-sync-vs-async-communication.md) — HTTP vs Messaging trade-offs, temporal coupling.
- [ ] [08-message-brokers-event-driven.md](./02-intermediate/08-message-brokers-event-driven.md) — RabbitMQ, Kafka basics, Publish/Subscribe semantics.
- [ ] [09-microservices-security-jwt-mtls.md](./02-intermediate/09-microservices-security-jwt-mtls.md) — Token propagation, OAuth2, and Mutual TLS (Zero Trust).
- [ ] [10-resilience-circuit-breaker-retry.md](./02-intermediate/10-resilience-circuit-breaker-retry.md) — Circuit Breakers, Bulkheads, Timeouts, and gracefully degrading functionality.
- [ ] [11-distributed-tracing-observability.md](./02-intermediate/11-distributed-tracing-observability.md) — OpenTelemetry, Jaeger, Correlation IDs, and centralized logging.

---

## Part III: Advanced Data & Transactions
#### 📂 `03-advanced/`

> The hardest part of distributed systems: managing state and guaranteeing consistency across the network.

- [ ] [12-database-per-service-pattern.md](./03-advanced/12-database-per-service-pattern.md) — Data sovereignty, the dangers of shared databases, and API Composition.
- [ ] [13-outbox-pattern.md](./03-advanced/13-outbox-pattern.md) — Guaranteeing message delivery and the dual-write problem.
- [ ] [14-idempotency-at-least-once.md](./03-advanced/14-idempotency-at-least-once.md) — Preventing double-charging in asynchronous networks.
- [ ] [15-distributed-transactions-saga.md](./03-advanced/15-distributed-transactions-saga.md) — Choreography vs Orchestration and compensating transactions.
- [ ] [16-cqrs-and-event-sourcing.md](./03-advanced/16-cqrs-and-event-sourcing.md) — Event stores, immutable state, and Read/Write model separation.
- [ ] [17-service-mesh-istio-dapr.md](./03-advanced/17-service-mesh-istio-dapr.md) — Sidecar proxies, abstracting infrastructure out of the application code.

---

## Part IV: Real-World Scenarios
#### 📂 `04-scenarios/`

> Architectural system design interviews and practical application of the patterns above.

- [ ] [18-scenario-monolith-migration.md](./04-scenarios/18-scenario-monolith-migration.md) — The Strangler Fig Pattern and incremental modernization.
- [ ] [19-scenario-order-processing-system.md](./04-scenarios/19-scenario-order-processing-system.md) — Applying Saga, API Gateway, and Outbox patterns together.
- [ ] [20-scenario-cascade-failure-recovery.md](./04-scenarios/20-scenario-cascade-failure-recovery.md) — Post-mortem analysis of a system collapse and how resilience patterns could have saved it.

---

## 🔗 Links to LLD Code
- Strategy Pattern (used in service routing): [`LLDMaster.Patterns/03_Behavioral/12_Strategy/`](../../LLDMaster.Patterns/03_Behavioral/12_Strategy/)
- Observer Pattern (event-driven): [`LLDMaster.Patterns/03_Behavioral/11_Observer/`](../../LLDMaster.Patterns/03_Behavioral/11_Observer/)
