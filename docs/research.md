# Research: Dual-Entry Accounting & Invoicing Service

**Feature**: backend-api  
**Date**: 2026-02-08 (Updated from 2026-02-06)  
**Phase**: Phase 0 - Technical Research

This document consolidates technical research decisions for implementing the dual-entry accounting and invoicing service, including resolutions for all NEEDS CLARIFICATION items from Constitution Check.

---

## 1. Fixed-Point Arithmetic for Financial Calculations

### Decision
Use `decimal` type in .NET with `decimal(19,4)` precision in PostgreSQL for all monetary values.

### Rationale
- **Exactness**: `decimal` provides exact decimal representation, unlike `float` or `double` which introduce rounding errors
- **Financial Standard**: 19 digits total with 4 decimal places supports values up to $999,999,999,999,999.9999 - sufficient for ride fare accounting
- **Regulatory Compliance**: Financial systems must maintain exact accuracy per GAAP and accounting standards
- **Ledger Balance**: Debits and credits must match exactly; floating-point errors would violate double-entry accounting principles

###Alternatives Considered
- **Integer (cents)**: Store monetary values as integers (e.g., $10.50 as 1050 cents)
  - **Rejected**: While eliminates rounding, requires manual scaling logic throughout codebase; `decimal` provides better readability and is .NET/EF Core native
- **Money library** (e.g., `NodaMoney`): Third-party library for monetary calculations
  - **Rejected**: Adds dependency; .NET `decimal` with proper rounding modes (MidpointRounding.ToEven) is sufficient and built-in

### Implementation Notes
```csharp
// Domain value object
public record Money
{
    private const int Scale = 4; // 4 decimal places
    private const decimal MaxValue = 999_999_999_999_999.9999m;
    
    public decimal Amount { get; }
    
    public Money(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative");
        if (amount > MaxValue) throw new ArgumentException($"Amount exceeds max value {MaxValue}");
        Amount = Math.Round(amount, Scale, MidpointRounding.ToEven);
    }
    
    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);
}
```

EF Core Configuration:
```csharp
builder.Property(e => e.Amount)
    .HasColumnType("decimal(19,4)")
    .HasPrecision(19, 4);
```

---

## 2. Multi-Tenant Isolation in PostgreSQL

### Decision
Implement **Row-Level Security (RLS)** combined with **application-level tenant filtering** using `TenantId` column on all tables.

### Rationale
- **Defense in Depth**: Two layers of isolation prevent tenant data leakage
- **Performance**: Row-level filtering with indexed `tenant_id` column is faster than schema-per-tenant approach
- **Scalability**: Single schema supports unlimited tenants without schema proliferation
- **Query Simplicity**: Application queries automatically include `WHERE tenant_id = @tenantId` via EF Core global query filters

### Alternatives Considered
- **Schema-per-tenant**: Each tenant gets dedicated PostgreSQL schema (`tenant_001`, `tenant_002`)
  - **Rejected**: Doesn't scale well (thousands of schemas); complicates migrations, backups, and monitoring
- **Database-per-tenant**: Each tenant gets dedicated database
  - **Rejected**: Massive operational overhead; insufficient for 10,000+ accounts goal
- **Application-level only** (no RLS): Rely solely on EF Core query filters
  - **Rejected**: Single point of failure; developer error could bypass filter; RLS provides database-level guarantee

### Implementation Notes
```sql
-- Enable Row-Level Security on tables
ALTER TABLE accounts ENABLE ROW LEVEL SECURITY;
ALTER TABLE ledger_entries ENABLE ROW LEVEL SECURITY;
ALTER TABLE invoices ENABLE ROW LEVEL SECURITY;

-- Create policy (enforced at database level)
CREATE POLICY tenant_isolation_policy ON accounts
    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);

CREATE POLICY tenant_isolation_policy ON ledger_entries
    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
```

EF Core Global Query Filter:
```csharp
public class AccountingDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.GetTenantId());
            
        modelBuilder.Entity<LedgerEntryEntity>()
            .HasQueryFilter(e => e.TenantId == _tenantProvider.GetTenantId());
    }
}
```

