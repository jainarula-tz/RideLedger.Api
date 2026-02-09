using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Domain.Enums;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Commands.Accounts;

/// <summary>
/// Command to create a new account
/// </summary>
public sealed record CreateAccountCommand : IRequest<Result<AccountResponse>>
{
    public required Guid AccountId { get; init; }
    public required string Name { get; init; }
    public required AccountType Type { get; init; }
}
