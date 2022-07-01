using Bencodex.Types;
using Libplanet.Crypto;
using Org.BouncyCastle.Math;
using static NUnit.Framework.Assert;
using BigInteger = System.Numerics.BigInteger;

namespace PoCPlanet.Tests;

public class ActionTest : FixtureBase
{
    private static readonly InitializeAction InitializeAction = new (Address);
    private static readonly Dictionary SerializedInitializeAction = Dictionary.Empty
        .Add(IAction.ActionTypeIdKey, InitializeAction.ActionTypeId)
        .Add(
            IAction.ValuesKey,
            Dictionary.Empty
                .Add(InitializeAction.AddressKey, Address)
        );
    private static readonly TransferAction TransferAction = new(
        PublicKey: PublicKey,
        Recipient: Address,
        Amount: 0);
    private static readonly Dictionary SerializedTransferAction = Dictionary.Empty
        .Add(IAction.ActionTypeIdKey, TransferAction.ActionTypeId)
        .Add(
            IAction.ValuesKey,
            Dictionary.Empty
                .Add(TransferAction.PublicKeyKey, PublicKey.ToImmutableArray(false))
                .Add(TransferAction.RecipientKey, Address)
                .Add(TransferAction.AmountKey, ((BigInteger)0).ToByteArray())
            );
    
    [Test]
    public void ActionDeserialize()
    {
        That(IAction.Deserialize(SerializedInitializeAction), Is.EqualTo(InitializeAction));
        That(InitializeAction.Deserialize(SerializedInitializeAction), Is.EqualTo(InitializeAction));
        That(IAction.Deserialize(SerializedTransferAction), Is.EqualTo(TransferAction));
        That(TransferAction.Deserialize(SerializedTransferAction), Is.EqualTo(TransferAction));

        Throws(
            typeof(ArgumentException),
            () => IAction.Deserialize(
                SerializedInitializeAction
                    .SetItem(IAction.ActionTypeIdKey, "invalid")
            )
        );
        
        Throws(
            typeof(ArgumentException),
            () => TransferAction.Deserialize(
                SerializedInitializeAction
                    .SetItem(IAction.ActionTypeIdKey, "invalid")
            )
        );
        
        Throws(
            typeof(KeyNotFoundException),
            () => IAction.Deserialize(
                SerializedTransferAction
                    .SetItem(IAction.ValuesKey, Dictionary.Empty)
            )
        );
    }

    [Test]
    public void ActionSerialize()
    {
        That(TransferAction.Serialize(), Is.EqualTo(SerializedTransferAction));
        That(InitializeAction.Serialize(), Is.EqualTo(SerializedInitializeAction));
    }
}