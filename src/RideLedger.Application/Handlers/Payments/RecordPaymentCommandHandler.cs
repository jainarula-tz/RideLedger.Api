using FluentResults;
using MediatR;
using RideLedger.Application.Commands.Payments;
using RideLedger.Application.Common;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Handlers.Payments;

/// <summary>
/// Handler for recording payments to account ledger
/// </summary>
public sealed class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Result<Guid>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public RecordPaymentCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Guid>> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        // Load account aggregate
        var accountId = AccountId.Create(request.AccountId);
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);

        if (account is null)
        {
            return Result.Fail<Guid>($"Account with ID '{request.AccountId}' not found");
        }

        // Create value objects
        var paymentReferenceId = PaymentReferenceId.Create(request.PaymentReferenceId);
        var amount = Money.Create(request.Amount);
        var createdBy = _tenantProvider.GetTenantId().ToString(); // TODO: Get actual user ID from JWT

        // Parse payment mode if provided
        PaymentMode? paymentMode = null;
        if (!string.IsNullOrWhiteSpace(request.PaymentMode) &&
            Enum.TryParse<PaymentMode>(request.PaymentMode, out var mode))
        {
            paymentMode = mode;
        }

        // Execute domain logic
        var result = account.RecordPayment(
            paymentReferenceId,
            amount,
            request.PaymentDate,
            paymentMode,
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

        // Return ledger entry ID (first new entry from this payment)
        var latestEntry = account.LedgerEntries.LastOrDefault();
        return Result.Ok(latestEntry?.Id ?? Guid.Empty);
    }
}
