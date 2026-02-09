namespace RideLedger.Infrastructure.Persistence.Entities;

/// <summary>
/// INFRASTRUCTURE LAYER - Persistence Entity
/// EF Core entity for InvoiceLineItem persistence
/// </summary>
public sealed class InvoiceLineItemEntity
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public required string RideId { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; } = string.Empty;
    public required string LedgerEntryIds { get; set; } = "[]"; // JSON array of Guids

    // Navigation properties
    public InvoiceEntity Invoice { get; set; } = null!;
}
