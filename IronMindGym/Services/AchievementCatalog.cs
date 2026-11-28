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
    ReviewsWritten
}

public sealed class AchievementDefinition
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string ConditionDescription { get; init; }
    public required string NameUk { get; init; }
    public required string ConditionDescriptionUk { get; init; }
    public required AchievementMetricType MetricType { get; init; }
    public required double TargetValue { get; init; }

    public bool ShowsProgress { get; init; } = true;
}

public static class AchievementCatalog
{
    public const int LongWorkoutMinutes = 60;

    public static IReadOnlyList<AchievementDefinition> All
    {
        get;
    } = new List<AchievementDefinition>
    {
        new() { Code = "WORKOUT_001", Name = "First Step", NameUk = "Перший крок", ConditionDescription = "Complete 1 workout", ConditionDescriptionUk = "Завершіть 1 тренування", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 1 },
        new() { Code = "WORKOUT_005", Name = "Warm Engine", NameUk = "Розігрітий мотор", ConditionDescription = "Complete 5 workouts", ConditionDescriptionUk = "Завершіть 5 тренувань", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 5 },
        new() { Code = "WORKOUT_010", Name = "Consistency", NameUk = "Сталість", ConditionDescription = "Complete 10 workouts", ConditionDescriptionUk = "Завершіть 10 тренувань", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 10 },
        new() { Code = "WORKOUT_020", Name = "Champion Mode", NameUk = "Режим чемпіона", ConditionDescription = "Complete 20 workouts", ConditionDescriptionUk = "Завершіть 20 тренувань", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 20 },
        new() { Code = "WORKOUT_030", Name = "Iron Routine", NameUk = "Залізний розпорядок", ConditionDescription = "Complete 30 workouts", ConditionDescriptionUk = "Завершіть 30 тренувань", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 30 },
        new() { Code = "WORKOUT_050", Name = "No Excuses", NameUk = "Без відмовок", ConditionDescription = "Complete 50 workouts", ConditionDescriptionUk = "Завершіть 50 тренувань", MetricType = AchievementMetricType.WorkoutsCompleted, TargetValue = 50 },

        new() { Code = "SETS_050", Name = "Set Collector I", NameUk = "Збирач підходів I", ConditionDescription = "Log 50 sets", ConditionDescriptionUk = "Запишіть 50 підходів", MetricType = AchievementMetricType.TotalSets, TargetValue = 50 },
        new() { Code = "SETS_100", Name = "Set Collector II", NameUk = "Збирач підходів II", ConditionDescription = "Log 100 sets", ConditionDescriptionUk = "Запишіть 100 підходів", MetricType = AchievementMetricType.TotalSets, TargetValue = 100 },
        new() { Code = "SETS_250", Name = "Set Collector III", NameUk = "Збирач підходів III", ConditionDescription = "Log 250 sets", ConditionDescriptionUk = "Запишіть 250 підходів", MetricType = AchievementMetricType.TotalSets, TargetValue = 250 },
        new() { Code = "SETS_500", Name = "Set Collector IV", NameUk = "Збирач підходів IV", ConditionDescription = "Log 500 sets", ConditionDescriptionUk = "Запишіть 500 підходів", MetricType = AchievementMetricType.TotalSets, TargetValue = 500 },
        new() { Code = "SETS_1000", Name = "Set Collector V", NameUk = "Збирач підходів V", ConditionDescription = "Log 1000 sets", ConditionDescriptionUk = "Запишіть 1000 підходів", MetricType = AchievementMetricType.TotalSets, TargetValue = 1000 },

        new() { Code = "TONNAGE_5K", Name = "Volume Builder I", NameUk = "Майстер тоннажу I", ConditionDescription = "Lift 5,000 kg total volume in 30 days", ConditionDescriptionUk = "Наберіть 5000 кг тоннажу за 30 днів", MetricType = AchievementMetricType.MonthlyTonnage, TargetValue = 5000 },
        new() { Code = "TONNAGE_10K", Name = "Volume Builder II", NameUk = "Майстер тоннажу II", ConditionDescription = "Lift 10,000 kg total volume in 30 days", ConditionDescriptionUk = "Наберіть 10 000 кг тоннажу за 30 днів", MetricType = AchievementMetricType.MonthlyTonnage, TargetValue = 10000 },
        new() { Code = "TONNAGE_20K", Name = "Volume Builder III", NameUk = "Майстер тоннажу III", ConditionDescription = "Lift 20,000 kg total volume in 30 days", ConditionDescriptionUk = "Наберіть 20 000 кг тоннажу за 30 днів", MetricType = AchievementMetricType.MonthlyTonnage, TargetValue = 20000 },
        new() { Code = "TONNAGE_40K", Name = "Volume Builder IV", NameUk = "Майстер тоннажу IV", ConditionDescription = "Lift 40,000 kg total volume in 30 days", ConditionDescriptionUk = "Наберіть 40 000 кг тоннажу за 30 днів", MetricType = AchievementMetricType.MonthlyTonnage, TargetValue = 40000 },

        new() { Code = "BENCH_40", Name = "Bench Apprentice", NameUk = "Учень жиму", ConditionDescription = "Bench press 40 kg", ConditionDescriptionUk = "Жим лежачи 40 кг", MetricType = AchievementMetricType.BenchPr, TargetValue = 40 },
        new() { Code = "BENCH_60", Name = "Bench Adept", NameUk = "Адепт жиму", ConditionDescription = "Bench press 60 kg", ConditionDescriptionUk = "Жим лежачи 60 кг", MetricType = AchievementMetricType.BenchPr, TargetValue = 60 },
        new() { Code = "BENCH_80", Name = "Bench Specialist", NameUk = "Спеціаліст жиму", ConditionDescription = "Bench press 80 kg", ConditionDescriptionUk = "Жим лежачи 80 кг", MetricType = AchievementMetricType.BenchPr, TargetValue = 80 },
        new() { Code = "BENCH_100", Name = "Bench Master", NameUk = "Майстер жиму", ConditionDescription = "Bench press 100 kg", ConditionDescriptionUk = "Жим лежачи 100 кг", MetricType = AchievementMetricType.BenchPr, TargetValue = 100 },
        new() { Code = "BENCH_120", Name = "Bench Titan", NameUk = "Титан жиму", ConditionDescription = "Bench press 120 kg", ConditionDescriptionUk = "Жим лежачи 120 кг", MetricType = AchievementMetricType.BenchPr, TargetValue = 120 },

        new() { Code = "SQUAT_60", Name = "Squat Apprentice", NameUk = "Учень присідань", ConditionDescription = "Squat 60 kg", ConditionDescriptionUk = "Присідання 60 кг", MetricType = AchievementMetricType.SquatPr, TargetValue = 60 },
        new() { Code = "SQUAT_100", Name = "Squat Adept", NameUk = "Адепт присідань", ConditionDescription = "Squat 100 kg", ConditionDescriptionUk = "Присідання 100 кг", MetricType = AchievementMetricType.SquatPr, TargetValue = 100 },
        new() { Code = "SQUAT_140", Name = "Squat Specialist", NameUk = "Спеціаліст присідань", ConditionDescription = "Squat 140 kg", ConditionDescriptionUk = "Присідання 140 кг", MetricType = AchievementMetricType.SquatPr, TargetValue = 140 },
        new() { Code = "SQUAT_180", Name = "Squat Master", NameUk = "Майстер присідань", ConditionDescription = "Squat 180 kg", ConditionDescriptionUk = "Присідання 180 кг", MetricType = AchievementMetricType.SquatPr, TargetValue = 180 },
        new() { Code = "SQUAT_220", Name = "Squat Titan", NameUk = "Титан присідань", ConditionDescription = "Squat 220 kg", ConditionDescriptionUk = "Присідання 220 кг", MetricType = AchievementMetricType.SquatPr, TargetValue = 220 },

        new() { Code = "DEADLIFT_80", Name = "Deadlift Apprentice", NameUk = "Учень тяги", ConditionDescription = "Deadlift 80 kg", ConditionDescriptionUk = "Станова тяга 80 кг", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 80 },
        new() { Code = "DEADLIFT_120", Name = "Deadlift Adept", NameUk = "Адепт тяги", ConditionDescription = "Deadlift 120 kg", ConditionDescriptionUk = "Станова тяга 120 кг", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 120 },
        new() { Code = "DEADLIFT_180", Name = "Deadlift Specialist", NameUk = "Спеціаліст тяги", ConditionDescription = "Deadlift 180 kg", ConditionDescriptionUk = "Станова тяга 180 кг", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 180 },
        new() { Code = "DEADLIFT_220", Name = "Deadlift Master", NameUk = "Майстер тяги", ConditionDescription = "Deadlift 220 kg", ConditionDescriptionUk = "Станова тяга 220 кг", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 220 },
        new() { Code = "DEADLIFT_260", Name = "Deadlift Titan", NameUk = "Титан тяги", ConditionDescription = "Deadlift 260 kg", ConditionDescriptionUk = "Станова тяга 260 кг", MetricType = AchievementMetricType.DeadliftPr, TargetValue = 260 },

        new() { Code = "NUTRITION_007", Name = "Food Tracker I", NameUk = "Облік харчування I", ConditionDescription = "Log nutrition for 7 days", ConditionDescriptionUk = "Записуйте харчування 7 днів", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 7 },
        new() { Code = "NUTRITION_014", Name = "Food Tracker II", NameUk = "Облік харчування II", ConditionDescription = "Log nutrition for 14 days", ConditionDescriptionUk = "Записуйте харчування 14 днів", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 14 },
        new() { Code = "NUTRITION_030", Name = "Food Tracker III", NameUk = "Облік харчування III", ConditionDescription = "Log nutrition for 30 days", ConditionDescriptionUk = "Записуйте харчування 30 днів", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 30 },
        new() { Code = "NUTRITION_060", Name = "Food Tracker IV", NameUk = "Облік харчування IV", ConditionDescription = "Log nutrition for 60 days", ConditionDescriptionUk = "Записуйте харчування 60 днів", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 60 },
        new() { Code = "NUTRITION_120", Name = "Food Tracker V", NameUk = "Облік харчування V", ConditionDescription = "Log nutrition for 120 days", ConditionDescriptionUk = "Записуйте харчування 120 днів", MetricType = AchievementMetricType.NutritionLogs, TargetValue = 120 },

        new() { Code = "FRIENDS_001", Name = "Open Circle", NameUk = "Відкрите коло", ConditionDescription = "Add 1 accepted friend", ConditionDescriptionUk = "Додайте в друзі 1 людину", MetricType = AchievementMetricType.FriendsCount, TargetValue = 1 },
        new() { Code = "FRIENDS_003", Name = "Team Spirit", NameUk = "Командний дух", ConditionDescription = "Add 3 accepted friends", ConditionDescriptionUk = "Додайте в друзі 3 людей", MetricType = AchievementMetricType.FriendsCount, TargetValue = 3 },
        new() { Code = "FRIENDS_005", Name = "Community Core", NameUk = "Серце спільноти", ConditionDescription = "Add 5 accepted friends", ConditionDescriptionUk = "Додайте в друзі 5 людей", MetricType = AchievementMetricType.FriendsCount, TargetValue = 5 },

        new() { Code = "STREAK_003", Name = "Mini Streak", NameUk = "Міні-серія", ConditionDescription = "Train 3 days in a row", ConditionDescriptionUk = "Тренуйтеся 3 дні поспіль", MetricType = AchievementMetricType.WorkoutStreakDays, TargetValue = 3 },
        new() { Code = "STREAK_007", Name = "Week Warrior", NameUk = "Воїн тижня", ConditionDescription = "Train 7 days in a row", ConditionDescriptionUk = "Тренуйтеся 7 днів поспіль", MetricType = AchievementMetricType.WorkoutStreakDays, TargetValue = 7 },
        new() { Code = "STREAK_014", Name = "Two-Week Engine", NameUk = "Двотижневий мотор", ConditionDescription = "Train 14 days in a row", ConditionDescriptionUk = "Тренуйтеся 14 днів поспіль", MetricType = AchievementMetricType.WorkoutStreakDays, TargetValue = 14 },
        new() { Code = "STREAK_030", Name = "Unbreakable", NameUk = "Незламність", ConditionDescription = "Train 30 days in a row", ConditionDescriptionUk = "Тренуйтеся 30 днів поспіль", MetricType = AchievementMetricType.WorkoutStreakDays, TargetValue = 30 },

        new() { Code = "LONG_005", Name = "Endurance Session I", NameUk = "Довге тренування I", ConditionDescription = "Complete 5 workouts of 60 min or more", ConditionDescriptionUk = "Завершіть 5 тренувань від 60 хв", MetricType = AchievementMetricType.LongWorkouts, TargetValue = 5 },
        new() { Code = "LONG_020", Name = "Endurance Session II", NameUk = "Довге тренування II", ConditionDescription = "Complete 20 workouts of 60 min or more", ConditionDescriptionUk = "Завершіть 20 тренувань від 60 хв", MetricType = AchievementMetricType.LongWorkouts, TargetValue = 20 },

        new() { Code = "REVIEW_FIRST", Name = "Mindful Athlete", NameUk = "Усвідомлений підхід", ConditionDescription = "Leave your first workout review", ConditionDescriptionUk = "Залиште перший відгук про тренування", MetricType = AchievementMetricType.ReviewsWritten, TargetValue = 1, ShowsProgress = false },
        new() { Code = "PROFILE_SETUP", Name = "Fully Set", NameUk = "Повна готовність", ConditionDescription = "Complete profile, anthropometry and setup", ConditionDescriptionUk = "Заповніть профіль, антропометрію та початкове налаштування", MetricType = AchievementMetricType.ProfileCompleted, TargetValue = 1, ShowsProgress = false }
    };
}
