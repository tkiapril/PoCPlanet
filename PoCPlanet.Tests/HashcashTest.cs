using System.Security.Cryptography;

namespace PoCPlanet.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Answer()
    {
        Random rnd = new Random();
        var sha256 = SHA256.Create();
        Byte[] challenge = new byte[40];
        int[] bits = { 4, 8, 12, 16 };
        for (var i = 0; i < 3; i++)
        {
            rnd.NextBytes(challenge);
            foreach (var j in bits)
            {
                byte[] Stamp(Nonce nonce) => challenge.Concat(nonce.Data).ToArray();
                var answer = Hashcash.Answer(Stamp, j);
                var digest = new Hash(sha256.ComputeHash(Stamp(answer)));
                Assert.IsTrue(Hashcash.HasLeadingZeroBits(digest, j));
            }
        }
    }

    [Test]
    public void HasLeadingZeroBits()
    {
        Assert.IsTrue(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0x80, 0x61, 0x62, 0x63 }), 0));
        Assert.IsFalse(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0x80, 0x61, 0x62, 0x63 }), 1));
        for (int i = 0; i < 9; i++)
        {
            Assert.IsTrue(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x80 }), 1));
        }
        Assert.IsFalse(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x80 }), 9));
        Assert.IsTrue(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x7f }), 9));
        Assert.IsFalse(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x7f }), 10));
        Assert.IsTrue(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x3f }), 10));
        Assert.IsFalse(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0 }), 9));
    }
}