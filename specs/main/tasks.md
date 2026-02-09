# Tasks: Dual-Entry Accounting & Invoicing Service

**Feature**: main  
**Branch**: `backend-api`  
**Generated**: 2026-02-08  
**Total Tasks**: 201

---

## Task Organization

Tasks are organized by **user story** to enable independent implementation and testing. Each user story phase can be completed and tested independently before moving to the next.

**Priority Mapping**:
- **P1 Stories** (US1, US2): Core accounting functionality - Record charges and view balances
- **P2 Stories** (US3, US4): Payment tracking and invoicing
- **P3 Stories** (US5, US6): Account management and reporting

---

## Phase 1: Setup & Project Initialization

**Goal**: Initialize .NET 10 solution with DDD layers, configure infrastructure, and set up development environment.

**Completion Criteria**: Solution compiles, all projects reference correctly, Docker builds successfully.

### Tasks

- [X] T001 Create .NET 10 solution file at AccountingService.sln
- [X] T002 [P] Create AccountingService.Domain project (Class Library, .NET 10) at src/AccountingService.Domain/
- [X] T003 [P] Create AccountingService.Application project (Class Library, .NET 10) at src/AccountingService.Application/
- [X] T004 [P] Create AccountingService.Infrastructure project (Class Library, .NET 10) at src/AccountingService.Infrastructure/
- [X] T005 [P] Create AccountingService.API project (ASP.NET Core Web API, .NET 10) at src/AccountingService.API/
- [X] T006 [P] Create AccountingService.UnitTests project (xUnit, .NET 10) at tests/AccountingService.UnitTests/
- [X] T007 [P] Create AccountingService.IntegrationTests project (xUnit, .NET 10) at tests/AccountingService.IntegrationTests/
- [X] T008 Add project references: Application → Domain, Infrastructure → Domain + Application, API → All layers
- [X] T009 Configure Directory.Build.props at root/ with common properties (nullable enabled, implicit usings, treat warnings as errors)
- [X] T010 Configure Directory.Packages.props at root/ with centralized NuGet package versions
- [X] T011 [P] Add NuGet packages to Domain: FluentResults
- [X] T012 [P] Add NuGet packages to Application: FluentValidation, Mapperly, MediatR (optional per research decision)
- [X] T013 [P] Add NuGet packages to Infrastructure: Npgsql.EntityFrameworkCore.PostgreSQL 8.0, Polly, Serilog, OpenTelemetry, Confluent.Kafka
- [X] T014 [P] Add NuGet packages to API: Microsoft.AspNetCore.OpenApi, Swashbuckle.AspNetCore, FluentValidation.AspNetCore
- [ ] T015 Create .editorconfig at root/ with C# coding standards from constitution
- [X] T016 Create appsettings.json at src/AccountingService.API/ with ConnectionStrings, Kafka, Redis, OpenTelemetry configuration
- [X] T017 Create appsettings.Development.json with local development overrides
- [ ] T018 Create Dockerfile at root/ with multi-stage build (build → runtime)
- [ ] T019 Create docker-compose.yml at root/ with PostgreSQL, Redis, Kafka services for local development
- [X] T020 Create .dockerignore at root/ excluding bin, obj, .git

---

## Phase 2: Foundational Layer (Non-Negotiable Prerequisites)

**Goal**: Implement core infrastructure that ALL user stories depend on (database context, value objects, result pattern, API infrastructure).

**Completion Criteria**: Database connects, migrations run, API accepts requests, global error handling works, OpenTelemetry exports telemetry.

**Blocking**: Must complete before ANY user story implementation.

### Tasks

