using FluentResults;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Errors;

/// <summary>
/// Domain errors related to Account operations
/// </summary>
public static class AccountErrors
{
    public static Error NotFound(AccountId accountId) =>
        new Error($"Account with ID '{accountId}' was not found")
            .WithMetadata("ErrorCode", "ACCOUNT_NOT_FOUND")
            .WithMetadata("AccountId", accountId.Value);

    public static Error Inactive(AccountId accountId) =>
        new Error($"Account '{accountId}' is inactive and cannot accept transactions")
            .WithMetadata("ErrorCode", "ACCOUNT_INACTIVE")
            .WithMetadata("AccountId", accountId.Value);

    public static Error TenantMismatch(AccountId accountId, Guid expectedTenantId, Guid actualTenantId) =>
        new Error($"Account '{accountId}' belongs to a different tenant")
            .WithMetadata("ErrorCode", "ACCOUNT_TENANT_MISMATCH")
            .WithMetadata("AccountId", accountId.Value)
            .WithMetadata("ExpectedTenantId", expectedTenantId)
            .WithMetadata("ActualTenantId", actualTenantId);

    public static Error AlreadyExists(AccountId accountId) =>
        new Error($"Account with ID '{accountId}' already exists")
            .WithMetadata("ErrorCode", "ACCOUNT_ALREADY_EXISTS")
            .WithMetadata("AccountId", accountId.Value);

    public static Error InvalidName(string name) =>
        new Error($"Account name '{name}' is invalid")
            .WithMetadata("ErrorCode", "ACCOUNT_INVALID_NAME")
            .WithMetadata("Name", name);
}
