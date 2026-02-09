namespace RideLedger.Domain.Enums;

/// <summary>
/// Payment mode/method classification
/// </summary>
public enum PaymentMode
{
    /// <summary>
    /// Cash payment
    /// </summary>
    Cash = 1,

    /// <summary>
    /// Card payment (credit/debit)
    /// </summary>
    Card = 2,

    /// <summary>
    /// Bank transfer or wire
    /// </summary>
    BankTransfer = 3,

    /// <summary>
    /// Digital wallet or UPI
    /// </summary>
    DigitalWallet = 4,

    /// <summary>
    /// Other payment methods
    /// </summary>
    Other = 99
}