- [X] T021 Implement Money value object at src/AccountingService.Domain/ValueObjects/Money.cs with decimal(19,4), operators (+, -), validation
- [X] T022 [P] Implement AccountId value object at src/AccountingService.Domain/ValueObjects/AccountId.cs as strongly-typed GUID wrapper
- [X] T023 [P] Implement RideId value object at src/AccountingService.Domain/ValueObjects/RideId.cs as strongly-typed string wrapper
- [X] T024 [P] Implement PaymentReferenceId value object at src/AccountingService.Domain/ValueObjects/PaymentReferenceId.cs
- [ ] T025 Implement Result pattern base classes at src/AccountingService.Application/Common/Result.cs using FluentResults
- [X] T026 [P] Create domain error definitions at src/AccountingService.Domain/Errors/AccountErrors.cs (NotFound, Inactive, TenantMismatch)
- [X] T027 [P] Create domain error definitions at src/AccountingService.Domain/Errors/LedgerErrors.cs (DuplicateCharge, DuplicatePayment, InvalidAmount)
- [X] T028 Create AccountingDbContext at src/AccountingService.Infrastructure/Persistence/DbContext/AccountingDbContext.cs with DbSets
- [X] T029 Configure DbContext with lowercase_snake_case naming convention, PostgreSQL timestamptz, JSONB support
- [X] T029a Configure EF Core to use timestamptz type for all DateTime properties and add integration test validating UTC storage
- [X] T030 Implement ITenantProvider at src/AccountingService.Application/Common/ITenantProvider.cs interface
- [X] T031 Implement TenantProvider at src/AccountingService.Infrastructure/MultiTenancy/TenantProvider.cs extracting tenant_id from JWT claims
- [X] T032 Configure EF Core global query filters for tenant_id in AccountingDbContext OnModelCreating
- [ ] T033a Configure PostgreSQL Row-Level Security on accounts table with tenant_isolation_policy in migration
- [ ] T033b Configure PostgreSQL Row-Level Security on ledger_entries table with tenant_isolation_policy in migration
- [ ] T033c Configure PostgreSQL Row-Level Security on invoices table with tenant_isolation_policy in migration
- [ ] T034 Create base repository interface IRepository<T> at src/AccountingService.Domain/Repositories/IRepository.cs
- [ ] T035 Implement base repository at src/AccountingService.Infrastructure/Persistence/Repositories/RepositoryBase.cs with tenant filtering
- [ ] T036 Configure Polly resilience policies at src/AccountingService.Infrastructure/Resilience/ResiliencePolicies.cs (retry, circuit breaker, timeout)
- [X] T037 Create global exception handling middleware at src/AccountingService.API/Middleware/GlobalExceptionHandlerMiddleware.cs returning RFC 9457 Problem Details
- [X] T038 Configure Serilog with structured logging at src/AccountingService.API/Program.cs (console + file sinks, correlation IDs)
- [ ] T039 Configure OpenTelemetry tracing at src/AccountingService.API/Program.cs (traces, metrics, logs with OTLP exporter)
- [ ] T040 Add ActivitySource for custom spans at src/AccountingService.Application/Common/ActivitySources.cs
- [X] T041 Configure Swagger/OpenAPI generation at src/AccountingService.API/Program.cs with JWT bearer auth, examples
- [X] T042 Create health check endpoints at src/AccountingService.API/HealthChecks/ (/health/live, /health/ready, /health/startup)
- [X] T043 Configure CORS policy at src/AccountingService.API/Program.cs allowing frontend origin
- [X] T044 Create initial EF Core migration "InitialCreate" with outbox_messages table schema
- [ ] T045 Implement database seeding for development at src/AccountingService.Infrastructure/Persistence/Seed/DatabaseSeeder.cs

---

## Phase 3: User Story 1 - Record Ride Charges (P1)

**Story Goal**: Enable system to record completed ride charges to account ledger with double-entry accounting.

**Independent Test Criteria**: 
- ✅ Can create an account via API
- ✅ Can post a ride charge and verify two ledger entries created (debit AR, credit Revenue)
- ✅ Duplicate charge for same Ride ID returns 409 Conflict
- ✅ Charge for inactive account returns 400 Bad Request
- ✅ Ledger entries are immutable after creation

### Tasks

