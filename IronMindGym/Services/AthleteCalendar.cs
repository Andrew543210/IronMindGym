using IronMindGym.Data;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class AthleteCalendar(TimeZoneInfo zone, TimeProvider clock)
{
    public TimeZoneInfo Zone { get; } = zone;

    public DateTime UtcNow => clock.UtcNow();

    public DateOnly Today => DayOf(UtcNow);

    public DateTime MonthAgoUtc => UtcNow.AddDays(-30);

    public void EnsureJournalDay(DateOnly day)
    {
        if (day > Today || day < Today.AddYears(-5))
            throw new DomainValidationException("Choose a date within the last five years, up to today.", "Date");
    }

    public DateTime CurrentWeekStartUtc => WeekStartUtc(UtcNow);

    public string IanaZoneId => TimeZoneInfo.TryConvertWindowsIdToIanaId(Zone.Id, out var iana) ? iana : Zone.Id;

    public static AthleteCalendar InZone(string? timeZoneId, TimeProvider clock) =>
        new(TryFindZone(timeZoneId, out var zone) ? zone : TimeZoneInfo.Utc, clock);

    public static bool TryFindZone(string? timeZoneId, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Utc;
        if (string.IsNullOrWhiteSpace(timeZoneId) || timeZoneId.Length > 64)
            return false;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }

    public DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public DateOnly DayOf(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));

    public DateTime StartOfDayUtc(DateOnly day)
    {
        var start = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // A daylight-saving jump can skip local midnight; the day then starts at the next valid hour.
        while (Zone.IsInvalidTime(start))
            start = start.AddHours(1);
        return TimeZoneInfo.ConvertTimeToUtc(start, Zone);
    }

    public DateTime WeekStartUtc(DateTime utc) => StartOfDayUtc(WeekStart(DayOf(utc)));

    public static DateOnly WeekStart(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    public static int LongestStreak(IEnumerable<DateOnly> days)
    {
        var ordered = days.Distinct().Order().ToList();
        int longest = 0, current = 0;
        for (var i = 0; i < ordered.Count; i++)
        {
            current = i > 0 && ordered[i] == ordered[i - 1].AddDays(1) ? current + 1 : 1;
            longest = Math.Max(longest, current);
        }
        return longest;
    }
}

// Reads each athlete's time zone once per request; Forget drops it after the athlete picks another zone.
public sealed class AthleteCalendars(IronMindDbContext db, TimeProvider clock)
{
    private readonly Dictionary<int, AthleteCalendar> calendars = [];

    public async Task<AthleteCalendar> ForAsync(int userId, CancellationToken cancellationToken)
    {
        if (!calendars.TryGetValue(userId, out var calendar))
        {
            var zone = await db.Profiles.AsNoTracking().Where(p => p.UserId == userId).Select(p => p.TimeZone).FirstOrDefaultAsync(cancellationToken);
            calendars[userId] = calendar = AthleteCalendar.InZone(zone, clock);
        }
        return calendar;
    }

    public AthleteCalendar InZone(string? timeZoneId) => AthleteCalendar.InZone(timeZoneId, clock);

    public void Forget(int userId) => calendars.Remove(userId);
}

public static class Clock
{
    public static DateTime UtcNow(this TimeProvider clock) => clock.GetUtcNow().UtcDateTime;
}
