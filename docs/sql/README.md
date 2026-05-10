# SQL & Databases — Knowledge Hub

> A structured, progressive learning path to master relational databases, query optimization, data architecture, and distributed systems. Designed to be read in order — from Foundation to Solution Architect.

**Legend:** [ ] Planned · [x] Complete

---

## 📁 Folder Structure

```
docs/sql/
├── 01-beginner/        Part I: Relational Foundation
├── 02-intermediate/    Part II: Intermediate Querying & Mechanics
├── 03-advanced/        Part III: Advanced Optimization & Administration
├── 04-architecture/    Part IV: Distributed Architecture
└── 05-scenarios/       Part V: Real-World Scenarios
```

---

## Part I: Relational Foundation
#### 📂 `01-beginner/`

> Understanding how data is structured, related, and retrieved.

- [ ] [01-relational-basics-and-queries.md](./01-beginner/01-relational-basics-and-queries.md) — SELECT, WHERE, Aggregates, and NULL handling.
- [ ] [02-joins-and-relationships.md](./01-beginner/02-joins-and-relationships.md) — INNER, LEFT, RIGHT, CROSS, and Self Joins.
- [ ] [03-schema-design-constraints.md](./01-beginner/03-schema-design-constraints.md) — PKs, FKs, UUID vs INT debate, and Data Type optimization.
- [ ] [04-normalization-1nf-to-3nf.md](./01-beginner/04-normalization-1nf-to-3nf.md) — Eliminating data redundancy and anomalies.

---

## Part II: Intermediate Querying & Mechanics
#### 📂 `02-intermediate/`

> Moving beyond basic queries to understand how the database engine actually works.

- [ ] [05-ctes-and-window-functions.md](./02-intermediate/05-ctes-and-window-functions.md) — Advanced analytics, ROW_NUMBER(), OVER().
- [ ] [06-views-functions-stored-procedures.md](./02-intermediate/06-views-functions-stored-procedures.md) — Database programmability and security.
- [ ] [07-transactions-and-acid.md](./02-intermediate/07-transactions-and-acid.md) — Atomicity, Consistency, Isolation, Durability.
- [ ] [08-indexes-clustered-vs-non-clustered.md](./02-intermediate/08-indexes-clustered-vs-non-clustered.md) — B-Trees, physical storage, and index fragmentation.
- [ ] [09-query-execution-plans.md](./02-intermediate/09-query-execution-plans.md) — Reading the engine's mind, Table Scans vs Index Seeks.
- [ ] [10-database-security-and-rbac.md](./02-intermediate/10-database-security-and-rbac.md) — Role-Based Access Control, securing schemas, and least privilege.

---

## Part III: Advanced Optimization & Administration
#### 📂 `03-advanced/`

> Solving problems at 1 Terabyte of data and 10,000 queries per second.

- [ ] [11-index-optimization-and-covering.md](./03-advanced/11-index-optimization-and-covering.md) — Composite indexes, avoiding Key Lookups, and SARGability.
- [ ] [12-isolation-levels-and-deadlocks.md](./03-advanced/12-isolation-levels-and-deadlocks.md) — Dirty reads, Phantom reads, and fixing deadlocks.
- [ ] [13-sql-anti-patterns-cursors-n-plus-1.md](./03-advanced/13-sql-anti-patterns-cursors-n-plus-1.md) — Why cursors are evil, ORM pitfalls, and set-based thinking.
- [ ] [14-denormalization-patterns.md](./03-advanced/14-denormalization-patterns.md) — Breaking 3NF for read performance, materialized views.
- [ ] [15-backups-dr-and-high-availability.md](./03-advanced/15-backups-dr-and-high-availability.md) — RPO, RTO, Point-in-Time Restore, and AlwaysOn Availability.

---

## Part IV: Distributed Architecture
#### 📂 `04-architecture/`

> Scaling the database across multiple physical servers globally.

- [ ] [16-partitioning-and-sharding.md](./04-architecture/16-partitioning-and-sharding.md) — Horizontal vs Vertical scaling across servers.
- [ ] [17-read-replicas-and-write-scaling.md](./04-architecture/17-read-replicas-and-write-scaling.md) — High availability and CQRS data routing.
- [ ] [18-sql-vs-nosql-tradeoffs.md](./04-architecture/18-sql-vs-nosql-tradeoffs.md) — When relational databases fail.
- [ ] [19-cap-theorem-and-eventual-consistency.md](./04-architecture/19-cap-theorem-and-eventual-consistency.md) — The limits of distributed data.

---

## Part V: Real-World Scenarios
#### 📂 `05-scenarios/`

> Architectural system design interviews and practical applications.

- [ ] [20-scenario-ecommerce-schema-design.md](./05-scenarios/20-scenario-ecommerce-schema-design.md) — Designing Products, Orders, Customers, and Inventory.
- [ ] [21-scenario-optimizing-slow-queries.md](./05-scenarios/21-scenario-optimizing-slow-queries.md) — A step-by-step guide to fixing a production bottleneck.
- [ ] [22-scenario-designing-for-high-read-workloads.md](./05-scenarios/22-scenario-designing-for-high-read-workloads.md) — Caching strategies and replica routing.
- [ ] [23-scenario-handling-concurrent-transactions.md](./05-scenarios/23-scenario-handling-concurrent-transactions.md) — Preventing overselling inventory with row-level locks.
- [ ] [24-scenario-disaster-recovery-simulation.md](./05-scenarios/24-scenario-disaster-recovery-simulation.md) — Recovering from a dropped database table.
