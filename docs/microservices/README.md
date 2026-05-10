# Microservices — Knowledge Hub

> Architecture patterns, communication styles, resilience, and operational concerns.

## Topics

### Beginner
- [ ] [Monolith vs Microservices — Trade-offs](./01-beginner/monolith-vs-microservices.md)
- [ ] [What makes a good service boundary?](./01-beginner/service-boundaries.md)
- [ ] [REST vs gRPC vs GraphQL](./01-beginner/api-styles.md)
- [ ] [Docker basics for microservices](./01-beginner/docker-basics.md)

### Intermediate
- [ ] [API Gateway Pattern](./02-intermediate/api-gateway.md)
- [ ] [Service Discovery](./02-intermediate/service-discovery.md)
- [ ] [Synchronous vs Asynchronous Communication](./02-intermediate/sync-vs-async.md)
- [ ] [Message Brokers (RabbitMQ / Azure Service Bus)](./02-intermediate/message-brokers.md)
- [ ] [Saga Pattern (Choreography vs Orchestration)](./02-intermediate/saga-pattern.md)
- [ ] [Circuit Breaker, Retry, Bulkhead](./02-intermediate/resilience-patterns.md)
- [ ] [Distributed Tracing & Logging](./02-intermediate/observability.md)

### Advanced
- [ ] [CQRS Pattern](./03-advanced/cqrs.md)
- [ ] [Event Sourcing](./03-advanced/event-sourcing.md)
- [ ] [Outbox Pattern](./03-advanced/outbox-pattern.md)
- [ ] [Idempotency & At-least-once delivery](./03-advanced/idempotency.md)
- [ ] [Service Mesh (Istio / Dapr)](./03-advanced/service-mesh.md)
- [ ] [Distributed Transactions](./03-advanced/distributed-transactions.md)

---

## Scenarios
- [ ] [Design an Order Processing System](./scenarios/order-processing.md)
- [ ] [Handle a cascade failure](./scenarios/cascade-failure.md)
- [ ] [Migrate monolith to microservices](./scenarios/monolith-migration.md)

---

## 🔗 Links to LLD Code
- Strategy Pattern (used in service routing): [`LLDMaster.Patterns/03_Behavioral/12_Strategy/`](../../LLDMaster.Patterns/03_Behavioral/12_Strategy/)
- Observer Pattern (event-driven): [`LLDMaster.Patterns/03_Behavioral/11_Observer/`](../../LLDMaster.Patterns/03_Behavioral/11_Observer/)
