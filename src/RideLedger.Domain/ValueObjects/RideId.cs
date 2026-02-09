using RideLedger.Domain.Primitives;

namespace RideLedger.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for Ride (external system reference)
/// </summary>
public sealed class RideId : ValueObject
{
    public string Value { get; }

    private RideId(string value)
    {
        Value = value;
    }

    public static RideId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Ride ID cannot be empty", nameof(value));
        }

        if (value.Length > 100)
        {
            throw new ArgumentException("Ride ID cannot exceed 100 characters", nameof(value));
        }

        return new RideId(value.Trim());
    }

    public static implicit operator string(RideId rideId) => rideId.Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