Indexes:
```sql
CREATE INDEX idx_accounts_tenant_id ON accounts(tenant_id);
CREATE INDEX idx_ledger_entries_tenant_id ON ledger_entries(tenant_id);
CREATE INDEX idx_invoices_tenant_id ON invoices(tenant_id);
```

---

## 3. Idempotency Patterns for REST APIs

### Decision
Use **idempotency keys** embedded in business identifiers (`RideId` for charges, `PaymentReferenceId` for payments) with database unique constraints.

### Rationale
- **Natural Keys**: Business identifiers (Ride ID, Payment Reference) are natural idempotency keys
- **Zero-Cost Storage**: No separate idempotency tracking table needed
- **Database Enforcement**: Unique constraints prevent duplicates even under race conditions
- **Standards Compliance**: Aligns with Stripe API and industry best practices

### Alternatives Considered
- **Separate Idempotency Keys** (`Idempotency-Key` request header):
  - **Rejected**: Requires separate tracking table; adds complexity; clients must generate UUIDs; business keys are more natural
- **Distributed Lock** (Redis/database advisory locks):
  - **Rejected**: Adds latency; requires external dependency; database constraints are simpler and more reliable
- **Optimistic Concurrency** (version stamps):
  - **Rejected**: Does not prevent duplicates; only prevents concurrent updates to same entity

### Implementation Notes
Database Constraints:
```sql
-- Prevent duplicate charges for same ride + account
CREATE UNIQUE INDEX uq_ledger_entries_ride_charge 
ON ledger_entries(account_id, source_reference_id) 
WHERE source_type = 'Ride';

-- Prevent duplicate payments for same payment reference
CREATE UNIQUE INDEX uq_ledger_entries_payment 
ON ledger_entries(account_id, source_reference_id) 
WHERE source_type = 'Payment';
```

Domain Layer:
```csharp
public class Account : AggregateRoot
{
    public Result RecordCharge(RideId rideId, Money amount, DateTime serviceDate, string fleetId)
    {
        // Check for duplicate in memory (optimistic check)
        if (_ledgerEntries.Any(e => e.SourceReferenceId == rideId.Value && e.SourceType == SourceType.Ride))
        {
            return Result.Failure(LedgerErrors.DuplicateCharge(rideId));
        }
        
        // Create ledger entries (database constraint will enforce on save)
        var debitEntry = LedgerEntry.CreateDebit(LedgerAccountType.AccountsReceivable, amount, rideId, serviceDate);
        var creditEntry = LedgerEntry.CreateCredit(LedgerAccountType.ServiceRevenue, amount, rideId, serviceDate);
        
        _ledgerEntries.Add(debitEntry);
        _ledgerEntries.Add(creditEntry);
        
        RaiseDomainEvent(new ChargeRecordedEvent(Id, rideId, amount, serviceDate));
        return Result.Success();
    }
}
```

---

## 4. OpenTelemetry Configuration for .NET 10

### Decision
Use **OpenTelemetry SDK with OTLP exporter** for traces, metrics, and logs. Export to collector (Jaeger/Prometheus/Grafana stack).

### Rationale
- **Vendor-Neutral**: OpenTelemetry is CNCF standard; avoids vendor lock-in
- **Unified Observability**: Single SDK for traces, metrics, logs reduces complexity
- **Native .NET Support**: First-class .NET integration via `OpenTelemetry.Extensions.Hosting`
- **Constitution Requirement**: Observability is non-negotiable per Principle VII

### Alternatives Considered
- **Application Insights**: Azure-specific monitoring
  - **Rejected**: Vendor lock-in; incompatible with multi-cloud strategy
- **Serilog + Prometheus + Jaeger** (separate tools):
  - **Rejected**: Requires managing 3 separate SDKs; OpenTelemetry provides unified approach
- **ELK Stack** (Elasticsearch, Logstash, Kibana):
  - **Rejected**: Heavy infrastructure footprint; OpenTelemetry collector is lighter

### Implementation Notes
NuGet Packages:
```xml
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.10.*" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.10.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.9.*" />
<PackageReference Include="Npgsql.OpenTelemetry" Version="8.0.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.*" />
```

