using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>
/// Every award in <see cref="AchievementCatalog"/> is earned once its metric reaches the target. The metrics are
/// counted from the athlete's saved data, and an earned award stays earned even if the metric later drops.
/// </summary>
public sealed class AchievementService(IronMindDbContext db, AthleteCalendars calendars)
{
    /// <summary>Adds the awards whose target the athlete has reached since the last check.</summary>
    public async Task AwardEarnedAsync(int userId, CancellationToken cancellationToken)
    {
        var earned = await EarnedCodesAsync(userId, cancellationToken);
        var achievementIds = await db.Achievements.AsNoTracking().ToDictionaryAsync(a => a.Code, a => a.Id, cancellationToken);
        var metrics = await MetricsAsync(userId, cancellationToken);
        var now = (await calendars.ForAsync(userId, cancellationToken)).UtcNow;
        foreach (var award in AchievementCatalog.All)
        {
            if (!earned.Contains(award.Code) && metrics[award.MetricType] >= award.TargetValue
                && achievementIds.TryGetValue(award.Code, out var achievementId))
            {
                db.UserAchievements.Add(new UserAchievement { UserId = userId, AchievementId = achievementId, AchievedAt = now });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AwardProgressItem>> ProgressAsync(int userId, CancellationToken cancellationToken)
    {
        var earned = await EarnedCodesAsync(userId, cancellationToken);
        var metrics = await MetricsAsync(userId, cancellationToken);
        return AchievementCatalog.All.Select(award =>
        {
            var unlocked = earned.Contains(award.Code);
            var current = metrics[award.MetricType];
            return new AwardProgressItem
            {
                Code = award.Code,
                Name = Localized.Pick(award.Name, award.NameUk),
                ConditionDescription = Localized.Pick(award.ConditionDescription, award.ConditionDescriptionUk),
                IsUnlocked = unlocked,
                IsNumeric = award.ShowsProgress,
                CurrentValue = award.ShowsProgress ? current : 0,
                TargetValue = award.ShowsProgress ? award.TargetValue : 0,
                ProgressPercent = award.ShowsProgress ? Math.Min(100.0, current / award.TargetValue * 100.0) : unlocked ? 100.0 : 0.0
            };
        }).ToList();
    }

    private Task<HashSet<string>> EarnedCodesAsync(int userId, CancellationToken cancellationToken) =>
        db.UserAchievements.Where(x => x.UserId == userId).Select(x => x.Achievement.Code).ToHashSetAsync(cancellationToken);

    private async Task<Dictionary<AchievementMetricType, double>> MetricsAsync(int userId, CancellationToken cancellationToken)
    {
        var calendar = await calendars.ForAsync(userId, cancellationToken);
        var monthAgo = calendar.MonthAgoUtc;
        var workouts = await db.WorkoutSessions.Where(x => x.UserId == userId)
            .Select(x => new { x.SessionDate, x.DurationMinutes }).ToListAsync(cancellationToken);
        var sets = db.Sets.Where(x => x.PerformedExercise.WorkoutSession.UserId == userId);

        return new Dictionary<AchievementMetricType, double>
        {
            [AchievementMetricType.ProfileCompleted] = await AthleteSetup.IsCompleteAsync(db, userId, cancellationToken) ? 1 : 0,
            [AchievementMetricType.ReviewsWritten] = await db.WorkoutSessions.CountAsync(s => s.UserId == userId && s.Notes != null, cancellationToken),
            [AchievementMetricType.WorkoutsCompleted] = workouts.Count,
            [AchievementMetricType.TotalSets] = await sets.CountAsync(cancellationToken),
            [AchievementMetricType.MonthlyTonnage] = await sets
                .Where(x => x.PerformedExercise.WorkoutSession.SessionDate >= monthAgo)
                .SumAsync(x => (double?)(x.WeightKg * x.Reps), cancellationToken) ?? 0,
            [AchievementMetricType.BenchPr] = await BestRecordAsync(userId, KeyLift.BenchPress, cancellationToken),
            [AchievementMetricType.SquatPr] = await BestRecordAsync(userId, KeyLift.Squat, cancellationToken),
            [AchievementMetricType.DeadliftPr] = await BestRecordAsync(userId, KeyLift.Deadlift, cancellationToken),
            [AchievementMetricType.NutritionLogs] = await db.DailyNutritions.CountAsync(x => x.UserId == userId, cancellationToken),
            [AchievementMetricType.FriendsCount] = await db.Friendships.CountAsync(x =>
                (x.User1Id == userId || x.User2Id == userId) && x.Status == FriendshipStatus.Accepted, cancellationToken),
            [AchievementMetricType.WorkoutStreakDays] = AthleteCalendar.LongestStreak(workouts.Select(x => calendar.DayOf(x.SessionDate))),
            [AchievementMetricType.LongWorkouts] = workouts.Count(x => x.DurationMinutes >= AchievementCatalog.LongWorkoutMinutes)
        };
    }

    private async Task<double> BestRecordAsync(int userId, KeyLift lift, CancellationToken cancellationToken) =>
        await db.PersonalRecords.Where(x => x.UserId == userId && x.Exercise.KeyLift == lift)
            .MaxAsync(x => (double?)x.MaxWeightKg, cancellationToken) ?? 0;
}
