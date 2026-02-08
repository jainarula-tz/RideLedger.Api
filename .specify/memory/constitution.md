# RideLedger Constitution

## Core Principles

### I. Production-Grade Code Mandate
All code MUST implement resilience patterns (retry, circuit breaker, timeout) via Polly. All services MUST include structured logging (Serilog) with correlation IDs, OpenTelemetry distributed tracing, proper CancellationToken propagation, and resource disposal patterns. Performance-optimized queries MUST use AsNoTracking() for read operations.

### II. Feature/Domain-First Architecture
Services MUST follow domain-driven design with single bounded context. Each service MUST use database-per-service pattern. Domain entities MUST NOT be shared across service boundaries. Clear domain boundaries are mandatory.

### III. Security by Default
All API endpoints MUST require JWT authentication. Input validation MUST use FluentValidation at API boundary. Policy-based authorization MUST be enforced. Secrets MUST be stored in Key Vault, never in code. Sensitive data MUST be masked in logs.

### IV. Performance-First Design
All APIs MUST define performance targets (p95 latency). List queries MUST implement pagination. Read operations MUST use AsNoTracking(). Database queries MUST have indexing strategy for WHERE/JOIN/ORDER BY clauses.

### V. Strong Typing
.NET projects MUST enable nullable reference types. Dynamic types are forbidden. Result pattern MUST be used for expected failures instead of exceptions.

### VI. Domain-Driven Design Layers
Projects MUST separate into clear layers: Domain (zero infrastructure dependencies), Application (use cases), Infrastructure (persistence, external services), Presentation (API endpoints). Domain entities MUST be separate from persistence entities. Domain MUST have zero infrastructure dependencies.

### VII. Distributed System Resilience
All operations MUST be idempotent. Services MUST implement retry with exponential backoff, circuit breaker, and timeouts. Graceful degradation MUST be supported. Correlation IDs MUST be propagated for observability.

### VIII. Result Pattern Over Exceptions
Business validation errors MUST return Result types. Exceptions MUST be reserved for infrastructure failures only. API errors MUST follow RFC 9457 Problem Details format.

### IX. Database Standards
PostgreSQL MUST use lowercase_snake_case naming. Entity separation (domain vs persistence) is mandatory. Read operations MUST use AsNoTracking(). Indexes MUST be created for WHERE/JOIN/ORDER BY clauses. Migrations MUST have Up and Down methods.

### X. Testing Requirements
Unit tests MUST cover domain logic and handlers. Integration tests MUST cover repositories and database operations. API tests MUST cover all endpoints. Contract tests MUST validate inter-service communication contracts. Minimum 70% code coverage required.

### XI. CQRS & Event-Driven Architecture
Commands and queries MUST be separated. Aggregates MUST have proper boundaries. Integration events MUST be defined for cross-service communication. Event contracts MUST use semantic versioning. Outbox pattern MUST be implemented for reliable event publishing.

### XII. Data Consistency & Saga Patterns
Services within single bounded context MUST use strong consistency. Ledger transactions MUST guarantee all debits equal credits. Outbox pattern MUST be implemented for future cross-service extensibility.

## Quality Gates

### Performance Thresholds
- Write operations: <100ms (p95)
- Read operations: <50ms (p95)
- Complex operations (reports, invoicing): <2 seconds

### Availability Standards
- Service uptime: 99.9% minimum
- Zero data loss for financial transactions
- Multi-tenant data isolation: 100% (zero cross-tenant leakage)

### Code Quality
- No compiler warnings allowed
- Nullable reference types enabled
- Treat warnings as errors in builds

## Governance

This constitution supersedes all other development practices. Any architecture decision requiring exception to these principles MUST be documented in plan.md Complexity Tracking section with explicit justification.

All pull requests MUST verify constitutional compliance before merge. Complexity requiring deviation from principles MUST be justified with simpler alternatives documented.

**Version**: 1.0.0 | **Ratified**: 2026-02-08 | **Last Amended**: 2026-02-08
