using System.Collections.Immutable;
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
    IReadOnlyList<IAction> Actions,
    DateTime Timestamp
) : IFormattable
{
    public static readonly byte[] SenderKey = { Convert.ToByte('s') };
    public static readonly byte[] PublicKeyKey = { Convert.ToByte('P') };
    public static readonly byte[] SignatureKey = { Convert.ToByte('S') };
    public static readonly byte[] RecipientKey = { Convert.ToByte('r') };
    public static readonly byte[] ActionsKey = { Convert.ToByte('A') };
    public static readonly byte[] TimestampKey = { Convert.ToByte('t') };

    public static Transaction Make(PrivateKey privateKey, Address recipient, IReadOnlyList<IAction> actions, DateTime timestamp)
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
        return tx with { Signature = new Signature(privateKey.Sign(tx.Bencode(sign: false))) };
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
        var verified = false;
        try
        {
            verified = PublicKey.Verify(message: Bencode(sign: false), signature: Signature);
        }
        catch (ArgumentNullException)
        {
        }

        if (!verified)
        {
            throw new TransactionSignatureError(message: $"The signature {Signature} failed to verify.");
        }

        if (new Address(PublicKey) != Sender)
        {
            throw new TransactionPublicKeyError(
                message: $"The public key {Convert.ToHexString(PublicKey.Format(compress: false))} "
                         + $"does not match the address {Sender}"
                );
        }
    }

    public Dictionary Serialize(bool sign)
    {
        Dictionary dict = Dictionary.Empty
            .Add(SenderKey, Sender)
            .Add(PublicKeyKey, PublicKey.ToImmutableArray(compress: true))
            .Add(RecipientKey, Recipient)
            .Add(ActionsKey, from action in Actions select action.Serialize())
            .Add(TimestampKey, Timestamp.ToRfc3339());

        if (sign)
        {
            dict = dict.Add(SignatureKey, Signature);
        }

        return dict;
    }

    public static Transaction Deserialize(Dictionary data)
    {
        var actions = (from a in data.GetValue<List>(Transaction.ActionsKey)
            select IAction.Deserialize((Dictionary)a)).ToImmutableArray();
        var tx = new Transaction(
            Sender: new Address(data.GetValue<Binary>(SenderKey)),
            PublicKey: new PublicKey(data.GetValue<Binary>(PublicKeyKey).ByteArray),
            Signature: new Signature(data.GetValue<Binary>(SignatureKey)),
            Recipient: new Address(data.GetValue<Binary>(RecipientKey)),
            Actions: actions,
            Timestamp: DateTimeExtensions.Rfc3339ToDateTime(data.GetValue<Text>(TimestampKey))
        );
        tx.Validate();
        return tx;
    }

    public byte[] Bencode(bool sign) => new Codec().Encode(Serialize(sign: sign));

    public virtual bool Equals(Transaction? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Sender.Equals(other.Sender)
               && PublicKey.Equals(other.PublicKey)
               && Signature.Equals(other.Signature)
               && Recipient.Equals(other.Recipient)
               && Actions.SequenceEqual(other.Actions)
               && Timestamp.Equals(other.Timestamp);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Sender, PublicKey, Signature, Recipient, Actions, Timestamp);
    }

    public override string ToString() => Convert.ToHexString(Id).ToLower();
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();
}

public class TransactionError : ArgumentException {
    public TransactionError(string? message) : base(message)
    {
    }
}

public class TransactionIdError : TransactionError {
    public TransactionIdError(string? message) : base(message)
    {
    }
}

public class TransactionSignatureError : TransactionError {
    public TransactionSignatureError(string? message) : base(message)
    {
    }
}

public class TransactionPublicKeyError : TransactionError {
    public TransactionPublicKeyError(string? message) : base(message)
    {
    }
}

public record TxId(byte[] Bytes) : ImmutableHexBytes(Bytes);

public record Signature(byte[] Bytes) : ImmutableHexBytes(Bytes);