using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RideLedger.Infrastructure.Persistence;

/// <summary>
/// INFRASTRUCTURE LAYER - Design-Time DbContext Factory
/// Used by EF Core tools for migrations
/// </summary>
public sealed class AccountingDbContextFactory : IDesignTimeDbContextFactory<AccountingDbContext>
{
    public AccountingDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Build DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<AccountingDbContext>();
        optionsBuilder.UseNpgsql(
            configuration.GetConnectionString("PostgreSQL") ?? "Host=localhost;Database=rideledger;Username=postgres;Password=Pass@123",
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("RideLedger.Infrastructure");
            });

        return new AccountingDbContext(optionsBuilder.Options);
    }
}
