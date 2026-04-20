# System Design — Knowledge Hub

> High-level architecture, scalability principles, and production-level thinking.

## Topics

### Core Concepts
- [ ] [How to approach a System Design interview](./01-foundations/interview-approach.md)
- [ ] [Back-of-envelope calculations](./01-foundations/capacity-estimation.md)
- [ ] [CAP Theorem](./01-foundations/cap-theorem.md)
- [ ] [Horizontal vs Vertical Scaling](./01-foundations/scaling.md)
- [ ] [Load Balancing strategies](./01-foundations/load-balancing.md)
- [ ] [CDN & Edge caching](./01-foundations/cdn.md)

### Building Blocks
- [ ] [Databases — SQL vs NoSQL decision](./02-building-blocks/database-selection.md)
- [ ] [Caching strategies (Write-through, Read-through, Cache-aside)](./02-building-blocks/caching.md)
- [ ] [Message Queues & Event Streaming](./02-building-blocks/queues.md)
- [ ] [API Design (REST, rate limiting, versioning)](./02-building-blocks/api-design.md)
- [ ] [Search (Elasticsearch, full-text)](./02-building-blocks/search.md)
- [ ] [Blob Storage & Content Delivery](./02-building-blocks/blob-storage.md)

### Distributed Systems Patterns
- [ ] [Consistent Hashing](./03-patterns/consistent-hashing.md)
- [ ] [Leader Election](./03-patterns/leader-election.md)
- [ ] [Distributed Locking](./03-patterns/distributed-locking.md)
- [ ] [Two-Phase Commit vs Saga](./03-patterns/2pc-vs-saga.md)

---

## Case Studies
- [ ] [Design Twitter / X](./case-studies/twitter.md)
- [ ] [Design a URL Shortener](./case-studies/url-shortener.md)
- [ ] [Design a Notification System](./case-studies/notification-system.md)
- [ ] [Design a Rate Limiter](./case-studies/rate-limiter.md)
- [ ] [Design an Inventory / Booking System](./case-studies/booking-system.md)
- [ ] [Design a Chat Application](./case-studies/chat-app.md)

---

## 🔗 Links to LLD Code
- When interviewers drill down to LLD from HLD:
  - Factory / Abstract Factory: [`LLDMaster.Patterns/01_Creational/`](../../LLDMaster.Patterns/01_Creational/)
  - Observer (event-driven design): [`LLDMaster.Patterns/03_Behavioral/11_Observer/`](../../LLDMaster.Patterns/03_Behavioral/11_Observer/)
  - SOLID in service design: [`LLDMaster.SOLID/`](../../LLDMaster.SOLID/)
