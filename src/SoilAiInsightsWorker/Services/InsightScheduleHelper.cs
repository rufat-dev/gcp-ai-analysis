namespace SoilAiInsightsWorker.Services;

/// <summary>
/// Logical insight day for daily 00:00 batch: date in configured timezone (or UTC).
/// insight_date stored as DATETIME at midnight for that local calendar day.
/// </summary>
public static class InsightScheduleHelper
{
    public static DateTime GetInsightDateStartUtc(DateTime utcNow, string? ianaTimeZoneId)
    {
        if (string.IsNullOrWhiteSpace(ianaTimeZoneId))
        {
            var d = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
            return d;
        }

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), tz);
            var localMidnight = new DateTime(local.Year, local.Month, local.Day, 0, 0, 0, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localMidnight, tz);
        }
        catch
        {
            var d = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
            return d;
        }
    }
}
