# LLD — Concept Bridge

> This section **does not duplicate code**. It explains the *why* behind each implementation in the LLD projects and links directly to code.

---

## OOP Concepts

| Pillar | Code Location | Key Insight |
|--------|--------------|-------------|
| Encapsulation | [`LLDMaster.OOP/1.Encapsulation/`](../../LLDMaster.OOP/1.Encapsulation/) | Hide state, expose behavior |
| Abstraction | [`LLDMaster.OOP/2.Abstraction/`](../../LLDMaster.OOP/2.Abstraction/) | Define contracts, hide implementation |
| Inheritance | [`LLDMaster.OOP/3.Inheritance/`](../../LLDMaster.OOP/3.Inheritance/) | Reuse behavior, model "is-a" relationships |
| Polymorphism | [`LLDMaster.OOP/4.Polymorphism/`](../../LLDMaster.OOP/4.Polymorphism/) | Same interface, different behavior |

> Domain examples: Bank, Hospital, Payment → [`LLD.OOPs.Concepts/`](../../LLD.OOPs.Concepts/)

---

## SOLID Principles

| Principle | What It Prevents | Code Reference |
|-----------|-----------------|----------------|
| SRP | God classes | [`LLDMaster.SOLID/PaymentModule/`](../../LLDMaster.SOLID/PaymentModule/) |
| OCP | Modification cascades | ↑ same |
| LSP | Broken inheritance hierarchies | ↑ same |
| ISP | Fat interfaces | ↑ same |
| DIP | Tight coupling to concretions | ↑ same |

---

## Design Patterns

### Creational (How objects are created)

| Pattern | Problem Solved | Code |
|---------|---------------|------|
| Singleton | One instance globally | [`01_Creational/01_Singleton/`](../../LLDMaster.Patterns/01_Creational/01_Singleton/) |
| Factory Method | Decouple object creation | [`01_Creational/02_FactoryMethod/`](../../LLDMaster.Patterns/01_Creational/02_FactoryMethod/) |
| Abstract Factory | Families of related objects | [`01_Creational/03_AbstractFactory/`](../../LLDMaster.Patterns/01_Creational/03_AbstractFactory/) |
| Builder | Step-by-step complex objects | [`01_Creational/04_Builder/`](../../LLDMaster.Patterns/01_Creational/04_Builder/) |
| Prototype | Clone existing objects | [`01_Creational/05_Prototype/`](../../LLDMaster.Patterns/01_Creational/05_Prototype/) |

### Structural (How objects are composed)

| Pattern | Problem Solved | Code |
|---------|---------------|------|
| Adapter | Incompatible interface bridge | [`02_Structural/06_Adapter/`](../../LLDMaster.Patterns/02_Structural/06_Adapter/) |
| Bridge | Decouple abstraction from implementation | [`02_Structural/07_Bridge/`](../../LLDMaster.Patterns/02_Structural/07_Bridge/) |
| Composite | Tree structures, uniform treatment | [`02_Structural/08_Composite/`](../../LLDMaster.Patterns/02_Structural/08_Composite/) |
| Decorator | Add behavior without subclassing | [`02_Structural/09_Decorator/`](../../LLDMaster.Patterns/02_Structural/09_Decorator/) |
| Facade | Simplified entry point | [`02_Structural/10_Facade/`](../../LLDMaster.Patterns/02_Structural/10_Facade/) |

### Behavioral (How objects communicate)

| Pattern | Problem Solved | Code |
|---------|---------------|------|
| Observer | Decouple publishers from subscribers | [`03_Behavioral/11_Observer/`](../../LLDMaster.Patterns/03_Behavioral/11_Observer/) |
| Strategy | Swap algorithms at runtime | [`03_Behavioral/12_Strategy/`](../../LLDMaster.Patterns/03_Behavioral/12_Strategy/) |
| Command | Encapsulate requests as objects | [`03_Behavioral/13_Command/`](../../LLDMaster.Patterns/03_Behavioral/13_Command/) |
| Iterator | Sequential access without exposing internals | [`03_Behavioral/14_Iterator/`](../../LLDMaster.Patterns/03_Behavioral/14_Iterator/) |
| Template Method | Define algorithm skeleton, defer steps | [`03_Behavioral/15_TemplateMethod/`](../../LLDMaster.Patterns/03_Behavioral/15_TemplateMethod/) |

---

## 🧩 Pattern → Real-World Mapping

| Pattern | Where you see it in production |
|---------|-------------------------------|
| Singleton | DI containers, configuration providers |
| Factory | `IHttpClientFactory`, logger factories |
| Builder | `WebApplicationBuilder`, `StringBuilder` |
| Decorator | Middleware pipeline in ASP.NET Core |
| Observer | `INotificationHandler` in MediatR, domain events |
| Strategy | Payment processors, sorting algorithms |
| Command | MediatR commands, CQRS write side |
