using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideLedger.Application.Commands.Charges;
using RideLedger.Application.DTOs.Charges;

namespace RideLedger.Presentation.Controllers;

/// <summary>
/// PRESENTATION LAYER - Controller
/// HTTP endpoints for recording ride charges
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "TenantAccess")]
public sealed class ChargesController : ControllerBase
{
    private readonly ILogger<ChargesController> _logger;
    private readonly IMediator _mediator;

    public ChargesController(ILogger<ChargesController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Records a ride service charge
    /// </summary>
    /// <param name="request">Charge details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recorded charge details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChargeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordCharge(
        [FromBody] RecordChargeRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Recording charge for account {AccountId}, ride {RideId}, amount {Amount}",
            request.AccountId,
            request.RideId,
            request.Amount);

        var command = new RecordChargeCommand
        {
            AccountId = request.AccountId,
            RideId = request.RideId,
            Amount = request.Amount,
            ServiceDate = request.ServiceDate,
            FleetId = request.FleetId
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault()?.Message ?? "Charge recording failed";

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Account Not Found",
                    Detail = error
                });
            }

            if (error.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Duplicate Charge",
                    Detail = error
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Charge Recording Failed",
                Detail = error
            });
        }

        var response = new ChargeResponse
        {
            LedgerEntryId = result.Value,
            AccountId = request.AccountId,
            RideId = request.RideId,
            Amount = request.Amount,
            Currency = "USD",
            ServiceDate = request.ServiceDate,
            RecordedAt = DateTime.UtcNow
        };

        return CreatedAtAction(
            nameof(RecordCharge),
            response);
    }
}
