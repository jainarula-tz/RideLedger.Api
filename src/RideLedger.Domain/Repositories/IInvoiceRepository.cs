using FluentResults;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Repositories;

/// <summary>
/// Repository interface for Invoice aggregate
/// </summary>
public interface IInvoiceRepository
{
    /// <summary>
    /// Gets an invoice by ID
    /// </summary>
    Task<Result<Invoice>> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an invoice by invoice number
    /// </summary>
    Task<Result<Invoice>> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new invoice
    /// </summary>
    Task<Result> AddAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing invoice
    /// </summary>
    Task<Result> UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoices for an account
    /// </summary>
    Task<Result<IEnumerable<Invoice>>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken = default);
}
