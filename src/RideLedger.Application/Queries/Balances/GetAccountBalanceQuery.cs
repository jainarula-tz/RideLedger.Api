using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Balances;

namespace RideLedger.Application.Queries.Balances;

/// <summary>
/// Query to get account balance
/// </summary>
public sealed record GetAccountBalanceQuery : IRequest<Result<AccountBalanceResponse>>
{
    public required Guid AccountId { get; init; }
}
