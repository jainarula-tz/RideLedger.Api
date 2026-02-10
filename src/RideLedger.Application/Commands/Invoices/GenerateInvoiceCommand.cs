using FluentResults;
using MediatR;
using RideLedger.Domain.Enums;

namespace RideLedger.Application.Commands.Invoices;

/// <summary>
/// Command to generate an invoice for an account
/// </summary>
public sealed record GenerateInvoiceCommand : IRequest<Result<(Guid InvoiceId, string InvoiceNumber)>>
{
    public required Guid AccountId { get; init; }
    public required DateTime BillingPeriodStart { get; init; }
    public required DateTime BillingPeriodEnd { get; init; }
    public required BillingFrequency BillingFrequency { get; init; }
}
