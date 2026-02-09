using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideLedger.Application.Commands.Invoices;
using RideLedger.Application.DTOs.Invoices;

namespace RideLedger.Presentation.Controllers;

/// <summary>
/// PRESENTATION LAYER - Controller
/// HTTP endpoints for invoice generation operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "TenantAccess")]
public sealed class InvoicesController : ControllerBase
{
    private readonly ILogger<InvoicesController> _logger;
    private readonly IMediator _mediator;

    public InvoicesController(ILogger<InvoicesController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Generates an invoice for an account for the specified billing period
    /// </summary>
    /// <param name="request">Invoice generation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated invoice ID</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateInvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateInvoice(
        [FromBody] GenerateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating invoice for account {AccountId} for period {Start} to {End} with billing frequency {BillingFrequency}",
            request.AccountId,
            request.BillingPeriodStart,
            request.BillingPeriodEnd,
            request.BillingFrequency);

        var command = new GenerateInvoiceCommand
        {
            AccountId = request.AccountId,
            BillingPeriodStart = request.BillingPeriodStart,
            BillingPeriodEnd = request.BillingPeriodEnd,
            BillingFrequency = request.BillingFrequency
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault()?.Message ?? "Invoice generation failed";
            
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
                Title = "Invoice Generation Failed",
                Detail = error
            });
        }

        var invoiceId = result.Value;
        var response = new GenerateInvoiceResponse
        {
            InvoiceId = invoiceId
        };

        _logger.LogInformation("Successfully generated invoice {InvoiceId}", invoiceId);

        return CreatedAtAction(
            nameof(GenerateInvoice),
            new { id = invoiceId },
            response);
    }
}
