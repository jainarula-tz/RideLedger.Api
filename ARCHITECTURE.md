# RideLedger.Api - Clean Architecture Implementation

## 🏗️ Architecture Overview

This project implements **Clean/Onion Architecture** with clear separation of concerns following Domain-Driven Design (DDD) principles. Built for a senior .NET developer with focus on production-grade patterns.

```
┌─────────────────────────────────────────────────────────┐
│                     API Layer                            │
│  Controllers │ Middleware │ Filters │ Extensions        │
│              (Presentation)                              │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│              Application Layer                           │
│  Commands │ Queries │ Handlers │ Validators             │
│              (Use Cases)                                 │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│              Domain Layer                                │
│  Aggregates │ Entities │ Value Objects │ Events         │
│  (Business Logic - Zero Dependencies)                    │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│           Infrastructure Layer                           │
│  DbContext │ Repositories │ Persistence │ Auth          │
│              (External Concerns)                         │
└─────────────────────────────────────────────────────────┘
```

## ✅ Implemented Components

### 1. **Domain Layer** (Zero Dependencies)
- ✅ **Primitives**: `ValueObject`, `Entity<TId>`, `AggregateRoot<TId>`, `IDomainEvent`
- ✅ **Value Objects**: `Money` (fixed-point arithmetic), `AccountId`, `RideId`, `PaymentReferenceId`
- ✅ **Enums**: `AccountType`, `AccountStatus`, `LedgerAccountType`, `SourceType`, `PaymentMode`, `BillingFrequency`
- ✅ **Entities**: `LedgerEntry` (immutable)
- ✅ **Aggregates**: `Account` (with business logic for RecordCharge, RecordPayment, GetBalance)
- ✅ **Domain Events**: `AccountCreatedEvent`, `ChargeRecordedEvent`, `PaymentReceivedEvent`, etc.
- ✅ **Domain Errors**: `AccountErrors`, `LedgerErrors`, `InvoiceErrors` (using FluentResults)
- ✅ **Repository Interfaces**: `IAccountRepository`, `IUnitOfWork`

### 2. **Application Layer** (Use Cases)
- ✅ **Commands**: `CreateAccountCommand`, `RecordChargeCommand`, `RecordPaymentCommand`
- ✅ **Command Handlers**: (To be completed - MediatR handlers)
- ✅ **Queries**: (To be completed - GetBalance, GetStatement, etc.)
- ✅ **Interfaces**: `ITenantProvider`

### 3. **Infrastructure Layer** (External Concerns)
- ✅ **Authentication**: `TenantProvider` (extracts tenant/user from JWT claims)
- ⏳ **DbContext**: (To be completed - EF Core with PostgreSQL)
- ⏳ **Repositories**: (To be completed - Account, Invoice repositories)
- ⏳ **Outbox Pattern**: (To be completed - for reliable event publishing)

### 4. **API Layer** (Presentation)

#### ✅ **Middleware** (Comprehensive Logging & Exception Handling)
```csharp
// 1. GlobalExceptionHandlerMiddleware
//    - Catches all unhandled exceptions
//    - Returns RFC 9457 Problem Details responses
//    - Logs with TraceId for debugging
//    - Maps exceptions to appropriate HTTP status codes

// 2. RequestLoggingMiddleware
//    - Logs all HTTP requests/responses
//    - Tracks correlation IDs (X-Correlation-Id header)
//    - Measures request duration
//    - structured logging with Serilog
```

#### ✅ **Filters** (Authorization & Validation)
```csharp
// 1. TenantAuthorizationFilter
//    - Validates JWT token
//    - Extracts tenant_id claim
//    - Enforces multi-tenant isolation
//    - Adds TenantId to HttpContext.Items

// 2. ValidationFilter
//    - FluentValidation integration
//    - Validates request models at API boundary
//    - Returns structured validation errors

// 3. PerformanceMonitoringFilter
//    - Tracks action execution time
//    - Logs slow requests (>1000ms threshold)
//    - Adds X-Response-Time-Ms header
```

#### ✅ **JWT Authentication**
```csharp
// AuthenticationExtensions.cs
builder.Services.AddJwtAuthentication(configuration);
builder.Services.AddAuthorizationPolicies();

// Policies:
// - AdminOnly: Requires Admin or SuperAdmin role
// - BillingAdmin: Requires Admin or BillingAdmin role
// - TenantAccess: Requires tenant_id claim
```

