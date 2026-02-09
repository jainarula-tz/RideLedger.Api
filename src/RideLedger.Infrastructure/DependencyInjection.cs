using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RideLedger.Application.Common;
using RideLedger.Application.Services;
using RideLedger.Domain.Repositories;
using RideLedger.Infrastructure.Authentication;
using RideLedger.Infrastructure.Persistence;
using RideLedger.Infrastructure.Persistence.Repositories;
using RideLedger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore.InMemory;

namespace RideLedger.Infrastructure;

/// <summary>
/// INFRASTRUCTURE LAYER - Dependency Injection
/// Extension methods for registering infrastructure services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Context - Use InMemory for testing, PostgreSQL for production
        var useInMemoryDatabase = configuration["UseInMemoryDatabase"] == "true" || 
                                  configuration["UseInMemoryDatabase"] ==  "True";
        
        if (useInMemoryDatabase)
        {
            // In-memory database for testing - use database name from configuration or default
            var databaseName = configuration["InMemoryDatabaseName"] ?? $"TestDatabase_{Guid.NewGuid()}";
            services.AddDbContext<AccountingDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.EnableSensitiveDataLogging(true);
                options.EnableDetailedErrors(true);
            });
        }
        else
        {
            // PostgreSQL database for production
            services.AddDbContext<AccountingDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("PostgreSQL"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                        npgsqlOptions.CommandTimeout(30);
                        npgsqlOptions.MigrationsAssembly("RideLedger.Infrastructure");
                    });

                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);
            });
        }

        // Tenant Provider
        services.AddScoped<ITenantProvider, TenantProvider>();

        // Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();

        // Health Checks - Only add PostgreSQL check if not using in-memory database
        if (!useInMemoryDatabase)
        {
            services.AddHealthChecks()
                .AddNpgSql(
                    configuration.GetConnectionString("PostgreSQL") ?? throw new InvalidOperationException("PostgreSQL connection string is missing"),
                    name: "postgresql",
                    tags: new[] { "db", "sql", "postgresql" });
        }
        else
        {
            // Add a simple health check for testing
            services.AddHealthChecks();
        }

        return services;
    }
}
