namespace RideLedger.Domain.Enums;

/// <summary>
/// Source type for ledger entries (transaction origin)
/// </summary>
public enum SourceType
{
    /// <summary>
    /// Entry originated from a ride charge
    /// </summary>
    Ride = 1,

    /// <summary>
    /// Entry originated from a payment
    /// </summary>
    Payment = 2
}