- [X] T046 [P] [US1] Create Account aggregate root at src/AccountingService.Domain/Aggregates/Account.cs with private setters, collections
- [X] T047 [P] [US1] Create LedgerEntry entity at src/AccountingService.Domain/Entities/LedgerEntry.cs with DebitAmount/CreditAmount, immutability
- [X] T048 [US1] Add RecordCharge method to Account aggregate validating status, checking duplicates, creating debit/credit entries
- [X] T049 [P] [US1] Create AccountType enum at src/AccountingService.Domain/Enums/AccountType.cs (Organization, Individual)
- [X] T050 [P] [US1] Create AccountStatus enum at src/AccountingService.Domain/Enums/AccountStatus.cs (Active, Inactive)
- [X] T051 [P] [US1] Create LedgerAccountType enum at src/AccountingService.Domain/Enums/LedgerAccountType.cs (AccountsReceivable, ServiceRevenue, Cash)
- [X] T052 [P] [US1] Create SourceType enum at src/AccountingService.Domain/Enums/SourceType.cs (Ride, Payment)
- [X] T052a [P] [US3] Create PaymentMode enum at src/AccountingService.Domain/Enums/PaymentMode.cs (Cash, Card, BankTransfer)
- [ ] T053 [US1] Create ChargeRecordedEvent domain event at src/AccountingService.Domain/Events/ChargeRecordedEvent.cs
- [ ] T054 [US1] Create RecordChargeCommand at src/AccountingService.Application/Commands/RecordChargeCommand.cs with properties
- [X] T055 [US1] Create RecordChargeCommandValidator at src/AccountingService.Application/Validators/RecordChargeCommandValidator.cs using FluentValidation
- [X] T056 [US1] Create RecordChargeCommandHandler at src/AccountingService.Application/Handlers/RecordChargeCommandHandler.cs returning Result<Guid>
- [X] T057 [US1] Implement handler logic: load account, call RecordCharge, save, publish event to outbox
- [X] T058 [P] [US1] Create AccountEntity (persistence model) at src/AccountingService.Infrastructure/Persistence/Entities/AccountEntity.cs
- [X] T059 [P] [US1] Create LedgerEntryEntity (persistence model) at src/AccountingService.Infrastructure/Persistence/Entities/LedgerEntryEntity.cs
- [X] T060 [P] [US1] Create AccountEntityConfiguration at src/AccountingService.Infrastructure/Persistence/Configurations/AccountEntityConfiguration.cs with table name, indexes
- [X] T061 [P] [US1] Create LedgerEntryEntityConfiguration with unique index on (account_id, source_reference_id) WHERE source_type = 'Ride'
- [ ] T061a [US1] Configure ledger_entries table as insert-only via database trigger preventing UPDATE/DELETE operations for immutability enforcement
- [ ] T062 [US1] Create domain-to-persistence mapper at src/AccountingService.Infrastructure/Mappers/AccountMapper.cs using Mapperly
- [X] T063 [US1] Create IAccountRepository at src/AccountingService.Domain/Repositories/IAccountRepository.cs with GetByIdAsync, SaveAsync
- [X] T064 [US1] Implement AccountRepository at src/AccountingService.Infrastructure/Persistence/Repositories/AccountRepository.cs
- [X] T065 [US1] Create RecordChargeRequest DTO at src/AccountingService.API/Models/RecordChargeRequest.cs
- [X] T066 [US1] Create RecordChargeResponse DTO at src/AccountingService.API/Models/RecordChargeResponse.cs
- [X] T067 [US1] Create ChargesController at src/AccountingService.API/Controllers/ChargesController.cs with POST /api/v1/charges endpoint
- [X] T068 [US1] Add EF Core migration "AddChargeSupport" with accounts, ledger_entries tables
- [X] T069 [US1] Write unit test for Account.RecordCharge at tests/AccountingService.UnitTests/Domain/AccountTests.cs
- [X] T070 [US1] Write unit test for RecordChargeCommandHandler at tests/AccountingService.UnitTests/Handlers/RecordChargeCommandHandlerTests.cs
- [ ] T071 [US1] Write integration test for POST /api/v1/charges at tests/AccountingService.IntegrationTests/Controllers/ChargesControllerTests.cs
- [ ] T071a [US1] Write integration test verifying ledger entries include populated audit fields (source_type, source_reference_id, created_at, created_by from JWT)

---

## Phase 4: User Story 2 - Calculate and Retrieve Balance (P1)

**Story Goal**: Enable balance queries to understand current financial position.

**Independent Test Criteria**:
- ✅ Account with $500 charges returns balance = $500
- ✅ Account with $500 charges and $300 payments returns balance = $200
- ✅ Account with no transactions returns balance = $0
- ✅ Balance query completes in <50ms (p95)

### Tasks

