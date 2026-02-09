# RideLedger.Api - Onion Architecture Structure

## 🧅 Onion Architecture Layers

This project follows **Onion Architecture** (Clean Architecture) with strict dependency rules flowing inward toward the domain core.

```
┌─────────────────────────────────────────────────────────────┐
│                  PRESENTATION LAYER (API)                    │
│  - Controllers (HTTP Routing)                               │
│  - Middleware (Request/Response Pipeline)                   │
│  - Filters (Authorization, Validation, Monitoring)          │
│                                                             │
│  Dependencies: → Application Layer                          │
└──────────────────────┬──────────────────────────────────────┘
                       │ uses ↓
┌──────────────────────▼──────────────────────────────────────┐
│              INFRASTRUCTURE LAYER                            │
│  - EF Core DbContext & Repositories                         │
│  - External APIs Integration                                │
│  - Logging (Serilog), Caching (Redis)                       │
│  - Authentication (JWT, TenantProvider)                     │
│                                                             │
│  Dependencies: → Application Layer ← implements             │
└──────────────────────┬──────────────────────────────────────┘
                       │ implements ↓
┌──────────────────────▼──────────────────────────────────────┐
│              APPLICATION LAYER (Business Logic)              │
│  - Services (Business Operations)                           │
│  - DTOs (Data Transfer Objects)                             │
│  - Commands & Queries (CQRS)                                │
│  - Interfaces (IRepository, IUnitOfWork)                    │
│  - Validators (FluentValidation)                            │
│                                                             │
│  Dependencies: → Domain Layer                               │
└──────────────────────┬──────────────────────────────────────┘
                       │ depends on ↓
┌──────────────────────▼──────────────────────────────────────┐
│              DOMAIN LAYER (Entities/Core)                    │
│  - Entities (Account, LedgerEntry)                          │
│  - Value Objects (Money, AccountId)                         │
│  - Domain Events (ChargeRecorded, PaymentReceived)          │
│  - Business Rules (Double-Entry Logic)                      │
│                                                             │
│  Dependencies: NONE ✓ (Pure Business Logic)                │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

```
RideLedger.Api/
│
├── src/
│   │
│   ├── 🎯 RideLedger.Domain/              # DOMAIN LAYER (Core)
│   │   ├── Aggregates/                    # Aggregate Roots
│   │   │   └── Account.cs                 # Account aggregate with business logic
│   │   ├── Entities/                      # Domain entities
│   │   │   └── LedgerEntry.cs             # Immutable ledger entry
│   │   ├── ValueObjects/                  # Value Objects (Identity, Money)
│   │   │   ├── Money.cs                   # Fixed-point arithmetic
│   │   │   ├── AccountId.cs               # Strongly-typed ID
│   │   │   ├── RideId.cs
│   │   │   └── PaymentReferenceId.cs
│   │   ├── Enums/                         # Domain enumerations
│   │   │   ├── AccountType.cs
│   │   │   ├── AccountStatus.cs
│   │   │   ├── LedgerAccountType.cs
│   │   │   ├── SourceType.cs
│   │   │   ├── PaymentMode.cs
│   │   │   └── BillingFrequency.cs
│   │   ├── Events/                        # Domain events
│   │   │   └── DomainEvents.cs
│   │   ├── Errors/                        # Domain errors (FluentResults)
│   │   │   ├── AccountErrors.cs
│   │   │   ├── LedgerErrors.cs
│   │   │   └── InvoiceErrors.cs
│   │   ├── Primitives/                    # Base classes
│   │   │   ├── Entity.cs
│   │   │   ├── AggregateRoot.cs
│   │   │   ├── ValueObject.cs
│   │   │   └── IDomainEvent.cs
│   │   └── Repositories/                  # Repository interfaces
│   │       ├── IAccountRepository.cs
│   │       └── IUnitOfWork.cs
│   │
│   ├── 🔵 RideLedger.Application/         # APPLICATION LAYER (Use Cases)
│   │   ├── Commands/                      # CQRS Commands
│   │   │   ├── Accounts/
│   │   │   │   └── CreateAccountCommand.cs
│   │   │   ├── Charges/
│   │   │   │   └── RecordChargeCommand.cs
│   │   │   └── Payments/
│   │   │       └── RecordPaymentCommand.cs
│   │   ├── Queries/                       # CQRS Queries (To be implemented)
│   │   ├── DTOs/                          # ⭐ Data Transfer Objects
│   │   │   ├── Accounts/
│   │   │   │   └── AccountDTOs.cs         # CreateAccountRequest, AccountResponse
│   │   │   ├── Charges/
│   │   │   │   └── ChargeDTOs.cs          # RecordChargeRequest, ChargeResponse
│   │   │   ├── Payments/
│   │   │   │   └── PaymentDTOs.cs
│   │   │   └── Balances/
│   │   │       └── BalanceDTOs.cs
│   │   ├── Services/                      # ⭐ Service Interfaces
│   │   │   └── Interfaces.cs              # IAccountService, IChargeService, IPaymentService
│   │   ├── Validators/                    # ⭐ FluentValidation
│   │   │   ├── CreateAccountCommandValidator.cs
│   │   │   ├── RecordChargeCommandValidator.cs
│   │   │   └── RecordPaymentCommandValidator.cs
│   │   └── Common/
│   │       └── ITenantProvider.cs         # Tenant context interface
│   │
│   ├── ⚙️ RideLedger.Infrastructure/      # INFRASTRUCTURE LAYER (External)
│   │   ├── Persistence/                   # EF Core (To be implemented)
│   │   │   ├── DbContext/
│   │   │   ├── Configurations/            # Entity configurations
│   │   │   ├── Repositories/              # Repository implementations
│   │   │   └── Migrations/
│   │   ├── Authentication/                # ⭐ JWT & Tenant Context
│   │   │   └── TenantProvider.cs          # Extracts tenant from JWT claims
│   │   ├── Logging/                       # Serilog configuration (To be added)
│   │   ├── Caching/                       # Redis caching (To be added)
│   │   └── ExternalAPIs/                  # Integration with external services
│   │
│   └── 🌐 RideLedger.API/                 # PRESENTATION LAYER (HTTP)
│       ├── Controllers/                   # ⭐ HTTP Endpoints
│       │   ├── AccountsController.cs      # Account management endpoints
│       │   ├── ChargesController.cs       # Charge recording endpoints
│       │   └── PaymentsController.cs      # Payment recording endpoints
│       ├── Middleware/                    # ⭐ Request Pipeline
│       │   ├── GlobalExceptionHandlerMiddleware.cs  # RFC 9457 error handling
│       │   └── RequestLoggingMiddleware.cs          # Structured logging + correlation
│       ├── Filters/                       # ⭐ Action Filters
│       │   ├── TenantAuthorizationFilter.cs         # JWT validation & tenant extraction
│       │   ├── ValidationFilter.cs                  # FluentValidation integration
│       │   └── PerformanceMonitoringFilter.cs       # Slow request detection
│       ├── Extensions/                    # Configuration helpers
│       │   └── AuthenticationExtensions.cs          # JWT + Authorization policies
│       ├── Program.cs                     # ⭐ Application entry point
│       └── appsettings.json               # Configuration (JWT, CORS, DB)
│
└── tests/                                  # Test Projects
    ├── RideLedger.Domain.Tests/           # Domain logic tests
    ├── RideLedger.Application.Tests/      # Use case tests
    ├── RideLedger.Infrastructure.Tests/   # Repository & integration tests
    └── RideLedger.API.Tests/              # API endpoint tests
