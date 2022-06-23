using Libplanet.Crypto;
using Org.BouncyCastle.Crypto.Digests;

namespace PoCPlanet;

public record Address(byte[] Data)
{
    public Address(PublicKey publicKey) : this(DeriveAddress(publicKey))
    {
    }

    private static byte[] DeriveAddress(PublicKey publicKey)
    {
        var digest = new KeccakDigest(256);
        var output = new byte[digest.GetDigestSize()];
        var hashPayload = publicKey.Format(false).Skip(1).ToArray();
        digest.BlockUpdate(hashPayload, 0, hashPayload.Length);
        digest.DoFinal(output, 0);
        return output.Skip(output.Length - 20).ToArray();
    }
}