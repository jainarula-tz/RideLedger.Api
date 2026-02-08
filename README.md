# RideLedger.Api

**Double-entry accounting and invoicing microservice for ride service platform**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## 📋 Overview

RideLedger.Api is a production-grade .NET 9 microservice implementing dual-entry accounting principles for ride service platforms. It provides comprehensive APIs for:

- 💰 **Account Management** - Create and manage financial accounts
- 🚗 **Charge Recording** - Record ride charges with automatic ledger entries
- 💳 **Payment Processing** - Handle payments with balance reconciliation
- 📄 **Invoice Generation** - Generate invoices from unbilled charges
- 📊 **Financial Reporting** - Retrieve account balances and transaction history

---

## 🏗️ Architecture

### Clean Architecture (DDD)

```
src/
├── RideLedger.Domain/          # Domain models, aggregates, value objects
├── RideLedger.Application/     # Use cases, DTOs, interfaces
├── RideLedger.Infrastructure/  # EF Core, repositories, external services
└── RideLedger.API/             # ASP.NET Core Minimal APIs, endpoints

tests/
├── RideLedger.Domain.Tests/
├── RideLedger.Application.Tests/
├── RideLedger.Infrastructure.Tests/
└── RideLedger.API.Tests/
```

### Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Runtime** | .NET | 9.0 |
| **Framework** | ASP.NET Core | 9.0 |
| **Database** | PostgreSQL | 17 |
| **ORM** | Entity Framework Core | 9.0 |
| **Messaging** | Apache Kafka | 3.6+ |
| **Caching** | Redis | 7.x |
| **Resilience** | Polly | 8.x |
| **Logging** | Serilog | 3.x |
| **Monitoring** | OpenTelemetry | 1.7+ |
| **Testing** | xUnit | 2.6+ |
| **Containerization** | Docker | 24+ |

---

## 🚀 Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PostgreSQL 17](https://www.postgresql.org/download/) (or via Docker)

### 1. Clone Repository

```bash
git clone https://github.com/jainarula-tz/RideLedger.Api.git
cd RideLedger.Api
```

### 2. Start Infrastructure

```bash
# Start PostgreSQL, Kafka, Redis via Docker Compose
docker-compose up -d

# Verify containers are running
docker ps
```

### 3. Configure Database

```bash
# Update connection string in appsettings.Development.json
cd src/RideLedger.API

# Apply EF Core migrations
dotnet ef database update
```

### 4. Run Application

```bash
# From solution root
dotnet run --project src/RideLedger.API

# API will be available at:
# - HTTP: http://localhost:5000
# - HTTPS: https://localhost:5001
# - Swagger: http://localhost:5000/swagger
```

### 5. Run Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReporter=lcov
```

---

## 📚 API Documentation

### Base URL

```
Development: http://localhost:5000/api/v1
Production:  https://api.ridledger.com/api/v1
```

### Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/accounts/{id}` | Get account details with balance |
| `POST` | `/accounts` | Create new account |
| `GET` | `/accounts/{id}/transactions` | List account transactions |
| `POST` | `/charges` | Record ride charge |
| `POST` | `/payments` | Record payment |
| `POST` | `/invoices/generate` | Generate invoice from unbilled charges |
| `GET` | `/invoices/{id}` | Get invoice details |
| `GET` | `/invoices/{id}/pdf` | Download invoice as PDF |

### Swagger/OpenAPI

Interactive API documentation available at `/swagger` when running locally.

---

## 🔧 Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Database=ridledger;Username=postgres;Password=***"

# Kafka
Kafka__BootstrapServers="localhost:9092"
Kafka__GroupId="ridledger-api"

# Redis
Redis__ConnectionString="localhost:6379"

# Authentication
JWT__Secret="your-secret-key-here"
JWT__Issuer="https://api.ridledger.com"
JWT__Audience="https://app.ridledger.com"

# Logging
Serilog__MinimumLevel="Information"
Serilog__WriteTo__0__Name="Console"
Serilog__WriteTo__1__Name="File"
Serilog__WriteTo__1__Args__path="logs/ridledger-.log"
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ridledger;Username=postgres"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "ridledger-api"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 🗄️ Database Schema

### Core Tables

- **Accounts** - Financial accounts (Asset, Liability, Equity, Revenue, Expense)
- **LedgerEntries** - Double-entry transactions (debit/credit pairs)
- **Invoices** - Customer invoices with line items
- **Payments** - Payment records linked to invoices

### Multi-Tenancy

Row-Level Security (RLS) policies enforce tenant isolation at database level.

```sql
CREATE POLICY tenant_isolation ON accounts
    USING (tenant_id = current_setting('app.tenant_id')::uuid);
```

---

## 🧪 Testing

### Test Structure

- **Unit Tests** - Domain logic, business rules
- **Integration Tests** - API endpoints, database operations
- **Performance Tests** - Load testing, benchmarks

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/RideLedger.Domain.Tests

# With coverage
dotnet test /p:CollectCoverage=true

# Filter by category
dotnet test --filter "Category=Integration"
```

---

## 📦 Deployment

### Docker

```bash
# Build image
docker build -t ridledger-api:latest .

# Run container
docker run -d -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  ridledger-api:latest
```

### Kubernetes

```bash
# Apply manifests
kubectl apply -f k8s/

# Check deployment
kubectl get pods -n ridledger
```

---

## 📊 Monitoring

### Health Checks

- `/health` - Overall health status
- `/health/ready` - Readiness probe (for K8s)
- `/health/live` - Liveness probe (for K8s)

### Metrics (OpenTelemetry)

- Request duration (p50, p95, p99)
- Error rates
- Database query performance
- Kafka message lag

### Logging (Serilog)

Structured logs with correlation IDs for request tracing.

---

## 🤝 Contributing

See [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) for development guidelines.

### Development Workflow

1. Create feature branch: `git checkout -b feature/your-feature`
2. Follow coding conventions in [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md)
3. Write tests (70%+ coverage required)
4. Create pull request

---

## 📝 Documentation

- **[Specification](docs/spec.md)** - Feature requirements and user stories
- **[Implementation Plan](docs/plan.md)** - Architecture and technical decisions
- **[Tasks](docs/tasks.md)** - 199 implementation tasks
- **[Data Model](docs/data-model.md)** - Database schema and domain model
- **[Research](docs/research.md)** - Technical research and decisions

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/jainarula-tz/RideLedger.Api/issues)
- **Documentation**: [Wiki](https://github.com/jainarula-tz/RideLedger.Api/wiki)
- **Email**: support@ridledger.com

---

**Built with ❤️ for ride service platforms**