```

## 🔗 Dependency Flow

```
API Layer
   ↓ uses
Application Layer (DTOs, Services, Validators)
   ↓ depends on
Domain Layer (Entities, Business Rules)
   ↑ implements
Infrastructure Layer (Repositories, DbContext, Auth)
```

### ✅ Valid Dependencies
- ✅ API → Application
- ✅ API → Infrastructure (Dependency Injection only)
- ✅ Application → Domain
- ✅ Infrastructure → Application (implements interfaces)
- ✅ Infrastructure → Domain (to persist entities)

### ❌ Invalid Dependencies (Violations)
- ❌ Domain → Application
- ❌ Domain → Infrastructure
- ❌ Domain → API
- ❌ Application → Infrastructure (except interfaces)

## 🎯 Layer Responsibilities

### **DOMAIN LAYER** (Core)
**Location**: `src/RideLedger.Domain/`  
**Purpose**: Pure business logic with ZERO external dependencies

- ✅ **Entities**: `Account`, `LedgerEntry` (with behavior)
- ✅ **Value Objects**: `Money`, `AccountId`, `RideId`
- ✅ **Business Rules**: Double-entry accounting, idempotency
- ✅ **Domain Events**: `ChargeRecorded`, `PaymentReceived`
- ✅ **Validation**: Amount > 0, Account must be Active

### **APPLICATION LAYER** (Use Cases)
**Location**: `src/RideLedger.Application/`  
**Purpose**: Orchestrate business workflows

- ✅ **DTOs**: Request/Response models for API layer
- ✅ **Commands**: `CreateAccountCommand`, `RecordChargeCommand`
- ✅ **Queries**: `GetAccountBalanceQuery` (to be implemented)
- ✅ **Validators**: FluentValidation rules for commands
- ✅ **Services**: `IAccountService`, `IChargeService`
- ✅ **Interfaces**: `IRepository`, `ITenantProvider`

### **INFRASTRUCTURE LAYER** (External Concerns)
**Location**: `src/RideLedger.Infrastructure/`  
**Purpose**: Implement external integrations

- ✅ **EF Core**: `AccountingDbContext`, repositories
- ✅ **Authentication**: `TenantProvider` (JWT claim extraction)
- ⏳ **Logging**: Serilog configuration (to be added)
- ⏳ **Caching**: Redis implementation (to be added)
- ⏳ **External APIs**: Integration with ride/payment services

### **PRESENTATION LAYER** (HTTP API)
**Location**: `src/RideLedger.API/`  
**Purpose**: Handle HTTP requests/responses

- ✅ **Controllers**: `AccountsController`, `ChargesController`, `PaymentsController`
- ✅ **Middleware**: Exception handling, request logging
- ✅ **Filters**: Authorization, validation, performance monitoring
- ✅ **Extensions**: JWT authentication setup

## 🚀 Request Processing Flow

### Example: Record Charge Request

```
1. HTTP POST /api/v1/charges
   ↓