- [X] T072 [US2] Create GetAccountBalanceQuery at src/AccountingService.Application/Queries/GetAccountBalanceQuery.cs
- [X] T073 [US2] Create GetAccountBalanceQueryHandler at src/AccountingService.Application/Handlers/GetAccountBalanceQueryHandler.cs
- [X] T074 [US2] Implement handler with EF Core query: SELECT SUM(debit_amount) - SUM(credit_amount) using AsNoTracking()
- [ ] T075 [US2] Add composite index on ledger_entries(account_id, tenant_id) INCLUDE (debit_amount, credit_amount) in migration
- [ ] T076 [US2] Create AccountBalanceResponse DTO at src/AccountingService.API/Models/AccountBalanceResponse.cs
- [X] T077 [US2] Add GET /api/v1/accounts/{accountId}/balance endpoint to AccountsController
- [X] T078 [US2] Write unit test for GetAccountBalanceQueryHandler at tests/AccountingService.UnitTests/Handlers/GetAccountBalanceQueryHandlerTests.cs
- [X] T079 [US2] Write integration test for GET balance endpoint verifying calculation correctness
- [ ] T080 [US2] Add performance test verifying p95 latency ≤ 50ms with 10,000 transactions

---

## Phase 5: User Story 3 - Record Payments (P2)

**Story Goal**: Record received payments to reduce Accounts Receivable balance.

**Independent Test Criteria**:
- ✅ Payment of $300 against $500 balance reduces balance to $200
- ✅ Overpayment of $600 against $500 balance creates -$100 credit balance
- ✅ Duplicate Payment Reference ID returns 409 Conflict
- ✅ Payment to invalid account returns 404 Not Found

### Tasks

- [X] T081 [US3] Add RecordPayment method to Account aggregate creating debit Cash, credit AR entries with optional PaymentMode parameter
- [ ] T082 [US3] Create PaymentReceivedEvent at src/AccountingService.Domain/Events/PaymentReceivedEvent.cs
- [ ] T083 [US3] Create RecordPaymentCommand at src/AccountingService.Application/Commands/RecordPaymentCommand.cs
- [X] T084 [US3] Create RecordPaymentCommandValidator with FluentValidation rules
- [X] T085 [US3] Create RecordPaymentCommandHandler at src/AccountingService.Application/Handlers/RecordPaymentCommandHandler.cs
- [ ] T086 [US3] Add unique index on ledger_entries(source_reference_id) WHERE source_type = 'Payment' in migration
- [X] T087 [US3] Create RecordPaymentRequest DTO at src/AccountingService.API/Models/RecordPaymentRequest.cs
- [X] T088 [US3] Create RecordPaymentResponse DTO at src/AccountingService.API/Models/RecordPaymentResponse.cs
- [X] T089 [US3] Create PaymentsController with POST /api/v1/payments endpoint
- [X] T090 [US3] Write unit tests for Account.RecordPayment covering full payment, partial, overpayment scenarios
- [ ] T091 [US3] Write integration test for POST /api/v1/payments endpoint
- [ ] T092 [US3] Write integration test verifying idempotency with duplicate Payment Reference ID

---

## Phase 6: User Story 4 - Generate Invoices (P2)

**Story Goal**: Generate invoices for accounts with flexible billing periods.

**Independent Test Criteria**:
- ✅ Monthly invoice for January includes all 5 rides from that month
- ✅ Per-ride invoice contains only single ride
- ✅ Invoice references specific ledger entry IDs for traceability
- ✅ Invoice generation completes in <2 seconds
- ✅ Invoices are immutable once generated

### Tasks

