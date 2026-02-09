using RideLedger.Domain.Enums;

namespace RideLedger.Infrastructure.Persistence.Entities;

/// <summary>
/// INFRASTRUCTURE LAYER - Persistence Entity
/// EF Core entity for Invoice aggregate persistence
/// </summary>
public sealed class InvoiceEntity
{
    public Guid Id { get; set; }
    public required string InvoiceNumber { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public Guid TenantId { get; set; }
    public BillingFrequency BillingFrequency { get; set; }
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalPaymentsApplied { get; set; }
    public InvoiceStatus Status { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAtUtc { get; set; }

    // Navigation properties
    public AccountEntity Account { get; set; } = null!;
    public ICollection<InvoiceLineItemEntity> LineItems { get; set; } = new List<InvoiceLineItemEntity>();
}
