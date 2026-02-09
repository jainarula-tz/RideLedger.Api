using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Application.Queries.Accounts;
using RideLedger.Domain.Repositories;

namespace RideLedger.Application.Handlers.Accounts;

/// <summary>
/// Handler for searching accounts
/// </summary>
public sealed class SearchAccountsQueryHandler : IRequestHandler<SearchAccountsQuery, Result<SearchAccountsResponse>>
{
    private readonly IAccountRepository _accountRepository;

    public SearchAccountsQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<SearchAccountsResponse>> Handle(SearchAccountsQuery request, CancellationToken cancellationToken)
    {
        // Search accounts using repository
        var (accounts, totalCount) = await _accountRepository.SearchAsync(
            request.SearchTerm,
            request.Type,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        // Map to response DTOs
        var accountResponses = accounts.Select(a => new AccountResponse
        {
            AccountId = a.Id.Value,
            Name = a.Name,
            Type = a.Type,
            Status = a.Status,
            Balance = a.GetBalance().Amount,
            Currency = a.GetBalance().Currency,
            CreatedAt = a.CreatedAtUtc,
            UpdatedAt = a.UpdatedAtUtc
        }).ToList();

        var response = new SearchAccountsResponse
        {
            Accounts = accountResponses,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Ok(response);
    }
}
