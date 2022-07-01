using System.Globalization;

namespace PoCPlanet;

public static class DateTimeExtensions
{
    public static string ToRfc3339(this DateTime dateTime)
    {
        if (dateTime.Kind.Equals(DateTimeKind.Unspecified))
        {
            throw new ArgumentException("Exepcted an timezone-aware DateTime");
        }
        return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);
    }
}