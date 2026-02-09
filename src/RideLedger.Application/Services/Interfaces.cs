using FluentResults;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Application.DTOs.Charges;
using RideLedger.Application.DTOs.Payments;
using RideLedger.Application.DTOs.Balances;

namespace RideLedger.Application.Services;

/// <summary>
/// APPLICATION LAYER - Service Interface
/// Defines account-related business operations
/// </summary>
public interface IAccountService
{
    Task<Result<AccountResponse>> CreateAccountAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AccountResponse>> GetAccountByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<Result<AccountBalanceResponse>> GetAccountBalanceAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// APPLICATION LAYER - Service Interface
/// Defines charge recording operations
/// </summary>
public interface IChargeService
{
    Task<Result<ChargeResponse>> RecordChargeAsync(
        RecordChargeRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// APPLICATION LAYER - Service Interface
/// Defines payment recording operations
/// </summary>
public interface IPaymentService
{
    Task<Result<PaymentResponse>> RecordPaymentAsync(
        RecordPaymentRequest request,
        CancellationToken cancellationToken = default);
}
