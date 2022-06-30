using System.Numerics;
using System.Security.Cryptography;

namespace PoCPlanet;

public static class Hashcash
{
    public static Nonce Answer(Stamp stamp, int difficulty)
    {
        var sha256 = SHA256.Create();
        BigInteger counter = 1;
        while (true)
        {
            var nonceVal = counter.ToByteArray();
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(nonceVal);
            var answer = new Nonce(nonceVal);
            var digest = new Hash(sha256.ComputeHash(stamp(answer)));
            if (HasLeadingZeroBits(digest, difficulty))
            {
                return answer;
            }

            counter++;
        }
    }

    public static bool HasLeadingZeroBits(Hash digest, int bits)
    {
        var leadingBytes = bits / 8;
        var trailingBits = bits % 8;
        for (var i = 0; i < leadingBytes; i++)
        {
            if (digest[i] != Convert.ToByte(0))
            {
                return false;
            }
        }

        if (trailingBits > 0)
        {
            if (digest.Length <= leadingBytes)
            {
                return false;
            }

            var mask = (byte)(0xff << (8 - trailingBits) & 0xff);
            return (mask & digest[leadingBytes]) == 0x0;
        }

        return true;
    }
}

public delegate byte[] Stamp(Nonce nonce);

public record Nonce(byte[] Bytes) : ImmutableHexBytes(Bytes);

public record Hash(byte[] Bytes) : ImmutableHexBytes(Bytes);