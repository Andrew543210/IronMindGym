namespace IronMindGym.Services;

public enum AchievementMetricType
{
    WorkoutsCompleted,
    TotalSets,
    MonthlyTonnage,
    BenchPr,
    SquatPr,
    DeadliftPr,
    NutritionLogs,
    FriendsCount,
    WorkoutStreakDays,
    LongWorkouts,
    ProfileCompleted,
    ReviewsWritten,
    NonNumeric
}

public sealed class AchievementDefinition
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
    public required AchievementMetricType MetricType
    {
        get; init;
    }
    public double? TargetValue
    {
        get; init;
    }
}

public static class AchievementCatalog
{
    public static IReadOnlyList<AchievementDefinition> All
    {
        get;
    } = new List<AchievementDefinition>
    {
        new() { Code = "WORKOUT_001", Name = "First Step", ConditionDescription = "Complete 1 workout", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 1 },
        new() { Code = "WORKOUT_005", Name = "Warm Engine", ConditionDescription = "Complete 5 workouts", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 5 },
        new() { Code = "WORKOUT_010", Name = "Consistency", ConditionDescription = "Complete 10 workouts", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 10 },
        new() { Code = "WORKOUT_020", Name = "Champion Mode", ConditionDescription = "Complete 20 workouts", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 20 },
        new() { Code = "WORKOUT_030", Name = "Iron Routine", ConditionDescription = "Complete 30 workouts", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 30 },
        new() { Code = "WORKOUT_050", Name = "No Excuses", ConditionDescription = "Complete 50 workouts", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 50 },

        new() { Code = "SETS_050", Name = "Set Collector I", ConditionDescription = "Log 50 sets", MetricType = AchievementMetricType.TotalSets, TargetValue = 50 },
        new() { Code = "SETS_100", Name = "Set Collector II", ConditionDescription = "Log 100 sets", MetricType = AchievementMetricType.TotalSets, TargetValue = 100 },
        new() { Code = "SETS_250", Name = "Set Collector III", ConditionDescription = "Log 250 sets", MetricType = AchievementMetricType.TotalSets, TargetValue = 250 },
        new() { Code = "SETS_500", Name = "Set Collector IV", ConditionDescription = "Log 500 sets", MetricType = AchievementMetricType.TotalSets, TargetValue = 500 },
        new() { Code = "SETS_1000", Name = "Set Collector V", ConditionDescription = "Log 1000 sets", MetricType = AchievementMetricType.TotalSets, TargetValue = 1000 },

        new() { Code = "TONNAGE_5K", Name = "Volume Builder I", ConditionDescription = "Lift 5,000 kg total volume in 30 days", MetricType = AchievementMetricType.MonthlyTonnage, TargetValue = 5000 },
        new() { Code = "TONNAGE_10K", Name = "Volume Builder II", ConditionDescription = "Lift 10,000 kg total volume in 30 days", MetricType = AchievementMetricType.MonthlyTonnage, TargetValue = 10000 },
        new() { Code = "TONNAGE_20K", Name = "Volume Builder III", ConditionDescription = "Lift 20,000 kg total volume in 30 days", MetricType = AchievementMetricType.MonthlyTonnage, TargetValue = 20000 },
        new() { Code = "TONNAGE_40K", Name = "Volume Builder IV", ConditionDescription = "Lift 40,000 kg total volume in 30 days", MetricType = AchievementMetricType.MonthlyTonnage, TargetValue = 40000 },

        new() { Code = "BENCH_40", Name = "Bench Apprentice", ConditionDescription = "Bench press 40 kg", MetricType = AchievementMetricType.BenchPr, TargetValue = 40 },
        new() { Code = "BENCH_60", Name = "Bench Adept", ConditionDescription = "Bench press 60 kg", MetricType = AchievementMetricType.BenchPr, TargetValue = 60 },
        new() { Code = "BENCH_80", Name = "Bench Specialist", ConditionDescription = "Bench press 80 kg", MetricType = AchievementMetricType.BenchPr, TargetValue = 80 },
        new() { Code = "BENCH_100", Name = "Bench Master", ConditionDescription = "Bench press 100 kg", MetricType = AchievementMetricType.BenchPr, TargetValue = 100 },
        new() { Code = "BENCH_120", Name = "Bench Titan", ConditionDescription = "Bench press 120 kg", MetricType = AchievementMetricType.BenchPr, TargetValue = 120 },

        new() { Code = "SQUAT_60", Name = "Squat Apprentice", ConditionDescription = "Squat 60 kg", MetricType = AchievementMetricType.SquatPr, TargetValue = 60 },
        new() { Code = "SQUAT_100", Name = "Squat Adept", ConditionDescription = "Squat 100 kg", MetricType = AchievementMetricType.SquatPr, TargetValue = 100 },
        new() { Code = "SQUAT_140", Name = "Squat Specialist", ConditionDescription = "Squat 140 kg", MetricType = AchievementMetricType.SquatPr, TargetValue = 140 },
        new() { Code = "SQUAT_180", Name = "Squat Master", ConditionDescription = "Squat 180 kg", MetricType = AchievementMetricType.SquatPr, TargetValue = 180 },
        new() { Code = "SQUAT_220", Name = "Squat Titan", ConditionDescription = "Squat 220 kg", MetricType = AchievementMetricType.SquatPr, TargetValue = 220 },

        new() { Code = "DEADLIFT_80", Name = "Deadlift Apprentice", ConditionDescription = "Deadlift 80 kg", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 80 },
        new() { Code = "DEADLIFT_120", Name = "Deadlift Adept", ConditionDescription = "Deadlift 120 kg", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 120 },
        new() { Code = "DEADLIFT_180", Name = "Deadlift Specialist", ConditionDescription = "Deadlift 180 kg", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 180 },
        new() { Code = "DEADLIFT_220", Name = "Deadlift Master", ConditionDescription = "Deadlift 220 kg", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 220 },
        new() { Code = "DEADLIFT_260", Name = "Deadlift Titan", ConditionDescription = "Deadlift 260 kg", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 260 },

        new() { Code = "NUTRITION_007", Name = "Food Tracker I", ConditionDescription = "Log nutrition for 7 days", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 7 },
        new() { Code = "NUTRITION_014", Name = "Food Tracker II", ConditionDescription = "Log nutrition for 14 days", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 14 },
        new() { Code = "NUTRITION_030", Name = "Food Tracker III", ConditionDescription = "Log nutrition for 30 days", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 30 },
        new() { Code = "NUTRITION_060", Name = "Food Tracker IV", ConditionDescription = "Log nutrition for 60 days", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 60 },
        new() { Code = "NUTRITION_120", Name = "Food Tracker V", ConditionDescription = "Log nutrition for 120 days", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 120 },

        new() { Code = "FRIENDS_001", Name = "Open Circle", ConditionDescription = "Add 1 accepted friend", MetricType = AchievementMetricType.FriendsCount, TargetValue = 1 },
        new() { Code = "FRIENDS_003", Name = "Team Spirit", ConditionDescription = "Add 3 accepted friends", MetricType = AchievementMetricType.FriendsCount, TargetValue = 3 },
        new() { Code = "FRIENDS_005", Name = "Community Core", ConditionDescription = "Add 5 accepted friends", MetricType = AchievementMetricType.FriendsCount, TargetValue = 5 },

        new() { Code = "STREAK_003", Name = "Mini Streak", ConditionDescription = "Train 3 days in a row", MetricType = AchievementMetricType.WorkoutStreakDays, TargetValue = 3 },
        new() { Code = "STREAK_007", Name = "Week Warrior", ConditionDescription = "Train 7 days in a row", MetricType = AchievementMetricType.WorkoutStreakDays, TargetValue = 7 },
        new() { Code = "STREAK_014", Name = "Two-Week Engine", ConditionDescription = "Train 14 days in a row", MetricType = AchievementMetricType.WorkoutStreakDays, TargetValue = 14 },
        new() { Code = "STREAK_030", Name = "Unbreakable", ConditionDescription = "Train 30 days in a row", MetricType = AchievementMetricType.WorkoutStreakDays, TargetValue = 30 },

        new() { Code = "LONG_005", Name = "Endurance Session I", ConditionDescription = "Complete 5 workouts longer than 60 min", MetricType = AchievementMetricType.LongWorkouts, TargetValue = 5 },
        new() { Code = "LONG_020", Name = "Endurance Session II", ConditionDescription = "Complete 20 workouts longer than 60 min", MetricType = AchievementMetricType.LongWorkouts, TargetValue = 20 },

        new() { Code = "REVIEW_FIRST", Name = "Mindful Athlete", ConditionDescription = "Leave your first workout review", MetricType = AchievementMetricType.NonNumeric },
        new() { Code = "PROFILE_SETUP", Name = "Fully Set", ConditionDescription = "Complete profile, anthropometry and setup", MetricType = AchievementMetricType.NonNumeric }
    };
}

