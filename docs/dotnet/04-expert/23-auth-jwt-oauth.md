# Authentication & Authorization — Complete Deep Dive

## Part 1 — The Fundamentals

### 1. Plain English Explanation
- **Authentication (AuthN):** "Who are you?" Proving your identity (e.g., logging in with a username and password, or swiping a badge at the front desk).
- **Authorization (AuthZ):** "What are you allowed to do?" Checking if your identity has the correct permissions (e.g., the front desk confirmed who you are, but the security guard checks your badge to see if you are allowed to enter the Server Room).

In modern APIs, Authentication is usually handled via **JSON Web Tokens (JWT)**. The client logs in once, the server gives them a signed JWT, and the client attaches that token to every subsequent HTTP request.

### 2. Real-World Analogy
- **Logging In:** You show your Passport at the airport check-in desk.
- **The JWT:** The agent gives you a Boarding Pass. The boarding pass contains your name (Claims), your flight number (Permissions), an expiration time, and a cryptographic stamp from the airline (Signature) proving it's not a fake.
- **Authorization:** When you try to board the plane, the gate agent doesn't ask for your Passport again; they just scan the Boarding Pass. If the signature is valid, and the flight number matches, you get on the plane.

---

## Part 2 — JWTs in ASP.NET Core

### 3. C# .NET 8 Code Example

```csharp
// ==========================================
// 1. Program.cs (Configuration)
// ==========================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Over18Only", policy => policy.RequireClaim("Age", "18", "19", "20+"));
});

// VERY IMPORTANT: AuthN must come BEFORE AuthZ in the pipeline
var app = builder.Build();
app.UseAuthentication(); 
app.UseAuthorization();

// ==========================================
// 2. Controller Usage
// ==========================================
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    // ❌ Fails if you don't send a valid JWT in the "Authorization: Bearer <token>" header
    [HttpGet("public")]
    [Authorize] 
    public IActionResult GetGeneralReport() => Ok();

    // ❌ Fails if the JWT doesn't contain the "role: Admin" claim
    [HttpGet("secret")]
    [Authorize(Policy = "RequireAdminRole")]
    public IActionResult GetSecretReport() => Ok();
}
```

### 4. Under the Hood
A JWT is just a Base64-encoded string made of three parts: `Header.Payload.Signature`.
The Payload contains **Claims** (key-value pairs like `"role": "Admin"`). 
When ASP.NET Core receives the HTTP request, the `JwtBearer` middleware intercepts it. It reads the token, decodes the Payload, and uses your server's secret key to mathematically verify the Signature. If a hacker intercepts the token and changes `"role": "User"` to `"role": "Admin"`, the cryptographic signature breaks. The middleware detects the tampering and instantly rejects the request with a `401 Unauthorized`.

### 5. Production Relevance: OAuth2 and OIDC
In enterprise systems, your API rarely generates the JWTs itself. Instead, you delegate Authentication to an **Identity Provider (IdP)** like Auth0, Azure Active Directory, or Okta, using the **OAuth2 / OpenID Connect (OIDC)** protocols. 
Your React frontend redirects the user to the Auth0 login page. Auth0 authenticates the user and gives the React app a JWT. The React app sends that JWT to your .NET API. Your .NET API downloads Auth0's public cryptographic keys on startup and uses them to validate the signature. Your API completely trusts Auth0 without ever seeing the user's password.

### 6. Architectural Trade-offs

| Method | State | Security | Best For |
| :--- | :--- | :--- | :--- |
| **Cookies / Sessions** | Stateful | High (Immune to XSS if HttpOnly). Requires CSRF protection. | Server-rendered apps (MVC, Razor Pages). |
| **JWT (Bearer Tokens)** | Stateless | High, but tokens cannot be easily revoked before expiration. | REST APIs, SPAs (React/Angular), Microservices. |

### 7. Common Mistakes and Misconceptions
- **Mistake:** Storing highly sensitive data (like a credit card number) inside the JWT payload. **JWTs are encoded, not encrypted.** Anyone who intercepts a JWT can paste it into `jwt.io` and read the JSON payload in plain text.
- **Mistake:** Setting JWT expiration times to 30 days so users don't have to log in frequently. If a 30-day token is stolen by a hacker, they have absolute access for 30 days. Because JWTs are stateless, there is no easy way for the server to invalidate a specific token.
- **Solution:** Set the JWT expiration to 15 minutes. Issue a long-lived **Refresh Token** (saved securely in the database). When the 15-minute JWT expires, the frontend sends the Refresh Token to the server to get a new JWT. If a user is banned, the server deletes the Refresh Token from the database, effectively locking them out within 15 minutes.

### Mock Interview Block

**Interviewer (Junior):** What is the difference between Authentication and Authorization?
**Candidate:** Authentication is verifying who the user is, usually via a login mechanism. Authorization happens afterward; it checks what the authenticated user is allowed to do, like checking if they have the "Admin" role before letting them delete a record.

**Interviewer (Mid):** Explain why the order of `app.UseAuthentication()` and `app.UseAuthorization()` in the middleware pipeline is critical.
**Candidate:** `app.UseAuthentication()` must always come first. The authentication middleware is responsible for reading the token, validating it, and populating the `HttpContext.User` object with the user's identity and claims. If `UseAuthorization` runs first, it will look at an empty `HttpContext.User`, determine that you are an anonymous guest, and reject your request with a 401/403, even if you sent a perfectly valid token.

**Interviewer (Senior):** A former employee's account was deactivated in the database at 9:00 AM. However, they were still able to download sensitive files from the API at 9:30 AM. They were using a JWT. Why did this happen, and how do you architect a solution to prevent it?
**Candidate:** This happened because JWT validation is stateless. The .NET API only mathematically verifies the token's cryptographic signature and expiration date; it does not hit the database on every request to check if the user is still active. If their token was issued at 8:50 AM and was valid for 1 hour, the API will accept it until 9:50 AM.
To solve this, we must use short-lived JWTs (e.g., 5 to 10 minutes) combined with Refresh Tokens. When the JWT expires, the client must request a new one using the Refresh Token. During the refresh process, we check the database. Since the employee is deactivated, we deny the refresh, and their access drops within minutes.

**Interviewer (Architect):** We are building a Microservice architecture. The API Gateway authenticates the user via Auth0. The Gateway then routes the request to the Orders Microservice, which then makes an internal HTTP call to the Inventory Microservice. How do we pass the user's identity securely through this chain, ensuring the internal microservices don't have to constantly re-authenticate with Auth0?
**Candidate:** We use a pattern called **Token Forwarding** or **Claims Propagation**. The API Gateway handles the heavy lifting of validating the Auth0 JWT. Once validated, the Gateway strips the external token and generates a highly trusted, internal JWT signed by the Gateway's own private key. This internal token contains the user's claims and is attached to the request sent to the Orders Microservice. The Orders service validates the Gateway's signature, processes its logic, and forwards the exact same internal token down to the Inventory service. This ensures identity is preserved across the entire cluster without redundant Auth0 lookups, while keeping internal services secure from direct external access.
