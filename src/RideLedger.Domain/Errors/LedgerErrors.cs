using FluentResults;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Errors;

/// <summary>
/// Domain errors related to Ledger operations
/// </summary>
public static class LedgerErrors
{
    public static Error DuplicateCharge(RideId rideId, AccountId accountId) =>
        new Error($"A charge for ride '{rideId}' has already been recorded for account '{accountId}'")
            .WithMetadata("ErrorCode", "LEDGER_DUPLICATE_CHARGE")
            .WithMetadata("RideId", rideId.Value)
            .WithMetadata("AccountId", accountId.Value);

    public static Error DuplicatePayment(PaymentReferenceId paymentReferenceId) =>
        new Error($"A payment with reference '{paymentReferenceId}' has already been recorded")
            .WithMetadata("ErrorCode", "LEDGER_DUPLICATE_PAYMENT")
            .WithMetadata("PaymentReferenceId", paymentReferenceId.Value);

    public static Error InvalidAmount(decimal amount) =>
        new Error($"Amount '{amount}' is invalid. Amount must be positive.")
            .WithMetadata("ErrorCode", "LEDGER_INVALID_AMOUNT")
            .WithMetadata("Amount", amount);

    public static Error BalanceCalculationFailed(AccountId accountId, string reason) =>
        new Error($"Failed to calculate balance for account '{accountId}': {reason}")
            .WithMetadata("ErrorCode", "LEDGER_BALANCE_CALCULATION_FAILED")
            .WithMetadata("AccountId", accountId.Value)
            .WithMetadata("Reason", reason);

    public static Error UnbalancedEntry(decimal totalDebits, decimal totalCredits) =>
        new Error($"Ledger entries are unbalanced. Debits: {totalDebits}, Credits: {totalCredits}")
            .WithMetadata("ErrorCode", "LEDGER_UNBALANCED_ENTRY")
            .WithMetadata("TotalDebits", totalDebits)
            .WithMetadata("TotalCredits", totalCredits);
}
