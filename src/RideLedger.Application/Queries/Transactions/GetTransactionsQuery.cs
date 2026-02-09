using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Transactions;

namespace RideLedger.Application.Queries.Transactions;

/// <summary>
/// Query to get account transactions with pagination
/// </summary>
public sealed record GetTransactionsQuery : IRequest<Result<TransactionsResponse>>
{
    public required Guid AccountId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
