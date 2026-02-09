using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideLedger.Application.Commands.Invoices;
using RideLedger.Application.DTOs.Invoices;
using RideLedger.Application.Queries.Invoices;
using RideLedger.Domain.Enums;
using System.Text;

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
            nameof(GetInvoiceById),
            new { id = invoiceId },
            response);
    }

    /// <summary>
    /// Gets invoices with optional filters
    /// </summary>
    /// <param name="accountId">Optional account ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated invoice list</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetInvoicesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] Guid? accountId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting invoices: AccountId={AccountId}, Status={Status}, Page={Page}",
            accountId,
            status,
            page);

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = new GetInvoicesQuery
        {
            AccountId = accountId,
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailed)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to Retrieve Invoices",
                Detail = result.Errors.FirstOrDefault()?.Message ?? "Failed to retrieve invoices"
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a specific invoice by ID with full details
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invoice details with line items</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetInvoiceById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving invoice with ID: {InvoiceId}", id);

        var query = new GetInvoiceQuery { InvoiceId = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault()?.Message ?? "Invoice not found";

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Invoice Not Found",
                    Detail = error
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to Retrieve Invoice",
                Detail = error
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Downloads an invoice as PDF
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file download</returns>
    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadInvoicePdf(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating PDF for invoice: {InvoiceId}", id);

        // First, get the invoice details
        var query = new GetInvoiceQuery { InvoiceId = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault()?.Message ?? "Invoice not found";

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Invoice Not Found",
                    Detail = error
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to Generate PDF",
                Detail = error
            });
        }

        var invoice = result.Value;

        // Generate simple text-based PDF content (placeholder for now)
        // TODO: Replace with proper PDF library like QuestPDF or iTextSharp
        var pdfContent = GenerateSimplePdfContent(invoice);
        var pdfBytes = Encoding.UTF8.GetBytes(pdfContent);

        _logger.LogInformation("Successfully generated PDF for invoice {InvoiceId}", id);

        return File(
            pdfBytes,
            "application/pdf",
            $"Invoice_{invoice.InvoiceNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    private string GenerateSimplePdfContent(InvoiceResponse invoice)
    {
        // Placeholder: Generate simple text content
        // TODO: Implement proper PDF generation using a library like QuestPDF
        var content = new StringBuilder();
        content.AppendLine($"INVOICE");
        content.AppendLine($"Invoice Number: {invoice.InvoiceNumber}");
        content.AppendLine($"Generated: {invoice.GeneratedAtUtc:yyyy-MM-dd}");
        content.AppendLine($"Status: {invoice.Status}");
        content.AppendLine();
        content.AppendLine($"Account ID: {invoice.AccountId}");
        content.AppendLine($"Billing Period: {invoice.BillingPeriodStart:yyyy-MM-dd} to {invoice.BillingPeriodEnd:yyyy-MM-dd}");
        content.AppendLine($"Billing Frequency: {invoice.BillingFrequency}");
        content.AppendLine();
        content.AppendLine("LINE ITEMS:");
        content.AppendLine("---------------------------------------------------");

        foreach (var item in invoice.LineItems)
        {
            content.AppendLine($"Ride ID: {item.RideId}");
            content.AppendLine($"Service Date: {item.ServiceDate:yyyy-MM-dd}");
            content.AppendLine($"Description: {item.Description}");
            content.AppendLine($"Amount: {item.Amount:C} {invoice.Currency}");
            content.AppendLine();
        }

        content.AppendLine("---------------------------------------------------");
        content.AppendLine($"Subtotal: {invoice.Subtotal:C} {invoice.Currency}");
        content.AppendLine($"Payments Applied: {invoice.TotalPaymentsApplied:C} {invoice.Currency}");
        content.AppendLine($"Outstanding Balance: {invoice.OutstandingBalance:C} {invoice.Currency}");

        return content.ToString();
    }
}
