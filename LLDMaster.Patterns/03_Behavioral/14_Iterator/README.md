# 14 — Iterator Pattern

> **In one line**: Traverse a collection sequentially without knowing or caring how it's stored internally.

---

## 🧠 What Is It?

Iterator provides a **standard cursor** for sequentially accessing elements of a collection. The caller uses `MoveNext()` / `Current` (or C# `foreach`) without knowing if the underlying storage is a `List`, a database cursor, a file stream, or a computed sequence.

---

## 🎯 The Problem It Solves

A payment system's transaction history can have thousands of records. Multiple callers need different traversals:
- **UI**: paginated — "give me 20 records, then next 20"
- **Audit service**: reverse chronological — most recent first
- **Reconciliation job**: filtered — only `FAILED` transactions
- **Report generator**: high-value filter — `SUCCESS` + amount > ₹5,000

**Without Iterator** — expose the raw `List<Transaction>`:
```csharp
// ❌ Raw collection exposed — callers can mutate, delete, or sort it
public List<Transaction> GetAll() => _transactions; // anyone can _transactions.Clear()!
// Switch to DB-paged queries tomorrow → break every caller that uses list indexing
```

**With Iterator** — expose traversal without internals:
```csharp
// ✅ Each traversal pattern has a name; storage is hidden
foreach (var txn in history.Failed())          { ... } // only failed
foreach (var txn in history.AllReverse())      { ... } // most recent first
foreach (var txn in history.Page(2, 20))       { ... } // page 2, 20 per page
// Swap List for DB cursor internally → callers unchanged
```

---

## 💡 Analogy

A **TV remote's channel buttons**. You press `Channel Up` — you get the next channel. You don't need to know if channels are stored in an array, database, satellite feed, or computed dynamically. The remote (iterator) abstracts the traversal. You can't "edit" the channel list by pressing the remote — just navigate.

---

## 📐 Structure

```
TransactionHistory (Aggregate — owns the collection)
  + All()                → IEnumerable<Transaction>  forward
  + AllReverse()         → IEnumerable<Transaction>  backward
  + Failed()             → IEnumerable<Transaction>  filtered
  + HighValue(threshold) → IEnumerable<Transaction>  filtered
  + Page(n, size)        → IEnumerable<Transaction>  paginated

PaginatedTransactionCursor : IEnumerator<IReadOnlyList<Transaction>>
  + MoveNext() / MovePrev()   ← stateful navigation
  + HasNext / HasPrev         ← UI "load more" support
  + CurrentPage / TotalPages  ← pagination metadata

C# built-in iterator:
  yield return txn; ← compiler generates IEnumerator<T> state machine
```

---

## ✅ When to Use

- **Multiple traversal strategies** over the same collection (forward, reverse, filtered, paginated)
- Want to **hide the storage mechanism** (list today → DB cursor tomorrow → no caller changes)
- Need **lazy evaluation** — don't load all 100,000 records into memory at once
- Support **uniform iteration** via `foreach` across custom types

## ❌ When NOT to Use

- Plain `List<T>` with simple LINQ — no custom iterator needed, LINQ is Iterator already
- **Single-pass, no filtering** — `IEnumerable<T>` and LINQ are sufficient
- **Random access** is needed — Iterator is sequential; use indexing for random access

---

## 🔑 Top Interview Questions

### Q1: "How does C# implement Iterator natively?"

C# uses `IEnumerable<T>` / `IEnumerator<T>` as the Iterator pattern interfaces:
```csharp
// IEnumerator<T>: the cursor
bool MoveNext();       // advance to next element
T Current { get; }    // current element
void Reset();          // back to start

// IEnumerable<T>: the collection that creates a cursor
IEnumerator<T> GetEnumerator(); // factory for the cursor

// foreach compiles to:
var enumerator = collection.GetEnumerator();
while (enumerator.MoveNext()) { var item = enumerator.Current; ... }
```

### Q2: "What is `yield return` and how does it work?"

`yield return` is **compiler magic** that creates a state machine implementing `IEnumerator<T>`:
```csharp
// You write:
public IEnumerable<Transaction> Failed()
{
    foreach (var txn in _transactions)
        if (txn.Status == "FAILED")
            yield return txn; // pause here, resume on next MoveNext()
}

// Compiler generates: a class with fields for local variables and a switch statement
// tracking which line to resume at on each MoveNext() call.
// Key benefit: LAZY — elements are produced one at a time, never all at once.
```

**Why lazy matters**: `history.Failed()` over 1,000,000 records — with `yield return`, only ONE record is in memory at any point. Without it, all 1,000,000 are loaded first.

### Q3: "What's the difference between `IEnumerable<T>` and `IQueryable<T>`?"

| | `IEnumerable<T>` | `IQueryable<T>` |
|---|---|---|
| Where filters run | **In memory** (C# code) | **In the database** (SQL) |
| Mechanism | Iterator / `yield return` | Expression tree → translated to SQL |
| Example | `list.Where(t => t.Status == "FAILED")` | `dbContext.Transactions.Where(t => t.Status == "FAILED")` |
| Performance | Loads all, then filters | DB filters before loading |

This is a very common interview question for .NET roles — know it cold.

### Q4: "How do you implement an async iterator?"

```csharp
// C# 8+ async stream
public async IAsyncEnumerable<Transaction> GetTransactionsAsync(
    [EnumeratorCancellation] CancellationToken ct)
{
    await foreach (var txn in _dbContext.Transactions.AsAsyncEnumerable().WithCancellation(ct))
        yield return txn;
}

// Caller:
await foreach (var txn in history.GetTransactionsAsync(ct))
    await ProcessAsync(txn);
// Streams from DB one row at a time — no List<> in memory
```

---

## 🆚 Common Confusions

**Iterator vs LINQ**: LINQ is a fluent API built **on top of** `IEnumerable<T>` (the Iterator pattern). Every `.Where()`, `.Select()`, `.Skip()` returns a new `IEnumerable<T>` — which is a lazy iterator. LINQ = Iterator pattern with a fluent syntax. Custom Iterator is needed when you need stateful, bidirectional navigation (HasPrev, GoToPage).

**Iterator vs Repository pattern**: Repository pattern defines HOW you query data (abstracts the DB). Iterator defines HOW you traverse results (abstracts the cursor). They work at different levels.

---

## 📂 File in This Folder

**[14_TransactionIterator.cs](14_TransactionIterator.cs)** — Contains:
- `TransactionHistory` — aggregate with 5 traversal methods using `yield return`
- `PaginatedTransactionCursor` — custom `IEnumerator<IReadOnlyList<Transaction>>` with `HasNext/HasPrev/MovePrev`
- Seeded dataset of 7 transactions for demo
- Demo: all → reverse → failed → high-value → paginated → cursor navigation

---

## 💼 FAANG Interview Tip

In C# interviews, `yield return` and `IAsyncEnumerable<T>` are the practical focus — not the raw `IEnumerator` interface:

> *"For streaming large datasets in .NET, I use `IAsyncEnumerable<T>` with `yield return` — the EF Core query streams from the DB one row at a time, never loading all records into memory. For the UI pagination use case, I'd implement a cursor with `HasNext`/`HasPrev` properties so the frontend knows when to show 'Load More'."*

Mentioning memory efficiency and streaming is a senior-engineer signal.
