using System.Globalization;

namespace PoCPlanet;

public static class DateTimeExtensions
{
    public static string ToRfc3339(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);
    }
}