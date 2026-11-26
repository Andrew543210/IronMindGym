using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class AwardProgressItem
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string ConditionDescription { get; init; }
    public bool IsUnlocked { get; init; }
    public bool IsNumeric { get; init; }
    public double CurrentValue { get; init; }
    public double TargetValue { get; init; }
    public double ProgressPercent { get; init; }
}

/// <summary>
/// Every award in <see cref="AchievementCatalog"/> is earned once its metric reaches the target. The metrics are
/// counted from the athlete's saved data, and an earned award stays earned even if the metric later drops.
/// </summary>
public sealed class AchievementService(IronMindDbContext db)
{
    /// <summary>Adds the awards whose target the athlete has reached since the last check.</summary>
    public void AwardEarned(int userId)
    {
        var earned = EarnedCodes(userId);
        var achievementIds = db.Achievements.AsNoTracking().ToDictionary(a => a.Code, a => a.Id);
        var metrics = Metrics(userId);
        foreach (var award in AchievementCatalog.All)
        {
            if (!earned.Contains(award.Code) && metrics[award.MetricType] >= award.TargetValue
                && achievementIds.TryGetValue(award.Code, out var achievementId))
            {
                db.UserAchievements.Add(new UserAchievement { UserId = userId, AchievementId = achievementId, AchievedAt = DateTime.UtcNow });
            }
        }
        db.SaveChanges();
    }

    public List<AwardProgressItem> Progress(int userId)
    {
        var earned = EarnedCodes(userId);
        var metrics = Metrics(userId);
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

    private HashSet<string> EarnedCodes(int userId) =>
        db.UserAchievements.Where(x => x.UserId == userId).Select(x => x.Achievement.Code).ToHashSet();

    private Dictionary<AchievementMetricType, double> Metrics(int userId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var workouts = db.WorkoutSessions.Where(x => x.UserId == userId).Select(x => new { x.SessionDate, x.DurationMinutes }).ToList();
        var sets = db.Sets.Where(x => x.PerformedExercise.WorkoutSession.UserId == userId);
        var calendar = AthleteCalendar.For(db, userId);

        return new Dictionary<AchievementMetricType, double>
        {
            [AchievementMetricType.ProfileCompleted] = AthleteSetup.IsComplete(db, userId) ? 1 : 0,
            [AchievementMetricType.ReviewsWritten] = db.WorkoutSessions.Count(s => s.UserId == userId && s.Notes != null),
            [AchievementMetricType.WorkoutsCompleted] = workouts.Count,
            [AchievementMetricType.TotalSets] = sets.Count(),
            [AchievementMetricType.MonthlyTonnage] = sets
                .Where(x => x.PerformedExercise.WorkoutSession.SessionDate >= thirtyDaysAgo)
                .Sum(x => (double?)(x.WeightKg * x.Reps)) ?? 0,
            [AchievementMetricType.BenchPr] = BestRecord(userId, KeyLift.BenchPress),
            [AchievementMetricType.SquatPr] = BestRecord(userId, KeyLift.Squat),
            [AchievementMetricType.DeadliftPr] = BestRecord(userId, KeyLift.Deadlift),
            [AchievementMetricType.NutritionLogs] = db.DailyNutritions.Count(x => x.UserId == userId),
            [AchievementMetricType.FriendsCount] = db.Friendships.Count(x =>
                (x.User1Id == userId || x.User2Id == userId) && x.Status == FriendshipStatus.Accepted),
            [AchievementMetricType.WorkoutStreakDays] = AthleteCalendar.LongestStreak(workouts.Select(x => calendar.DayOf(x.SessionDate))),
            [AchievementMetricType.LongWorkouts] = workouts.Count(x => x.DurationMinutes >= AchievementCatalog.LongWorkoutMinutes)
        };
    }

    private double BestRecord(int userId, KeyLift lift) =>
        db.PersonalRecords.Where(x => x.UserId == userId && x.Exercise.KeyLift == lift).Select(x => (double?)x.MaxWeightKg).Max() ?? 0;
}
