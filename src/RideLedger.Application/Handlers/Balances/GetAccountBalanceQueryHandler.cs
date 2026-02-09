using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Balances;
using RideLedger.Application.Queries.Balances;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Handlers.Balances;

/// <summary>
/// Handler for retrieving account balance
/// </summary>
public sealed class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, Result<AccountBalanceResponse>>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountBalanceQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<AccountBalanceResponse>> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        // Load account aggregate with ledger entries
        var accountId = AccountId.Create(request.AccountId);
        var account = await _accountRepository.GetByIdWithLedgerEntriesAsync(accountId, cancellationToken);

        if (account is null)
        {
            return Result.Fail<AccountBalanceResponse>($"Account with ID '{request.AccountId}' not found");
        }

        // Calculate balance using domain method
        var balance = account.GetBalance();

        // Map to response DTO
        var response = new AccountBalanceResponse
        {
            AccountId = account.Id.Value,
            Balance = balance.Amount,
            Currency = balance.Currency,
            AsOfDate = DateTime.UtcNow
        };

        return Result.Ok(response);
    }
}
