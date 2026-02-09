using RideLedger.Domain.Enums;

namespace RideLedger.Application.DTOs.Transactions;

/// <summary>
/// Transaction (ledger entry) details response
/// </summary>
public sealed record TransactionResponse
{
    public required Guid LedgerEntryId { get; init; }
    public required Guid AccountId { get; init; }
    public required string EntryType { get; init; } // "Debit" or "Credit"
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required DateTime TransactionDate { get; init; }
    public required string Description { get; init; }
    public string? RideId { get; init; }
    public string? PaymentReferenceId { get; init; }
    public required DateTime RecordedAt { get; init; }
    public required decimal RunningBalance { get; init; }
}

/// <summary>
/// Paginated transactions response
/// </summary>
public sealed record TransactionsResponse
{
    public required Guid AccountId { get; init; }
    public required List<TransactionResponse> Transactions { get; init; } = new();
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required decimal CurrentBalance { get; init; }
    public required string Currency { get; init; }
}
