using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideLedger.Application.Commands.Accounts;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Application.DTOs.Balances;
using RideLedger.Application.DTOs.Transactions;
using RideLedger.Application.Queries.Accounts;
using RideLedger.Application.Queries.Balances;
using RideLedger.Application.Queries.Statements;
using RideLedger.Application.Queries.Transactions;
using RideLedger.Application.Handlers.Statements;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Repositories;

namespace RideLedger.Presentation.Controllers;

/// <summary>
/// PRESENTATION LAYER - Controller
/// HTTP endpoints for account management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
// TODO: Re-enable after implementing authentication
// [Authorize(Policy = "TenantAccess")]
public sealed class AccountsController : ControllerBase
{
    private readonly ILogger<AccountsController> _logger;
    private readonly IMediator _mediator;

    public AccountsController(ILogger<AccountsController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new account
    /// </summary>
    /// <param name="request">Account creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created account details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating account with ID: {AccountId}, Name: {Name}",
            request.AccountId,
            request.Name);

        var command = new CreateAccountCommand
        {
            AccountId = request.AccountId,
            Name = request.Name,
            Type = request.Type
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault()?.Message ?? "Account creation failed";
            
            if (error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Account Already Exists",
                    Detail = error
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Account Creation Failed",
                Detail = error
            });
        }

        return CreatedAtAction(
            nameof(GetAccountById),
            new { id = request.AccountId },
            result.Value);
    }

    /// <summary>
    /// Gets account details by ID
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Account details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccountById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving account with ID: {AccountId}", id);

        var query = new GetAccountQuery { AccountId = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault()?.Message ?? "Account not found";

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Account Not Found",
                    Detail = error
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to Retrieve Account",
                Detail = error
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets account balance
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Account balance</returns>
    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(typeof(AccountBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountBalance(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving balance for account: {AccountId}", id);

        var query = new GetAccountBalanceQuery { AccountId = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault()?.Message ?? "Account not found";

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Account Not Found",
                    Detail = error
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to Retrieve Balance",
                Detail = error
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets account transactions with pagination
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated transactions</returns>
    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType(typeof(TransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactions(
        [FromRoute] Guid id,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving transactions for account: {AccountId}, Page: {Page}, PageSize: {PageSize}",
            id,
            page,
            pageSize);

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;

        var query = new GetTransactionsQuery
        {
            AccountId = id,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault()?.Message ?? "Failed to retrieve transactions";

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Account Not Found",
                    Detail = error
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to Retrieve Transactions",
                Detail = error
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Searches accounts with filters
    /// </summary>
    /// <param name="searchTerm">Search term (searches in name and account ID)</param>
    /// <param name="type">Optional account type filter</param>
    /// <param name="status">Optional account status filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchAccountsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchAccounts(
        [FromQuery] string? searchTerm,
        [FromQuery] AccountType? type,
        [FromQuery] AccountStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Searching accounts: SearchTerm='{SearchTerm}', Type={Type}, Status={Status}, Page={Page}",
            searchTerm,
            type,
            status,
            page);

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = new SearchAccountsQuery
        {
            SearchTerm = searchTerm,
            Type = type,
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailed)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Search Failed",
                Detail = result.Errors.FirstOrDefault()?.Message ?? "Failed to search accounts"
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves account statement for a specific period
    /// Includes opening balance, all transactions, and closing balance
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <param name="startDate">Statement period start date (inclusive)</param>
    /// <param name="endDate">Statement period end date (inclusive)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Account statement with transactions and balances</returns>
    [HttpGet("{id:guid}/statements")]
    [ProducesResponseType(typeof(AccountStatementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccountStatement(
        Guid id,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving account statement: AccountId={AccountId}, Period={StartDate} to {EndDate}",
            id,
            startDate,
            endDate);

        // Validate date range
        if (startDate > endDate)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Date Range",
                Detail = "Start date must be before or equal to end date"
            });
        }

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;

        var query = new GetAccountStatementQuery
        {
            AccountId = id,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize
        };

        // Inject handler directly (not using MediatR to keep it simple)
        var accountRepository = HttpContext.RequestServices.GetRequiredService<IAccountRepository>();
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<GetAccountStatementQueryHandler>>();
        var handler = new GetAccountStatementQueryHandler(accountRepository, logger);

        var result = await handler.Handle(query, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault();
            if (error?.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Account Not Found",
                    Detail = $"Account with ID {id} was not found"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Statement Generation Failed",
                Detail = error?.Message ?? "Failed to generate account statement"
            });
        }

        return Ok(result.Value);
    }
}
