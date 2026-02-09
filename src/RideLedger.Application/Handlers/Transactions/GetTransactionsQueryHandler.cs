using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Transactions;
using RideLedger.Application.Queries.Transactions;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Handlers.Transactions;

/// <summary>
/// Handler for retrieving account transactions
/// </summary>
public sealed class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, Result<TransactionsResponse>>
{
    private readonly IAccountRepository _accountRepository;

    public GetTransactionsQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<TransactionsResponse>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        // Load account with ledger entries
        var accountId = AccountId.Create(request.AccountId);
        var account = await _accountRepository.GetByIdWithLedgerEntriesAsync(accountId, cancellationToken);

        if (account is null)
        {
            return Result.Fail<TransactionsResponse>($"Account with ID '{request.AccountId}' not found");
        }

        // Get ledger entries
        var ledgerEntries = account.LedgerEntries
            .OrderByDescending(e => e.TransactionDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToList();

        // Apply date filters if provided
        if (request.StartDate.HasValue)
        {
            ledgerEntries = ledgerEntries
                .Where(e => e.TransactionDate >= request.StartDate.Value.Date)
                .ToList();
        }

        if (request.EndDate.HasValue)
        {
            ledgerEntries = ledgerEntries
                .Where(e => e.TransactionDate <= request.EndDate.Value.Date)
                .ToList();
        }

        var totalCount = ledgerEntries.Count;

        // Apply pagination
        var paginatedEntries = ledgerEntries
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // Calculate running balances
        var balance = account.GetBalance();
        var runningBalance = balance.Amount;

        var transactions = new List<TransactionResponse>();
        foreach (var entry in paginatedEntries)
        {
            var isDebit = entry.DebitAmount is not null;
            var amount = isDebit ? entry.DebitAmount! : entry.CreditAmount!;

            transactions.Add(new TransactionResponse
            {
                LedgerEntryId = entry.Id,
                AccountId = account.Id.Value,
                EntryType = isDebit ? "Debit" : "Credit",
                Amount = amount.Amount,
                Currency = amount.Currency,
                TransactionDate = entry.TransactionDate,
                Description = $"{entry.SourceType} - {entry.LedgerAccount}",
                RideId = entry.SourceType == Domain.Enums.SourceType.Ride ? entry.SourceReferenceId : null,
                PaymentReferenceId = entry.SourceType == Domain.Enums.SourceType.Payment ? entry.SourceReferenceId : null,
                RecordedAt = entry.CreatedAtUtc,
                RunningBalance = runningBalance
            });

            // Update running balance (subtract debits, add credits going backwards)
            runningBalance -= isDebit ? amount.Amount : -amount.Amount;
        }

        var response = new TransactionsResponse
        {
            AccountId = account.Id.Value,
            Transactions = transactions,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            CurrentBalance = balance.Amount,
            Currency = balance.Currency
        };

        return Result.Ok(response);
    }
}
