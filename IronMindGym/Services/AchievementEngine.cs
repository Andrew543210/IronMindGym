using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class AwardProgressItem
{
    public required string Code
    {
        get; init;
    }
    public required string Name
    {
        get; init;
    }
    public required string ConditionDescription
    {
        get; init;
    }
    public bool IsUnlocked
    {
        get; init;
    }
    public bool IsNumeric
    {
        get; init;
    }
    public double CurrentValue
    {
        get; init;
    }
    public double TargetValue
    {
        get; init;
    }
    public double ProgressPercent
    {
        get; init;
    }
}

public static class AchievementEngine
{
    public static void EnsureCatalog(IronMindDbContext db)
    {
        var existingByCode = db.Achievements.ToDictionary(a => a.Code, a => a);

        foreach (var definition in AchievementCatalog.All)
        {
            if (existingByCode.TryGetValue(definition.Code, out var existing))
            {
                existing.Name = definition.Name;
                existing.ConditionDescription = definition.ConditionDescription;
                continue;
            }

            db.Achievements.Add(new Achievement
            {
                Code = definition.Code,
                Name = definition.Name,
                ConditionDescription = definition.ConditionDescription
            });
        }

        db.SaveChanges();
    }

    public static void AssignAchievements(IronMindDbContext db, int userId)
    {
        var unlockedCodes = db.UserAchievements
            .Where(x => x.UserId == userId)
            .Select(x => x.Achievement.Code)
            .ToHashSet();

        var achievementIds = db.Achievements
            .AsNoTracking()
            .ToDictionary(x => x.Code, x => x.Id);

        var metrics = BuildMetrics(db, userId);

        foreach (var definition in AchievementCatalog.All)
        {
            if (unlockedCodes.Contains(definition.Code))
            {
                continue;
            }

            var shouldUnlock = definition.MetricType switch
            {
                AchievementMetricType.NonNumeric => EvaluateNonNumeric(definition.Code, metrics),
                _ => definition.TargetValue.HasValue &&
                     metrics.TryGetValue(definition.MetricType, out var current) &&
                     current >= definition.TargetValue.Value
            };

            if (!shouldUnlock || !achievementIds.TryGetValue(definition.Code, out var achievementId))
            {
                continue;
            }

            db.UserAchievements.Add(new UserAchievement
            {
                UserId = userId,
                AchievementId = achievementId,
                AchievedAt = DateTime.UtcNow
            });
        }

        db.SaveChanges();
    }

    public static List<AwardProgressItem> BuildAwardsProgress(IronMindDbContext db, int userId)
    {
        var unlockedCodes = db.UserAchievements
            .Where(x => x.UserId == userId)
            .Select(x => x.Achievement.Code)
            .ToHashSet();

        var metrics = BuildMetrics(db, userId);

        return AchievementCatalog.All.Select(definition =>
        {
            var unlocked = unlockedCodes.Contains(definition.Code);
            var isNumeric = definition.MetricType != AchievementMetricType.NonNumeric && definition.TargetValue.HasValue;

            var current = 0.0;
            var target = definition.TargetValue ?? 0.0;
            var progress = 0.0;

            if (isNumeric && metrics.TryGetValue(definition.MetricType, out var metricValue))
            {
                current = metricValue;
                if (target > 0)
                {
                    progress = Math.Min(100.0, current / target * 100.0);
                }
            }
            else if (unlocked)
            {
                progress = 100.0;
            }

            return new AwardProgressItem
            {
                Code = definition.Code,
                Name = definition.Name,
                ConditionDescription = definition.ConditionDescription,
                IsUnlocked = unlocked,
                IsNumeric = isNumeric,
                CurrentValue = current,
                TargetValue = target,
                ProgressPercent = progress
            };
        }).ToList();
    }

