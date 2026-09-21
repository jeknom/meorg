using System.Globalization;

namespace MeOrg.Extensions;

public static class DateTimeExtensions
{
    public static string ToYearMonthDate(this DateTime dateTime) => dateTime.ToString(Constants.YEAR_MONTH_DATE_FORMAT, CultureInfo.InvariantCulture);

    public static DateTime SpecifyUtcAndConvertToLocal(this DateTime dateTime)
    {
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();
    }
}
