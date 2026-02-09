using RideLedger.Domain.Enums;

namespace RideLedger.Application.DTOs.Accounts;

/// <summary>
/// APPLICATION LAYER - Data Transfer Object
/// Request DTO for creating a new account
/// </summary>
public sealed record CreateAccountRequest
{
    public required Guid AccountId { get; init; }
    public required string Name { get; init; }
    public required AccountType Type { get; init; }
}

/// <summary>
/// APPLICATION LAYER - Data Transfer Object
/// Response DTO for account operations
/// </summary>
public sealed record AccountResponse
{
    public required Guid AccountId { get; init; }
    public required string Name { get; init; }
    public required AccountType Type { get; init; }
    public required AccountStatus Status { get; init; }
    public required decimal Balance { get; init; }
    public required string Currency { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
