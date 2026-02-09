using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Domain.Enums;

namespace RideLedger.Application.Queries.Accounts;

/// <summary>
/// Query to search accounts with filters
/// </summary>
public sealed record SearchAccountsQuery : IRequest<Result<SearchAccountsResponse>>
{
    public string? SearchTerm { get; init; }
    public AccountType? Type { get; init; }
    public AccountStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Search accounts response
/// </summary>
public sealed record SearchAccountsResponse
{
    public required List<AccountResponse> Accounts { get; init; } = new();
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}
