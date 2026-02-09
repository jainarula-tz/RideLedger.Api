using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Entities;

/// <summary>
/// Represents a single line item on an invoice
/// Typically corresponds to a single ride charge or grouped charges
/// </summary>
public sealed class InvoiceLineItem
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string RideId { get; private set; } = string.Empty;
    public DateTime ServiceDate { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Ledger entry IDs that contribute to this line item (for traceability)
   /// </summary>
    private readonly List<Guid> _ledgerEntryIds = new();
    public IReadOnlyCollection<Guid> LedgerEntryIds => _ledgerEntryIds.AsReadOnly();

    private InvoiceLineItem()
    {
    }

    /// <summary>
    /// Factory method to create an invoice line item
    /// </summary>
    public static InvoiceLineItem Create(
        Guid invoiceId,
        string rideId,
        DateTime serviceDate,
        Money amount,
        string description,
        IEnumerable<Guid> ledgerEntryIds)
    {
        var lineItem = new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            RideId = rideId,
            ServiceDate = serviceDate,
            Amount = amount,
            Description = description
        };

        lineItem._ledgerEntryIds.AddRange(ledgerEntryIds);

        return lineItem;
    }
}
