namespace JoFit.Application.Common.Utilities;

/// <summary>The business time zone is Asia/Amman. Dates and slots are read and written in it.</summary>
public static class AmmanTime
{
    private static readonly TimeZoneInfo Zone = Find();

    private static TimeZoneInfo Find()
    {
        foreach (var id in new[] { "Asia/Amman", "Jordan Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Amman", TimeSpan.FromHours(3), "Amman", "Amman");
    }

    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone).DateTime);

    /// <summary>The instant of a local Amman date and hour, carrying the Amman offset (for example +03:00).</summary>
    public static DateTimeOffset At(DateOnly date, int hour, int minute = 0)
    {
        var local = date.ToDateTime(new TimeOnly(hour, minute), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local));
    }

    public static DateTimeOffset ToAmman(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);
}
