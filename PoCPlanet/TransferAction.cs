using System.Collections.Immutable;
using System.Numerics;
using System.Reflection;
using Bencodex.Types;
using Libplanet.Crypto;

namespace PoCPlanet;

[Serializable()]
public record TransferAction(
        PublicKey PublicKey,
        Address Recipient,
        BigInteger Amount
        ) : IAction
{
    public static string ActionTypeId => "transfer";

    public static readonly byte[] PublicKeyKey = { Convert.ToByte('p') };
    public static readonly byte[] RecipientKey = { Convert.ToByte('r') };
    public static readonly byte[] AmountKey = { Convert.ToByte('a') };

    public ImmutableHashSet<Address> RequestStates(Address from, Address to) =>
        ImmutableHashSet.Create(from, to);

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

        var fromBalance = states[from].Keys.Any() ? Balance.Deserialize(states[from]) : new Balance(0, PublicKey);
        var toBalance = states[to].Keys.Any() ? Balance.Deserialize(states[to]) : new Balance(0, PublicKey);
        return ImmutableDictionary<Address, Dictionary>.Empty
            .Add(from, fromBalance.Decrement(Amount).Serialize())
            .Add(to, toBalance.Increment(Amount).Serialize());
    }

    public Dictionary Serialize() =>
        Dictionary.Empty
            .Add(IAction.ActionTypeIdKey, ActionTypeId)
            .Add(IAction.ValuesKey, Dictionary.Empty
                .Add(PublicKeyKey, PublicKey.ToImmutableArray(false))
                .Add(RecipientKey, Recipient)
                .Add(AmountKey, Amount.ToByteArray())
            );

    public static TransferAction Deserialize(Dictionary data)
    {
        if (data.GetValue<Text>(IAction.ActionTypeIdKey) != ActionTypeId)
        {
            throw new ArgumentException(
                $"Input data does not match the type {MethodBase.GetCurrentMethod()!.DeclaringType}"
                );
        }

        data = data.GetValue<Dictionary>(IAction.ValuesKey);

        return new TransferAction(
            PublicKey: new PublicKey(data.GetValue<Binary>(PublicKeyKey).ToByteArray()),
            Recipient: new Address(data.GetValue<Binary>(RecipientKey)),
            Amount: new BigInteger(data.GetValue<Binary>(AmountKey))
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