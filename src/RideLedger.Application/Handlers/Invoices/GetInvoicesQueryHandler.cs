using FluentResults;
using MediatR;
using RideLedger.Application.Queries.Invoices;
using RideLedger.Domain.Repositories;

namespace RideLedger.Application.Handlers.Invoices;

/// <summary>
/// Handler for getting invoices with filters
/// </summary>
public sealed class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, Result<GetInvoicesResponse>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAccountRepository _accountRepository;

    public GetInvoicesQueryHandler(
        IInvoiceRepository invoiceRepository,
        IAccountRepository accountRepository)
    {
        _invoiceRepository = invoiceRepository;
        _accountRepository = accountRepository;
    }

    public async Task<Result<GetInvoicesResponse>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        // Search invoices using repository
        var (invoices, totalCount) = await _invoiceRepository.SearchAsync(
            request.AccountId,
            request.Status,
            request.StartDate,
            request.EndDate,
            request.Page,
            request.PageSize,
            cancellationToken);

        // Map to response DTOs
        var invoiceSummaries = new List<InvoiceSummaryResponse>();

        foreach (var invoice in invoices)
        {
            // Get account name
            var account = await _accountRepository.GetByIdAsync(
                Domain.ValueObjects.AccountId.Create(invoice.AccountId.Value),
                cancellationToken);

            invoiceSummaries.Add(new InvoiceSummaryResponse
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                AccountId = invoice.AccountId.Value,
                AccountName = account?.Name ?? "Unknown",
                BillingFrequency = invoice.BillingFrequency,
                BillingPeriodStart = invoice.BillingPeriodStart,
                BillingPeriodEnd = invoice.BillingPeriodEnd,
                GeneratedAtUtc = invoice.GeneratedAtUtc,
                Status = invoice.Status,
                Subtotal = invoice.Subtotal.Amount,
                OutstandingBalance = invoice.OutstandingBalance.Amount,
                Currency = invoice.Subtotal.Currency
            });
        }

        var response = new GetInvoicesResponse
        {
            Invoices = invoiceSummaries,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Ok(response);
    }
}
