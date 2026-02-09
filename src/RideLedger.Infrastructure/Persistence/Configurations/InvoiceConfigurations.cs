using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RideLedger.Infrastructure.Persistence.Entities;

namespace RideLedger.Infrastructure.Persistence.Configurations;

public sealed class InvoiceEntityConfiguration : IEntityTypeConfiguration<InvoiceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceEntity> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(i => i.Id);
        
        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(i => i.BillingFrequency).IsRequired().HasConversion<string>();
        builder.Property(i => i.Status).IsRequired().HasConversion<string>();
        builder.Property(i => i.Subtotal).HasPrecision(19, 4);
        builder.Property(i => i.TotalPaymentsApplied).HasPrecision(19, 4);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
        
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => new { i.AccountId, i.BillingPeriodStart, i.BillingPeriodEnd });
        builder.HasIndex(i => i.TenantId);
        
        builder.HasOne(i => i.Account)
            .WithMany()
            .HasForeignKey(i => i.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InvoiceLineItemEntityConfiguration : IEntityTypeConfiguration<InvoiceLineItemEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItemEntity> builder)
    {
        builder.ToTable("invoice_line_items");
        builder.HasKey(l => l.Id);
        
        builder.Property(l => l.RideId).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.Property(l => l.Amount).HasPrecision(19, 4);
        builder.Property(l => l.LedgerEntryIds).IsRequired().HasColumnType("jsonb");
        
        builder.HasIndex(l => l.InvoiceId);
        builder.HasIndex(l => l.RideId);
        
        builder.HasOne(l => l.Invoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(o => o.MessageId);
        
        builder.Property(o => o.EventType).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(o => o.OccurredAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(o => o.RetryCount).IsRequired().HasDefaultValue(0);
        
        builder.HasIndex(o => new { o.ProcessedAt, o.OccurredAt })
            .HasFilter("processed_at IS NULL");
        builder.HasIndex(o => o.TenantId);
    }
}
