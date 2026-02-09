using RideLedger.Domain.Primitives;

namespace RideLedger.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for Account aggregate
/// </summary>
public sealed class AccountId : ValueObject
{
    public Guid Value { get; }

    private AccountId(Guid value)
    {
        Value = value;
    }

    public static AccountId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Account ID cannot be empty", nameof(value));
        }

        return new AccountId(value);
    }

    public static AccountId CreateNew() => new(Guid.NewGuid());

    public static implicit operator Guid(AccountId accountId) => accountId.Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
