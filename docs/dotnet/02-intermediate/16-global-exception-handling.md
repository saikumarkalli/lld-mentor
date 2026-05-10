# Global Exception Handling — Complete Deep Dive

## Part 1 — The Problem with Try/Catch

### 1. Plain English Explanation
**WHAT:** Global Exception Handling is a centralized safety net for your application. If a bug in your code throws an error (e.g., dividing by zero, database offline), it prevents the application from crashing and instead returns a polite, standardized error message (JSON) to the client calling your API.

**WHY:** If you wrap every single method in your application with a `try/catch` block, your code becomes incredibly messy, repetitive, and hard to read. Furthermore, if you forget a `try/catch` block just once, the user receives an ugly, unformatted HTML stack trace, exposing sensitive internal system details. Global handling catches *everything* in one single place.

### 2. Real-World Analogy
Imagine a massive hotel.
- **Local Try/Catch:** Every single room has its own mechanic. If a TV breaks, the mechanic in that specific room tries to fix it. If the mechanic is sleeping, the room catches fire. Highly inefficient.
- **Global Exception Handler:** The Front Desk Manager. No matter what room breaks, an alarm instantly alerts the Front Desk. The Manager pauses, apologizes to the specific guest ("Error 500, we'll fix it"), logs the exact details in the hotel registry, and ensures the rest of the hotel keeps running perfectly.

### 3. C# .NET 8 Code Example (The Modern Approach)

Prior to .NET 8, developers used Exception Filters or custom Middleware. .NET 8 introduced the clean `IExceptionHandler` interface.

```csharp
// 1. Create the Global Handler
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        // 1. Log the actual error for the developers
        _logger.LogError(exception, "A catastrophic error occurred.");

        // 2. Create a safe, standardized response for the client (ProblemDetails)
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server Error",
            Detail = "An unexpected error occurred. Our team has been notified."
        };

        // Custom mapping: If it's a domain exception, return 400 instead of 500
        if (exception is ArgumentException)
        {
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Title = "Invalid Input";
            problemDetails.Detail = exception.Message;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        
        // 3. Write the JSON response
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Return true to tell the framework: "I handled this, stop processing the error."
        return true; 
    }
}

// 2. Registration in Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register our custom handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Add ProblemDetails support (standardized error JSON formatting)
builder.Services.AddProblemDetails();

var app = builder.Build();

// Enable the middleware at the VERY TOP of the pipeline
app.UseExceptionHandler(); 

app.MapGet("/api/crash", () => { throw new Exception("Boom!"); });

app.Run();
```

### 4. Under the Hood
When you call `app.UseExceptionHandler()`, ASP.NET Core inserts a massive `try/catch` block around the entire downstream middleware pipeline. If an exception bubbles all the way up to this middleware, it iterates through any registered `IExceptionHandler` classes. If your handler returns `true`, the middleware considers the issue resolved, writes the response to the socket, and prevents the framework from throwing a raw 500 HTML page.

### 5. Production Relevance: ProblemDetails
In enterprise APIs, you cannot return arbitrary error formats (e.g., API A returns `{ error: "bad" }` and API B returns `{ message: "bad", code: 1 }`). 
**ProblemDetails** (RFC 7807) is an industry-standard specification for returning HTTP errors. It guarantees a consistent JSON structure containing `type`, `title`, `status`, and `detail`. The .NET `AddProblemDetails()` system forces your global exception handler to adhere to this global standard, making life infinitely easier for frontend developers consuming your API.

### 6. Architectural Trade-offs

| Approach | Setup | Catch Range | Best Use Case |
| :--- | :--- | :--- | :--- |
| **`IExceptionHandler` (.NET 8+)** | Clean, DI native | Entire Pipeline | **The modern standard.** Use for all new APIs. |
| **Custom Middleware** | Verbose `try/catch` | Entire Pipeline | Legacy .NET 6/7 apps. |
| **Exception Filters** | Ties to MVC | Only Controllers | **Obsolete.** Misses routing & binding errors. |
| **Local Try/Catch** | Clutters code | Method specific | Only use when you intend to *recover* from an error and continue execution, not for HTTP responses. |

### 7. Common Mistakes and Misconceptions
- **Mistake:** Returning the raw Exception Message or Stack Trace in the HTTP Response. This is a massive security vulnerability. Hackers use stack traces to map your internal database structures and framework versions. Always log the real exception internally, but return a generic string to the client.
- **Misconception:** "I don't need `try/catch` blocks anymore because of the Global Handler."
  **Reality:** You don't need `try/catch` blocks for *fatal* errors you want to abort the request. But if you call a non-critical third-party analytics API that times out, you *must* use a local `try/catch` to swallow the error so the user's primary checkout flow can still succeed. Global handlers are a safety net, not control flow.

### Mock Interview Block

**Interviewer (Junior):** What is the main benefit of using a Global Exception Handler?
**Candidate:** It prevents the application from crashing and ensures that all unhandled errors are caught in one central location. This removes the need to write repetitive `try/catch` blocks in every single controller method, keeping the code clean and ensuring the client always receives a properly formatted error response.

**Interviewer (Mid):** Why should you avoid using an MVC `ExceptionFilter` for global error handling?
**Candidate:** An `ExceptionFilter` only executes within the MVC controller pipeline. If an exception is thrown earlier in the request lifecycle—such as during authentication, CORS evaluation, or model binding—the filter will completely miss it. `IExceptionHandler` or Exception Middleware wraps the entire pipeline, guaranteeing 100% coverage.

**Interviewer (Senior):** What is the "ProblemDetails" standard, and why does .NET explicitly support it?
**Candidate:** ProblemDetails is an RFC-standardized JSON structure for representing HTTP API errors. Instead of every developer inventing their own error JSON format, ProblemDetails enforces standard fields like `status`, `title`, and `detail`. .NET supports it natively via `AddProblemDetails()`, which ensures that whether a request fails due to a model binding error, a 404, or an unhandled 500 exception, the API consumer receives a highly predictable, standardized JSON shape to parse.

**Interviewer (Architect):** We are implementing a distributed system where an API call might fail deep inside a domain layer with a custom `InsufficientFundsException`. How do you architect the exception handling to translate deep domain exceptions into appropriate HTTP status codes (like 402 Payment Required) without coupling the domain layer to ASP.NET Core HTTP concerns?
**Candidate:** The domain layer must remain pure; it throws the `InsufficientFundsException` completely unaware of the web context. At the API boundary, I implement an `IExceptionHandler`. This handler acts as the translator. It uses a mapping dictionary or pattern matching to map specific Domain Exception types to specific HTTP Status Codes (e.g., `InsufficientFundsException` -> `402`, `ResourceNotFoundException` -> `404`). It formats the domain exception's message into a `ProblemDetails` object and returns it. This keeps the core domain pristine while providing semantically correct REST responses.
