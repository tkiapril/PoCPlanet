using Libplanet.Crypto;
using Org.BouncyCastle.Crypto.Digests;

namespace PoCPlanet;

public record Address(byte[] Bytes) : ImmutableBytes(Bytes), IFormattable
{
    public Address(PublicKey publicKey) : this(Derive(publicKey))
    {
    }

    private static Address Derive(PublicKey publicKey)
    {
        var digest = new KeccakDigest(256);
        var output = new byte[digest.GetDigestSize()];
        var hashPayload = publicKey.Format(false).Skip(1).ToArray();
        digest.BlockUpdate(hashPayload, 0, hashPayload.Length);
        digest.DoFinal(output, 0);
        return new Address(output.Skip(output.Length - 20).ToArray());
    }

    public override string ToString() =>
        $"0x{Convert.ToHexString(this).ToLower()}";

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();
}