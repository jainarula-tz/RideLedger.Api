using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Invoices;
using RideLedger.Domain.Enums;

namespace RideLedger.Application.Queries.Invoices;

/// <summary>
/// Query to get invoices with filters
/// </summary>
public sealed record GetInvoicesQuery : IRequest<Result<GetInvoicesResponse>>
{
    public Guid? AccountId { get; init; }
    public InvoiceStatus? Status { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Get invoices response
/// </summary>
public sealed record GetInvoicesResponse
{
    public required List<InvoiceSummaryResponse> Invoices { get; init; } = new();
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

/// <summary>
/// Invoice summary (without line items)
/// </summary>
public sealed record InvoiceSummaryResponse
{
    public required Guid InvoiceId { get; init; }
    public required string InvoiceNumber { get; init; }
    public required Guid AccountId { get; init; }
    public required string AccountName { get; init; }
    public required BillingFrequency BillingFrequency { get; init; }
    public required DateTime BillingPeriodStart { get; init; }
    public required DateTime BillingPeriodEnd { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
    public required InvoiceStatus Status { get; init; }
    public required decimal Subtotal { get; init; }
    public required decimal OutstandingBalance { get; init; }
    public required string Currency { get; init; }
}
