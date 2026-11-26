namespace IronMindGym.Contracts;

public sealed record TrainingStatistics
{
    public int WorkoutsCompleted { get; init; }
    public int TotalWorkoutMinutes { get; init; }
    public double AvgWorkoutDurationMinutes { get; init; }
    public int LongestStreak { get; init; }
    public double AvgWeeklyWorkouts { get; init; }

    public int TotalSetsCount { get; init; }
    public double AvgSetsPerWorkout { get; init; }
    public int AvgRestSeconds { get; init; }

    public double TotalVolume { get; init; }
    public double MonthlyTotalTonnage { get; init; }
    public double WeeklyProgress { get; init; }
    public int Last7DaysWorkouts { get; init; }
    public int Last7DaysMinutes { get; init; }

    public int UniqueExercisesCount { get; init; }
    public int ExercisesThisMonth { get; init; }
    public string? FavoriteExerciseName { get; init; }
    public int FavoriteExerciseCount { get; init; }

    public double BestProgressPercent { get; init; }
    public string? BestProgressExercise { get; init; }
    public int TotalAwardsCount { get; init; }

    public List<PersonalRecordItem> RecentRecords { get; init; } = [];
}

public sealed record PersonalRecordItem
{
    public string MuscleGroupName { get; init; } = string.Empty;
    public string MuscleSubGroupName { get; init; } = string.Empty;
    public string ExerciseName { get; init; } = string.Empty;
    public double MaxWeightKg { get; init; }
    public DateTime AchievedAt { get; init; }
}

public sealed record AwardProgressItem
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
