using RideLedger.Domain.Enums;

namespace RideLedger.Application.DTOs.Invoices;

/// <summary>
/// Request to generate an invoice for an account
/// </summary>
public sealed record GenerateInvoiceRequest
{
    /// <summary>
    /// Account ID to generate invoice for
    /// </summary>
    public required Guid AccountId { get; init; }

    /// <summary>
    /// Billing period start date (inclusive)
    /// </summary>
    public required DateTime BillingPeriodStart { get; init; }

    /// <summary>
    /// Billing period end date (exclusive)
    /// </summary>
    public required DateTime BillingPeriodEnd { get; init; }

    /// <summary>
    /// Billing frequency (PerRide, Daily, Weekly, Monthly)
    /// </summary>
    public required BillingFrequency BillingFrequency { get; init; }
}