- [X] T093 [P] [US4] Create Invoice aggregate at src/AccountingService.Domain/Aggregates/Invoice.cs
- [X] T094 [P] [US4] Create InvoiceLineItem entity at src/AccountingService.Domain/Entities/InvoiceLineItem.cs
- [X] T095 [P] [US4] Create BillingFrequency enum at src/AccountingService.Domain/Enums/BillingFrequency.cs (PerRide, Daily, Weekly, Monthly)
- [X] T096 [US4] Implement Invoice.Generate static factory method calculating subtotal, payments, outstanding balance
- [X] T096a [US4] Implement InvoiceNumberGenerator service at src/AccountingService.Application/Services/InvoiceNumberGenerator.cs with tenant-scoped sequential pattern (INV-{Sequence:D6})
- [ ] T096b [US4] Add IsImmutable shadow property to Invoice entity, enforce read-only constraint in SaveChanges override
- [X] T097 [US4] Create InvoiceGeneratedEvent at src/AccountingService.Domain/Events/InvoiceGeneratedEvent.cs
- [X] T098 [US4] Create GenerateInvoiceCommand at src/AccountingService.Application/Commands/GenerateInvoiceCommand.cs
- [X] T099 [US4] Create GenerateInvoiceCommandValidator with date range validation
- [X] T100 [US4] Create GenerateInvoiceCommandHandler at src/AccountingService.Application/Handlers/GenerateInvoiceCommandHandler.cs
- [X] T101 [US4] Implement handler logic: query ledger entries for date range, group by billing frequency, create invoice
- [X] T102 [P] [US4] Create InvoiceEntity persistence model at src/AccountingService.Infrastructure/Persistence/Entities/InvoiceEntity.cs
- [X] T103 [P] [US4] Create InvoiceLineItemEntity at src/AccountingService.Infrastructure/Persistence/Entities/InvoiceLineItemEntity.cs
- [X] T104 [P] [US4] Create InvoiceEntityConfiguration with invoices table, indexes on account_id, billing_period
- [X] T105 [US4] Create IInvoiceRepository at src/AccountingService.Domain/Repositories/IInvoiceRepository.cs
- [X] T106 [US4] Implement InvoiceRepository at src/AccountingService.Infrastructure/Persistence/Repositories/InvoiceRepository.cs
- [X] T107 [US4] Create GenerateInvoiceRequest DTO at src/AccountingService.API/Models/GenerateInvoiceRequest.cs
- [X] T108 [US4] Create InvoiceResponse DTO at src/AccountingService.API/Models/InvoiceResponse.cs
- [X] T109 [US4] Create InvoicesController with POST /api/v1/invoices/generate endpoint
- [X] T110 [US4] Add EF Core migration "AddInvoiceSupport" with invoices and invoice_line_items tables
- [ ] T111 [US4] Write unit test for Invoice.Generate with multiple line items
- [ ] T112 [US4] Write integration test for invoice generation with monthly billing frequency
- [ ] T113 [US4] Write integration test for empty date range (no billable items) returning validation error
- [ ] T113a [US4] Write integration test verifying invoice traceability (each line item references specific ledger entry IDs per FR-015)

---

## Phase 7: User Story 5 - Account Management (P3)

**Story Goal**: Create and manage accounts for organizations and individuals.

**Independent Test Criteria**:
- ✅ Can create account with valid details, returns 201 Created
- ✅ Account initializes with empty ledger and zero balance
- ✅ Duplicate Account ID returns 409 Conflict
- ✅ Can retrieve account details with GET /api/v1/accounts/{id}
- ✅ Transactions to inactive account return 400 Bad Request

### Tasks

- [X] T114 [US5] Implement Account.Create static factory method at Account aggregate
- [X] T115 [US5] Create AccountCreatedEvent at src/AccountingService.Domain/Events/AccountCreatedEvent.cs
- [X] T116 [US5] Create CreateAccountCommand at src/AccountingService.Application/Commands/CreateAccountCommand.cs
- [X] T117 [US5] Create CreateAccountCommandValidator with name, type validation
- [X] T118 [US5] Create CreateAccountCommandHandler at src/AccountingService.Application/Handlers/CreateAccountCommandHandler.cs
- [X] T119 [US5] Create GetAccountQuery at src/AccountingService.Application/Queries/GetAccountQuery.cs
- [X] T120 [US5] Create GetAccountQueryHandler at src/AccountingService.Application/Handlers/GetAccountQueryHandler.cs
- [X] T121 [US5] Create CreateAccountRequest DTO at src/AccountingService.API/Models/CreateAccountRequest.cs
- [X] T122 [US5] Create AccountResponse DTO at src/AccountingService.API/Models/AccountResponse.cs
- [X] T123 [US5] Create AccountsController with POST /api/v1/accounts and GET /api/v1/accounts/{id} endpoints
- [X] T124 [US5] Add unique index on accounts(account_id, tenant_id) in migration
- [X] T125 [US5] Write unit test for Account.Create factory method
- [ ] T126 [US5] Write integration test for POST /api/v1/accounts endpoint
- [ ] T127 [US5] Write integration test for GET /api/v1/accounts/{id} endpoint

---

## Phase 8: User Story 6 - Account Statements (P3)

**Story Goal**: Generate account statements showing all transactions for date range.

**Independent Test Criteria**:
- ✅ Statement for month 2 shows opening balance, transactions, closing balance
- ✅ Statement with no transactions shows opening balance = closing balance
- ✅ Transactions are chronologically ordered
- ✅ Statement generation completes in <3 seconds for 1 year of transactions

