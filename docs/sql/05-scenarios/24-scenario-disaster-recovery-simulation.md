# Scenario: Disaster Recovery Simulation (Post-Mortem)

## 1. The Incident
**System Context:** A B2B Financial Platform.
**The Timeline:**
- **2:00 AM:** The nightly Full Backup runs successfully.
- **8:00 AM:** Employees log in and begin processing financial trades. Transaction Log backups are running every 15 minutes.
- **11:32 AM:** A disgruntled DBA with excessive privileges connects directly to the Production database via Management Studio. They highlight the `Trades` table and execute `DROP TABLE Trades`.
- **11:33 AM:** The C# application begins throwing thousands of `SqlException: Invalid object name 'Trades'`. The platform goes completely offline.

## 2. The Initial Panic (The Wrong Way)
The engineering team panics. 
The Junior DBA says: "We have the 2:00 AM Full Backup! Let's restore it immediately!"
**The Architect steps in:** "If you restore the 2:00 AM backup over the live database, you will permanently erase every single financial trade made between 8:00 AM and 11:32 AM. We will lose millions of dollars. Do not touch the production server."

## 3. The Recovery Plan (Point-in-Time Restore)

Because the architect properly configured the database in the **Full Recovery Model** and scheduled 15-minute Transaction Log backups, the data is not lost. The team must perform a **Point-in-Time Restore**.

### Step 1: The "Tail-Log" Backup
Even though the table is gone, the database is still running, and the active Transaction Log (the `.ldf` file) contains the exact record of the `DROP TABLE` command, as well as any trades that occurred right before it.
The very first action is to run a **Tail-Log Backup**. This backs up the current, active log file *right now* (11:35 AM) and safely shuts down the database by putting it into the `RESTORING` state so no new connections can be made.

### Step 2: Restore the Full Backup (With NORECOVERY)
The team takes the 2:00 AM Full Backup file (`.bak`) and restores it.
*Crucial Detail:* They use the `WITH NORECOVERY` command. This tells the SQL engine: "I am giving you the foundation, but do not open the database yet, I have more puzzle pieces to give you."

### Step 3: Replay the Log Chain
The team takes every 15-minute log backup (`.trn` files) generated since 2:00 AM (2:15, 2:30, 2:45... up to 11:30 AM) and restores them sequentially `WITH NORECOVERY`. The SQL Engine mathematically "replays" every single `INSERT` and `UPDATE` that happened that morning.

### Step 4: The Surgical Strike (STOPAT)
Finally, they have the Tail-Log backup (from 11:35 AM) which contains the fatal `DROP TABLE` command executed at 11:32:00.
They restore this final log using the `STOPAT` command:
```sql
RESTORE LOG FinancialDB 
FROM DISK = 'TailLog_1135.trn' 
WITH STOPAT = '2023-10-05 11:31:59.000', RECOVERY;
```
**The Magic:** The SQL engine replays the log, executing the financial trades made at 11:31:58. When the internal clock hits 11:31:59, the engine stops. It ignores the `DROP TABLE` command that occurs one second later. 
The `WITH RECOVERY` command brings the database online. 
**Result:** 100% of the financial trades are saved. Zero data loss.

## 4. Post-Mortem Action Items (Preventing it next time)

Why did this happen in the first place, and how do we ensure it never happens again?

1. **Fix the Security Model (RBAC):** The disgruntled DBA had `db_owner` access to the Production environment. We must implement **Principle of Least Privilege**. Humans should only have `db_datareader` access in Production. Any schema changes (like `DROP TABLE`) must be executed exclusively by a CI/CD pipeline Service Account (via Terraform or EF Core Migrations) that requires two code-review approvals in Git.
2. **Implement DDL Triggers:** We can write a Database-Level Trigger that explicitly blocks the `DROP TABLE` command.
   ```sql
   CREATE TRIGGER PreventTableDrop 
   ON DATABASE FOR DROP_TABLE 
   AS 
   BEGIN
       PRINT 'Dropping tables in Production is strictly forbidden.'
       ROLLBACK;
   END
   ```
3. **Temporal Tables (System-Versioning):** For critical financial tables, we enable Temporal Tables. If someone deletes or updates a row, the database engine invisibly and permanently saves the old version to a hidden History table. This prevents data loss from malicious `UPDATE` statements without needing a full database restore.

---

## Mock Interview Block

**Interviewer:** In the scenario above, the team ran a "Tail-Log Backup" immediately after the disaster occurred. Why is this step absolutely critical, and what would happen if they skipped it and just started restoring from the 2:00 AM Full Backup?
**Candidate:** The Tail-Log backup captures the live, active transactions sitting in the `.ldf` file that have not yet been saved to a scheduled 15-minute `.trn` backup file. In this scenario, the last scheduled log backup was at 11:30 AM. The disaster happened at 11:32 AM. If the team skipped the Tail-Log backup, they would permanently lose all the financial trades that occurred during those 2 critical minutes. Capturing the tail log is the only way to achieve an RPO (Recovery Point Objective) of zero in this disaster.

**Interviewer:** You attempt to restore the 2:00 AM Full Backup using `RESTORE DATABASE WITH RECOVERY`. Then, you try to apply the 2:15 AM Transaction Log backup. The SQL Server throws a fatal error and refuses to apply the log. What did you do wrong?
**Candidate:** Using `WITH RECOVERY` tells the SQL engine that the restore sequence is completely finished. The engine runs its final undo/redo phases to guarantee ACID consistency and opens the database for users to connect. Once a database is in the "Recovered" state, you cannot apply any further transaction logs to it. 
To apply a chain of logs, every single restore step (except the absolute final one) must be executed `WITH NORECOVERY`. This leaves the database in a locked, "Restoring" state, signaling to the engine that more log files are coming.

**Interviewer:** The disaster recovery took 4 hours to complete because the database was 5 Terabytes in size. The CEO is furious about the 4-hour RTO (Recovery Time Objective) and demands that the system architecture be changed so that if a table is dropped again, the system is back online in under 5 minutes. As an Architect, what technologies or patterns do you deploy to meet this 5-minute RTO?
**Candidate:** A Point-in-Time restore of a 5TB database will always take hours due to physical disk I/O limits. We cannot bypass physics.
To achieve a 5-minute RTO for a human-error disaster like `DROP TABLE`, we must decouple data recovery from the physical database restore process. 
I would implement **Database Snapshots** (if supported by the SAN or SQL Server). A Snapshot is an instantaneous, read-only pointer to the data state. If we take snapshots every hour, and someone drops a table, we don't restore 5TB of data. We simply write a cross-database query to `SELECT` the missing table out of the Snapshot and `INSERT` it back into the live database. This copies only the missing megabytes, restoring the system in seconds.
*(Note: I would also explicitly point out to the CEO that High Availability clusters like AlwaysOn will NOT protect against a `DROP TABLE` command, because the command will replicate instantly to the secondary nodes, destroying them too. HA protects against hardware failure, not human error).*
