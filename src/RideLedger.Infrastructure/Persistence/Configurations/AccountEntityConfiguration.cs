using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RideLedger.Infrastructure.Persistence.Entities;

namespace RideLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// INFRASTRUCTURE LAYER - Entity Configuration
/// EF Core configuration for AccountEntity
/// </summary>
public sealed class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.AccountId);

        builder.Property(a => a.AccountId)
            .ValueGeneratedNever(); // Client-generated GUID

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.UpdatedAt);

        // Indexes
        builder.HasIndex(a => new { a.AccountId, a.TenantId })
            .IsUnique()
            .HasDatabaseName("ix_accounts_account_id_tenant_id");

        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("ix_accounts_tenant_id");

        // Relationships
        builder.HasMany(a => a.LedgerEntries)
            .WithOne(l => l.Account)
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