### Tasks

- [ ] T128 [US6] Create GetAccountStatementQuery at src/AccountingService.Application/Queries/GetAccountStatementQuery.cs with date range
- [ ] T129 [US6] Create GetAccountStatementQueryHandler at src/AccountingService.Application/Handlers/GetAccountStatementQueryHandler.cs
- [ ] T130 [US6] Implement handler: calculate opening balance, query transactions with pagination, calculate closing balance
- [ ] T131 [US6] Add composite index on ledger_entries(account_id, transaction_date DESC) in migration
- [ ] T132 [US6] Create AccountStatementResponse DTO at src/AccountingService.API/Models/AccountStatementResponse.cs
- [ ] T133 [US6] Add GET /api/v1/accounts/{accountId}/statements endpoint to AccountsController
- [ ] T134 [US6] Write unit test for statement query handler
- [ ] T135 [US6] Write integration test for GET statement endpoint with date range filter
- [ ] T154 [US6] Write performance test verifying p95 latency ≤ 3s with 10,000 transactions

---

## Phase 9: Edge Case Integration Tests

**Goal**: Validate all edge cases documented in spec.md to prevent production bugs.

**Completion Criteria**: All 8 edge cases from spec.md have passing integration tests.

### Tasks

- [ ] T155 [EDGE] Write integration test for concurrent charge recording to same account (multiple threads, verify data integrity and no race conditions)
- [ ] T156 [EDGE] Write integration test for zero-amount charge rejection (verify validator rejects $0.00 charges per FR-021)
- [ ] T157 [EDGE] Write integration test for negative-amount charge rejection (verify validator rejects negative amounts per FR-021)
- [ ] T158 [EDGE] Write integration test for zero-amount payment rejection (verify validator rejects $0.00 payments per FR-021)
- [ ] T159 [EDGE] Write integration test for negative-amount payment rejection (verify validator rejects negative amounts per FR-021)
- [ ] T160 [EDGE] Write integration test for charge to inactive account rejection (verify returns 400 Bad Request per FR-022)
- [ ] T161 [EDGE] Write integration test for payment to inactive account rejection (verify returns 400 Bad Request per FR-022)
- [ ] T162 [EDGE] Write integration test for large transaction volume (10,000 ledger entries, verify balance query performance and pagination)

---

## Phase 10: Event-Driven Architecture & Outbox Pattern

**Goal**: Implement reliable event publishing via Outbox pattern for integration events.

**Completion Criteria**: Events persisted to outbox table in same transaction as domain changes, background processor publishes to Kafka, consumers receive events.

### Tasks

- [X] T163 [P] Create OutboxMessage entity at src/AccountingService.Infrastructure/Outbox/OutboxMessage.cs
- [X] T164 [P] Add DbSet<OutboxMessage> to AccountingDbContext
- [X] T165 Create OutboxMessageConfiguration at src/AccountingService.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs
- [ ] T166 Add outbox_messages table to migration with indexes on (processed_at, occurred_at), tenant_id
- [ ] T167 Implement IDomainEventHandler interface at src/AccountingService.Application/Common/IDomainEventHandler.cs
- [ ] T168 Create OutboxWriter at src/AccountingService.Infrastructure/Outbox/OutboxWriter.cs to serialize events to outbox
- [ ] T169 Hook OutboxWriter into SaveChangesAsync in AccountingDbContext to intercept domain events
- [ ] T170 Create IKafkaProducer interface at src/AccountingService.Application/Common/IKafkaProducer.cs
- [ ] T171 Implement KafkaProducer at src/AccountingService.Infrastructure/Messaging/KafkaProducer.cs with Polly resilience
- [ ] T172 Create OutboxProcessor background service at src/AccountingService.Infrastructure/Outbox/OutboxProcessor.cs (IHostedService)
- [ ] T173 Implement OutboxProcessor logic: query unprocessed messages every 5s, publish to Kafka, mark processed
- [ ] T174 Add retry logic with exponential backoff, circuit breaker after 5 failures, dead letter after 10 retries
- [ ] T175 Configure Kafka producer settings at src/AccountingService.Infrastructure/Messaging/KafkaConfiguration.cs
- [ ] T176 Create event serializers at src/AccountingService.Infrastructure/Messaging/EventSerializer.cs (domain event → JSON)
- [ ] T177 Register OutboxProcessor in API Program.cs as hosted service
- [ ] T178 Write unit test for OutboxWriter verifying events serialized correctly
- [ ] T179 Write integration test for OutboxProcessor publishing events to Kafka
- [ ] T180 Write integration test verifying transaction atomicity (domain changes + outbox in same transaction)

