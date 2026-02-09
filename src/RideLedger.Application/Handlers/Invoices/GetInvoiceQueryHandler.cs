using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Invoices;
using RideLedger.Application.Queries.Invoices;
using RideLedger.Domain.Repositories;

namespace RideLedger.Application.Handlers.Invoices;

/// <summary>
/// Handler for getting a specific invoice with full details
/// </summary>
public sealed class GetInvoiceQueryHandler : IRequestHandler<GetInvoiceQuery, Result<InvoiceResponse>>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<InvoiceResponse>> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        // Load invoice with line items using repository
        var invoice = await _invoiceRepository.GetByIdWithLineItemsAsync(request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result.Fail<InvoiceResponse>($"Invoice with ID '{request.InvoiceId}' not found");
        }

        // Map to response DTO
        var response = new InvoiceResponse
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            AccountId = invoice.AccountId.Value,
            BillingFrequency = invoice.BillingFrequency,
            BillingPeriodStart = invoice.BillingPeriodStart,
            BillingPeriodEnd = invoice.BillingPeriodEnd,
            GeneratedAtUtc = invoice.GeneratedAtUtc,
            Status = invoice.Status,
            Subtotal = invoice.Subtotal.Amount,
            TotalPaymentsApplied = invoice.TotalPaymentsApplied.Amount,
            OutstandingBalance = invoice.OutstandingBalance.Amount,
            Currency = invoice.Subtotal.Currency,
            LineItems = invoice.LineItems.Select(li => new InvoiceLineItemResponse
            {
                LineItemId = li.Id,
                RideId = li.RideId,
                ServiceDate = li.ServiceDate,
                Amount = li.Amount.Amount,
                Description = li.Description,
                LedgerEntryIds = li.LedgerEntryIds.ToList()
            }).ToList()
        };

        return Result.Ok(response);
    }
}
