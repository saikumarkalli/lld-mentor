# Scenario: Monolith Migration (The Strangler Fig Pattern)

## 1. The Business Problem
A company has a highly profitable, 10-year-old E-Commerce application. It is a single massive C# Monolith connected to a single 5TB SQL Server database.
**The Pain Points:**
- **Deployment Fear:** Deploying a simple CSS change requires rebooting the entire system, causing 5 minutes of downtime. They only deploy at 2:00 AM on Sundays.
- **Tangled Code:** If the "Shipping" team updates a library, it breaks the "Billing" team's code. 
- **The Mandate:** The CTO orders a rewrite to microservices so teams can deploy independently.

## 2. The Anti-Pattern (The Big Bang Rewrite)
The team decides to build the new Microservices system from scratch in secret. They estimate it will take 6 months. During this time, they stop adding features to the Monolith.
**What Actually Happens:**
1. 6 months turns into 2 years.
2. The business gets angry because no new features are being released.
3. They finally flip the switch to the new microservices system. It instantly crashes under production load because it was never tested with real users. The business loses millions. They rollback to the Monolith in shame.

## 3. The Solution: The Strangler Fig Pattern

The Strangler Fig is a vine that grows up an existing tree. Over time, it completely covers the host tree until the host tree dies, leaving only the vine standing in the exact shape of the original tree.

We apply this to software: We slowly replace the Monolith piece by piece, while both are running in production simultaneously.

### Step 1: Put a proxy in front
We put an API Gateway (like YARP or Nginx) in front of the Monolith. 
The Gateway is configured with one simple rule:
`/*  ---> Route to Monolith`
Everything still works exactly as it did yesterday. The users notice nothing.

### Step 2: Extract one Bounded Context
We pick the *easiest, least critical* domain to migrate first. Let's pick `User Profiles`.
We build the new `Profile Microservice`. We give it its own empty Database.

### Step 3: Synchronize the Data
We cannot just turn the new service on, because the existing 5 million users are still in the Monolith's database.
We write a one-time migration script to copy the 5 million users into the new `Profile Microservice` database.
*Wait, what if a user updates their profile on the Monolith while we are testing the new service?*
We implement **Change Data Capture (CDC)** or trigger events in the Monolith to constantly stream any profile updates into the new Microservice database. The new database is now perfectly synchronized with the Monolith in real-time.

### Step 4: Route Traffic (The Strangulation)
We update the API Gateway:
`/api/profiles/* ---> Route to Profile Microservice`
`/*             ---> Route to Monolith`

Now, when a user clicks "My Profile", the request hits the new Microservice. If they click "Checkout", it hits the Monolith. 
**Crucial Benefit:** If the new `Profile Microservice` crashes, we just revert the Gateway routing rule back to the Monolith. Zero risk.

### Step 5: Delete the Monolith Code
Once the `Profile Microservice` has been running flawlessly for a month, we delete the "Profile" C# code from the Monolith codebase. We drop the "Profile" tables from the Monolith database. 
The Monolith is now 5% smaller.

### Step 6: Repeat
We repeat this process for `Inventory`, then `Shipping`, then `Billing`. 
Two years later, the API Gateway is routing 100% of traffic to 20 different microservices. The Monolith receives zero traffic. We turn the Monolith server off. We successfully migrated without a single minute of downtime.

---

## Mock Interview Block

**Interviewer:** You are leading the migration of a massive legacy monolith to microservices. The monolith uses a single, highly relational database where `Orders` have foreign keys to `Users`. You are extracting the `UserService` first. How do you handle the fact that the monolith's `Orders` table still needs to JOIN against the `Users` table, but the `Users` table is being moved to a completely different database?
**Candidate:** This is the "Data Strangling" problem. We cannot break the monolith's `Orders` table overnight. 
The solution is to use a **Materialized View** or an **Event-Driven Projection**. 
When we build the new `UserService`, it becomes the sole source of truth for user data. However, the `UserService` must publish an event (e.g., `UserUpdated`) to a message broker every time a user changes.
We modify the legacy Monolith to listen to these events. The Monolith maintains a read-only, localized copy of the `Users` table in its own database. The monolith's `Orders` logic can continue doing SQL JOINs against this local read-only copy. The monolith doesn't care that the real "write" operations are happening in the new microservice. This allows us to extract the service safely while giving the `Orders` team time to eventually refactor their own code in the future.
