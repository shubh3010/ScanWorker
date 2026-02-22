# ScanWorker

## Source Code

The source code is available on GitHub: https://github.com/shubh3010/ScanWorker

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- SQL Server (local or Docker) — the default connection expects it at `127.0.0.1,1434`
- PowerShell (for the mock API script)

---

## External Dependencies (NuGet)

These are restored automatically via `dotnet restore`:

| Package | Version |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.11 |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.11 |
| `Microsoft.Extensions.Hosting` | 8.0.0 |
| `Microsoft.Extensions.Http` | 8.0.0 |

---

## Configuration

Update `ScanWorker/appsettings.json` with your environment values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1,1434;Database=ScanWorker;User=<user>;Password=<password>;TrustServerCertificate=True;"
  },
  "ScanApi": {
    "BaseUrl": "https://localhost:5001/",
    "TimeoutSeconds": 30
  }
}
```

For local development, `appsettings.Development.json` overrides `BaseUrl` to point at the mock API (`http://localhost:5099/`).

---

## Database Setup

First, install the EF Core CLI tool (if not already installed):

```bash
dotnet tool install --global dotnet-ef
```

Then run the EF Core migration to create the schema:

```bash
dotnet ef database update --project ScanWorker
```

---

## Running the Application

### 1. Start the Mock API (development only)

```powershell
.\mock-api.ps1
```

This starts a lightweight HTTP listener on `http://localhost:5099/` that returns sample scan events.

### 2. Start the Worker

```bash
dotnet run --project ScanWorker
```

The worker will begin polling the scan API, processing events, and persisting results to SQL Server.

---

## Running the Tests

```bash
dotnet test ScanWorker.Tests
```

---

## Assumptions

### API Contract
1. New event types and status codes can show up over time, so `Type` and `StatusCode` are stored as strings instead of enums.

### Processing
2. The worker can be stopped and started at any point, so it needs a persistent cursor. `LastProcessedEventId` is stored in an `EventProcessingState` table.

### Storage
3. `EventId` is globally unique and works well as the primary key for stored scan events.
4. Storing the raw scan events is enough to answer the required queries later (latest event per parcel, pickup/delivery times) without duplicating those values onto the Parcel row.
5. Creating a dedicated Parcel table was over-engineering for this exercise, so `ParcelId` is stored as a string on the ScanEvent record. A Parcel table can always be expanded later if more info about each parcel is needed.

### Operational
6. This is a dev/exercise setup, so `appsettings.json` is used for the API base URL and DB connection string.

---

## Improvements / Productionising

- **Make polling and retry settings configurable** — batch size, poll interval, retry count, and backoff delay are hardcoded constants right now. Moving them to `appsettings.json` means ops can tune without redeploying.
- **Event handling** — if the same event keeps failing, log it somewhere (dead-letter table) and move past it after N tries so it doesn't block the whole pipeline.
- **Health checks** — add a `/health` endpoint so the host (K8s, systemd, etc.) knows the worker is alive and can restart it if it gets stuck.
- **Metrics** — emit counters like events processed, duplicates skipped, deserialization failures, and batch duration. Something like OpenTelemetry or Prometheus so we can build dashboards and set alerts.
- **Least-privilege DB access** — the SQL user currently has more access than it needs. In production it should only have the specific permissions required.
- **Tests** — add unit tests for the event processing logic and integration tests that run against a test database to verify end-to-end functionality.

---

## Enabling Downstream Workers

If another worker needed to act on the same scan events (e.g. send notifications, update analytics), the cleanest approach would be the **Transactional Outbox** pattern:

1. Add an `OutboxEvents` table to the existing database.
2. When `ScanEventProcessorService` saves a processed event, it also writes a row to the outbox table **in the same transaction** — so either both succeed or neither does.
3. A separate hosted service (or a second worker) polls the outbox table, publishes each unpublished row to a message broker (RabbitMQ, Azure Service Bus, etc.), and marks it as published.
4. Downstream workers subscribe to the broker and process events independently.

### Why this approach

- **No data loss** — the outbox write shares a transaction with the scan event write, so nothing slips through the cracks.
- **Minimal change to this app** — just a new `DbSet<OutboxEvent>`, a few extra lines in the existing save path, and a small publisher service.
- **Downstream independence** — each consumer has its own subscription/queue, so they can fail, restart, or scale without affecting ingestion or each other.

### Trade-offs

- **Extra DB load** — every processed event now results in two writes (ScanEvent + OutboxEvent) instead of one, and the publisher adds constant polling against the outbox table.
- **Eventual consistency** — downstream workers don't see events instantly. There's a small delay between the event being saved and the publisher picking it up and pushing it to the broker.
- **Outbox cleanup** — the outbox table grows over time. A scheduled job or retention policy is needed to prune old published rows.

### What the overall system would look like

```
Scan Event API
      │
      ▼
┌──────────────┐       ┌─────────────────────┐
│  ScanWorker  │──TX──▶│  SQL Server          │
│  (this app)  │       │  - ScanEvents        │
│              │       │  - EventProcessing   │
│              │──TX──▶│  - OutboxEvents      │
└──────────────┘       └─────────┬────────────┘
                                 │
                       ┌─────────▼────────────┐
                       │  Outbox Publisher     │
                       │  (hosted service)     │
                       └─────────┬────────────┘
                                 │
                       ┌─────────▼────────────┐
                       │  Message Broker       │
                       │  (RabbitMQ / ASB)     │
                       └────┬────────────┬────┘
                            │            │
                  ┌─────────▼──┐  ┌──────▼─────────┐
                  │ Worker A   │  │ Worker B        │
                  │ (notify)   │  │ (analytics)     │
                  └────────────┘  └────────────────┘
```

