namespace RideLedger.Application.DTOs.Charges;

/// <summary>
/// APPLICATION LAYER - Data Transfer Object
/// Request DTO for recording a ride charge
/// </summary>
public sealed record RecordChargeRequest
{
    public required Guid AccountId { get; init; }
    public required string RideId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime ServiceDate { get; init; }
    public required string FleetId { get; init; }
}

/// <summary>
/// APPLICATION LAYER - Data Transfer Object
/// Response DTO for charge recording operations
/// </summary>
public sealed record ChargeResponse
{
    public required Guid LedgerEntryId { get; init; }
    public required Guid AccountId { get; init; }
    public required string RideId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required DateTime ServiceDate { get; init; }
    public required DateTime RecordedAt { get; init; }
}