2. RequestLoggingMiddleware (logs request + correlation ID)
   ↓
3. CORS Middleware
   ↓
4. Authentication Middleware (validates JWT token)
   ↓
5. TenantAuthorizationFilter (extracts tenant_id from JWT)
   ↓
6. ValidationFilter (validates RecordChargeRequest using FluentValidation)
   ↓
7. PerformanceMonitoringFilter (starts timer)
   ↓
8. ChargesController.RecordCharge()
   ↓
9. Application Layer: RecordChargeCommand → Handler
   ↓
10. Domain Layer: Account.RecordCharge() (business logic)
   ↓
11. Infrastructure Layer: AccountRepository.Save() (EF Core)
   ↓
12. Response: 201 Created with ChargeResponse DTO
   ↓
13. GlobalExceptionHandlerMiddleware (catches any errors)
   ↓
14. RequestLoggingMiddleware (logs response + duration)
```

## 📊 Benefits of This Structure

### 1. **Testability**
- Domain layer has zero dependencies → easy unit testing
- Application layer uses interfaces → mockable services
- Controllers test HTTP concerns only

### 2. **Maintainability**
- Clear separation of concerns
- Changes in one layer don't affect others
- Easy to locate and fix bugs

### 3. **Scalability**
- Swap EF Core for Dapper without touching domain
- Add Redis caching without changing business logic
- Replace JWT with OAuth without domain changes

### 4. **Team Collaboration**
- Frontend team works with DTOs only
- Domain experts focus on business rules
- Infrastructure team handles external services

## 🔐 Security Features

### **JWT Authentication** (Infrastructure + API)
- Bearer token validation in `AuthenticationExtensions.cs`
- Claims extraction in `TenantProvider.cs`
- Multi-tenant isolation via `TenantAuthorizationFilter.cs`

### **Authorization Policies** (API)
- `AdminOnly`: Requires Admin or SuperAdmin role
- `BillingAdmin`: Requires billing permissions
- `TenantAccess`: Requires tenant_id claim (default)

## 📝 Next Implementation Steps

1. ✅ **Domain Layer** - Complete
2. ✅ **Application DTOs & Validators** - Complete
3. ✅ **API Controllers & Filters** - Complete
4. ⏳ **Infrastructure Repositories** - To be implemented
5. ⏳ **MediatR Handlers** - To be implemented
6. ⏳ **Unit Tests** - To be implemented

---

**Architecture Status**: ✅ **Onion Architecture Fully Structured**  
**All layers properly separated with correct dependencies!**
