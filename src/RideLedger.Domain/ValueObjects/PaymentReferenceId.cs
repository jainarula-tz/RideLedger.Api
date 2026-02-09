using RideLedger.Domain.Primitives;

namespace RideLedger.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for Payment Reference (external system reference)
/// </summary>
public sealed class PaymentReferenceId : ValueObject
{
    public string Value { get; }

    private PaymentReferenceId(string value)
    {
        Value = value;
    }

    public static PaymentReferenceId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Payment Reference ID cannot be empty", nameof(value));
        }

        if (value.Length > 100)
        {
            throw new ArgumentException("Payment Reference ID cannot exceed 100 characters", nameof(value));
        }

        return new PaymentReferenceId(value.Trim());
    }

    public static implicit operator string(PaymentReferenceId paymentReferenceId) => paymentReferenceId.Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
