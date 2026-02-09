# Multi-stage Dockerfile for RideLedger API
# Build stage: Compiles the application
# Runtime stage: Runs the compiled application with minimal dependencies

# ========================================
# Build Stage
# ========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY RideLedger.sln ./

# Copy project files for restore
COPY src/RideLedger.Domain/RideLedger.Domain.csproj src/RideLedger.Domain/
COPY src/RideLedger.Application/RideLedger.Application.csproj src/RideLedger.Application/
COPY src/RideLedger.Infrastructure/RideLedger.Infrastructure.csproj src/RideLedger.Infrastructure/
COPY src/RideLedger.Presentation/RideLedger.Presentation.csproj src/RideLedger.Presentation/
COPY tests/RideLedger.Domain.Tests/RideLedger.Domain.Tests.csproj tests/RideLedger.Domain.Tests/
COPY tests/RideLedger.Application.Tests/RideLedger.Application.Tests.csproj tests/RideLedger.Application.Tests/
COPY tests/RideLedger.Infrastructure.Tests/RideLedger.Infrastructure.Tests.csproj tests/RideLedger.Infrastructure.Tests/
COPY tests/RideLedger.Presentation.Tests/RideLedger.Presentation.Tests.csproj tests/RideLedger.Presentation.Tests/

# Restore dependencies (cached layer if project files unchanged)
RUN dotnet restore

# Copy all source code
COPY src/ src/
COPY tests/ tests/

# Build and publish the application
# Use Release configuration for optimizations
# Output to /app/publish directory
WORKDIR /src/src/RideLedger.Presentation
RUN dotnet publish -c Release -o /app/publish --no-restore

# ========================================
# Runtime Stage
# ========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r rideledger && useradd -r -g rideledger rideledger

# Copy published application from build stage
COPY --from=build /app/publish .

# Set ownership to non-root user
RUN chown -R rideledger:rideledger /app

# Switch to non-root user
USER rideledger

# Expose ports
# 8080: HTTP (default in .NET 9 without HTTPS in container)
# 8081: HTTPS (if configured)
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health/live || exit 1

# Entry point
ENTRYPOINT ["dotnet", "RideLedger.Presentation.dll"]
