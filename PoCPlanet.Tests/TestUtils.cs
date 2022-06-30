namespace PoCPlanet.Tests;

public static class TestUtils
{
    private static readonly Random Random = new Random();

    public static byte[] RandomBytes(int count)
    {
        var randomBytes = new byte[count];
        Random.NextBytes(randomBytes);
        return randomBytes;
    }

}