using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;

namespace PoCPlanet;

[Serializable()]
public record Transaction(
    Address Sender,
    PublicKey PublicKey,
    Signature Signature,
    Address Recipient,
    List<IAction> Actions,
    DateTime Timestamp
)
{
    private static readonly byte[] SenderKey = { Convert.ToByte('s') };
    private static readonly byte[] PublicKeyKey = { Convert.ToByte('P') };
    private static readonly byte[] SignatureKey = { Convert.ToByte('S') };
    private static readonly byte[] RecipientKey = { Convert.ToByte('r') };
    private static readonly byte[] ActionsKey = { Convert.ToByte('A') };
    private static readonly byte[] TimestampKey = { Convert.ToByte('t') };

    public Transaction(Transaction tx, Signature sig) : this(
        Sender: tx.Sender,
        PublicKey: tx.PublicKey,
        Signature: sig,
        Recipient: tx.Recipient,
        Actions: tx.Actions,
        Timestamp: tx.Timestamp
    )
    {
    }

    public static Transaction Make(PrivateKey privateKey, Address recipient, List<IAction> actions, DateTime timestamp)
    {
        var publicKey = privateKey.PublicKey;
        var tx = new Transaction(
            Sender: new Address(publicKey),
            PublicKey: publicKey,
            Recipient: recipient,
            Actions: actions,
            Timestamp: timestamp,
            Signature: new Signature(Array.Empty<byte>())
        );
        return new Transaction(tx: tx, sig: new Signature(privateKey.Sign(tx.Bencode(sign: false))));
    }

    public TxId Id
    {
        get
        {
            var sha256 = SHA256.Create();
            return new TxId(sha256.ComputeHash(Bencode(sign: false)));
        }
    }

    public void Validate()
    {
        bool verified = false;
        try
        {
            verified = PublicKey.Verify(message: Bencode(sign: false), signature: Signature.Data);
        }
        catch (ArgumentNullException)
        {
        }

        if (!verified)
        {
            throw new Exception(message: $"The signature {Convert.ToHexString(Signature.Data)} failed to verify.");
        }

        if (!new Address(PublicKey).Data.SequenceEqual(Sender.Data))
        {
            throw new Exception(
                message: $"The public key {Convert.ToHexString(PublicKey.Format(compress: false))} "
                         + $"does not match the address {Convert.ToHexString(Sender.Data)}"
                );
        }
    }

    public Dictionary Serialize(bool sign)
    {
        Dictionary dict = Dictionary.Empty
            .Add(SenderKey, Sender.Data)
            .Add(PublicKeyKey, PublicKey.ToImmutableArray(compress: true))
            .Add(RecipientKey, Recipient.Data)
            .Add(ActionsKey, from action in Actions select action.Serialize())
            .Add(TimestampKey, Timestamp.ToRfc3339());

        if (sign)
        {
            dict = dict.Add(SignatureKey, Signature.Data);
        }

        return dict;
    }

    public byte[] Bencode(bool sign) => new Codec().Encode(Serialize(sign: sign));
}

public record TxId(byte[] Data);

public record Signature(byte[] Data);