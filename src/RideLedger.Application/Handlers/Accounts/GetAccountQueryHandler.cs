using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Application.Queries.Accounts;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Handlers.Accounts;

/// <summary>
/// Handler for retrieving account details
/// </summary>
public sealed class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, Result<AccountResponse>>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<AccountResponse>> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        // Load account with ledger entries for balance calculation
        var accountId = AccountId.Create(request.AccountId);
        var account = await _accountRepository.GetByIdWithLedgerEntriesAsync(accountId, cancellationToken);

        if (account is null)
        {
            return Result.Fail<AccountResponse>($"Account with ID '{request.AccountId}' not found");
        }

        // Calculate current balance
        var balance = account.GetBalance();

        // Map to response
        var response = new AccountResponse
        {
            AccountId = account.Id.Value,
            Name = account.Name,
            Type = account.Type,
            Status = account.Status,
            Balance = balance.Amount,
            Currency = balance.Currency,
            CreatedAt = account.CreatedAtUtc,
            UpdatedAt = account.UpdatedAtUtc
        };

        return Result.Ok(response);
    }
}