### 5. **Configuration**
- ✅ **Directory.Build.props**: Centralized project config (nullable enabled, warnings as errors)
- ✅ **Directory.Packages.props**: Centralized NuGet package management
- ✅ **.editorconfig**: C# coding standards (naming, formatting, style rules)
- ✅ **appsettings.json**: JWT, CORS, connection strings

## 📦 Solution Structure

```
RideLedger.Api/
├── Directory.Build.props              # Common project properties
├── Directory.Packages.props           # Centralized NuGet packages
├── .editorconfig                      # C# coding standards
├── RideLedger.sln                     # Solution file
│
├── src/
│   ├── RideLedger.Domain/            # ✅ Domain Layer (Complete)
│   │   ├── Aggregates/
│   │   │   └── Account.cs
│   │   ├── Entities/
│   │   │   └── LedgerEntry.cs
│   │   ├── Enums/
│   │   │   ├── AccountType.cs
│   │   │   ├── AccountStatus.cs
│   │   │   ├── LedgerAccountType.cs
│   │   │   ├── SourceType.cs
│   │   │   ├── PaymentMode.cs
│   │   │   └── BillingFrequency.cs
│   │   ├── Errors/
│   │   │   ├── AccountErrors.cs
│   │   │   ├── LedgerErrors.cs
│   │   │   └── InvoiceErrors.cs
│   │   ├── Events/
│   │   │   └── DomainEvents.cs
│   │   ├── Primitives/
│   │   │   ├── AggregateRoot.cs
│   │   │   ├── Entity.cs
│   │   │   ├── ValueObject.cs
│   │   │   └── IDomainEvent.cs
│   │   ├── Repositories/
│   │   │   ├── IAccountRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── ValueObjects/
│   │       ├── Money.cs
│   │       ├── AccountId.cs
│   │       ├── RideId.cs
│   │       └── PaymentReferenceId.cs
│   │
│   ├── RideLedger.Application/       # ⏳ Application Layer (Partial)
│   │   ├── Commands/
│   │   │   ├── Accounts/
│   │   │   │   └── CreateAccountCommand.cs
│   │   │   ├── Charges/
│   │   │   │   └── RecordChargeCommand.cs
│   │   │   └── Payments/
│   │   │       └── RecordPaymentCommand.cs
│   │   └── Common/
│   │       └── ITenantProvider.cs
│   │
│   ├── RideLedger.Infrastructure/    # ⏳ Infrastructure Layer (Partial)
│   │   └── Authentication/
│   │       └── TenantProvider.cs
│   │
│   └── RideLedger.API/               # ✅ API Layer (Complete)
│       ├── Extensions/
│       │   └── AuthenticationExtensions.cs
│       ├── Filters/
│       │   ├── TenantAuthorizationFilter.cs
│       │   ├── ValidationFilter.cs
│       │   └── PerformanceMonitoringFilter.cs
│       ├── Middleware/
│       │   ├── GlobalExceptionHandlerMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Program.cs                # ✅ Complete middleware pipeline
│       └── appsettings.json          # ✅ JWT, CORS, connection strings
│
└── tests/                             # ⏳ Test Projects (To be implemented)
    ├── RideLedger.Domain.Tests/
    ├── RideLedger.Application.Tests/
    ├── RideLedger.Infrastructure.Tests/
    └── RideLedger.API.Tests/
```

## 🔐 JWT Authentication Setup

### 1. Configure JWT Settings

Edit `appsettings.json`:
```json
"JwtSettings": {
  "Issuer": "https://your-auth-server.com",
  "Audience": "rideledger-api",
  "SecretKey": "YOUR_SECURE_SECRET_KEY_MIN_32_CHARS",
  "ExpirationMinutes": 60
}
```

### 2. Required JWT Claims

The middleware expects these claims in the JWT token:
- `tenant_id` (Guid) - **Required** for multi-tenant isolation
- `sub` or `NameIdentifier` - User ID
- `email` - User email (optional)
- `role` - User roles (e.g., "Admin", "BillingAdmin")

### 3. Example JWT Payload

```json
{
  "sub": "user-123-guid",
"email": "user@example.com",
  "tenant_id": "tenant-456-guid",
  "role": ["Admin", "BillingAdmin"],
  "iss": "https://your-auth-server.com",
  "aud": "rideledger-api",
  "exp": 1735689600
}
```

## middleware Pipeline Order (Critical!)

The middleware in `Program.cs` executes in this order:

```csharp
1. HTTPS Redirection              // Security first
2. Swagger (Dev only)             // API documentation
3. RequestLoggingMiddleware       // ✅ Log all requests
4. CORS                           // Allow frontend origins
5. Authentication                 // ✅ Validate JWT token
6. Authorization                  // ✅ Check permissions
7. GlobalExceptionHandlerMiddleware // ✅ Catch all exceptions
8. Controllers (with Filters)     // ✅ Business logic
```

