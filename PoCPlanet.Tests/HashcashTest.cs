using System.Security.Cryptography;
using static NUnit.Framework.Assert;

namespace PoCPlanet.Tests;

public class HashcashTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test, TestCaseSource(nameof(AnswerSource))]
    public void Answer(byte[] challenge, int bits)
    {
        byte[] Stamp(Nonce nonce) => challenge.Concat(nonce).ToArray();
        var answer = Hashcash.Answer(Stamp, bits);
        var digest = new Hash(SHA256.Create().ComputeHash(Stamp(answer)));
        That(Hashcash.HasLeadingZeroBits(digest, bits));
    }

    public static IEnumerable<TestCaseData> AnswerSource() =>
        from challenge in
                from _ in Enumerable.Range(0, 5) select TestUtils.RandomBytes(40)
            from bits in
                from i in Enumerable.Range(1, 4) select i * 4
            select new TestCaseData(challenge, bits);

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