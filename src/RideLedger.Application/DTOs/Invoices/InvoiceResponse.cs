using RideLedger.Domain.Enums;

namespace RideLedger.Application.DTOs.Invoices;

/// <summary>
/// Invoice details response
/// </summary>
public sealed record InvoiceResponse
{
    public required Guid InvoiceId { get; init; }
    public required string InvoiceNumber { get; init; }
    public required Guid AccountId { get; init; }
    public required BillingFrequency BillingFrequency { get; init; }
    public required DateTime BillingPeriodStart { get; init; }
    public required DateTime BillingPeriodEnd { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
    public required InvoiceStatus Status { get; init; }
    public required decimal Subtotal { get; init; }
    public required decimal TotalPaymentsApplied { get; init; }
    public required decimal OutstandingBalance { get; init; }
    public required string Currency { get; init; }
    public required List<InvoiceLineItemResponse> LineItems { get; init; } = new();
}

/// <summary>
/// Invoice line item details
/// </summary>
public sealed record InvoiceLineItemResponse
{
    public required Guid LineItemId { get; init; }
    public required string RideId { get; init; }
    public required DateTime ServiceDate { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public required List<Guid> LedgerEntryIds { get; init; } = new();
}
