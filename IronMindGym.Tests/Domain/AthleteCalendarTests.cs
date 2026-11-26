using IronMindGym.Services;

namespace IronMindGym.Tests.Domain;

public class AthleteCalendarTests
{
    private static readonly AthleteCalendar Kyiv = AthleteCalendar.FromId("Europe/Kyiv");

    [Fact]
    public void KnowsRealZonesAndFallsBackToUtc()
    {
        Assert.True(AthleteCalendar.TryFindZone("Europe/Kyiv", out _));
        Assert.False(AthleteCalendar.TryFindZone("Mars/Olympus", out _));
        Assert.False(AthleteCalendar.TryFindZone(null, out _));
        Assert.Same(AthleteCalendar.Utc, AthleteCalendar.FromId(null));
        Assert.Same(AthleteCalendar.Utc, AthleteCalendar.FromId("Mars/Olympus"));
        Assert.Equal("Europe/Kyiv", Kyiv.IanaZoneId);
    }

    [Fact]
    public void TheTrainingWeekStartsOnTheLocalMonday()
    {
        // Sunday 23:30 UTC is already Monday 01:30 in Kyiv (UTC+2 in November).
        var lateSundayUtc = new DateTime(2026, 11, 22, 23, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 11, 22, 22, 0, 0), Kyiv.WeekStartUtc(lateSundayUtc));
        Assert.Equal(new DateTime(2026, 11, 16), AthleteCalendar.Utc.WeekStartUtc(lateSundayUtc));
        Assert.Equal(new DateTime(2026, 11, 23), Kyiv.DayOf(lateSundayUtc));
        Assert.Equal("2026-11-23 01:30", Kyiv.Format(lateSundayUtc, "yyyy-MM-dd HH:mm"));
    }

    [Fact]
    public void TheWeekStartUsesTheSummerOffset()
    {
        // Kyiv is UTC+3 in summer, so the local Monday midnight is 21:00 UTC on Sunday.
        Assert.Equal(new DateTime(2026, 7, 12, 21, 0, 0), Kyiv.WeekStartUtc(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void ADayStartsAtTheFirstValidHourWhenMidnightIsSkipped()
    {
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2000, 1, 1), new DateTime(2100, 12, 31), TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 3, 1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 10, 1));
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test/MidnightJump", TimeSpan.Zero, "Test", "Test", "Test summer", [rule]);

        Assert.Equal(new DateTime(2027, 3, 1, 0, 0, 0), new AthleteCalendar(zone).StartOfDayUtc(new DateTime(2027, 3, 1)));
    }

    private static readonly int[] StreakDays = [1, 2, 2, 3, 5, 6];

    [Fact]
    public void TheLongestStreakCountsConsecutiveCalendarDays()
    {
        Assert.Equal(3, AthleteCalendar.LongestStreak(StreakDays.Select(d => new DateTime(2026, 11, d, 18, 0, 0))));
        Assert.Equal(1, AthleteCalendar.LongestStreak([new DateTime(2026, 11, 1)]));
        Assert.Equal(0, AthleteCalendar.LongestStreak([]));
    }
}