---

## Phase 11: Polish & Cross-Cutting Concerns

**Goal**: Add logging, metrics, documentation, and production readiness features.

**Completion Criteria**: OpenTelemetry exports metrics, Swagger UI documents all endpoints, health checks pass, containerized app runs.

### Tasks

- [ ] T181 [P] Add high-performance LoggerMessage attributes to all handlers at src/AccountingService.Application/Handlers/
- [ ] T182 [P] Create custom metrics at src/AccountingService.Application/Metrics/AccountingMetrics.cs (charges recorded, payments recorded, invoice generation duration)
- [ ] T183 [P] Add ActivitySource spans to critical operations (RecordCharge, RecordPayment, GenerateInvoice)
- [ ] T184 Configure database health check at src/AccountingService.API/HealthChecks/DatabaseHealthCheck.cs
- [ ] T185 Configure Kafka health check at src/AccountingService.API/HealthChecks/KafkaHealthCheck.cs
- [ ] T186 Add Swagger examples for all request DTOs using IExamplesProvider
- [ ] T187 Add XML documentation comments to all public APIs in Domain, Application layers
- [ ] T188 Configure XML doc generation in csproj files
- [ ] T189 Add rate limiting middleware at src/AccountingService.API/Middleware/RateLimitingMiddleware.cs (100 req/min per tenant)
- [ ] T190 Add request/response logging middleware with correlation IDs
- [ ] T191 Create README.md at  with architecture overview, setup instructions, API documentation links
- [ ] T192 Create CHANGELOG.md at  with version history
- [ ] T193 Configure Docker health check in Dockerfile (curl /health/live)
- [ ] T194 Create kubernetes manifests at k8s/ (deployment.yaml, service.yaml, configmap.yaml, secrets.yaml)
- [ ] T195 Configure resource limits in Kubernetes deployment (CPU: 500m-1000m, Memory: 512Mi-1Gi)
- [ ] T196 Add liveness probe (GET /health/live), readiness probe (GET /health/ready), startup probe (GET /health/startup)
- [ ] T197 Create CI/CD pipeline at .github/workflows/backend-ci.yml (build, lint, test, docker build, push)
- [ ] T198 Add code coverage report generation to CI pipeline with 70% threshold
- [ ] T199 Add dependency vulnerability scanning to CI pipeline (dotnet list package --vulnerable)

---

## Dependency Graph (User Story Completion Order)

```
Phase 1 (Setup) → Phase 2 (Foundational)
                        ↓
                   ┌────────────┐
                   │   US5 (P3) │  ← Account Management (can be done first if needed)
                   │  Create    │
                   │  Accounts  │
                   └─────┬──────┘
                         ↓
        ┌────────────────┴────────────────┐
        ↓                                 ↓
   ┌────────────┐                    ┌────────────┐
   │   US1 (P1) │                    │   US2 (P1) │
   │   Record   │                    │  Calculate │
   │  Charges   │                    │   Balance  │
   └─────┬──────┘                    └─────┬──────┘
         │                                 │
         └────────────┬────────────────────┘
                      ↓
                 ┌────────────┐
                 │   US3 (P2) │
                 │   Record   │
                 │  Payments  │
                 └─────┬──────┘
                       ↓
        ┌──────────────┴──────────────┐
        ↓                             ↓
   ┌────────────┐                ┌────────────┐
   │   US4 (P2) │                │   US6 (P3) │
   │  Generate  │                │  Account   │
   │  Invoices  │                │ Statements │
   └────────────┘                └────────────┘
```

**Notes**:
- US5 (Create Accounts) can be implemented before US1 if desired, but test data can also use direct database seeding
- US1 and US2 are independent and can be developed in parallel after foundational layer
- US3 depends on US1 (needs charge recording to exist)
- US4 and US6 both depend on having charges/payments recorded

---

## Parallel Execution Opportunities

### High Parallelization (Can Work Simultaneously)

