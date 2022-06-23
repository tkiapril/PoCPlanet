using Bencodex.Types;
using Libplanet.Crypto;

namespace PoCPlanet;

[Serializable()]
public record Balance(
    int BalanceValue,
    PublicKey PublicKey
    ): IState
{
    public static readonly byte[] PublicKeyKey = { Convert.ToByte('p') };
    public static readonly byte[] BalanceKey = { Convert.ToByte('b') };

    public static Balance Deserialize(Dictionary data) => 
        new Balance(
            BalanceValue: data.GetValue<Integer>(BalanceKey),
            PublicKey: new PublicKey(data.GetValue<Binary>(PublicKeyKey).ByteArray)
            );

    public Dictionary Serialize() =>
        Dictionary.Empty
            .Add(BalanceKey, BalanceValue)
            .Add(PublicKeyKey, PublicKey.ToImmutableArray(compress: true));
}