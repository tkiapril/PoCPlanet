using static NUnit.Framework.Assert;

namespace PoCPlanet.Tests;

public class AddressTest : FixtureBase
{
    [Test]
    public void NewAddress()
    {
        var expected = new Address(new byte[]
        {
            0x32, 0x08, 0xA7, 0x0E, 0xB9, 0x8A, 0x8E, 0x3D, 0x0D, 0xDA, 0x31, 0x2F, 0xB4, 0xAB, 0xD9, 0x3E, 0x61, 0x59,
            0x6D, 0xF3
        });
        That(expected, Is.EqualTo(Address));
    }

    [Test]
    public new void ToString()
    {
        const string expected = "0x3208a70eb98a8e3d0dda312fb4abd93e61596df3";
        That(Address.ToString(), Is.EqualTo(expected));
    }
}