using FluentResults;
using RideLedger.Domain.Entities;
using RideLedger.Domain.Enums;
using RideLedger.Domain.Events;
using RideLedger.Domain.Primitives;
using RideLedger.Domain.ValueObjects;

namespace RideLedger.Domain.Aggregates;

/// <summary>
/// Invoice aggregate - Read model/projection generated from ledger entries
/// Represents a billing document for a specific period
/// </summary>
public sealed class Invoice : AggregateRoot<Guid>
{
    private readonly List<InvoiceLineItem> _lineItems = new();

    public string InvoiceNumber { get; private set; } = string.Empty;
    public AccountId AccountId { get; private set; } = null!;
    public BillingFrequency BillingFrequency { get; private set; }
    public DateTime BillingPeriodStart { get; private set; }
    public DateTime BillingPeriodEnd { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
    public InvoiceStatus Status { get; private set; }

    /// <summary>
    /// Gets the read-only collection of line items
    /// </summary>
    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>
    /// Calculates the subtotal from all line items
    /// </summary>
    public Money Subtotal => Money.Create(_lineItems.Sum(li => li.Amount.Amount));

    /// <summary>
    /// Total payments applied to this invoice
    /// </summary>
    public Money TotalPaymentsApplied { get; private set; } = Money.Zero();

    /// <summary>
    /// Outstanding balance (Subtotal - TotalPaymentsApplied)
    /// </summary>
    public Money OutstandingBalance => Money.Create(Subtotal.Amount - TotalPaymentsApplied.Amount);

    private Invoice()
    {
    }

    /// <summary>
    /// Factory method to generate an invoice from ledger entries
    /// </summary>
    public static Result<Invoice> Generate(
        string invoiceNumber,
        AccountId accountId,
        BillingFrequency billingFrequency,
        DateTime billingPeriodStart,
        DateTime billingPeriodEnd,
        IEnumerable<InvoiceLineItem> lineItems,
        Money totalPayments,
        Guid tenantId)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return Result.Fail<Invoice>("Invoice number is required");
        }

        if (billingPeriodStart >= billingPeriodEnd)
        {
            return Result.Fail<Invoice>("Billing period start must be before end date");
        }

        var lineItemsList = lineItems.ToList();
        if (!lineItemsList.Any())
        {
            return Result.Fail<Invoice>("Invoice must have at least one line item");
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            AccountId = accountId,
            BillingFrequency = billingFrequency,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            GeneratedAtUtc = DateTime.UtcNow,
            Status = InvoiceStatus.Generated,
            TotalPaymentsApplied = totalPayments,
            TenantId = tenantId
        };

        invoice._lineItems.AddRange(lineItemsList);

        // Raise domain event
        invoice.RaiseDomainEvent(new InvoiceGeneratedEvent(
            invoiceNumber,
            accountId,
            billingPeriodStart,
            billingPeriodEnd,
            invoice.Subtotal,
            invoice.OutstandingBalance,
            tenantId));

        return Result.Ok(invoice);
    }

    /// <summary>
    /// Voids the invoice
    /// </summary>
    public Result Void()
    {
        if (Status == InvoiceStatus.Voided)
        {
            return Result.Fail("Invoice is already voided");
        }

        Status = InvoiceStatus.Voided;
        return Result.Ok();
    }
}
