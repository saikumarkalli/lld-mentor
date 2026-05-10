# Resilience Patterns (Circuit Breaker, Retry, Bulkhead) — Complete Deep Dive

## Part 1 — Designing for Failure

### 1. Plain English Explanation
**WHAT:** In a monolithic application, if you call a function, it works. In a microservices architecture, you call an API over the network, and it might fail because of a router glitch, a saturated database, or a cloud provider outage.
**Resilience Patterns** are defensive coding strategies that assume the network will fail. They ensure that when Service B goes down, Service A doesn't crash along with it.

**WHY:** Without resilience, microservices suffer from **Cascading Failures**. If the `RecommendationService` is slow, the `ProductService` gets stuck waiting for it. The `ProductService` runs out of threads. The `Gateway` gets stuck waiting for the `ProductService`. Soon, the entire application is offline, all because a non-critical feature (Recommendations) was slow.

### 2. Real-World Analogy
- **Retry Pattern:** You call a plumber. The line is busy. You wait 5 minutes and call again. (Handling temporary glitches).
- **Circuit Breaker Pattern:** You turn on your hairdryer, and it shorts out the wiring. Instead of burning your house down, the electrical panel "trips the breaker," cutting off electricity to that specific room immediately. You have to manually reset it when the danger is gone.
- **Bulkhead Pattern:** A submarine is divided into multiple watertight compartments. If a torpedo blows a hole in compartment 3, the heavy steel doors (Bulkheads) seal it off. Compartment 3 floods, but the rest of the submarine stays dry and doesn't sink.

---

## Part 2 — The Core Patterns (Using Polly in .NET)

### 3. The Retry Pattern
Use when a failure is likely **Transient** (a temporary network blip or a momentary database deadlock).
*Rule:* Never retry instantly. Always use **Exponential Backoff with Jitter**. Retry after 1 second, then 2 seconds, then 4 seconds. Jitter adds random milliseconds so 10,000 failing clients don't all retry at the exact same millisecond and accidentally DDoS the recovering server.

### 4. The Circuit Breaker Pattern
Use when a failure is likely **Systemic** (the database is completely offline).
If the `PaymentService` is down, sending it 5,000 retries per second will only keep it pinned to the floor. 
The Circuit Breaker watches the failure rate. If 50% of requests fail within 10 seconds, the breaker **TRIPS (Opens)**.
- **Open State:** For the next 30 seconds, any code that tries to call the `PaymentService` instantly receives a `BrokenCircuitException` *without making a network call*. This gives the `PaymentService` time to recover.
- **Half-Open State:** After 30 seconds, it lets *one* test request through. If it succeeds, the breaker closes (heals). If it fails, it trips open again.

### 5. The Bulkhead Pattern
If your API Gateway has 100 threads, and an underlying `AvatarImageService` hangs for 30 seconds, all 100 threads will quickly get stuck waiting for avatars. Now users can't reach the `CheckoutService` because there are no threads left.
The Bulkhead pattern isolates resources. You assign a strict limit: "A maximum of 10 threads can be used to call the Avatar service." If the Avatar service hangs, 10 threads get stuck, but the remaining 90 threads are perfectly safe and continue processing checkouts.

### 6. C# .NET 8 Code Example

```csharp
using Polly;
using Polly.CircuitBreaker;

// Registration in Program.cs using Microsoft.Extensions.Http.Resilience
builder.Services.AddHttpClient("InventoryClient", client => 
    client.BaseAddress = new Uri("https://inventory-service"))
    .AddStandardResilienceHandler(options => 
    {
        // 1. Retry: 3 times with exponential backoff
        options.Retry.MaxRetryAttempts = 3;
        
        // 2. Circuit Breaker: Trip if 10% of requests fail. Wait 30s to heal.
        options.CircuitBreaker.FailureRatio = 0.1;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        
        // 3. Timeout: Give up on a single network call if it takes > 2 seconds
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
    });
```

