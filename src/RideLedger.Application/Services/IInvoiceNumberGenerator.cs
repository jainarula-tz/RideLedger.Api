namespace RideLedger.Application.Services;

/// <summary>
/// Service for generating sequential invoice numbers
/// </summary>
public interface IInvoiceNumberGenerator
{
    /// <summary>
    /// Generates the next invoice number for a tenant
    /// Format: INV-{Sequence:D6} (e.g., INV-000001)
    /// </summary>
    Task<string> GenerateNextAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
