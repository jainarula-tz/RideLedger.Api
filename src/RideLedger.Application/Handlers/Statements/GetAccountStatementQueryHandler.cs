using FluentResults;
using Microsoft.Extensions.Logging;
using RideLedger.Application.Queries.Statements;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Handlers.Statements;

/// <summary>
/// Handles GetAccountStatementQuery
/// Calculates opening balance, retrieves transactions, and computes closing balance
/// </summary>
public sealed class GetAccountStatementQueryHandler
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<GetAccountStatementQueryHandler> _logger;
    private const int MaxPageSize = 100;

    public GetAccountStatementQueryHandler(
        IAccountRepository accountRepository,
        ILogger<GetAccountStatementQueryHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<Result<AccountStatementResponse>> Handle(
        GetAccountStatementQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating account statement for AccountId: {AccountId}, Period: {StartDate} to {EndDate}",
            query.AccountId,
            query.StartDate,
            query.EndDate);

        // Validate pagination
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 50 : Math.Min(query.PageSize, MaxPageSize);

        // Load account with all ledger entries
        var accountId = AccountId.Create(query.AccountId);
        var account = await _accountRepository.GetByIdWithLedgerEntriesAsync(
            accountId,
            cancellationToken);

        if (account is null)
        {
            _logger.LogWarning(
                "Account not found: {AccountId}",
                query.AccountId);
            return Result.Fail<AccountStatementResponse>("Account not found");
        }

        // Calculate opening balance (balance at start of period)
        // This is the sum of all entries BEFORE the start date
        var entriesBeforeStart = account.LedgerEntries
            .Where(e => e.CreatedAtUtc < query.StartDate)
            .ToList();

        var openingBalance = CalculateBalance(entriesBeforeStart);

        // Get all entries within the date range, ordered chronologically
        var entriesInPeriod = account.LedgerEntries
            .Where(e => e.CreatedAtUtc >= query.StartDate && e.CreatedAtUtc <= query.EndDate)
            .OrderBy(e => e.CreatedAtUtc)
            .ThenBy(e => e.Id)
            .ToList();

        var totalCount = entriesInPeriod.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Apply pagination
        var paginatedEntries = entriesInPeriod
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Calculate closing balance (balance at end of period)
        var closingBalance = openingBalance + CalculateBalance(entriesInPeriod);

        // Build transaction items with running balances
        var transactions = new List<StatementTransactionItem>();
        var runningBalance = openingBalance;

        foreach (var entry in paginatedEntries)
        {
            var isDebit = entry.DebitAmount is not null;
            var amount = isDebit ? entry.DebitAmount! : entry.CreditAmount!;

            // Update running balance
            runningBalance += isDebit ? amount.Amount : -amount.Amount;

            transactions.Add(new StatementTransactionItem
            {
                LedgerEntryId = entry.Id,
                TransactionDate = entry.CreatedAtUtc,
                Description = BuildDescription(entry.SourceType, entry.SourceReferenceId),
                SourceType = entry.SourceType.ToString(),
                SourceReferenceId = entry.SourceReferenceId,
                DebitAmount = entry.DebitAmount?.Amount,
                CreditAmount = entry.CreditAmount?.Amount,
                RunningBalance = runningBalance
            });
        }

        var response = new AccountStatementResponse
        {
            AccountId = account.Id.Value,
            AccountName = account.Name,
            StatementPeriodStart = query.StartDate,
            StatementPeriodEnd = query.EndDate,
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            Currency = "USD",
            Transactions = transactions,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        _logger.LogInformation(
            "Successfully generated statement for AccountId: {AccountId}. " +
            "OpeningBalance: {OpeningBalance}, ClosingBalance: {ClosingBalance}, " +
            "TotalTransactions: {TotalTransactions}",
            query.AccountId,
            openingBalance,
            closingBalance,
            totalCount);

        return Result.Ok(response);
    }

    private static decimal CalculateBalance(IEnumerable<RideLedger.Domain.Entities.LedgerEntry> entries)
    {
        decimal balance = 0;

        foreach (var entry in entries)
        {
            if (entry.DebitAmount is not null)
                balance += entry.DebitAmount.Amount;
            
            if (entry.CreditAmount is not null)
                balance -= entry.CreditAmount.Amount;
        }

        return balance;
    }

    private static string BuildDescription(RideLedger.Domain.Enums.SourceType sourceType, string sourceReferenceId)
    {
        return sourceType switch
        {
            RideLedger.Domain.Enums.SourceType.Ride => $"Ride charge - {sourceReferenceId}",
            RideLedger.Domain.Enums.SourceType.Payment => $"Payment received - {sourceReferenceId}",
            _ => $"{sourceType} - {sourceReferenceId}"
        };
    }
}
