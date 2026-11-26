using IronMindGym.Data;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>
/// Calendar rules for one athlete. Timestamps are stored in UTC; "today", training weeks
/// and streaks follow the time zone saved in the athlete's profile (UTC when unset).
/// </summary>
public sealed class AthleteCalendar(TimeZoneInfo zone)
{
    public static readonly AthleteCalendar Utc = new(TimeZoneInfo.Utc);

    public TimeZoneInfo Zone { get; } = zone;

    public DateOnly Today => DayOf(DateTime.UtcNow);

    public string IanaZoneId => TimeZoneInfo.TryConvertWindowsIdToIanaId(Zone.Id, out var iana) ? iana : Zone.Id;

    public static AthleteCalendar For(IronMindDbContext db, int userId) => FromId(db.Profiles.AsNoTracking()
        .Where(p => p.UserId == userId).Select(p => p.TimeZone).FirstOrDefault());

    public static AthleteCalendar FromId(string? timeZoneId) =>
        TryFindZone(timeZoneId, out var zone) ? new AthleteCalendar(zone) : Utc;

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

    public DateTime WeekStartUtc(DateTime utcNow) => StartOfDayUtc(WeekStart(DayOf(utcNow)));

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

public sealed class CurrentCalendar(IHttpContextAccessor accessor, IronMindDbContext db)
{
    private AthleteCalendar? calendar;

    public AthleteCalendar Value => calendar ??= accessor.HttpContext?.User.GetUserId() is int userId
        ? AthleteCalendar.For(db, userId)
        : AthleteCalendar.Utc;

    public DateOnly Today => Value.Today;
}
