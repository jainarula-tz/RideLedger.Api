namespace RideLedger.Domain.Enums;

/// <summary>
/// Invoice status enumeration
/// </summary>
public enum InvoiceStatus
{
    /// <summary>
    /// Invoice has been generated and is active
    /// </summary>
    Generated = 1,

    /// <summary>
    /// Invoice has been voided/cancelled
    /// </summary>
    Voided = 2
}
