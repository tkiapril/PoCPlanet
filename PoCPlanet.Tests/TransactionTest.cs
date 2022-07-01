using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Crypto;
using NUnit.Framework.Internal;
using static NUnit.Framework.Assert;

namespace PoCPlanet.Tests;

public class TransactionTest : FixtureBase
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Make()
    {
        var dt = new DateTime(2018, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var tx = Transaction.Make(
            PrivateKey,
            recipient: RecipientAddress,
            timestamp: dt,
            actions: ImmutableArray<IAction>.Empty
        );
        Multiple(() =>
        {
            That(tx.Sender, Is.EqualTo(Address));
            That(tx.PublicKey, Is.EqualTo(PublicKey));
            That(tx.Recipient, Is.EqualTo(RecipientAddress));
            That(tx.Timestamp, Is.EqualTo(dt));
            That(tx.Signature, Is.EqualTo(new byte[]
            {
                0x30, 0x45, 0x02, 0x21, 0x00, 0xF3, 0xA8, 0x32, 0x60, 0xCF, 0x8F, 0x47, 0x62, 0x15, 0x2B, 0xBF, 0x90,
                0xA0, 0x6F, 0x9A, 0x44, 0x4F, 0x43, 0xAF, 0x02, 0x99, 0xA8, 0x0A, 0x4B, 0x77, 0x78, 0x26, 0x20, 0xC6,
                0x13, 0x62, 0xD1, 0x02, 0x20, 0x1B, 0xB5, 0x3B, 0x7A, 0xF7, 0xC5, 0x67, 0xD9, 0x9C, 0x14, 0x06, 0x90,
                0x45, 0xBB, 0x40, 0x56, 0x40, 0x88, 0xAA, 0xEE, 0x5A, 0x6E, 0x97, 0x33, 0xFC, 0xFB, 0xD7, 0x08, 0x52,
                0x5E, 0x5D, 0xEC
            }));
        });
    }

    [Test]
    public void Validate()
    {
        Transaction.Validate();

        var txInvalidSignature = Transaction with { Timestamp = DateTime.Now.ToUniversalTime() };
        Throws(typeof(TransactionSignatureError), () => txInvalidSignature.Validate());

        var invalidSender = new Address(new PrivateKey().PublicKey);
        var txInvalidSender = Transaction with { Sender = invalidSender };
        txInvalidSender = txInvalidSender with
        {
            Signature = new Signature(PrivateKey.Sign(txInvalidSender.Bencode(sign: false)))
        };
        Throws(typeof(TransactionPublicKeyError), () => txInvalidSender.Validate());
    }

    private static readonly Dictionary SerializedTransaction =
        Dictionary.Empty
            .Add(Transaction.SenderKey, new byte[]
            {
                0x32, 0x08, 0xa7, 0x0e, 0xb9, 0x8a, 0x8e, 0x3d, 0x0d, 0xda, 0x31, 0x2f, 0xb4, 0xab, 0xd9, 0x3e, 0x61,
                0x59, 0x6d, 0xf3
            })
            .Add(Transaction.PublicKeyKey, new byte[]
            {
                0x02, 0xc1, 0x83, 0x3d, 0xcb, 0xf2, 0xc2, 0xb9, 0xdc, 0x9e, 0xfe, 0x96, 0x6a, 0x4f, 0x7b, 0x6b, 0x54,
                0xa2, 0xcf, 0x2e, 0x65, 0x75, 0xe1, 0x20, 0x06, 0xe2, 0x6e, 0x02, 0xd5, 0x55, 0x77, 0x4d, 0x70
            })
            .Add(Transaction.RecipientKey, new byte[]
            {
                0x80, 0x36, 0x46, 0xd3, 0x0c, 0xb8, 0x5d, 0x3a, 0x24, 0x8a, 0x87, 0x10, 0x90, 0xdb, 0x99, 0x98, 0xb2,
                0xe2, 0x2e, 0x71
            })
            .Add(Transaction.TimestampKey, "2018-01-02T03:04:05.006000Z")
            .Add(Transaction.ActionsKey, ImmutableList<IValue>.Empty)
            .Add(Transaction.SignatureKey, new byte[]
            {
                0x30, 0x45, 0x02, 0x21, 0x00, 0xf3, 0xa8, 0x32, 0x60, 0xcf, 0x8f, 0x47, 0x62, 0x15, 0x2b, 0xbf, 0x90,
                0xa0, 0x6f, 0x9a, 0x44, 0x4f, 0x43, 0xaf, 0x02, 0x99, 0xa8, 0x0a, 0x4b, 0x77, 0x78, 0x26, 0x20, 0xc6,
                0x13, 0x62, 0xd1, 0x02, 0x20, 0x1b, 0xb5, 0x3b, 0x7a, 0xf7, 0xc5, 0x67, 0xd9, 0x9c, 0x14, 0x06, 0x90,
                0x45, 0xbb, 0x40, 0x56, 0x40, 0x88, 0xaa, 0xee, 0x5a, 0x6e, 0x97, 0x33, 0xfc, 0xfb, 0xd7, 0x08, 0x52,
                0x5e, 0x5d, 0xec
            });

    [Test]
    public void Serialize()
    {
        That(Transaction.Serialize(sign: true), Is.EqualTo(SerializedTransaction));
        That(
            Transaction.Serialize(sign: false),
            Is.EqualTo(SerializedTransaction.Remove(new Binary(Transaction.SignatureKey)))
            );
    }

    [Test]
    public void Deserialize()
    {
        var tx = Transaction.Deserialize(SerializedTransaction);
        That(tx, Is.EqualTo(Transaction));
        var data = SerializedTransaction.SetItem(
            Transaction.SignatureKey,
            new Signature((from c in "invalid" select Convert.ToByte(c)).ToArray())
        );
        Throws(typeof(TransactionSignatureError), () => Transaction.Deserialize(data));
    }

    [Test]
    public void Hash()
    {
        That(Transaction.GetHashCode(), Is.EqualTo(Transaction.GetHashCode()));
        That(
            Transaction.GetHashCode(),
            Is.Not.EqualTo((Transaction with { Signature = new Signature(new byte[]{} )}).GetHashCode())
            );
    }

    [Test]
    public void ToString()
    {
        That(Transaction.ToString(), Is.EqualTo("646de2b55d243d5c27ab13b10e0389aaf14832102522178225c7ce19f6282c32"));
    }
}