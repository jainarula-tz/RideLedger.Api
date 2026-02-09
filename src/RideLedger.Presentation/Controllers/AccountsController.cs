using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideLedger.Application.Commands.Accounts;
using RideLedger.Application.DTOs.Accounts;
using RideLedger.Application.DTOs.Balances;
using RideLedger.Application.Queries.Accounts;
using RideLedger.Application.Queries.Balances;

namespace RideLedger.Presentation.Controllers;

/// <summary>
/// PRESENTATION LAYER - Controller
/// HTTP endpoints for account management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "TenantAccess")]
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
}