Program.cs Configuration:
```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "AccountingService",
        serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddNpgsql()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"]);
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("AccountingService.Application")
        .AddOtlpExporter())
    .WithLogging(logging => logging
        .AddOtlpExporter());
```

Custom Metrics:
```csharp
public class AccountMetrics
{
    private static readonly Meter Meter = new("AccountingService.Application", "1.0.0");
    
    public static readonly Counter<long> ChargesRecorded = Meter.CreateCounter<long>(
        "accounting.charges.recorded",
        description: "Total number of ride charges recorded");
        
    public static readonly Counter<long> PaymentsRecorded = Meter.CreateCounter<long>(
        "accounting.payments.recorded",
        description: "Total number of payments recorded");
        
    public static readonly Histogram<double> InvoiceGenerationDuration = Meter.CreateHistogram<double>(
        "accounting.invoice.generation.duration",
        unit: "ms",
        description: "Duration of invoice generation in milliseconds");
        
    public static readonly ObservableGauge<long> ActiveAccounts = Meter.CreateObservableGauge<long>(
        "accounting.accounts.active",
        () => GetActiveAccountCount(),
        description: "Number of active accounts");
}
```

---

## 5. EF Core Performance Optimization for High-Write Volumes

### Decision
Use **batching, change tracking optimization, and database-generated values** to achieve <100ms write latency target.

### Rationale
- **Batching**: EF Core 7+ supports automatic batching of INSERTs (up to 1000 per batch)
- **AsNoTracking**: Read queries don't need change tracking; reduces memory and improves speed
- **Compiled Queries**: Pre-compile frequent queries to avoid LINQ translation overhead
- **Database-Generated Values**: Let PostgreSQL generate IDs and timestamps reduces round trips

### Alternatives Considered
- **Dapper** (micro-ORM): Bypasses EF Core for raw SQL performance
  - **Rejected**: Loses type safety, change tracking, and migration support; performance gain marginal with proper EF Core tuning
- **BulkInsert Extensions**: Third-party libraries for bulk operations
  - **Rejected**: EF Core 7+ native batching is sufficient; avoids dependencies
- **Stored Procedures**: Move business logic to database
  - **Rejected**: Violates DDD principles; logic belongs in domain layer

### Implementation Notes

SaveChanges Performance:
```csharp
// Enable batch size configuration
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MaxBatchSize(100); // Batch up to 100 commands
            npgsqlOptions.CommandTimeout(30); // 30s timeout
        })
        .EnableSensitiveDataLogging(isDevelopment)
        .EnableDetailedErrors(isDevelopment);
}
```

Read Query Optimization:
```csharp
// AsNoTracking for read queries
public async Task<AccountBalanceDto> GetAccountBalanceAsync(Guid accountId, CancellationToken ct)
{
    return await _context.Accounts
        .AsNoTracking() // No change tracking needed
        .Where(a => a.Id == accountId)
        .Select(a => new AccountBalanceDto
        {
            AccountId = a.Id,
            Balance = a.LedgerEntries.Sum(e => e.DebitAmount ?? 0) - a.LedgerEntries.Sum(e => e.CreditAmount ?? 0)
        })
        .FirstOrDefaultAsync(ct);
}
```

Compiled Queries (for hot paths):
```csharp
private static readonly Func<AccountingDbContext, Guid, Task<AccountEntity>> GetAccountByIdCompiled =
    EF.CompileAsyncQuery((AccountingDbContext context, Guid id) =>
        context.Accounts.First(a => a.Id == id));

// Usage
var account = await GetAccountByIdCompiled(_context, accountId);
```

Database-Generated Values:
```sql
CREATE TABLE ledger_entries (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    -- other columns
);
```

```csharp
builder.Property(e => e.Id)
    .HasDefaultValueSql("gen_random_uuid()");
    
builder.Property(e => e.CreatedAt)
    .HasDefaultValueSql("CURRENT_TIMESTAMP");
```