    private static Dictionary<AchievementMetricType, double> BuildMetrics(IronMindDbContext db, int userId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var workouts = db.WorkoutSessions
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Id, x.SessionDate, x.DurationMinutes })
            .ToList();

        var workoutIds = workouts.Select(x => x.Id).ToList();
        var recentWorkoutIds = workouts
            .Where(x => x.SessionDate >= thirtyDaysAgo)
            .Select(x => x.Id)
            .ToList();

        var performedExerciseIds = db.PerformedExercises
            .Where(x => workoutIds.Contains(x.WorkoutSessionId))
            .Select(x => x.Id)
            .ToList();

        var recentPerformedExerciseIds = db.PerformedExercises
            .Where(x => recentWorkoutIds.Contains(x.WorkoutSessionId))
            .Select(x => x.Id)
            .ToList();

        var totalSets = performedExerciseIds.Count == 0
            ? 0
            : db.Sets.Count(x => performedExerciseIds.Contains(x.PerformedExerciseId));

        var monthlyTonnage = recentPerformedExerciseIds.Count == 0
            ? 0
            : db.Sets
                .Where(x => recentPerformedExerciseIds.Contains(x.PerformedExerciseId))
                .Select(x => (double?)(x.WeightKg * x.Reps))
                .Sum() ?? 0;

        var benchPr = GetBestRecord(db, userId, "Flat Bench Press");
        var squatPr = GetBestRecord(db, userId, "Barbell Back Squat");
        var deadliftPr = GetBestRecord(db, userId, "Deadlift");

        var nutritionLogs = db.DailyNutritions.Count(x => x.UserId == userId);

        var friendsCount = db.Friendships.Count(x => (x.User1Id == userId || x.User2Id == userId) && x.Status == "Accepted");

        var streak = CalculateWorkoutStreak(workouts.Select(x => x.SessionDate));

        var longWorkouts = workouts.Count(x => x.DurationMinutes >= 60);

        return new Dictionary<AchievementMetricType, double>
        {
            [AchievementMetricType.ProfileCompleted] = db.Users.Any(u => u.Id == userId && u.Profile != null && u.Benchmarks != null) && db.AnthropometryLogs.Any(a => a.UserId == userId) ? 1 : 0,
            [AchievementMetricType.ReviewsWritten] = db.WorkoutReviews.Count(r => r.WorkoutSession.UserId == userId && r.Comment != ""),
            [AchievementMetricType.WorkoutsCompleted] = workouts.Count,
            [AchievementMetricType.TotalSets] = totalSets,
            [AchievementMetricType.MonthlyTonnage] = monthlyTonnage,
            [AchievementMetricType.BenchPr] = benchPr,
            [AchievementMetricType.SquatPr] = squatPr,
            [AchievementMetricType.DeadliftPr] = deadliftPr,
            [AchievementMetricType.NutritionLogs] = nutritionLogs,
            [AchievementMetricType.FriendsCount] = friendsCount,
            [AchievementMetricType.WorkoutStreakDays] = streak,
            [AchievementMetricType.LongWorkouts] = longWorkouts
        };
    }

    private static bool EvaluateNonNumeric(string code, Dictionary<AchievementMetricType, double> metrics)
    {
        return code switch
        {
            "REVIEW_FIRST" => metrics[AchievementMetricType.ReviewsWritten] >= 1,
            "PROFILE_SETUP" => metrics[AchievementMetricType.ProfileCompleted] >= 1,
            _ => false
        };
    }

    private static double GetBestRecord(IronMindDbContext db, int userId, string exerciseNamePart)
    {
        return db.PersonalRecords
            .Where(x => x.UserId == userId && x.Exercise.Name == exerciseNamePart)
            .Select(x => (double?)x.MaxWeightKg)
            .Max() ?? 0;
    }

    private static int CalculateWorkoutStreak(IEnumerable<DateTime> dates)
    {
        var uniqueDays = dates
            .Select(x => x.Date)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        if (uniqueDays.Count == 0)
        {
            return 0;
        }

        var streak = 1;
        for (var i = 1; i < uniqueDays.Count; i++)
        {
            if ((uniqueDays[i - 1] - uniqueDays[i]).TotalDays == 1)
            {
                streak++;
                continue;
            }

            break;
        }

        return streak;
    }
}



