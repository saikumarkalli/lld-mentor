# Scenario: E-Commerce Schema Design

## 1. The Business Requirements
We are designing the relational database schema for a new online bookstore.
1. Users can register accounts and have multiple shipping addresses.
2. The store has thousands of Books. Books belong to multiple categories (e.g., "Sci-Fi" and "Thriller").
3. Users can add books to a Shopping Cart.
4. Users can Checkout, creating a permanent Order.
5. The price of a Book can change over time, but past Orders must reflect the price the user actually paid at the time of checkout.

## 2. Entity Relationship Analysis

Let's break down the core tables and their relationships (Normalization).

### 1. The Users
- `Users` (Id, Email, PasswordHash, CreatedAt).
- **Relationship:** A user can have many addresses (1-to-Many).
- `Addresses` (Id, UserId, Street, City, ZipCode, IsDefault).

### 2. The Products (Books)
- `Books` (Id, Title, Author, CurrentPrice, ISBN).
- **Relationship:** A Book can have many Categories, and a Category can have many Books. This is a **Many-to-Many (N:M)** relationship. Relational databases cannot do N:M natively. We must use a **Junction Table** (or Associative Entity).
- `Categories` (Id, Name).
- `BookCategories` (BookId, CategoryId). *(Composite Primary Key).*

### 3. The Shopping Cart
- A cart is just a temporary collection of items attached to a user.
- `CartItems` (Id, UserId, BookId, Quantity).
- *(Note: We do not store the Price in the Cart table. We always JOIN to the `Books` table to get the live price, because if the price of a book drops while it's sitting in the user's cart, they should get the new, lower price).*

### 4. The Order (The Critical Snapshot)
This is the most common pitfall in E-Commerce schema design.
An Order is a permanent historical record. 

**The Bad Design:**
`OrderItems` (Id, OrderId, BookId, Quantity).
*Why it fails:* If John bought a book yesterday for $10, and today the admin changes the price in the `Books` table to $15, John's historical receipt will suddenly say he paid $15. 

**The Good Design (Denormalization for History):**
We must intentionally break 3NF to capture the snapshot in time.
- `Orders` (Id, UserId, OrderDate, TotalAmount, ShippingStreet, ShippingCity). *(We copy the address text here. If the user deletes their address from their profile, we still need to know where we shipped the box!).*
- `OrderItems` (Id, OrderId, BookId, Quantity, **UnitPurchasePrice**). *(We copy the exact price at the moment of checkout into this row).*

## 3. Physical Schema Implementation (SQL)

```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Books (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    CurrentPrice DECIMAL(10, 2) NOT NULL CHECK (CurrentPrice >= 0),
    StockQuantity INT NOT NULL DEFAULT 0 CHECK (StockQuantity >= 0)
);

CREATE TABLE Orders (
    Id UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    OrderDate DATETIME2 DEFAULT SYSUTCDATETIME(),
    TotalAmount DECIMAL(10, 2) NOT NULL,
    -- Historical Address Snapshot
    ShippingStreet NVARCHAR(200) NOT NULL,
    ShippingCity NVARCHAR(100) NOT NULL
);

CREATE TABLE OrderItems (
    OrderId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Orders(Id),
    BookId INT NOT NULL FOREIGN KEY REFERENCES Books(Id),
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPurchasePrice DECIMAL(10, 2) NOT NULL, -- Historical Price Snapshot
    PRIMARY KEY (OrderId, BookId) -- Composite PK
);
```

## 4. Indexing Strategy
To ensure the application performs well, we anticipate the most common queries.
1. **User Login:** `SELECT * FROM Users WHERE Email = @email`.
   *Action:* `CREATE NONCLUSTERED INDEX IX_Users_Email ON Users(Email)`. (Though the `UNIQUE` constraint already created an index under the hood!)
2. **User Order History:** `SELECT * FROM Orders WHERE UserId = @userId`.
   *Action:* `CREATE NONCLUSTERED INDEX IX_Orders_UserId ON Orders(UserId) INCLUDE (OrderDate, TotalAmount)`. (Covering index to prevent Key Lookups).

---

## Mock Interview Block

**Interviewer:** In your schema, you chose to use `UNIQUEIDENTIFIER` (Sequential GUID) for the `Users` and `Orders` Primary Keys, but you chose an `INT IDENTITY` for the `Books` Primary Key. Why mix them?
**Candidate:** This is an optimization for Distributed Systems versus localized Read-Heavy tables. 
`Users` and `Orders` are generated continuously by thousands of concurrent users. In a modern distributed architecture (like mobile apps or microservices), we often want the client to generate the `OrderId` (UUID) upfront before hitting the database, to ensure idempotency if the network drops. Sequential GUIDs allow this without causing index fragmentation.
`Books`, however, are entered manually by an admin. The catalog size is relatively small (maybe 100,000 rows). By using an `INT` (4 bytes instead of 16 bytes), the Clustered Index is significantly smaller. Because `BookId` is used as a Foreign Key in `CartItems`, `OrderItems`, and `BookCategories`, using a tiny `INT` drastically reduces the total physical disk size of the entire database and allows more data to fit into the RAM buffer pool, speeding up all JOINs related to the catalog.

**Interviewer:** A user puts a Book in their `CartItems` table. The Book currently costs $20. Tomorrow, the admin runs a massive Black Friday script: `UPDATE Books SET CurrentPrice = CurrentPrice * 0.5`. Walk me through the exact architectural flow of what happens when the user clicks "Checkout" tomorrow, and how your schema handles the price change.
**Candidate:** The user placed the item in their cart at $20. Because my schema properly normalizes the Cart (`CartItems` only stores `BookId` and `Quantity`), the cart acts merely as a pointer. 
When the user goes to the checkout screen tomorrow, the API runs `SELECT c.Quantity, b.CurrentPrice FROM CartItems c JOIN Books b`. The user will happily see their cart now totals $10. 
When they click "Checkout", the C# API opens a SQL Transaction. It calculates the final total ($10), creates the `Orders` row, and creates the `OrderItems` row. Crucially, the API explicitly inserts the $10 value into the `OrderItems.UnitPurchasePrice` column. 
Even if the Black Friday sale ends the next day and the book goes back to $20, the user's historical receipt in `OrderItems` permanently locked in the $10 snapshot.
