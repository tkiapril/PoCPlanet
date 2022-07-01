using System.Numerics;
using Bencodex.Types;
using Libplanet.Crypto;

namespace PoCPlanet;

[Serializable()]
public record Balance(
    BigInteger BalanceValue,
    Address Address
    ): IState
{
    public static readonly byte[] AddressKey = { Convert.ToByte('a') };
    public static readonly byte[] BalanceValueKey = { Convert.ToByte('b') };

    public static Balance Deserialize(Dictionary data) => 
        new (
            BalanceValue: new BigInteger(data.GetValue<Binary>(BalanceValueKey).ToByteArray()),
            Address: new Address(data.GetValue<Binary>(AddressKey).ToByteArray())
            );

    public Dictionary Serialize() =>
        Dictionary.Empty
            .Add(BalanceValueKey, BalanceValue.ToByteArray())
            .Add(AddressKey, Address);

    public Balance Increment(BigInteger amount) => this with { BalanceValue = BalanceValue + amount };

    public Balance Decrement(BigInteger amount)
    {
        if (amount > BalanceValue)
        {
            throw new BalanceError("The amount to decrement is larger than the balance");
        }

        return this with { BalanceValue = BalanceValue - amount };
    }
}

public class BalanceError : StateTransitionError
{
    public BalanceError(string? message) : base(message)
    {
    }
}