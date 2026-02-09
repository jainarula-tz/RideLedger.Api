using FluentResults;
using MediatR;
using RideLedger.Domain.Enums;

namespace RideLedger.Application.Commands.Payments;

/// <summary>
/// Command to record a payment
/// </summary>
public sealed record RecordPaymentCommand : IRequest<Result<Guid>>
{
    public required Guid AccountId { get; init; }
    public required string PaymentReferenceId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime PaymentDate { get; init; }
    public string? PaymentMode { get; init; }
}
