using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Accounts;

namespace RideLedger.Application.Queries.Accounts;

/// <summary>
/// Query to get account details by ID
/// </summary>
public sealed record GetAccountQuery : IRequest<Result<AccountResponse>>
{
    public required Guid AccountId { get; init; }
}
