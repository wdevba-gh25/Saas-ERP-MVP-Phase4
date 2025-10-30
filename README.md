# SaaS ERP MVP – Phase 4: CQRS & Clean Architecture (Outbox + Redis Edition)

**Phase 4** begins the **Modular Monolith Transition Project** — refactoring the SaaS ERP MVP toward full microservices.  
The goal: introduce **CQRS**, **EF Core Outbox pattern**, **Redis Pub/Sub projection**, and **Clean Architecture** layering (Domain / Application / Infrastructure / API), ensuring tenant-safe, low-latency updates without breaking the existing UI.

---

## Migration Plan (Summary ≤ 10 lines)
1. Introduce **CQRS (Command–Query Responsibility Segregation)**.  
2. Implement **EF Core Outbox pattern** for durable domain events.  
3. Use **Redis Pub/Sub** for async projections into read-model tables.  
4. Keep schema-level multi-tenancy intact (`OrganizationId` everywhere).  
5. Create **InventoryRead** and **OutboxMessages** tables.  
6. Add **Hosted Services** (Dispatcher + Subscriber) inside the monolith.  
7. Expose new APIs: `/api/commands/*` and `/api/queries/*`.  
8. Preserve all existing Auth/GraphQL routes.  
9. Prepare codebase for per-module extraction.  
10. Test “INTENT/INSERT” stock flow end-to-end.  

---

## Environment Setup

### Prerequisites
- .NET 8 SDK (8.0.x)  
- SQL Server Developer Edition (local instance)  
- Redis Server 6+ (local)  
- Visual Studio 2022 or VS Code  
- Node 18 + npm (for optional frontend tests)

---

### 1 Database Setup

Run the included `SaaSMVPFull.sql` to create the baseline schema (Users, Projects, Inventory, etc.), then extend it with CQRS tables:

```sql
CREATE TABLE dbo.OutboxMessages (
  OutboxId UNIQUEIDENTIFIER PRIMARY KEY,
  OccurredAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  EventType NVARCHAR(100) NOT NULL,
  Payload NVARCHAR(MAX) NOT NULL,
  OrganizationId UNIQUEIDENTIFIER NOT NULL,
  Dispatched BIT NOT NULL DEFAULT(0),
  DispatchAttempts INT NOT NULL DEFAULT(0)
);

CREATE TABLE dbo.InventoryReads (
  InventoryReadId UNIQUEIDENTIFIER PRIMARY KEY,
  OrganizationId UNIQUEIDENTIFIER NOT NULL,
  ProjectId UNIQUEIDENTIFIER NOT NULL,
  ProductName NVARCHAR(200) NOT NULL,
  StockLevel INT NOT NULL,
  UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

---

### 2 AppSettings / Environment Variables

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=SaaSMvpDB;Trusted_Connection=True;TrustServerCertificate=True;"
},
"Redis": { "Connection": "localhost:6379" }
```

Or via environment variables:

```bash
MSSQL_CONN_STR="Server=localhost;Database=SaaSMvpDB;Trusted_Connection=True;TrustServerCertificate=True;"
REDIS_CONNECTION="localhost:6379"
```

---

### 3 Run the Backend

```bash
cd backend
dotnet restore
dotnet run
```

This launches the .NET 8 monolith with two background workers:
- **OutboxDispatcherHostedService** → publishes new events to Redis.  
- **ProjectionSubscriberHostedService** → listens on `inventory.events` and updates `InventoryReads`.

---

### 4 Redis Startup (Windows or WSL)

```bash
redis-server
# verify
redis-cli PING
# expect → PONG
```

---

### 5 Testing the “INTENT / INSERT” Stock Flow

#### a) Seed Sample Product
Run inside SQL Server Management Studio:
```sql
DECLARE @pid UNIQUEIDENTIFIER = NEWID();
INSERT INTO Inventory (InventoryId, ProjectId, ProductName, StockLevel)
VALUES (@pid, '<existingProjectGuid>', N'Keyboard Model X', 10);
SELECT @pid AS SeedProductId;
```
Keep the `SeedProductId`.

#### b) Send Adjust Stock Command
```bash
curl -X POST https://localhost:5100/api/commands/inventory/adjust   -H "Content-Type: application/json"   -d "{"organizationId":"<OrgGuid>","inventoryId":"<SeedProductId>","delta":5,"commandId":"cmd-1234"}"
```
✅ Expected: `{ "ok": true, "newLevel": 15 }`

#### c) Validate Outbox
```sql
SELECT TOP 1 * FROM dbo.OutboxMessages ORDER BY OccurredAt DESC;
```
Record is persisted → event will be published to Redis.

#### d) Validate Projection
```sql
SELECT * FROM dbo.InventoryReads WHERE ProductName = N'Keyboard Model X';
```
`StockLevel` reflects the increment within milliseconds.

---

### 6 Testing Queries (Read Side)

```bash
curl https://localhost:5100/api/queries/inventory/<orgId>/<productName>
```
Returns:
```json
{
 "productName": "Keyboard Model X",
 "stockLevel": 15,
 "updatedAt": "2025-10-30T17:00:00Z"
}
```

---

## Architecture Summary

- **Clean Architecture Layers:**  
  - `Domain`: Entities & Rules  
  - `Application`: MediatR Commands/Queries  
  - `Infrastructure`: EF Core + Redis Integration  
  - `API`: REST / GraphQL Endpoints  

- **Data Flow:**  
  Command → Write DB → Outbox → Redis → Projector → Read DB → Query  

- **Tenant Safety:**  
  All flows propagate `OrganizationId` from JWT claims.  

---

## CQRS Flow Diagram

```
          +-------------------+
          | React Frontend    |
          |  (JWT, GraphQL)   |
          +---------+---------+
                    |
                    | 1. Command: Adjust Stock
                    v
          +---------+---------+
          | .NET 8 API Layer  |
          | (MediatR Command) |
          +---------+---------+
                    |
                    | 2. EF Core SaveChanges()
                    |    → Inventory + Outbox
                    v
          +---------+---------+
          | SQL Server        |
          | (OutboxMessages)  |
          +---------+---------+
                    |
                    | 3. Redis Publish (OutboxDispatcher)
                    v
          +---------+---------+
          | Redis Pub/Sub     |
          | Channel: inventory.events |
          +---------+---------+
                    |
                    | 4. ProjectionSubscriber HostedService
                    v
          +---------+---------+
          | SQL Server        |
          | (InventoryReads)  |
          +---------+---------+
                    |
                    | 5. Query Read Model
                    v
          +---------+---------+
          | React Frontend    |
          | (GraphQL Query)   |
          +-------------------+
```

---

## Validation Checklist

| Component | Expected Result |
|------------|----------------|
| EF Migration | Tables `OutboxMessages` & `InventoryReads` exist |
| Redis | Channel `inventory.events` active |
| Command Handler | Writes Outbox row and updates Inventory |
| Dispatcher | Publishes event to Redis |
| Subscriber | Upserts `InventoryReads` record |
| Query API | Reflects new stock level instantly |

---

## Next Step
Once the CQRS slice is stable, the next phase (Phase 5) will extract the Inventory module as an independent microservice while retaining the same Redis Pub/Sub contracts and read-model projection.
