using System.Numerics;
using Bencodex.Types;
using Libplanet.Crypto;

namespace PoCPlanet;

[Serializable()]
public record Balance(
    BigInteger BalanceValue,
    PublicKey PublicKey
    ): IState
{
    public static readonly byte[] PublicKeyKey = { Convert.ToByte('p') };
    public static readonly byte[] BalanceValueKey = { Convert.ToByte('b') };

    public static Balance Deserialize(Dictionary data) => 
        new (
            BalanceValue: new BigInteger(data.GetValue<Binary>(BalanceValueKey).ToByteArray()),
            PublicKey: new PublicKey(data.GetValue<Binary>(PublicKeyKey).ByteArray)
            );

    public Dictionary Serialize() =>
        Dictionary.Empty
            .Add(BalanceValueKey, BalanceValue.ToByteArray())
            .Add(PublicKeyKey, PublicKey.ToImmutableArray(compress: true));

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

public class BalanceError : ArgumentException
{
    public BalanceError(string? message) : base(message)
    {
    }
}