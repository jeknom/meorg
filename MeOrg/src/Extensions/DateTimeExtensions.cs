using System.Globalization;

namespace MeOrg.Extensions;

public static class DateTimeExtensions
{
    public static string ToMeorgDateString(this DateTime dateTime) => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static DateTime SpecifyUtcAndConvertToLocal(this DateTime dateTime)
    {
        DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        return dateTime.ToLocalTime();
    }
}