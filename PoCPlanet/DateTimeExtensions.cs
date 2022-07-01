using System.Globalization;
using System.Xml;

namespace PoCPlanet;

public static class DateTimeExtensions
{
    public static string ToRfc3339(this DateTime dateTime)
    {
        if (dateTime.Kind.Equals(DateTimeKind.Unspecified))
        {
            throw new ArgumentException("Expected an timezone-aware DateTime");
        }
        return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);
    }

    public static DateTime Rfc3339ToDateTime(string rfc3339) =>
        XmlConvert.ToDateTime(rfc3339, XmlDateTimeSerializationMode.Utc);
}