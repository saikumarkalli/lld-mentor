# Scenario: Cascade Failure Recovery (Post-Mortem)

## 1. The Incident

**System Context:** A streaming video platform (like Netflix).
- **API Gateway:** Routes public traffic.
- **Video Service:** Streams the video files.
- **Recommendation Service:** Uses heavy Machine Learning to show "You might also like...".
- **User Service:** Manages user profiles.

**The Timeline of the Crash:**
* 8:00 PM (Friday Night): Traffic spikes by 300%.
* 8:05 PM: The `Recommendation Service` CPU hits 100% due to the complex ML algorithms. Its response time jumps from 50ms to 15 seconds.
* 8:07 PM: The `Video Service` is programmed to fetch recommendations to show at the bottom of the video player. It makes a synchronous HTTP call to the `Recommendation Service`.
* 8:08 PM: Because the `Recommendation Service` takes 15 seconds to reply, the `Video Service`'s internal HTTP threads get stuck waiting. Within 60 seconds, all 500 available threads on the `Video Service` are blocked.
* 8:09 PM: The `Video Service` stops responding to all incoming requests, including the requests to actually stream video.
* 8:10 PM: The `API Gateway` gets stuck waiting for the `Video Service`. The Gateway runs out of threads. The entire website goes offline.

**The Result:** A global outage. Millions of users cannot watch videos, all because a non-critical feature ("You might also like") was slow. This is a classic **Cascading Failure**.

## 2. The Post-Mortem Analysis

Why did the system fail?
1. **Synchronous Coupling:** The `Video Service` made a blocking HTTP call to a non-critical service.
2. **Missing Timeouts:** The HTTP client waited infinitely (or for the 100-second default) for a response, hoarding precious threads.
3. **Missing Circuit Breakers:** The system continued to send thousands of requests to a service that was obviously dead, ensuring it could never recover.
4. **Missing Bulkheads:** A failure in the "Recommendation" feature was allowed to consume threads needed for the "Streaming" feature.

## 3. The Architecture Fix (Preventing the next crash)

How do we architect the system so that next Friday night, if the `Recommendation Service` catches fire, the users can still watch videos?

### Fix 1: Implement Strict Timeouts
We configure the `HttpClient` in the `Video Service` with a hard **2-second timeout**. 
If the `Recommendation Service` doesn't reply in 2 seconds, the connection is instantly severed. The thread is freed up to process the next user's video stream.

### Fix 2: Implement the Circuit Breaker Pattern
We wrap the HTTP call in a Polly Circuit Breaker.
If 20% of requests to the `Recommendation Service` fail or timeout within a 10-second window, the breaker **Trips Open**.
For the next 60 seconds, the `Video Service` will not even attempt to make a network call to the `Recommendation Service`. It instantly throws a `BrokenCircuitException`. This prevents the `Video Service` from wasting threads, and it gives the `Recommendation Service` 60 seconds of zero traffic to catch its breath, scale up new pods, and recover its CPU.

### Fix 3: Graceful Degradation (Fallback)
When the Circuit Breaker is tripped, what does the user see? Do we show them a giant 500 Error screen? No.
We implement a **Fallback Policy**. When the `BrokenCircuitException` is caught, the code catches it and returns a hardcoded, cached list of the "Top 10 Most Popular Videos Globally." 
The user doesn't get personalized recommendations, but the page renders instantly, and they can still watch videos. The failure is completely invisible to the user.

### Fix 4: Architectural Decoupling (UI Composition)
Why is the `Video Service` calling the `Recommendation Service` at all? This is bad boundary design. The `Video Service` should only stream video.
We move the composition to the **API Gateway (or frontend UI)**. 
The Web UI makes two parallel, asynchronous calls:
1. `GET /api/video/123`
2. `GET /api/recommendations/123`
If call #2 hangs or fails, the Web UI simply hides the "Recommendations" HTML `<div>`. The backend microservices are no longer coupled together at all.

---

## Mock Interview Block

**Interviewer:** In the post-mortem, we identified that the `Video Service` ran out of threads because they were all waiting on the `Recommendation Service`. Besides Timeouts and Circuit Breakers, what architectural pattern specifically isolates resources to prevent one feature from starving the rest of the application?
**Candidate:** The **Bulkhead Pattern**. Just like a submarine uses bulkheads to prevent a leak in one room from sinking the whole ship, we can divide our application's thread pool or connection pool. We configure the `Video Service` so that a maximum of, say, 20% of its threads are allowed to be used for calling the `Recommendation Service`. If the recommendations hang, those 20% of threads get stuck, but the remaining 80% of threads are fiercely protected and remain available to serve the core video streaming functionality.

**Interviewer:** You implement a Circuit Breaker, and it successfully trips when the `Recommendation Service` gets overwhelmed. However, 60 seconds later, the breaker enters the "Half-Open" state and lets a few requests through to test if the service has recovered. The service instantly crashes again. Why does this "yo-yo" effect happen in microservices, and how do you prevent it?
**Candidate:** This happens because the `Recommendation Service` is being bombarded by requests that were queued up or retried by thousands of clients during the 60-second open period. The moment the breaker allows traffic, a massive "Thundering Herd" hits the recovering service, instantly overwhelming its CPU again.
To prevent this, we must implement **Exponential Backoff with Jitter** on all client Retry policies. This ensures that clients don't all retry at the exact same millisecond. Additionally, we should ensure the `Recommendation Service` has aggressive Auto-Scaling configured in Kubernetes, so that during the 60-second breather, the infrastructure spins up 10 new pods to handle the incoming herd before the breaker tests the connection again.
