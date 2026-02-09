namespace RideLedger.Domain.Enums;

/// <summary>
/// Account status for controlling transaction acceptance
/// </summary>
public enum AccountStatus
{
    /// <summary>
    /// Account is active and can accept transactions
    /// </summary>
    Active = 1,

    /// <summary>
    /// Account is inactive and cannot accept new transactions
    /// </summary>
    Inactive = 2
}
