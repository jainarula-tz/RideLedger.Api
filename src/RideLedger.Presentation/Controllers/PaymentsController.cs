using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideLedger.Application.Commands.Payments;
using RideLedger.Application.DTOs.Payments;

namespace RideLedger.Presentation.Controllers;

/// <summary>
/// PRESENTATION LAYER - Controller
/// HTTP endpoints for recording payments
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
// TODO: Re-enable after implementing authentication
// [Authorize(Policy = "TenantAccess")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly IMediator _mediator;

    public PaymentsController(ILogger<PaymentsController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Records a payment
    /// </summary>
    /// <param name="request">Payment details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recorded payment details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordPayment(
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Recording payment for account {AccountId}, reference {PaymentReferenceId}, amount {Amount}",
            request.AccountId,
            request.PaymentReferenceId,
            request.Amount);

        var command = new RecordPaymentCommand
        {
            AccountId = request.AccountId,
            PaymentReferenceId = request.PaymentReferenceId,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            PaymentMode = request.PaymentMode?.ToString()
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault()?.Message ?? "Payment recording failed";

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
                    Title = "Duplicate Payment",
                    Detail = error
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Payment Recording Failed",
                Detail = error
            });
        }

        var response = new PaymentResponse
        {
            LedgerEntryId = result.Value,
            AccountId = request.AccountId,
            PaymentReferenceId = request.PaymentReferenceId,
            Amount = request.Amount,
            Currency = "USD",
            PaymentDate = request.PaymentDate,
            PaymentMode = request.PaymentMode,
            RecordedAt = DateTime.UtcNow
        };

        return CreatedAtAction(
            nameof(RecordPayment),
            response);
    }
}
