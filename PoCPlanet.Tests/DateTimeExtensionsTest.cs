using Org.BouncyCastle.Asn1.X509;
using static NUnit.Framework.Assert;

namespace PoCPlanet.Tests;

public class DateTimeExtensionsTest
{
    [Test]
    public void ToRfc3339()
    {
        var utc = new DateTime(2018, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        That(utc.ToRfc3339(), Is.EqualTo("2018-01-02T03:04:05.006000Z"));
        Throws(typeof(ArgumentException), () => new DateTime(utc.Ticks, DateTimeKind.Unspecified).ToRfc3339());
        That(utc.ToLocalTime().ToRfc3339(), Is.EqualTo("2018-01-02T03:04:05.006000Z"));
    }

    [Test]
    public void FromRfc3339ToDateTime()
    {
        var expected = new DateTime(2018, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var expected2 = new DateTime(2018, 1, 2, 3, 4, 5, 0, DateTimeKind.Utc);
        That(DateTimeExtensions.Rfc3339ToDateTime("2018-01-02T03:04:05.006000Z"), Is.EqualTo(expected));
        That(DateTimeExtensions.Rfc3339ToDateTime("2018-01-02T03:04:05.000000Z"), Is.EqualTo(expected2));
        That(DateTimeExtensions.Rfc3339ToDateTime("2018-01-02T03:04:05Z"), Is.EqualTo(expected2));
    }
}