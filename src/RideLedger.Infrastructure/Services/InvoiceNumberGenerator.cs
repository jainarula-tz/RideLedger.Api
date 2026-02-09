using Microsoft.EntityFrameworkCore;
using RideLedger.Application.Services;
using RideLedger.Infrastructure.Persistence;

namespace RideLedger.Infrastructure.Services;

/// <summary>
/// Implementation of invoice number generator
/// Generates sequential invoice numbers per tenant
/// </summary>
public sealed class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly AccountingDbContext _context;

    public InvoiceNumberGenerator(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateNextAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Get the highest invoice number for this tenant
        var lastInvoice = await _context.Invoices
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        int nextSequence = 1;

        if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.InvoiceNumber))
        {
            // Extract sequence number from format "INV-000123"
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lastSequence))
            {
                nextSequence = lastSequence + 1;
            }
        }

        return $"INV-{nextSequence:D6}";
    }
}
