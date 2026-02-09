namespace RideLedger.Application.DTOs.Balances;

/// <summary>
/// APPLICATION LAYER - Data Transfer Object
/// Response DTO for account balance queries
/// </summary>
public sealed record AccountBalanceResponse
{
    public required Guid AccountId { get; init; }
    public required decimal Balance { get; init; }
    public required string Currency { get; init; }
    public required DateTime AsOfDate { get; init; }
}
