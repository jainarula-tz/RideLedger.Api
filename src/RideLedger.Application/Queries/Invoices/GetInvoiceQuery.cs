using FluentResults;
using MediatR;
using RideLedger.Application.DTOs.Invoices;

namespace RideLedger.Application.Queries.Invoices;

/// <summary>
/// Query to get a specific invoice by ID with full details
/// </summary>
public sealed record GetInvoiceQuery : IRequest<Result<InvoiceResponse>>
{
    public required Guid InvoiceId { get; init; }
}
