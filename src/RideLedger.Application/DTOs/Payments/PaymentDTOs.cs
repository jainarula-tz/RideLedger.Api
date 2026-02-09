using RideLedger.Domain.Enums;

namespace RideLedger.Application.DTOs.Payments;

/// <summary>
/// APPLICATION LAYER - Data Transfer Object
/// Request DTO for recording a payment
/// </summary>
public sealed record RecordPaymentRequest
{
    public required Guid AccountId { get; init; }
    public required string PaymentReferenceId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime PaymentDate { get; init; }
    public PaymentMode? PaymentMode { get; init; }
}

/// <summary>
/// APPLICATION LAYER - Data Transfer Object
/// Response DTO for payment operations
/// </summary>
public sealed record PaymentResponse
{
    public required Guid LedgerEntryId { get; init; }
    public required Guid AccountId { get; init; }
    public required string PaymentReferenceId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required DateTime PaymentDate { get; init; }
    public PaymentMode? PaymentMode { get; init; }
    public required DateTime RecordedAt { get; init; }
}
