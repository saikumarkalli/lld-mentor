# Clean Architecture & DDD — Complete Deep Dive

## Part 1 — Escaping the Monolith Spaghetti

### 1. Plain English Explanation
**WHAT:** Clean Architecture is a way of organizing your folders, projects, and dependencies so that the core business logic (The Domain) is isolated from everything else. It ensures that your business rules do not care if you are using SQL Server or MongoDB, or if you are an ASP.NET Web API or a Desktop app.
**Domain-Driven Design (DDD)** is a methodology used within Clean Architecture. It focuses on modeling the software to match the real-world business (e.g., instead of having a `User` class with 50 properties, you have an `AggregateRoot` that protects its own state).

**WHY:** In a traditional N-Tier architecture (UI -> Business -> Data), the Business layer depends heavily on the Data layer (Entity Framework). If you want to unit test the business logic, you are forced to mock the database. If Microsoft releases a new database technology, rewriting the Data layer breaks the Business layer. 
Clean Architecture uses **Dependency Inversion** to flip the arrows: Everything depends on the Domain, and the Domain depends on nothing.

### 2. Real-World Analogy
Imagine the Constitution of a country (The Domain). 
The Constitution states the absolute rules: "A citizen must be 18 to vote." 
It does not care *how* the votes are recorded (paper ballots vs electronic machines — the Infrastructure). It does not care *who* is asking the question (a news reporter vs a judge — the Presentation API). The Constitution stands completely alone. The voting machines and the reporters adapt to the Constitution, not the other way around.

### 3. The Folder/Project Structure (The "Onion")

A standard .NET Clean Architecture solution is split into 4 separate Projects (Class Libraries):

1. **`Core.Domain` (The Center):** Contains Entities, Value Objects, and Interfaces. Absolutely NO references to Entity Framework, ASP.NET, or JSON. Pure C#.
2. **`Core.Application`:** Contains Business Use Cases (CQRS Handlers). It references `Domain`. It defines Interfaces for external services (e.g., `IEmailSender`).
3. **`Infrastructure`:** Contains the actual implementations. It references `Application`. This is where Entity Framework `DbContext`, `SqlEmailSender`, and API integrations live.
4. **`Presentation` (Web API):** The entry point. Contains Controllers/Minimal APIs. It references `Application` and `Infrastructure` (only for Dependency Injection wiring in `Program.cs`).

### 4. C# .NET 8 Code Example (CQRS with MediatR)

In the Application Layer, we use the **CQRS (Command Query Responsibility Segregation)** pattern via the `MediatR` library to separate read operations from write operations.

```csharp
// ==========================================
// 1. DOMAIN LAYER (Pure C#, No external libraries)
// ==========================================
public class BankAccount 
{
    public Guid Id { get; private set; }
    public decimal Balance { get; private set; }

    // DDD Principle: Protect state. No public setters.
    public void Deposit(decimal amount) 
    {
        if (amount <= 0) throw new ArgumentException("Must be positive.");
        Balance += amount;
    }
}

// ==========================================
// 2. APPLICATION LAYER (Use Cases via MediatR)
// ==========================================
// The Command (The Intent)
public record DepositMoneyCommand(Guid AccountId, decimal Amount) : IRequest<bool>;

// The Handler (The Logic)
public class DepositMoneyHandler : IRequestHandler<DepositMoneyCommand, bool>
{
    private readonly IAccountRepository _repository; // Interface defined in Application layer

    public DepositMoneyHandler(IAccountRepository repository) => _repository = repository;

    public async Task<bool> Handle(DepositMoneyCommand request, CancellationToken ct)
    {
        // 1. Fetch domain object
        var account = await _repository.GetByIdAsync(request.AccountId);
        
        // 2. Execute pure domain logic
        account.Deposit(request.Amount); 
        
        // 3. Save
        await _repository.SaveAsync(account);
        return true;
    }
}

// ==========================================
// 3. INFRASTRUCTURE LAYER (The dirty details)
// ==========================================
public class SqlAccountRepository : IAccountRepository
{
    private readonly AppDbContext _db; // EF Core lives here
    public SqlAccountRepository(AppDbContext db) => _db = db;
    // Implements GetByIdAsync and SaveAsync using EF Core...
}

// ==========================================
// 4. PRESENTATION LAYER (Web API)
// ==========================================
[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AccountsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositMoneyCommand command)
    {
        // The API knows absolutely nothing about the database or the business rules.
        // It just forwards the command to MediatR.
        var success = await _mediator.Send(command);
        return success ? Ok() : BadRequest();
    }
}
```