Indexes for Performance:
```sql
-- Composite index for balance calculation
CREATE INDEX idx_ledger_entries_account_tenant 
ON ledger_entries(account_id, tenant_id) 
INCLUDE (debit_amount, credit_amount);

-- Index for statement date range queries
CREATE INDEX idx_ledger_entries_account_date 
ON ledger_entries(account_id, created_at DESC);

-- Partial index for active accounts only (80% of queries)
CREATE INDEX idx_accounts_active 
ON accounts(id, tenant_id) 
WHERE status = 'Active';
```

---

## 6. Result Pattern Implementation

### Decision
Use **FluentResults** library with custom error types to implement Result pattern across application layer.

### Rationale
- **Explicit Error Handling**: Forces callers to handle errors; no silent failures
- **Type Safety**: `Result<T>` makes success/failure explicit in method signatures
- **Readable**: Fluent API improves code readability vs custom implementation
- **Constitution Mandate**: Principle VIII requires Result pattern for business logic

### Alternatives Considered
- **Custom Result Implementation**: Build own `Result<T>` class
  - **Rejected**: Reinventing wheel; FluentResults is battle-tested and maintained
- **OneOf** library: Discriminated unions for return types
  - **Rejected**: More complex API; Result pattern is simpler for this use case
- **Exceptions**: Use `try/catch` for business rule failures
  - **Rejected**: Violates constitution; exceptions are for unexpected failures only

### Implementation Notes

Domain Errors:
```csharp
public static class AccountErrors
{
    public static Error NotFound(Guid accountId) =>
        new Error($"Account {accountId} not found").WithMetadata("ErrorCode", "ACCOUNT_NOT_FOUND");
        
    public static Error Inactive(Guid accountId) =>
        new Error($"Account {accountId} is inactive").WithMetadata("ErrorCode", "ACCOUNT_INACTIVE");
}

public static class LedgerErrors
{
    public static Error DuplicateCharge(RideId rideId) =>
        new Error($"Charge for ride {rideId} already recorded").WithMetadata("ErrorCode", "DUPLICATE_CHARGE");
        
    public static Error TenantMismatch() =>
        new Error("Tenant ID mismatch").WithMetadata("ErrorCode", "TENANT_MISMATCH");
}
```

Command Handler:
```csharp
public class RecordChargeCommandHandler
{
    public async Task<Result<Guid>> Handle(RecordChargeCommand command, CancellationToken ct)
    {
        var account = await _repository.GetByIdAsync(command.AccountId, ct);
        if (account == null)
            return Result.Fail<Guid>(AccountErrors.NotFound(command.AccountId));
            
        var chargeResult = account.RecordCharge(
            new RideId(command.RideId),
            new Money(command.Amount),
            command.ServiceDate,
            command.FleetId);
            
        if (chargeResult.IsFailed)
            return Result.Fail<Guid>(chargeResult.Errors);
            
        await _repository.SaveAsync(account, ct);
        
        return Result.Ok(account.Id);
    }
}
```

Controller (Error Mapping):
```csharp
[HttpPost]
public async Task<IActionResult> RecordCharge([FromBody] RecordChargeRequest request)
{
    var command = MapToCommand(request);
    var result = await _handler.Handle(command, HttpContext.RequestAborted);
    
    return result.ToActionResult(this); // Extension method
}

// Extension method for Result → IActionResult
public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
{
    if (result.IsSuccess)
        return controller.Ok(result.Value);
        
    var error = result.Errors.First();
    var errorCode = error.Metadata.GetValueOrDefault("ErrorCode")?.ToString();
    
    return errorCode switch
    {
        "ACCOUNT_NOT_FOUND" => controller.NotFound(new ProblemDetails 
        { 
            Title = "Account Not Found", 
            Detail = error.Message 
        }),
        "DUPLICATE_CHARGE" => controller.Conflict(new ProblemDetails 
        { 
            Title = "Duplicate Charge", 
            Detail = error.Message 
        }),
        "ACCOUNT_INACTIVE" => controller.BadRequest(new ProblemDetails 
        { 
            Title = "Account Inactive", 
            Detail = error.Message 
        }),
        _ => controller.StatusCode(500, new ProblemDetails 
        { 
            Title = "Internal Server Error", 
            Detail = "An unexpected error occurred" 
        })
    };
}
```

---

## Summary

