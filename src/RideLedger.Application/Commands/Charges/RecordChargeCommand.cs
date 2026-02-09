using FluentResults;
using MediatR;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Commands.Charges;

/// <summary>
/// Command to record a ride service charge
/// </summary>
public sealed record RecordChargeCommand : IRequest<Result<Guid>>
{
    public required Guid AccountId { get; init; }
    public required string RideId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime ServiceDate { get; init; }
    public required string FleetId { get; init; }
}