## 🔍 Filters Execution Order

Filters execute in this order:
```csharp
1. TenantAuthorizationFilter      // ✅ Validate JWT & extract tenant
2. ValidationFilter               // ✅ Validate request models
3. PerformanceMonitoringFilter    // ✅ Track execution time
```

## 🚀 Getting Started

### Prerequisites
- .NET 9 SDK
- PostgreSQL 17
- Visual Studio 2022 / VS Code / Rider

### Build & Run
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run API
cd src/RideLedger.API
dotnet run

# Access Swagger UI
# Navigate to: http://localhost:5000
```

### Test JWT Authentication in Swagger

1. Generate test JWT token (use jwt.io or your auth service)
2. Click "Authorize" button in Swagger UI
3. Enter: `Bearer <your-jwt-token>`
4. All requests will include Authorization header

## 📊 Logging & Observability

### Serilog Configuration
- **Console sink**: Real-time logs in terminal
- **File sink**: Rolling logs in `logs/rideledger-{Date}.log`
- **Retention**: 30 days
- **Enrichers**: MachineName, EnvironmentName, ThreadId, CorrelationId

### Log Structured Data
```csharp
_logger.LogInformation(
    "Charge recorded for account {AccountId}, amount: {Amount}",
    accountId,
    amount);
```

### Performance Monitoring
- Slow requests (>1000ms) are automatically logged with WARNING level
- All responses include `X-Response-Time-Ms` header
- Correlation IDs track requests across services

## 🛡️ Security Features

1. **JWT Authentication**: Industry-standard token validation
2. **Multi-Tenant Isolation**: Tenant ID validation at filter level
3. **HTTPS Enforcement**: Production requires HTTPS
4. **CORS Policy**: Whitelist frontend origins
5. **Exception Masking**: Internal errors hidden from clients
6. **Audit Logging**: All operations logged with user/tenant context

## 📋 Next Steps (In Priority Order)

### Phase 1: Complete Infrastructure Layer
- [ ] Implement `AccountingDbContext` (EF Core with PostgreSQL)
- [ ] Configure entity mappings with `EntityTypeConfiguration`
- [ ] Implement `AccountRepository` and `UnitOfWork`
- [ ] Add database migrations
- [ ] Implement Row-Level Security (RLS) for tenant isolation

### Phase 2: Complete Application Layer
- [ ] Implement MediatR command handlers
- [ ] Implement query handlers (GetBalance, GetStatement)
- [ ] Add FluentValidation validators for all commands
- [ ] Implement domain event dispatcher

### Phase 3: Complete API Layer
- [ ] Create controllers (AccountsController, ChargesController, PaymentsController)
- [ ] Add DTOs (Request/Response models)
- [ ] Implement mappings (Mapperly)

### Phase 4: Testing
- [ ] Domain unit tests (Account aggregate logic)
- [ ] Application tests (Handler logic)
- [ ] Integration tests (API endpoints with TestContainers)
- [ ] Performance tests

### Phase 5: Advanced Features
- [ ] Outbox pattern for reliable event publishing
- [ ] Redis caching for balance queries
- [ ] Polly resilience policies (retry, circuit breaker)
- [ ] OpenTelemetry tracing

## 📚 Resources

- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [DDD Aggregates](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457.html)
- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [ASP.NET Core Filters](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters)

## 👷 Author Notes (Senior Developer)

### Design Decisions
1. **Value Objects**: Prevent primitive obsession with strongly-typed IDs
2. **Result Pattern**: Avoid exceptions for expected failures (FluentResults)
3. **Domain Events**: Enable loose coupling between aggregates
4. **Immutable Entities**: LedgerEntry is append-only for audit compliance
5. **Middleware Over Filters**: Exception handling needs to catch filter errors
6. **Filter Order Matters**: Auth → Validation → Performance monitoring

### Production Checklist
- [ ] Replace `SecretKey` with RSA keys (Azure Key Vault)
- [ ] Enable HTTPS enforcement (UseHttpsRedirection)
- [ ] Configure connection string from environment variables
- [ ] Set up application insights / ELK stack
- [ ] Configure rate limiting
- [ ] Add health check dependencies (database, cache)
- [ ] Implement graceful shutdown
- [ ] Set resource limits in Kubernetes

---

**Status**: ✅ Clean Architecture Foundation Complete  
**Next**: Implement DbContext, Repositories, and Controllers
