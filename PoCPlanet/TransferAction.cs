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

    public ImmutableHashSet<Address> RequestStates(Address from, Address to) => ImmutableHashSet.Create(to);

    public ImmutableDictionary<Address, Dictionary> Execute(
        Address from,
        Address to,
        ImmutableDictionary<Address, Dictionary> states
        )
    {
        if (new Address(PublicKey) != from)
        {
            throw new ActionError("The public key does not match the transaction sender");
        }

        var balanceExists = states.TryGetValue(to, out var balanceState);
        var balance = balanceExists ? Balance.Deserialize(balanceState!) : new Balance(0, PublicKey);
        var newBalance = balance with { BalanceValue = balance.BalanceValue + Amount };
        return ImmutableDictionary<Address, Dictionary>.Empty.Add(to, newBalance.Serialize());
    }

    public Dictionary Serialize() =>
        Dictionary.Empty
            .Add(PublicKeyKey, PublicKey.ToImmutableArray(false))
            .Add(RecipientKey, Recipient)
            .Add(AmountKey, Amount);

    public static TransferAction Deserialize(Dictionary data)
    {
        return new TransferAction(
            PublicKey: new PublicKey(data.GetValue<Binary>(PublicKeyKey).ToByteArray()),
            Recipient: new Address(data.GetValue<Binary>(RecipientKey)),
            Amount: data.GetValue<Integer>(AmountKey)
        );
    }

    public virtual bool Equals(IAction? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return other is TransferAction action &&
               PublicKey.Equals(action.PublicKey) &&
               Recipient.Equals(action.Recipient) &&
               Amount == action.Amount;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PublicKey, Recipient, Amount);
    }
}