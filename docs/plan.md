# Implementation Plan: Dual-Entry Accounting & Invoicing Service

**Branch**: `backend-api` | **Date**: 2026-02-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/backend-api/spec.md`

## Summary

Build a production-grade dual-entry accounting and invoicing service as a .NET 10 microservice. The system records ride charges and payments as immutable ledger entries following double-entry bookkeeping principles, calculates account balances in real-time, and generates on-demand invoices with multiple billing frequencies. The service implements domain-driven design with clear layer separation, uses PostgreSQL for persistence with EF Core, enforces strong consistency for financial operations, and includes comprehensive resilience patterns (retry, circuit breaker, timeout) via Polly. Multi-tenant data isolation is mandatory. All operations are idempotent to prevent duplicate charges and payments.

## Technical Context

**Language/Version**: .NET 10 (C#) with Native AOT support for optimized cold starts  
**Primary Dependencies**: ASP.NET Core 10 (Minimal API), EF Core 10, FluentValidation, Polly (resilience), Serilog (structured logging), OpenTelemetry, Mapperly (source-generated mapping), Npgsql (PostgreSQL driver)  
**Storage**: PostgreSQL 17 with JSONB support for EDI payload archiving, indexed queries, and audit logs  
**Testing**: xUnit for unit tests, integration tests for repositories and API endpoints, contract tests for inter-service communication  
**Target Platform**: Linux server via Docker containers orchestrated by Kubernetes  
**Project Type**: Backend microservice (single deployable unit with clear layer separation)  
**Performance Goals**: Charge/payment recording <100ms (p95), Balance queries <50ms (p95), Invoice generation <2 seconds, Support 1,000 concurrent requests  
**Constraints**: 99.9% uptime, Strong consistency for ledger entries, Immutable append-only ledger, Idempotent operations, Multi-tenant data isolation, Fixed-point arithmetic for monetary calculations  
**Scale/Scope**: 10,000 accounts per tenant, Support horizontal scaling for read queries, Efficient query performance with up to 10,000 transactions per account

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Production-Grade Code Mandate ✅ PASS
- **Status**: Compliant
- **Evidence**: Service architecture includes resilience patterns (Polly), structured logging (Serilog), OpenTelemetry tracing, proper CancellationToken propagation, resource disposal patterns, and performance-optimized queries with AsNoTracking()

### Feature/Domain-First Architecture ✅ PASS
- **Status**: Compliant
- **Evidence**: Single bounded context (Accounting & Invoicing), database-per-service pattern, no cross-service shared domain entities, clear domain boundaries

### Security by Default ✅ PASS
- **Status**: Compliant
- **Evidence**: Input validation via FluentValidation at API boundary, JWT authentication required, policy-based authorization, no secrets in code (Key Vault), sensitive data masking in logs

### Performance-First Design ✅ PASS
- **Status**: Compliant
- **Evidence**: API targets defined (<100ms p95 for writes, <50ms p95 for balance queries), pagination for list queries, AsNoTracking() for reads, indexing strategy planned

### TypeScript Strict Mode & .NET Strong Typing ✅ PASS
- **Status**: Compliant
- **Evidence**: .NET 10 with nullable reference types enabled, no dynamic types, Result pattern for expected failures

### Domain-Driven Design Layers ✅ PASS
- **Status**: Compliant
- **Evidence**: Clear layer separation planned (Domain, Application, Infrastructure, Presentation), domain entities separate from persistence entities, domain has zero infrastructure dependencies

### Distributed System Resilience ✅ PASS
- **Status**: Compliant
- **Evidence**: Service implements idempotent operations, retry with exponential backoff, circuit breaker, timeouts via Polly, graceful degradation, correlation IDs for observability

### Result Pattern Over Exceptions ✅ PASS
- **Status**: Compliant
- **Evidence**: Business validation errors return Result types, exceptions reserved for infrastructure failures, RFC 9457 Problem Details for API errors

### Database Standards ✅ PASS
- **Status**: Compliant
- **Evidence**: PostgreSQL 17 with proper naming conventions (lowercase_snake_case), entity separation (domain vs persistence), AsNoTracking() for reads, indexing strategy for WHERE/JOIN/ORDER BY clauses, migrations with Up/Down methods

### Testing Requirements ✅ PASS
- **Status**: Compliant
- **Evidence**: Unit tests for domain logic and handlers, integration tests for repositories, API tests for endpoints, contract tests for future inter-service communication

### CQRS & Event-Driven Architecture ✅ PASS (Resolved)
- **Status**: Compliant - clarifications resolved in Phase 0 research
- **Evidence**: Commands/queries separation planned, aggregates with proper boundaries, integration events defined
- **Resolution**: 
  1. ✅ Invoice generation will emit `InvoiceGeneratedEvent` for Notification Service integration
  2. ✅ Payment recording will emit `PaymentReceivedEvent` for analytics and confirmation workflows
  3. ✅ Event contracts defined in research.md with semantic versioning strategy (v1.0.0)
  4. ✅ Outbox pattern implementation designed for reliable event publishing
  5. ✅ Consumer idempotency patterns established with processed message tracking
  6. ✅ Kafka partitioning by `tenant_id` for ordering and scalability

### Data Consistency & Saga Patterns ✅ PASS (Confirmed)
- **Status**: Compliant for current scope
- **Evidence**: Service operates within single bounded context, strong consistency for ledger transactions, Outbox pattern implemented for future extensibility
- **Confirmation**: Outbox pattern implemented from start despite single-context scope, enabling seamless future cross-service integration without architectural changes

**GATE EVALUATION**: ✅ **PASS** - All gates compliant. Event-driven architecture clarifications resolved in Phase 0 research. Ready for Phase 2 (Task Breakdown).

**POST-PHASE 1 RE-EVALUATION**: ✅ **CONFIRMED** - All constitution gates pass with research-backed decisions. Design artifacts (data-model.md, contracts/, quickstart.md) align with constitutional requirements. No violations.

## Project Structure

### Documentation (this feature)

```text
specs/backend-api/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
AccountingService/
├── src/
│   ├── AccountingService.Domain/              # Domain layer (zero infrastructure deps)
│   │   ├── Aggregates/                        # Account (root), Ledger, Invoice
│   │   ├── Entities/                          # Domain entities
│   │   ├── ValueObjects/                      # Money, AccountType, LedgerAccountType
│   │   ├── DomainEvents/                      # ChargeRecorded, PaymentReceived, InvoiceGenerated
│   │   ├── DomainExceptions/                  # Infrastructure failure exceptions only
│   │   └── Repositories/                      # Repository abstractions (interfaces)
│   │
│   ├── AccountingService.Application/         # Application layer (use cases)
│   │   ├── Commands/                          # RecordCharge, RecordPayment, GenerateInvoice
│   │   ├── Queries/                           # GetBalance, GetAccountStatement, GetInvoice
│   │   ├── Handlers/                          # CommandHandlers, QueryHandlers
│   │   ├── Validators/                        # FluentValidation validators
│   │   ├── DTOs/                              # Data transfer objects
│   │   ├── Mappers/                           # Mapperly source-generated mappers
│   │   └── Common/                            # Result pattern, shared application concerns
│   │
│   ├── AccountingService.Infrastructure/      # Infrastructure layer (EF Core, persistence)
│   │   ├── Persistence/
│   │   │   ├── DbContext/                     # AccountingDbContext
│   │   │   ├── Entities/                      # Persistence entities (EF Core models)
│   │   │   ├── Configurations/                # EF Core entity configurations
│   │   │   ├── Repositories/                  # Repository implementations
│   │   │   └── Migrations/                    # EF Core migrations
│   │   ├── Outbox/                            # Outbox pattern implementation
│   │   ├── Mappers/                           # Domain ↔ Persistence mappers
│   │   └── DependencyInjection/               # Infrastructure service registration
│   │
│   └── AccountingService.API/                 # Presentation layer (ASP.NET Core Minimal API)
│       ├── Controllers/                       # API controllers (or Endpoints for Minimal API)
│       ├── Middleware/                        # Global exception handler, logging, auth
│       ├── Models/                            # Request/Response models
│       ├── Extensions/                        # Service collection extensions
│       └── Program.cs                         # Application entry point
│
└── tests/
    ├── AccountingService.UnitTests/           # Domain logic, handlers, validators
    ├── AccountingService.IntegrationTests/    # Repository, database, API integration
    └── AccountingService.ContractTests/       # Event contract validation (future)
```

**Structure Decision**: Adopting DDD-aligned layered architecture for a single backend microservice. The four-layer separation (Domain, Application, Infrastructure, Presentation) enforces zero infrastructure dependency in Domain layer, encapsulates business logic in Application layer, and isolates persistence concerns in Infrastructure layer. This structure supports the Constitution's requirement for clear layer boundaries and enables independent testing of each layer. The Domain layer contains aggregates (Account, Ledger, Invoice) with private setters and domain events. Infrastructure uses separate persistence entities that are mapped to/from domain entities to avoid polluting domain with EF Core concerns.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Status**: No violations. All constitution gates pass without exceptions.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | No complexity violations in this feature | All architecture decisions align with constitutional principles |
