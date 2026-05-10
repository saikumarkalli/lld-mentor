# Database Security & RBAC — Complete Deep Dive

## Part 1 — Beyond the Connection String

### 1. Plain English Explanation
**WHAT:** Database Security involves protecting your data at rest, in transit, and at the access layer. **RBAC (Role-Based Access Control)** is the process of creating strict rules about *who* or *what* is allowed to interact with the database, and specifically restricting *which* tables they can read or write to.

**WHY:** In tutorials, developers often connect to the database using the `sa` (System Administrator) or `postgres` (Superuser) account. This is a catastrophic security vulnerability in production. If a hacker finds a SQL Injection vulnerability in your web app, and your web app connects using the `sa` account, the hacker can literally run `DROP DATABASE` or use `xp_cmdshell` to take over the physical Windows/Linux server. 
You must employ the **Principle of Least Privilege**.

### 2. The Core Layers of Database Security

#### Layer 1: Network Isolation
Your database server should **never** have a public IP address. It should reside in a private subnet (VNet/VPC) in the cloud. Only your backend API/Microservices (which reside in the same VNet) should be able to communicate with it. 

#### Layer 2: Authentication (Who are you?)
- **SQL Authentication:** A standard Username and Password stored in the database. (Easily leaked in Git connection strings).
- **Active Directory / Managed Identities (The Gold Standard):** The database relies on Microsoft Entra ID (Azure AD) or AWS IAM. Your C# microservice doesn't use a password. The cloud provider mathematically proves the identity of the server running the C# code and grants it access via tokens. Zero passwords to steal.

#### Layer 3: Authorization & RBAC (What can you do?)
Once connected, what are you allowed to do?
Instead of granting permissions directly to a User, you create a **Role**, grant permissions to the Role, and add the User to the Role.

```sql
-- 1. Create a Role
CREATE ROLE WebApiRole;

-- 2. Grant LEAST privilege to the Role
GRANT SELECT, INSERT, UPDATE ON dbo.Users TO WebApiRole;
GRANT SELECT, INSERT ON dbo.Orders TO WebApiRole;
-- Notice we intentionally DID NOT grant DELETE permissions.

-- 3. Add the application's login to the Role
ALTER ROLE WebApiRole ADD MEMBER [MyAppLogin];
```

### 3. Production Relevance: SQL Injection
SQL Injection occurs when you concatenate user input directly into a SQL string.
*Bad Code:*
`string query = "SELECT * FROM Users WHERE Email = '" + userInput + "'";`
If the user inputs `' OR 1=1; DROP TABLE Users; --`, the database will execute it.

*The Fix (Parameterization):*
Never concatenate. Always use parameters.
`string query = "SELECT * FROM Users WHERE Email = @email";`
When using parameters, the database engine receives the SQL command and the data payload separately. The engine treats the payload strictly as text, neutralizing any malicious SQL commands hidden inside it. Modern ORMs (like EF Core) do this automatically.

### 4. Advanced Security: Row-Level Security (RLS) & Masking
- **Row-Level Security (RLS):** You have a multi-tenant SaaS app. You want to ensure Tenant A can never accidentally query Tenant B's data, even if the C# developer forgets to write `WHERE TenantId = X`. You configure RLS at the database level so the SQL engine invisibly filters the data based on the current user's session context.
- **Dynamic Data Masking:** You have a `CreditCardNumber` column. You want the C# API to read the full number, but you want the BI/Reporting team to only see `XXXX-XXXX-XXXX-1234`. You apply a Masking rule to the column.

### 5. Architectural Trade-offs

| Security Feature | Benefit | Drawback |
| :--- | :--- | :--- |
| **Strict RBAC** | Limits blast radius if the app is hacked. | High administrative overhead for DBAs. |
| **Row-Level Security** | Perfect multi-tenant data isolation. | Can impact query execution performance. |
| **Transparent Data Encryption (TDE)** | Encrypts the raw `.mdf` files on the hard drive. Prevents stolen hard drives from being read. | Adds slight CPU overhead. Does not protect against SQL Injection. |

### 6. Common Mistakes and Misconceptions
- **Mistake:** Granting the `db_owner` role to the Application's connection string. The application only needs to read and write data. `db_owner` allows the application to drop tables, alter schemas, and delete the database. Only CI/CD pipeline service accounts running migrations should have schema-altering permissions.
- **Misconception:** "We use Entity Framework, so we are completely immune to SQL Injection."
  **Reality:** EF Core's LINQ (`.Where(u => u.Name == input)`) is perfectly safe. However, EF Core allows you to run raw SQL via `.FromSqlRaw()`. If a developer concatenates user input into `.FromSqlRaw(query)`, you are instantly vulnerable to SQL Injection, regardless of the ORM.

### Mock Interview Block

**Interviewer (Junior):** What is the "Principle of Least Privilege" in the context of database security?
**Candidate:** The Principle of Least Privilege means an application or user should only be granted the absolute minimum permissions required to do its job. For example, a reporting application should only be granted `SELECT` access, never `INSERT` or `DELETE`, so that if the application is compromised, the attacker cannot destroy the data.

**Interviewer (Mid):** Explain how Parameterized Queries protect an application against SQL Injection.
**Candidate:** SQL Injection happens when a database engine cannot distinguish between the SQL command and the malicious user data because they were concatenated into a single string. Parameterized queries separate the two. The SQL command is sent to the database engine first, where it is compiled. The user input is sent subsequently as a separate data payload. Because the engine has already compiled the command, any SQL syntax hidden in the user payload is treated strictly as literal text, not executable code.

**Interviewer (Senior):** We are deploying a C# microservice to Azure Kubernetes. Currently, the database connection string containing a Username and Password is saved in Azure KeyVault. However, the Security Team wants to completely eliminate passwords from the architecture. How do you architect the database connection to be password-less?
**Candidate:** I would implement **Managed Identities (IAM)**. 
We assign a Managed Identity to the Kubernetes Pod running the microservice. In the SQL Database, we create a Login mapped directly to that specific Azure Active Directory identity, and grant it RBAC permissions. 
The C# application removes the Username/Password from the connection string entirely. When EF Core attempts to connect, the Azure SDK automatically requests a short-lived OAuth token from the Azure metadata service using the Pod's identity, and passes that token to SQL Server. SQL Server verifies the token with Entra ID and grants access. There are zero secrets to rotate and zero passwords to steal.

**Interviewer (Architect):** We are building a multi-tenant healthcare application storing millions of records in a single shared database. A critical compliance requirement is that a developer bug in the C# API (e.g., forgetting to append `WHERE TenantId = ?` to a query) must *never* result in Hospital A seeing Hospital B's patient data. How do you guarantee this isolation at the infrastructure level?
**Candidate:** Relying on application developers to remember a `WHERE` clause in every LINQ query is prone to human error and fails strict compliance audits. 
The architectural solution is **Row-Level Security (RLS)** implemented at the database engine level. 
We create an RLS Security Policy and a Predicate Function in SQL Server. When the C# API opens a database connection, it executes a lightweight command to set the `SESSION_CONTEXT` to the current user's `TenantId`. 
When the C# code executes `SELECT * FROM Patients`, the database engine intercepts the query, reads the `SESSION_CONTEXT`, and invisibly injects `AND TenantId = @ContextId` deep into the execution plan. The isolation is mathematically enforced by the engine itself, making data leakage impossible even if the application code is poorly written.
