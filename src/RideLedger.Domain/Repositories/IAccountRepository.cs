using FluentResults;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Repositories;

/// <summary>
/// Repository interface for Account aggregate
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Gets account by ID with tenant filtering
    /// </summary>
    Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets account with all ledger entries loaded
    /// </summary>
    Task<Account?> GetByIdWithLedgerEntriesAsync(AccountId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new account
    /// </summary>
    Task<Result> AddAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing account
    /// </summary>
    Task<Result> UpdateAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if account exists
    /// </summary>
    Task<bool> ExistsAsync(AccountId id, CancellationToken cancellationToken = default);
}
