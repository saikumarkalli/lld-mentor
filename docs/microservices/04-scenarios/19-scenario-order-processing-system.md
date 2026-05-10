# Scenario: Designing an Order Processing System

## 1. The Business Requirements
We are designing the backend for an E-Commerce platform.
1. Users must be able to view their cart and click "Checkout".
2. The system must verify inventory stock.
3. The system must charge the customer's credit card via Stripe.
4. The system must notify the warehouse to ship the item.
5. The system must be highly available (if the warehouse is offline, customers can still place orders).
6. The system must never charge a credit card if the item is out of stock.

## 2. High-Level Architecture
We will design an **Event-Driven Microservices Architecture** using the **Saga Pattern (Orchestration)** to manage the distributed transaction, and the **Outbox Pattern** to guarantee message delivery.

**Components:**
1. **API Gateway:** The public entry point.
2. **Order Service (The Orchestrator):** Manages the Saga state machine.
3. **Inventory Service:** Manages stock levels.
4. **Payment Service:** Integrates with Stripe.
5. **Shipping Service:** Integrates with the warehouse.
6. **RabbitMQ / Kafka:** The message broker handling asynchronous communication.

## 3. The Happy Path (Sequence of Events)

1. **Client Request:** The mobile app sends `POST /api/orders` to the API Gateway.
2. **Order Creation:** The Gateway routes to the `Order Service`. The Order Service saves the order to its database with `Status = Pending`. 
3. **The Outbox:** In the exact same SQL transaction, the Order Service writes a `ProcessOrderCommand` to its local Outbox table. It immediately returns `202 Accepted` to the mobile app. (Requirement 5 satisfied: Fast, highly available checkout).
4. **Saga Orchestration Step 1 (Inventory):** The Outbox relay pushes the command to RabbitMQ. The `Order Service` Saga State Machine transitions to `WaitingForInventory`. It sends a `ReserveStockCommand` to the `Inventory Service`.
5. **Inventory Reserved:** The `Inventory Service` deducts the stock, saves it, and publishes an `InventoryReservedEvent`.
6. **Saga Orchestration Step 2 (Payment):** The `Order Service` hears the event. It transitions to `WaitingForPayment`. It sends a `ChargeCardCommand` to the `Payment Service`.
7. **Payment Success:** The `Payment Service` hits Stripe, succeeds, and publishes a `PaymentSucceededEvent`.
8. **Saga Orchestration Step 3 (Shipping):** The `Order Service` hears the event. It transitions to `WaitingForShipping`. It sends a `ShipOrderCommand` to the `Shipping Service`.
9. **Finalization:** The `Shipping Service` accepts the command. The `Order Service` marks the Order as `Completed`.

## 4. The Failure Path (Compensating Transactions)

What happens if the `Payment Service` tries to charge the card in Step 6, but the customer's credit card is declined? (Satisfying Requirement 6).

1. **Payment Fails:** The `Payment Service` publishes a `PaymentFailedEvent`.
2. **Saga Reaction:** The `Order Service` State Machine hears the failure. It realizes the Saga must be aborted.
3. **Compensating Action:** The `Order Service` looks at what has already been done. Inventory was reserved in Step 5. The `Order Service` sends a `ReleaseStockCommand` to the `Inventory Service`.
4. **Inventory Restored:** The `Inventory Service` adds the stock back to the shelf.
5. **Finalization:** The `Order Service` marks the Order as `Cancelled`. The system is back in a perfectly consistent state.

## 5. Handling Network Failures (Idempotency)

What happens if the `Order Service` sends the `ChargeCardCommand` to the `Payment Service`, the card is successfully charged, but RabbitMQ crashes before the `PaymentSucceededEvent` can be delivered back to the `Order Service`?

1. The `Order Service` Saga has a Timeout configured (e.g., 5 minutes).
2. After 5 minutes, the `Order Service` assumes the payment failed (or got lost) and **retries** the exact same `ChargeCardCommand`.
3. The `Payment Service` receives the command again. Because the `Payment Service` was built using **Idempotency** (using the `OrderId` as the Idempotency Key), it checks its database, sees it already charged this Order, safely skips the Stripe API call, and republishes the `PaymentSucceededEvent`.
4. The `Order Service` receives the event and the Saga continues normally. No double charges occur.

---

## Mock Interview Block

**Interviewer:** In this design, the `Order Service` returns a `202 Accepted` to the frontend immediately after saving the "Pending" order to the database, before the credit card is even charged. How does the frontend know if the order actually succeeded or failed?
**Candidate:** Because the architecture is completely asynchronous, we must implement a mechanism to notify the client when the eventual consistency is resolved. There are two standard approaches:
1. **Client Polling:** The frontend occasionally makes a `GET /api/orders/{id}` request to check if the status has changed from "Pending" to "Completed" or "Cancelled".
2. **Server-Sent Events (WebSockets / SignalR):** The frontend opens a WebSocket connection to a `Notification Service`. When the `Order Service` completes the Saga (success or failure), it publishes an `OrderFinalizedEvent`. The `Notification Service` consumes this event and pushes the result directly down the open WebSocket to the user's browser, providing a real-time, reactive UI experience.

**Interviewer:** Why did you choose Saga Orchestration instead of Saga Choreography for this Order Processing system?
**Candidate:** Order processing is a complex, multi-step business workflow with strict failure conditions and compensating transactions. If we used Choreography, the business logic of "what happens when a payment fails" would be scattered across the Inventory and Shipping services. By using Orchestration, the `Order Service` acts as the central brain. We can look at a single State Machine class in the `Order Service` code and instantly understand the entire business flow, making it vastly easier to debug, maintain, and monitor.
