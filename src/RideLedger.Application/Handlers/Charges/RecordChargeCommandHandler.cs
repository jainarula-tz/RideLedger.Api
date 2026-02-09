using FluentResults;
using MediatR;
using RideLedger.Application.Commands.Charges;
using RideLedger.Application.Common;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Handlers.Charges;

/// <summary>
/// Handler for recording ride charges to account ledger
/// </summary>
public sealed class RecordChargeCommandHandler : IRequestHandler<RecordChargeCommand, Result<Guid>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public RecordChargeCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Guid>> Handle(RecordChargeCommand request, CancellationToken cancellationToken)
    {
        // Load account aggregate
        var accountId = AccountId.Create(request.AccountId);
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);

        if (account is null)
        {
            return Result.Fail<Guid>($"Account with ID '{request.AccountId}' not found");
        }

        // Create value objects
        var rideId = RideId.Create(request.RideId);
        var amount = Money.Create(request.Amount);
        var createdBy = _tenantProvider.GetTenantId().ToString(); // TODO: Get actual user ID from JWT

        // Execute domain logic
        var result = account.RecordCharge(
            rideId,
            amount,
            request.ServiceDate,
            request.FleetId,
            createdBy);

        if (result.IsFailed)
        {
            return Result.Fail<Guid>(result.Errors);
        }

        // Persist changes
        var saveResult = await _accountRepository.UpdateAsync(account, cancellationToken);
        if (saveResult.IsFailed)
        {
            return Result.Fail<Guid>(saveResult.Errors);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return ledger entry ID (first new entry from this charge)
        var latestEntry = account.LedgerEntries.LastOrDefault();
        return Result.Ok(latestEntry?.Id ?? Guid.Empty);
    }
}