**Phase 1 Setup** (T002-T007):
- All project creation can happen in parallel
- NuGet package installation (T011-T014) can happen in parallel

**Phase 2 Foundational** (Value Objects T021-T024):
- Money, AccountId, RideId, PaymentReferenceId are independent

**Phase 2 Foundational** (Error Definitions T026-T027):
- AccountErrors and LedgerErrors are independent

**Per User Story**:
- **US1**: Persistence entities (T058-T059), entity configurations (T060-T061), DTOs (T065-T066) can be done in parallel
- **US3**: Similar parallelization as US1 for persistence layer
- **US4**: Invoice and InvoiceLineItem entities (T102-T103) can be done in parallel

### Low Parallelization (Sequential Dependencies)

**Cannot Parallelize**:
- Aggregate implementation (T046) must precede handler (T056)
- Repository interface (T063) must precede implementation (T064)
- Domain model must precede persistence model mapping
- Controller (T067) requires handler (T056) to be complete
- Tests require implementation to be complete

---

## Implementation Strategy

### MVP Scope (Deliver Value Fast)

**Week 1-2**: Phase 1 (Setup) + Phase 2 (Foundational) + US5 (Account Management)
- **Deliverable**: API that can create accounts and retrieve account details

**Week 3**: US1 (Record Charges) + US2 (Balance Calculation)
- **Deliverable**: Can record ride charges and view account balances
- **Business Value**: Core accounting functionality operational

**Week 4**: US3 (Record Payments)
- **Deliverable**: Can record payments, balance updates correctly
- **Business Value**: Complete transaction recording cycle

**Week 5**: US4 (Generate Invoices)
- **Deliverable**: Can generate invoices on-demand with multiple billing frequencies
- **Business Value**: Customer billing enabled

**Week 6**: US6 (Account Statements) + Phase 9 (Event-Driven) + Phase 10 (Polish)
- **Deliverable**: Full feature set with reliable event publishing and production readiness

### Incremental Delivery

After each user story phase:
1. Run integration tests
2. Update API documentation (Swagger)
3. Demo to stakeholders
4. Deploy to staging environment
5. Collect feedback before next phase

---

## Testing Summary

**Unit Tests**: 18 tests
- Domain logic: Account.RecordCharge, Account.RecordPayment, Invoice.Generate
- Handlers: All command/query handlers
- Validation: All FluentValidation validators

**Integration Tests**: 24 tests (includes 8 edge case tests)
- API endpoints: All POST/GET endpoints
- Database: Repository operations, migrations
- Outbox: Event publishing, transaction atomicity
- Performance: Balance query (p95 ≤ 50ms), invoice generation (p95 ≤ 2s), statement (p95 ≤ 3s)
- Edge cases: Concurrent transactions, zero/negative amounts, inactive accounts, large volumes

**Test Coverage Target**: 70%+ lines, 60%+ branches

**Edge Cases Covered**: All 8 edge cases from spec.md validated with integration tests

---

## Format Validation

✅ **ALL tasks follow checklist format**: `- [ ] [TaskID] [P?] [Story?] Description with file path`  
✅ **Task IDs sequential**: T001-T199  
✅ **User story labels**: [US1], [US2], [US3], [US4], [US5], [US6] applied correctly  
✅ **Parallelizable tasks marked**: [P] where applicable  
✅ **File paths specified**: All implementation tasks include file paths  

---

## Summary

- **Total Tasks**: 199
- **Setup Tasks**: 20
- **Foundational Tasks**: 27
- **User Story 1 (P1)**: 28 tasks (includes FR-016 immutability, FR-018 database enforcement)
- **User Story 2 (P1)**: 9 tasks
- **User Story 3 (P2)**: 13 tasks (includes FR-007 Payment Mode enum)
- **User Story 4 (P2)**: 23 tasks (includes FR-025 invoice number generator, FR-015 traceability test)
- **User Story 5 (P3)**: 14 tasks
- **User Story 6 (P3)**: 9 tasks
- **Edge Case Tests**: 8 tasks (validates all spec.md edge cases)
- **Event-Driven**: 18 tasks
- **Polish**: 19 tasks

**Estimated Effort**: 6-7 weeks (with 2-3 developers working in parallel)

**MVP Delivery**: Week 3 (US1 + US2 complete = core accounting functional)

**Status**: ✅ Ready for implementation - All CRITICAL and HIGH priority gaps resolved
