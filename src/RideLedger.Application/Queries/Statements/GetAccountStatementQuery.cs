using FluentResults;

namespace RideLedger.Application.Queries.Statements;

/// <summary>
/// Query to retrieve account statement for a specific date range
/// Includes opening balance, all transactions, and closing balance
/// </summary>
public sealed record GetAccountStatementQuery
{
    public Guid AccountId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

/// <summary>
/// Response containing account statement with transactions and balances
/// </summary>
public sealed record AccountStatementResponse
{
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public DateTime StatementPeriodStart { get; init; }
    public DateTime StatementPeriodEnd { get; init; }
    public decimal OpeningBalance { get; init; }
    public decimal ClosingBalance { get; init; }
    public string Currency { get; init; } = "USD";
    public List<StatementTransactionItem> Transactions { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>
/// Individual transaction item in an account statement
/// </summary>
public sealed record StatementTransactionItem
{
    public Guid LedgerEntryId { get; init; }
    public DateTime TransactionDate { get; init; }
    public string Description { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty; // "Ride" or "Payment"
    public string SourceReferenceId { get; init; } = string.Empty;
    public decimal? DebitAmount { get; init; }
    public decimal? CreditAmount { get; init; }
    public decimal RunningBalance { get; init; }
}
