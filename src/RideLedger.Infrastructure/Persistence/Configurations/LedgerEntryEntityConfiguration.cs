using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RideLedger.Infrastructure.Persistence.Entities;

namespace RideLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// INFRASTRUCTURE LAYER - Entity Configuration
/// EF Core configuration for LedgerEntryEntity
/// Enforces idempotency via unique indexes
/// </summary>
public sealed class LedgerEntryEntityConfiguration : IEntityTypeConfiguration<LedgerEntryEntity>
{
    public void Configure(EntityTypeBuilder<LedgerEntryEntity> builder)
    {
        builder.ToTable("ledger_entries");

        builder.HasKey(l => l.LedgerEntryId);

        builder.Property(l => l.LedgerEntryId)
            .ValueGeneratedOnAdd();

        builder.Property(l => l.AccountId)
            .IsRequired();

        builder.Property(l => l.TenantId)
            .IsRequired();

        builder.Property(l => l.LedgerAccount)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(l => l.DebitAmount)
            .HasPrecision(19, 4); // decimal(19,4) for fixed-point arithmetic

        builder.Property(l => l.CreditAmount)
            .HasPrecision(19, 4);

        builder.Property(l => l.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(l => l.SourceType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(l => l.SourceReferenceId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.TransactionDate)
            .IsRequired();

        builder.Property(l => l.Metadata)
            .HasColumnType("jsonb"); // PostgreSQL JSONB for flexible metadata

        builder.Property(l => l.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(l => l.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        // Indexes for idempotency (prevent duplicate charges/payments)
        builder.HasIndex(l => new { l.AccountId, l.SourceReferenceId })
            .IsUnique()
            .HasFilter("source_type = 'Ride'")
            .HasDatabaseName("ix_ledger_entries_ride_idempotency");

        builder.HasIndex(l => l.SourceReferenceId)
            .IsUnique()
            .HasFilter("source_type = 'Payment'")
            .HasDatabaseName("ix_ledger_entries_payment_idempotency");

        // Index for balance calculations
        builder.HasIndex(l => new { l.AccountId, l.TenantId })
            .IncludeProperties(l => new { l.DebitAmount, l.CreditAmount })
            .HasDatabaseName("ix_ledger_entries_balance_calc");

        // Index for statement queries
        builder.HasIndex(l => new { l.AccountId, l.TransactionDate })
            .HasDatabaseName("ix_ledger_entries_statements");

        builder.HasIndex(l => l.TenantId)
            .HasDatabaseName("ix_ledger_entries_tenant_id");

        // Relationships
        builder.HasOne(l => l.Account)
            .WithMany(a => a.LedgerEntries)
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