### 7. Architectural Trade-offs: The Fallback Pattern
When the Circuit Breaker trips, you must handle the error gracefully using a **Fallback**.
If the `RecommendationService` is broken, do you show the user a 500 Error? No. You catch the `BrokenCircuitException` and return a hardcoded list of "Top 10 Best Sellers." The user doesn't get personalized recommendations, but they can still buy products. This is called **Graceful Degradation**.

| Pattern | Solves | Danger |
| :--- | :--- | :--- |
| **Retry** | Network blips. | Retrying a heavy database query 5 times can crash the DB. |
| **Circuit Breaker** | Cascading failures. | Hard to tune. If set too sensitive, it trips during normal traffic spikes. |
| **Timeout** | Slow services hoarding threads. | If the timeout is 1 sec, but the backend actually succeeded in 1.1 sec, your systems are now out of sync. |

### 8. Common Mistakes and Misconceptions
- **Mistake:** Implementing Retry patterns on non-idempotent endpoints. If you send a `POST /checkout` request, and the server processes the payment but the network drops before it can send the `200 OK` back to you. If your Retry pattern automatically sends the `POST` again, you just charged the customer's credit card twice. You must only auto-retry `GET` requests, or `POST` requests that are explicitly built with Idempotency Keys.

### Mock Interview Block

**Interviewer (Junior):** What is the purpose of the Circuit Breaker pattern?
**Candidate:** It prevents an application from repeatedly trying to execute an operation that is likely to fail. If a downstream microservice is offline, the circuit breaker detects the high failure rate and trips "open". For a set amount of time, it immediately blocks all requests to that service without making a network call, preventing system lockups and giving the broken service time to recover.

**Interviewer (Mid):** Explain "Exponential Backoff with Jitter" and why it is important when using the Retry pattern.
**Candidate:** If 1,000 clients lose connection to a server, and they all instantly retry exactly 1 second later, they will hit the recovering server simultaneously and crash it again (a Thundering Herd). Exponential backoff spaces the retries out (e.g., 2s, 4s, 8s). Jitter adds random milliseconds to each client's wait time so their retry attempts are scattered smoothly, protecting the server.

**Interviewer (Senior):** A non-critical `ReviewService` in our E-commerce platform is causing the entire application to crash. The `ReviewService` database is deadlocked, so HTTP requests are hanging for 60 seconds. We have a Circuit Breaker configured, but the app is still crashing due to thread exhaustion before the breaker even trips. How do you fix this?
**Candidate:** The Circuit Breaker only trips after requests *fail*. Because the requests are hanging for 60 seconds, they aren't technically failing quickly enough; they are just holding the threads hostage. 
To fix this, we must wrap the Circuit Breaker in a **Timeout Policy** (e.g., 2 seconds). If the `ReviewService` doesn't respond in 2 seconds, the Timeout policy forcefully throws an exception, freeing the thread immediately. This fast failure will quickly trigger the Circuit Breaker threshold, tripping it open and stopping the cascade. I would also add a **Fallback Policy** to simply hide the Review UI components when the breaker is open.

**Interviewer (Architect):** We are designing a highly reliable B2B payment gateway. A client sends a synchronous `POST /transfer` request. Our API Gateway forwards it to the Core Banking Microservice. If the network between the Gateway and the Core Banking service drops right after the packet is sent, the Gateway's Polly policy will throw a Timeout. Should the Gateway automatically retry this request? Defend your architectural decision.
**Candidate:** Absolutely not. Retrying a state-mutating operation (`POST`) on a network timeout is incredibly dangerous because the Gateway does not know *why* the timeout occurred. The Core Banking service might have successfully processed the transfer, but the return packet containing the `200 OK` was dropped by the network router. If the Gateway auto-retries, it will duplicate the financial transfer.
Instead, the Gateway must return a `504 Gateway Timeout` or `500 Internal Error` to the external client. The burden of retry rests on the client, and the Core Banking Microservice *must* be implemented with **Idempotency Keys**. If the client retries the transfer passing the same `Idempotency-Key` header, the Core Banking service recognizes it as a duplicate, ignores the execution, and simply returns the previous success response.
