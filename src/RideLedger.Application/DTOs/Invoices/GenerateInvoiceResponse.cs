namespace RideLedger.Application.DTOs.Invoices;

/// <summary>
/// Response after successfully generating an invoice
/// </summary>
public sealed record GenerateInvoiceResponse
{
    /// <summary>
    /// Generated invoice ID
    /// </summary>
    public required Guid InvoiceId { get; init; }

    /// <summary>
    /// Generated invoice number for display
    /// </summary>
    public required string InvoiceNumber { get; init; }
}
