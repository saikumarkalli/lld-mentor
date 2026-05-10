# Microservices Security (JWT & mTLS) — Complete Deep Dive

## Part 1 — The Perimeter is Dead (Zero Trust)

### 1. Plain English Explanation
**WHAT:** In the old days of monoliths, security was like a medieval castle. The firewall (the moat) kept the bad guys out. Inside the castle, all the code trusted each other perfectly. 
In a Microservices architecture, the castle model is incredibly dangerous. If a hacker breaches one tiny, insignificant microservice (e.g., the PDF Generator service), they are now "inside" the network. If internal services implicitly trust each other, the hacker can use the PDF service to request full customer credit card data from the Billing service.
**Zero Trust Architecture** means exactly what it says: Service A does not trust Service B just because they are on the same network. Every single network call must be authenticated and authorized.

### 2. Real-World Analogy
- **Castle Security (The Old Way):** You badge into a corporate office building. Once you are past the front desk, you can walk into any meeting room, open any unlocked filing cabinet, and sit at any desk. If a thief steals a badge, they have access to the whole company.
- **Zero Trust (Microservices):** You badge into the building. But every single hallway has a laser scanner. Every single filing cabinet requires a fingerprint. Even the employees who have worked there for 20 years must scan their badge every time they open a door. A stolen badge only gets the thief into the lobby.

### 3. Securing User Identity: JWT Propagation

When a user logs in via the browser, they get a JSON Web Token (JWT). The browser sends the JWT to the API Gateway.
How does the `InventoryService`, which is three network hops deep, know who the user is?

**The Anti-Pattern (Implicit Trust):**
The API Gateway validates the JWT. It strips the token off, and makes an HTTP call to the Order Service: `GET /orders?userId=123`. The Order service trusts the Gateway blindly. A hacker inside the network could forge a request `GET /orders?userId=1`.

**The Solution (JWT Propagation):**
The API Gateway validates the token. When it calls the Order Service, it attaches the exact same `Authorization: Bearer <JWT>` header. The Order Service mathematically validates the signature of the JWT itself. When the Order Service calls the Inventory Service, it forwards the token again. Every service independently verifies the cryptographic signature of the token. 

### 4. Securing Machine Identity: Mutual TLS (mTLS)

JWTs prove who the *human user* is. But how does the Billing Service prove that the HTTP request it just received actually came from the official Order Service, and not from a compromised hacker container running on the same Kubernetes cluster?

**mTLS (Mutual Transport Layer Security)** solves this.
In standard TLS (HTTPS), the client verifies the server's certificate. (The browser proves Google.com is actually Google). 
In **Mutual TLS**, the server also verifies the client's certificate. 
- The Order Service presents a cryptographic certificate proving it is the Order Service.
- The Billing Service presents its certificate.
- They establish a heavily encrypted tunnel. If the hacker container tries to call the Billing Service without the highly guarded internal Order Service certificate, the connection is instantly rejected at the TCP level.

### 5. Production Relevance: Service Mesh (Istio)
Managing certificates for 50 microservices and rotating them every 30 days is an operational nightmare. Developers should *never* write C# code to manage mTLS certificates.
In production, you use a **Service Mesh (like Istio or Linkerd)**.
Istio injects a tiny proxy (Envoy) next to every single microservice container. 
Your C# code makes a plain, unencrypted HTTP call to `http://billing-service`. 
The Envoy proxy intercepts the call, wraps it in mTLS using automatically rotated certificates, sends it securely over the network, and the receiving Envoy proxy decrypts it before handing it to the Billing C# code. The developers do nothing, but the network is 100% Zero-Trust encrypted.

### 6. Architectural Trade-offs

| Security Mechanism | What it protects | Performance Impact | Complexity |
| :--- | :--- | :--- | :--- |
| **API Gateway Auth Only** | Protects the public perimeter. | Negligible | Low |
| **JWT Propagation** | Protects user data across internal hops. | Low (CPU Signature validation) | Medium (Must pass headers) |
| **mTLS (via Service Mesh)** | Protects machine-to-machine traffic. | Medium (Encryption overhead) | **High** (Requires K8s expertise) |

### 7. Common Mistakes and Misconceptions
- **Mistake:** Passing the public JWT (from Auth0/Okta) deep into the internal network. The public JWT might lack internal claims (like internal database IDs) and might be too large. 
- **Solution (Token Exchange):** The API Gateway validates the public JWT. It then uses an internal Identity Provider to exchange the public JWT for an *internal* JWT. This internal token contains rich, trusted internal data (like `TenantId = 5`) and is signed by a private key that only the internal microservices know. This token is propagated downwards.

### Mock Interview Block

**Interviewer (Junior):** What does "Zero Trust" mean in a microservices architecture?
**Candidate:** Zero Trust means that we do not assume a network request is safe just because it originates from inside our own internal network. Every single request, even between two of our own internal microservices, must be explicitly authenticated and authorized.

**Interviewer (Mid):** If the API Gateway successfully validates a user's JWT, why should the internal microservices also validate it?
**Candidate:** If internal services rely entirely on the API Gateway for security, it creates an "Implicit Trust" vulnerability. If a hacker breaches any internal service, they can bypass the Gateway and make unauthenticated requests to other services. By using JWT Propagation, every internal microservice cryptographically validates the token themselves, ensuring defense-in-depth.

**Interviewer (Senior):** How do you secure background asynchronous processes? If the Order Service drops a message onto a RabbitMQ queue, there is no HTTP header for the Inventory Service to read the JWT from. How does the Inventory Service know who initiated the action?
**Candidate:** Standard HTTP headers don't exist in message brokers, but message brokers support metadata or "Message Headers". When the Order Service publishes the event to RabbitMQ, it extracts the `UserId` and specific Claims from the current HTTP Context's JWT, and injects them as metadata properties into the RabbitMQ message envelope. When the Inventory Service consumes the message, it reads the metadata envelope, reconstructs a generic `ClaimsPrincipal`, and sets it on its local execution thread so its internal authorization logic functions correctly.

**Interviewer (Architect):** The security team mandates that all machine-to-machine traffic within our Kubernetes cluster must be encrypted, and services must verify each other's identities to prevent internal spoofing. A developer proposes modifying all 40 C# microservices to load X.509 certificates from Azure KeyVault and configure Kestrel to enforce HTTPS. Approve or reject this proposal.
**Candidate:** I strongly reject this proposal. Managing, distributing, and rotating X.509 certificates inside application code across 40 microservices is an operational nightmare and highly prone to developer error (e.g., forgetting to rotate an expiring certificate, bringing down the system).
Instead, I would implement a Service Mesh like Istio. Istio abstracts the network layer away from the application. It automatically injects sidecar proxies that enforce Mutual TLS (mTLS) between all pods, handles certificate generation, and automatically rotates them. The C# developers continue writing plain HTTP code, remaining completely ignorant of the underlying cryptography, while the security team's compliance mandate is perfectly satisfied at the infrastructure level.