All technical research is complete. Key decisions:

1. **Fixed-Point Arithmetic**: `decimal` type with `decimal(19,4)` precision
2. **Multi-Tenant Isolation**: Row-Level Security + application-level filtering
3. **Idempotency**: Business identifier-based with database unique constraints
4. **Observability**: OpenTelemetry with OTLP exporter
5. **EF Core Performance**: Batching, AsNoTracking, compiled queries, database-generated values
6. **Result Pattern**: FluentResults library with custom error types
7. **Event-Driven Architecture**: Publish integration events with Outbox pattern
8. **Event Versioning**: Semantic versioning with additive-only schema changes

**Status**: ✅ Ready for Phase 1 (Design & Contracts)

---

## 7. Event-Driven Architecture & Integration Events

**NEEDS CLARIFICATION Resolution**: Constitution Check identified event-driven architecture decisions as requiring research.

### Decision
Publish **integration events** for key financial operations (ChargeRecorded, PaymentReceived, InvoiceGenerated) using **Outbox pattern** for reliable delivery.

### Rationale
- **Loose Coupling**: External services subscribe to events without direct dependencies on Accounting Service
- **Future Extensibility**: New consumers can be added without modifying Accounting Service
- **Audit Trail**: Events provide complete history of financial operations
- **Constitution Compliance**: Meets Event-Driven Architecture principle (IX)

### Events to Publish

#### 1. ChargeRecordedEvent
**When**: After successfully recording a ride charge to ledger  
**Potential Consumers**:
- Ride Management Service (update ride billing status)
- Analytics Service (financial reporting)
- Notification Service (alert customer of charge)

**Payload**:
```json
{
  "event_id": "uuid",
  "event_type": "ChargeRecordedEvent.v1",
  "event_version": "1.0.0",
  "occurred_at": "2026-02-08T10:30:00Z",
  "tenant_id": "tenant-abc",
  "aggregate_type": "Account",
  "aggregate_id": "account-001",
  "payload": {
    "account_id": "account-001",
    "ride_id": "ride-12345",
    "fare_amount": 25.50,
    "service_date": "2026-02-08",
    "fleet_id": "fleet-001",
    "ledger_entry_ids": ["entry-001", "entry-002"]
  }
}
```

#### 2. PaymentReceivedEvent
**When**: After successfully recording a payment to ledger  
**Potential Consumers**:
- Payment Gateway Service (confirm processing completed)
- Analytics Service (cash flow tracking)
- Notification Service (send payment confirmation)

**Payload**:
```json
{
  "event_id": "uuid",
  "event_type": "PaymentReceivedEvent.v1",
  "event_version": "1.0.0",
  "occurred_at": "2026-02-08T14:15:00Z",
  "tenant_id": "tenant-abc",
  "aggregate_type": "Account",
  "aggregate_id": "account-001",
  "payload": {
    "account_id": "account-001",
    "payment_reference_id": "pay-67890",
    "amount": 100.00,
    "payment_date": "2026-02-08",
    "remaining_balance": 150.00,
    "ledger_entry_ids": ["entry-003", "entry-004"]
  }
}
```

#### 3. InvoiceGeneratedEvent
**When**: After successfully generating an invoice  
**Potential Consumers**:
- Notification Service (send invoice to customer)
- Document Storage Service (archive PDF)
- Analytics Service (billing cycle tracking)

**Payload**:
```json
{
  "event_id": "uuid",
  "event_type": "InvoiceGeneratedEvent.v1",
  "event_version": "1.0.0",
  "occurred_at": "2026-02-08T16:00:00Z",
  "tenant_id": "tenant-abc",
  "aggregate_type": "Invoice",
  "aggregate_id": "INV-00001",
  "payload": {
    "account_id": "account-001",
    "invoice_number": "INV-00001",
    "billing_period_start": "2026-02-01",
    "billing_period_end": "2026-02-28",
    "subtotal": 500.00,
    "total_payments_applied": 200.00,
    "outstanding_balance": 300.00,
    "line_item_count": 15
  }
}
```

