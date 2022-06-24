using System.Security.Cryptography;
using static NUnit.Framework.Assert;

namespace PoCPlanet.Tests;

public class HashcashTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Answer()
    {
        var rnd = new Random();
        var sha256 = SHA256.Create();
        var challenge = new byte[40];
        int[] bits = { 4, 8, 12, 16 };
        for (var i = 0; i < 3; i++)
        {
            rnd.NextBytes(challenge);
            foreach (var j in bits)
            {
                byte[] Stamp(Nonce nonce) => challenge.Concat(nonce).ToArray();
                var answer = Hashcash.Answer(Stamp, j);
                var digest = new Hash(sha256.ComputeHash(Stamp(answer)));
                That(Hashcash.HasLeadingZeroBits(digest, j));
            }
        }
    }

    [Test]
    public void HasLeadingZeroBits()
    {
        Assert.Multiple(() =>
        {
            That(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0x80, 0x61, 0x62, 0x63 }), 0));
            That(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0x80, 0x61, 0x62, 0x63 }), 1), Is.False);
            for (var i = 0; i < 9; i++)
            {
                That(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x80 }), 1));
            }
            That(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x80 }), 9), Is.False);
            That(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x7f }), 9));
            That(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x7f }), 10), Is.False);
            That(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0, 0x3f }), 10));
            That(Hashcash.HasLeadingZeroBits(new Hash(new byte[] { 0 }), 9), Is.False);
        });
    }
}