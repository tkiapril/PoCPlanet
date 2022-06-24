using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Crypto;

namespace PoCPlanet;

[Serializable()]
public record TransferAction(
        PublicKey PublicKey,
        Address Recipient,
        int Amount
        ) : IAction
{
    public static string ActionTypeId => "transfer";

    public static readonly byte[] PublicKeyKey = { Convert.ToByte('p') };
    public static readonly byte[] RecipientKey = { Convert.ToByte('r') };
    public static readonly byte[] AmountKey = { Convert.ToByte('a') };

    public ImmutableHashSet<Address> RequestStates(Address from, Address to) => ImmutableHashSet.Create<Address>(to);

    public Dictionary<Address, Dictionary> Execute(Address from, Address to, Dictionary<Address, Dictionary> states)
    {
        if (new Address(PublicKey) != from)
        {
            throw new Exception("The public key does not match the transaction sender");
        }

        var balanceExists = states.TryGetValue(to, out var balanceState);
        var balance = balanceExists ? Balance.Deserialize(balanceState!) : new Balance(0, PublicKey);
        var newBalance = balance with { BalanceValue = balance.BalanceValue + Amount };
        return new Dictionary<Address, Dictionary> {[to] = newBalance.Serialize()};
    }

    public Dictionary Serialize() =>
        Dictionary.Empty
            .Add(PublicKeyKey, PublicKey.ToImmutableArray(true))
            .Add(RecipientKey, Recipient)
            .Add(AmountKey, Amount);

    public static IAction Deserialize(Dictionary data)
    {
        return new TransferAction(
            PublicKey: new PublicKey(data.GetValue<Binary>(PublicKeyKey).ToByteArray()),
            Recipient: new Address(data.GetValue<Binary>(RecipientKey)),
            Amount: data.GetValue<Integer>(AmountKey)
        );
    }
}