### Alternatives Considered
- **No Events (API-Only)**: Rejected—creates tight coupling and requires polling
- **Event Sourcing**: Rejected for v1.0—too complex; ledger already provides append-only audit trail
- **Synchronous Webhooks**: Rejected—introduces latency and coupling; async events are superior

---

## 8. Outbox Pattern Implementation

### Decision
Store events in `outbox_messages` table within same transaction as domain changes; background processor publishes to Kafka.

### Rationale
- **Atomicity**: Events and domain changes commit together (no partial publishes)
- **Reliability**: Events persist even if Kafka is temporarily unavailable
- **Exactly-Once Semantics**: Combined with consumer idempotency, achieves effectively-once delivery

### Database Schema

```sql
CREATE TABLE outbox_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id VARCHAR(100) NOT NULL,
    event_type VARCHAR(200) NOT NULL,
    event_data JSONB NOT NULL,
    occurred_at TIMESTAMP NOT NULL,
    processed_at TIMESTAMP NULL,
    retry_count INT DEFAULT 0,
    error_message TEXT NULL,
    tenant_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX ix_outbox_messages_unprocessed 
ON outbox_messages(processed_at, occurred_at) 
WHERE processed_at IS NULL;

CREATE INDEX ix_outbox_messages_tenant 
ON outbox_messages(tenant_id);
```

### Background Processor

```csharp
public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<OutboxPublisher> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
                
                var unprocessedMessages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null && m.RetryCount < 10)
                    .OrderBy(m => m.OccurredAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);
                
                foreach (var message in unprocessedMessages)
                {
                    try
                    {
                        await _kafkaProducer.ProduceAsync(
                            topic: message.EventType,
                            key: message.AggregateId,
                            value: message.EventData.ToString(),
                            cancellationToken: stoppingToken);
                        
                        message.ProcessedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;
                        message.ErrorMessage = ex.Message;
                        _logger.LogError(ex, "Failed to publish event {EventId}", message.Id);
                    }
                }
                
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor cycle failed");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

### Retry Strategy
- **Interval**: Every 5 seconds
- **Circuit Breaker**: After 5 consecutive failures, back off to 30 seconds
- **Dead Letter**: After 10 retries, move to dead letter table for manual intervention
- **Alerting**: Prometheus alert if dead letter count > 0

---

## 9. Event Versioning & Schema Evolution

### Decision
Use **semantic versioning** (`Major.Minor.Patch`) with **additive-only changes** for event schemas.

### Versioning Rules

| Change Type | Example | Version Change | Backward Compatible? |
|-------------|---------|----------------|----------------------|
| Add optional field | `"fleet_name": "string"` | PATCH increment | ✅ Yes |
| Deprecate field | Mark `"rider_id"` deprecated | MINOR increment | ✅ Yes (with warnings) |
| Remove field | Delete `"rider_id"` | MAJOR increment | ❌ No (publish v2) |
| Change field type | `"amount": string` → `number` | MAJOR increment | ❌ No (publish v2) |
| Rename field | `"ride_id"` → `"trip_id"` | MAJOR increment | ❌ No (publish v2) |

### Migration Strategy

**For Breaking Changes (Major Version):**
1. Publish both `EventName.v1` and `EventName.v2` for 3 months (transition period)
2. Notify all consumers to migrate to v2
3. After 3 months, deprecate v1 (keep publishing for 3 more months with warnings)
4. After 6 months total, stop publishing v1

**Consumer Responsibilities:**
- MUST handle unknown fields gracefully (ignore or log)
- MUST check `event_version` field
- SHOULD subscribe to specific major versions

---

## 10. Consumer Idempotency for Event Handlers

### Decision
Event consumers MUST implement idempotency using **processed message tracking**.

### Rationale
- **At-Least-Once Delivery**: Kafka guarantees at-least-once, so duplicates are possible
- **Network Retries**: Transient failures may cause duplicate processing
- **Constitution Requirement**: Principle VII mandates idempotent operations

### Implementation Pattern

```csharp
public class ChargeRecordedEventHandler
{
    private readonly IProcessedMessagesRepository _processedMessages;
    
