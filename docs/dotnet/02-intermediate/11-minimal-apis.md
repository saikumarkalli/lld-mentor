# Minimal APIs vs Controllers — Complete Deep Dive

## Part 1 — The Evolution of .NET Routing

### 1. Plain English Explanation
**WHAT:** Minimal APIs are a modern, lightweight way to create HTTP endpoints in .NET. Instead of creating a complex `Controller` class, inheriting from a base class, and decorating methods with routing attributes, you simply map an HTTP verb (like GET or POST) directly to a C# method or lambda function in your `Program.cs`.

**WHY:** Traditional MVC Controllers carry a lot of legacy baggage. They were designed over a decade ago to return HTML views. As microservices became popular, developers wanted a faster, lower-ceremony way to build small APIs without the boilerplate of Controllers. Minimal APIs provide massive performance improvements and lower memory usage.

### 2. Real-World Analogy
- **Controllers (The Bureaucracy):** You want to file a tax form. You have to walk into a giant building, find the specific department (Controller), fill out a cover sheet (Attributes), wait for the receptionist to route you to an available agent (Controller Activation), and then finally hand over your form. It is highly structured, great for massive companies, but slow.
- **Minimal APIs (The Express Window):** You walk up to a drive-thru window labeled "Taxes Here", hand them the form, and drive away. No departments, no receptionists. It is direct and blazing fast.

### 3. C# .NET 8 Code Example

```csharp
// ==========================================
// ❌ Traditional Controller Approach (Heavy)
// ==========================================
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    // 1. Must use Constructor Injection
    public ProductsController(IProductService service) 
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _service.GetByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }
}

// ==========================================
// ✅ Minimal API Approach (Lightweight)
// ==========================================
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IProductService, ProductService>();
var app = builder.Build();

// 1. Define route and logic in one line. 
// 2. Inject dependencies directly into the method signature via [FromServices] or implicitly.
app.MapGet("/api/products/{id}", async (int id, IProductService service) => 
{
    var product = await service.GetByIdAsync(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.Run();
```

### 4. Under the Hood
When an HTTP request hits a **Controller**, ASP.NET Core has to use Reflection and the `IControllerActivator` to dynamically instantiate the Controller class, resolve all of its constructor dependencies (even the ones not used by the specific endpoint), execute action filters, and then invoke the method. This requires memory allocation on the heap.
When an HTTP request hits a **Minimal API**, ASP.NET Core uses Request Delegates. It compiles the lambda expression into a highly optimized delegate at startup. When the request arrives, it simply executes the delegate directly. There is no class instantiation, no constructor resolution overhead, and no filter pipeline overhead (unless explicitly added). This results in near-zero allocation routing.

### 5. Production Relevance
Minimal APIs are the recommended path for building high-performance microservices in .NET 8. However, if you dump 500 `app.MapGet()` calls into `Program.cs`, your code will become an unmaintainable nightmare.
In production, Minimal APIs must be structured using **Endpoint Groups** and extension methods (like the Carter library or the native `MapGroup` feature) to separate endpoints into logical files.

```csharp
// Production structuring of Minimal APIs
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/{id}", GetProduct);
        group.MapPost("/", CreateProduct);
    }

    // Extracted methods for cleanliness
    private static async Task<IResult> GetProduct(int id, IProductService svc) { ... }
}

// In Program.cs:
// app.MapProductEndpoints();
```

### 6. Architectural Trade-offs

| Feature | Controllers | Minimal APIs |
| :--- | :--- | :--- |
| **Performance** | Good. | **Excellent.** Less memory allocation. |
| **Boilerplate** | High. Base classes, constructors. | **Low.** Direct lambda mapping. |
| **Feature Set** | Full MVC pipeline (Action Filters, View Rendering). | Focused strictly on APIs. (Uses Endpoint Filters in .NET 7+). |
| **Best For** | Massive monolithic APIs, legacy migrations, teams heavily accustomed to MVC. | High-throughput microservices, Serverless functions, modern clean architecture. |

### 7. Common Mistakes and Misconceptions
- **Mistake:** Putting heavy business logic directly inside the `app.MapGet()` lambda in `Program.cs`. Minimal APIs are just a routing mechanism. Business logic still belongs in Services or MediatR Handlers.
- **Misconception:** "Minimal APIs can't use filters or authorization." 
  **Reality:** Since .NET 7, Minimal APIs support Endpoint Filters (`.AddEndpointFilter()`) and native authorization (`.RequireAuthorization()`). They are fully capable of enterprise security.

### Mock Interview Block

**Interviewer (Junior):** What is a Minimal API in .NET?
**Candidate:** It is a streamlined way to create HTTP endpoints without using traditional Controller classes. You map routes directly to methods or lambdas in your `Program.cs` file, which requires less boilerplate code.

**Interviewer (Mid):** How does Dependency Injection differ between a Controller and a Minimal API?
**Candidate:** In a Controller, dependencies are injected into the class constructor. This means if the controller has 5 endpoints, and only one endpoint needs a specific service, that service is still resolved every time the controller is instantiated. In Minimal APIs, dependencies are injected directly into the method signature of the endpoint itself. This means dependencies are only resolved exactly when that specific endpoint is hit, which is much more efficient.

**Interviewer (Senior):** Minimal APIs are faster than Controllers. Can you explain mechanically *why* they are faster at the CLR level?
**Candidate:** Controllers require the framework to dynamically allocate the Controller class on the managed heap per-request using `IControllerActivator`. It also has to run through the entire MVC Action Filter pipeline. Minimal APIs compile down to direct `RequestDelegate` execution. There is no class instantiation overhead, fewer heap allocations, and a drastically thinner middleware pipeline, which results in less Garbage Collection pressure and higher throughput.

**Interviewer (Architect):** We are migrating a massive legacy application with 300 Controller endpoints. The team wants to rewrite everything to Minimal APIs for "better performance." As the architect, how do you advise them?
**Candidate:** I would advise against a blanket rewrite. The performance gains of Minimal APIs, while measurable in micro-benchmarks, are largely irrelevant if the application spends 95% of its time waiting on slow SQL database queries. I would only mandate Minimal APIs for new microservices, or for specific hot-path endpoints (like a high-traffic telemetry ingestion route) where routing overhead is actually the bottleneck. For the rest of the monolith, the engineering cost of rewriting 300 controllers, adapting custom Action Filters to Endpoint Filters, and restructuring the project far outweighs the negligible performance benefit.
