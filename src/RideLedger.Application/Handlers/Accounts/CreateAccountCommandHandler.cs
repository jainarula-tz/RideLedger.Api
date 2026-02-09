using FluentResults;
using MediatR;
using RideLedger.Application.Commands.Accounts;
using RideLedger.Application.Common;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Handlers.Accounts;

/// <summary>
/// Handler for creating new accounts
/// </summary>
public sealed class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<AccountResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<AccountResponse>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var accountId = AccountId.Create(request.AccountId);
        var tenantId = _tenantProvider.GetTenantId();

        // Check if account already exists
        var existing = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (existing is not null)
        {
            return Result.Fail<AccountResponse>($"Account with ID '{request.AccountId}' already exists");
        }

        // Create account aggregate
        var account = Account.Create(accountId, request.Name, request.Type, tenantId);

        // Persist
        var saveResult = await _accountRepository.AddAsync(account, cancellationToken);
        if (saveResult.IsFailed)
        {
            return Result.Fail<AccountResponse>(saveResult.Errors);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to response
        var response = new AccountResponse
        {
            AccountId = account.Id.Value,
            Name = account.Name,
            Type = account.Type,
            Status = account.Status,
            Balance = 0m,
            Currency = Money.DefaultCurrency,
            CreatedAt = account.CreatedAtUtc,
            UpdatedAt = account.UpdatedAtUtc
        };

        return Result.Ok(response);
    }
}