    public async Task Handle(ChargeRecordedEvent @event, CancellationToken ct)
    {
        // Check if already processed
        if (await _processedMessages.ExistsAsync(@event.EventId, ct))
        {
            _logger.LogInformation("Event {EventId} already processed, skipping", @event.EventId);
            return;
        }
        
        // Process event
        await _rideManagementService.UpdateRideBillingStatus(
            rideId: @event.Payload.RideId,
            status: "Billed",
            ledgerEntryIds: @event.Payload.LedgerEntryIds,
            ct);
        
        // Mark as processed
        await _processedMessages.AddAsync(@event.EventId, DateTime.UtcNow, ct);
    }
}
```

### Processed Messages Table

```sql
CREATE TABLE processed_event_messages (
    event_id UUID PRIMARY KEY,
    event_type VARCHAR(200) NOT NULL,
    processed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    processor_service VARCHAR(100) NOT NULL
);

-- Retention: 7 days (sliding window)
CREATE INDEX ix_processed_event_messages_date 
ON processed_event_messages(processed_at);

-- Cleanup job (runs daily)
DELETE FROM processed_event_messages 
WHERE processed_at < NOW() - INTERVAL '7 days';
```

---

## 11. Kafka Configuration & Partitioning

### Decision
Partition by `tenant_id` to preserve ordering within tenant boundaries.

### Rationale
- **Ordering Guarantee**: Events for same tenant are processed in order
- **Scalability**: Different tenants can be processed in parallel
- **Load Balancing**: Kafka automatically distributes partitions across consumers

### Kafka Topic Configuration

```yaml
topics:
  - name: accounting.charge-recorded.v1
    partitions: 12
    replication-factor: 3
    partition-key: tenant_id
    retention-ms: 604800000  # 7 days
    
  - name: accounting.payment-received.v1
    partitions: 12
    replication-factor: 3
    partition-key: tenant_id
    retention-ms: 604800000
    
  - name: accounting.invoice-generated.v1
    partitions: 12
    replication-factor: 3
    partition-key: tenant_id
    retention-ms: 604800000
```

### Producer Configuration

```csharp
var producerConfig = new ProducerConfig
{
    BootstrapServers = _configuration["Kafka:BootstrapServers"],
    Acks = Acks.All,  // Wait for all replicas
    EnableIdempotence = true,  // Exactly-once producer semantics
    MaxInFlight = 5,
    MessageTimeoutMs = 30000,
    CompressionType = CompressionType.Snappy,
    PartitionerName = "murmur2_random"  // Consistent hashing by key
};
```

### Consumer Configuration

```csharp
var consumerConfig = new ConsumerConfig
{
    BootstrapServers = _configuration["Kafka:BootstrapServers"],
    GroupId = "ride-management-service",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false,  // Manual commit after processing
    EnableAutoOffsetStore = false,
    IsolationLevel = IsolationLevel.ReadCommitted,  // Only read committed messages
    MaxPollIntervalMs = 300000  // 5 minutes
};
```

---

## Summary (Updated)

All technical research complete, including resolution of Constitution Check clarifications:

1. **Fixed-Point Arithmetic**: `decimal` type with `decimal(19,4)` precision
2. **Multi-Tenant Isolation**: Row-Level Security + application-level filtering
3. **Idempotency**: Business identifier-based with database unique constraints
4. **Observability**: OpenTelemetry with OTLP exporter
5. **EF Core Performance**: Batching, AsNoTracking, compiled queries, database-generated values
6. **Result Pattern**: FluentResults library with custom error types
7. **Integration Events**: ChargeRecorded, PaymentReceived, InvoiceGenerated (JSON via Kafka)
8. **Outbox Pattern**: Atomic event storage with background publisher
9. **Event Versioning**: Semantic versioning with 3-month migration window for breaking changes
10. **Consumer Idempotency**: Processed message tracking with 7-day retention
11. **Kafka Partitioning**: Partition by tenant_id for ordering + scalability

**Constitution Check Resolutions:**
✅ Event-driven architecture decisions finalized
✅ Outbox pattern implementation designed
✅ Event contracts and versioning strategy defined
✅ Consumer idempotency patterns established

**Status**: ✅ Ready for Phase 1 (Design & Contracts)