### 5. Production Relevance: Why use MediatR?
By forcing the API Controller to send a command through MediatR, you create a pipeline. You can write MediatR `IPipelineBehavior` classes that intercept every single command.
Want to add performance logging to all 500 use cases in your app? You write one MediatR Logging Behavior. Want to enforce FluentValidation globally? You write one Validation Behavior. The presentation layer remains completely ignorant of these cross-cutting concerns.

### 6. Architectural Trade-offs

| Architecture | Complexity | Testability | Best For |
| :--- | :--- | :--- | :--- |
| **Traditional N-Tier** | Low | Hard (Tightly coupled) | Small CRUD apps, simple data entry. |
| **Clean Architecture / CQRS** | **High** (Many files/projects) | **Extremely Easy** | Complex enterprise domains, systems built to last 10+ years. |

### 7. Common Mistakes and Misconceptions
- **Mistake:** Putting `[Table("Users")]` or Entity Framework navigation attributes directly on the Domain entities. Doing this pollutes the pure Domain layer with Infrastructure concerns. If you do this, you are no longer doing Clean Architecture. The EF Core configuration must be handled in the Infrastructure layer using `IEntityTypeConfiguration` (Fluent API).
- **Misconception:** "I need Clean Architecture for my 5-table microservice."
  **Reality:** Clean Architecture requires immense boilerplate (Commands, Handlers, Repositories, Interfaces). If your microservice simply reads and writes basic CRUD data to 5 tables with zero complex business rules, Clean Architecture is overkill. A simple Minimal API calling EF Core directly is vastly superior for simple CRUD.

### Mock Interview Block

**Interviewer (Junior):** What is the main goal of Clean Architecture?
**Candidate:** The main goal is to isolate the core business logic (the Domain) from outside concerns like the database, the UI, or third-party APIs. By making the Domain independent, it becomes highly testable and resilient to technology changes.

**Interviewer (Mid):** Explain the Dependency Inversion Principle as it applies to Clean Architecture. How does the Application layer talk to the Database if it can't reference the Infrastructure layer?
**Candidate:** The Application layer defines an interface, like `IAccountRepository`. Because the interface is defined in the Application layer, the Application layer can use it to write business logic. The Infrastructure layer (which references the Application layer) provides the actual SQL implementation of that interface. At runtime, Dependency Injection wires the SQL class to the interface. This inverts the dependency: the database depends on the application's contract, not the other way around.

**Interviewer (Senior):** What is CQRS, and why do we commonly implement it alongside Clean Architecture using libraries like MediatR?
**Candidate:** CQRS stands for Command Query Responsibility Segregation. It dictates that operations that mutate state (Commands) should be strictly separated from operations that read state (Queries). We use MediatR to enforce this pattern by creating isolated Request/Handler classes for every specific use case. This fits perfectly with Clean Architecture because it keeps the Application layer incredibly modular; modifying the logic for "CreateOrder" has zero chance of breaking the logic for "CancelOrder", because they are completely isolated handlers.

**Interviewer (Architect):** A team has implemented strict DDD. Their `Order` Aggregate Root has a method `ProcessOrder()`. Inside this method, they need to verify the customer's payment method via a third-party Stripe API before finalizing the order. They plan to inject the `IStripeService` interface directly into the `Order` entity. Approve or reject this design, and explain why.
**Candidate:** I strongly reject this design. In strict DDD, Domain Entities must remain pure; they represent state and intrinsic business rules, and they should never perform I/O operations or depend on external services. Injecting a service into an entity pollutes the domain.
Instead, this orchestration belongs in the Application Layer (the Command Handler). The Handler should call the `IStripeService`, receive the result, and then pass that result (e.g., a boolean or a transaction ID) as an argument into the `order.ProcessOrder(paymentResult)` method on the pure Domain Entity. This keeps the Domain purely focused on logic and the Application layer focused on orchestration.
