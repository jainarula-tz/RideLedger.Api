using RideLedger.Domain.Primitives;

namespace RideLedger.Domain.ValueObjects;

/// <summary>
/// Value object representing a monetary amount with fixed-point precision (4 decimal places)
/// Enforces rules: amount must be non-negative, uses decimal for precision
/// </summary>
public sealed class Money : ValueObject
{
    public const string DefaultCurrency = "USD";
    private const int DecimalPlaces = 4;

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Creates a Money value object
    /// </summary>
    public static Money Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        }

        // Round to 4 decimal places for fixed-point precision
        var roundedAmount = Math.Round(amount, DecimalPlaces, MidpointRounding.AwayFromZero);

        return new Money(roundedAmount, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = DefaultCurrency) => Create(0, currency);

    // Arithmetic operators
    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return Create(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        
        var result = left.Amount - right.Amount;
        if (result < 0)
        {
            throw new InvalidOperationException("Subtraction would result in negative amount");
        }

        return Create(result, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        if (multiplier < 0)
        {
            throw new ArgumentException("Multiplier cannot be negative", nameof(multiplier));
        }

        return Create(money.Amount * multiplier, money.Currency);
    }

    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor <= 0)
        {
            throw new ArgumentException("Divisor must be positive", nameof(divisor));
        }

        return Create(money.Amount / divisor, money.Currency);
    }

    // Comparison operators
    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException($"Cannot operate on different currencies: {left.Currency} and {right.Currency}");
        }
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N4} {Currency}";
}
