using Microsoft.EntityFrameworkCore;
using RideLedger.Application.Common;
using RideLedger.Domain.Aggregates;
using RideLedger.Infrastructure.Persistence.Entities;

namespace RideLedger.Infrastructure.Persistence;

/// <summary>
/// INFRASTRUCTURE LAYER - Database Context
/// EF Core DbContext for the accounting service
/// Implements tenant isolation via global query filters
/// </summary>
public sealed class AccountingDbContext : DbContext
{
    private readonly ITenantProvider? _tenantProvider;
    private Guid _tenantId = Guid.Empty;

    public AccountingDbContext(DbContextOptions<AccountingDbContext> options)
        : base(options)
    {
    }

    public AccountingDbContext(
        DbContextOptions<AccountingDbContext> options,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
        
        try
        {
            _tenantId = tenantProvider.GetTenantId();
        }
        catch
        {
            // During migrations, tenant provider will fail - this is expected
            _tenantId = Guid.Empty;
        }
    }

    // DbSets for persistence entities  
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<LedgerEntryEntity> LedgerEntries => Set<LedgerEntryEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceLineItemEntity> InvoiceLineItems => Set<InvoiceLineItemEntity>();
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountingDbContext).Assembly);

        // Configure lowercase_snake_case naming convention for PostgreSQL
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table names
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            // Column names
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }

            // Foreign key names
            foreach (var key in entity.GetForeignKeys())
            {
                key.SetConstraintName(key.GetConstraintName()?.ToSnakeCase());
            }

            // Index names
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }

        //Global query filter for tenant isolation
        modelBuilder.Entity<AccountEntity>().HasQueryFilter(a => a.TenantId == _tenantId);
        modelBuilder.Entity<LedgerEntryEntity>().HasQueryFilter(l => l.TenantId == _tenantId);
        modelBuilder.Entity<InvoiceEntity>().HasQueryFilter(i => i.TenantId == _tenantId);
        modelBuilder.Entity<OutboxMessageEntity>().HasQueryFilter(o => o.TenantId == _tenantId);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Use timestamptz for all DateTime properties (UTC storage)
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp with time zone");
    }
}

/// <summary>
/// Extension for converting PascalCase to snake_case
/// </summary>
internal static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
