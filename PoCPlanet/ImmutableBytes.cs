using System.Collections;

namespace PoCPlanet;

public record ImmutableBytes(byte[] Bytes) : IReadOnlyList<byte>
{
    public static implicit operator byte[](ImmutableBytes immutableBytes) => immutableBytes.Bytes;
    public byte this[int index] => Bytes[index];
    public int Length => Bytes.Length;

    public virtual bool Equals(ImmutableBytes? other) =>
        !ReferenceEquals(null, other) && ReferenceEquals(this, other) || Bytes.SequenceEqual(other!.Bytes);

    public IEnumerator<byte> GetEnumerator() => Bytes.OfType<byte>().GetEnumerator();

    public override int GetHashCode() =>
        Bytes.Length == 0 ? 0 : Bytes.Aggregate(17, (current, b) => current * 31 + b.GetHashCode());

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => Length;
}

public record ImmutableHexBytes(byte[] Bytes) : ImmutableBytes(Bytes), IFormattable
{
    public override string ToString() => Convert.ToHexString(this);
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();
}