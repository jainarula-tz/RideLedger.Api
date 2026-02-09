namespace RideLedger.Domain.Enums;

/// <summary>
/// Billing frequency for invoice generation
/// </summary>
public enum BillingFrequency
{
    /// <summary>
    /// One invoice per ride
    /// </summary>
    PerRide = 1,

    /// <summary>
    /// Daily invoices
    /// </summary>
    Daily = 2,

    /// <summary>
    /// Weekly invoices
    /// </summary>
    Weekly = 3,

    /// <summary>
    /// Monthly invoices
    /// </summary>
    Monthly = 4
}
