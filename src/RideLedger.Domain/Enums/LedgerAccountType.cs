namespace RideLedger.Domain.Enums;

/// <summary>
/// Ledger account types for double-entry accounting
/// </summary>
public enum LedgerAccountType
{
    /// <summary>
    /// Asset account - money owed to the business
    /// </summary>
    AccountsReceivable = 1,

    /// <summary>
    /// Revenue account - income from services
    /// </summary>
    ServiceRevenue = 2,

    /// <summary>
    /// Asset account - cash or bank balance
    /// </summary>
    Cash = 3
}
