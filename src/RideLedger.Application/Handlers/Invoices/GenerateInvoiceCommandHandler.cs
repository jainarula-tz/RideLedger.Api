using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using RideLedger.Application.Commands.Invoices;
using RideLedger.Application.Common;
using RideLedger.Application.Services;
using RideLedger.Domain.Aggregates;
using RideLedger.Domain.Entities;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Errors;
using RideLedger.Domain.Repositories;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Application.Handlers.Invoices;

/// <summary>
/// Handler for generating invoices from ledger entries
/// </summary>
public sealed class GenerateInvoiceCommandHandler : IRequestHandler<GenerateInvoiceCommand, Result<(Guid InvoiceId, string InvoiceNumber)>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GenerateInvoiceCommandHandler> _logger;

    public GenerateInvoiceCommandHandler(
        IAccountRepository accountRepository,
        IInvoiceRepository invoiceRepository,
        IInvoiceNumberGenerator invoiceNumberGenerator,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILogger<GenerateInvoiceCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _invoiceRepository = invoiceRepository;
        _invoiceNumberGenerator = invoiceNumberGenerator;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task<Result<(Guid InvoiceId, string InvoiceNumber)>> Handle(GenerateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var accountId = AccountId.Create(request.AccountId);

        _logger.LogInformation(
            "Generating invoice for account {AccountId}, period {Start} to {End}",
            request.AccountId,
            request.BillingPeriodStart,
            request.BillingPeriodEnd);

        // Load account with ledger entries
        var account = await _accountRepository.GetByIdWithLedgerEntriesAsync(accountId, cancellationToken);
        if (account == null)
        {
            return Result.Fail<(Guid, string)>(AccountErrors.NotFound(accountId));
        }

        // Filter ledger entries for billing period (charges only)
        var chargeEntries = account.LedgerEntries
            .Where(e =>
                e.SourceType == SourceType.Ride &&
                e.TransactionDate >= request.BillingPeriodStart &&
                e.TransactionDate < request.BillingPeriodEnd &&
                e.LedgerAccount == LedgerAccountType.AccountsReceivable && // Debit entries (charges)
                e.DebitAmount != null)
            .ToList();

        if (!chargeEntries.Any())
        {
            return Result.Fail<(Guid, string)>("No billable charges found for the specified billing period");
        }

        // Group charges by billing frequency
        var lineItems = GroupChargesByBillingFrequency(chargeEntries, request.BillingFrequency);

        // Calculate total payments applied
        var paymentEntries = account.LedgerEntries
            .Where(e =>
                e.SourceType == SourceType.Payment &&
                e.TransactionDate >= request.BillingPeriodStart &&
                e.TransactionDate < request.BillingPeriodEnd &&
                e.LedgerAccount == LedgerAccountType.AccountsReceivable && // Credit entries (payments)
                e.CreditAmount != null)
            .ToList();

        var totalPayments = Money.Create(paymentEntries.Sum(p => p.CreditAmount!.Amount));

        // Generate invoice number
        var invoiceNumber = await _invoiceNumberGenerator.GenerateNextAsync(tenantId, cancellationToken);

        // Create invoice
        var invoiceResult = Invoice.Generate(
            invoiceNumber,
            accountId,
            request.BillingFrequency,
            request.BillingPeriodStart,
            request.BillingPeriodEnd,
            lineItems,
            totalPayments,
            tenantId);

        if (invoiceResult.IsFailed)
        {
            return Result.Fail<(Guid, string)>(invoiceResult.Errors);
        }

        var invoice = invoiceResult.Value;

        // Persist invoice
        var addResult = await _invoiceRepository.AddAsync(invoice, cancellationToken);
        if (addResult.IsFailed)
        {
            return Result.Fail<(Guid, string)>(addResult.Errors);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invoice {InvoiceNumber} generated successfully for account {AccountId}",
            invoiceNumber,
            request.AccountId);

        return Result.Ok((invoice.Id, invoiceNumber));
    }

    private static List<InvoiceLineItem> GroupChargesByBillingFrequency(
        List<LedgerEntry> chargeEntries,
        BillingFrequency billingFrequency)
    {
        return billingFrequency switch
        {
            BillingFrequency.PerRide => chargeEntries.Select(entry =>
                InvoiceLineItem.Create(
                    invoiceId: Guid.Empty, // Will be set when added to invoice
                    rideId: entry.SourceReferenceId,
                    serviceDate: entry.TransactionDate,
                    amount: entry.DebitAmount!,
                    description: $"Ride service - {entry.SourceReferenceId}",
                    ledgerEntryIds: new[] { entry.Id }))
                .ToList(),

            BillingFrequency.Daily => chargeEntries
                .GroupBy(e => e.TransactionDate.Date)
                .Select(g =>
                {
                    var total = Money.Create(g.Sum(e => e.DebitAmount!.Amount));
                    var ledgerIds = g.Select(e => e.Id).ToList();
                    var firstEntry = g.First();
                    return InvoiceLineItem.Create(
                        invoiceId: Guid.Empty,
                        rideId: "DAILY",
                        serviceDate: g.Key,
                        amount: total,
                        description: $"Daily rides - {g.Key:yyyy-MM-dd} ({g.Count()} rides)",
                        ledgerEntryIds: ledgerIds);
                })
                .ToList(),

            BillingFrequency.Weekly => chargeEntries
                .GroupBy(e => GetWeekStart(e.TransactionDate))
                .Select(g =>
                {
                    var total = Money.Create(g.Sum(e => e.DebitAmount!.Amount));
                    var ledgerIds = g.Select(e => e.Id).ToList();
                    return InvoiceLineItem.Create(
                        invoiceId: Guid.Empty,
                        rideId: "WEEKLY",
                        serviceDate: g.Key,
                        amount: total,
                        description: $"Weekly rides - Week of {g.Key:yyyy-MM-dd} ({g.Count()} rides)",
                        ledgerEntryIds: ledgerIds);
                })
                .ToList(),

            BillingFrequency.Monthly => new List<InvoiceLineItem>
            {
                InvoiceLineItem.Create(
                    invoiceId: Guid.Empty,
                    rideId: "MONTHLY",
                    serviceDate: chargeEntries.Min(e => e.TransactionDate),
                    amount: Money.Create(chargeEntries.Sum(e => e.DebitAmount!.Amount)),
                    description: $"Monthly rides ({chargeEntries.Count} rides)",
                    ledgerEntryIds: chargeEntries.Select(e => e.Id).ToList())
            },

            _ => throw new ArgumentOutOfRangeException(nameof(billingFrequency), billingFrequency, "Unsupported billing frequency")
        };
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        // Get the Monday of the week containing this date
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }
}
