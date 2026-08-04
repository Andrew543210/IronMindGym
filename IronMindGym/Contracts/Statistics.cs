namespace IronMindGym.Contracts;

public class TrainingStatistics
{
    public int WorkoutsCompleted { get; set; }
    public int TotalWorkoutMinutes { get; set; }
    public double AvgWorkoutDurationMinutes { get; set; }
    public int LongestStreak { get; set; }
    public double AvgWeeklyWorkouts { get; set; }

    public int TotalSetsCount { get; set; }
    public double AvgSetsPerWorkout { get; set; }
    public int AvgRestSeconds { get; set; }

    public double TotalVolume { get; set; }
    public double MonthlyTotalTonnage { get; set; }
    public double WeeklyProgress { get; set; }
    public int Last7DaysWorkouts { get; set; }
    public int Last7DaysMinutes { get; set; }

    public int UniqueExercisesCount { get; set; }
    public int ExercisesThisMonth { get; set; }
    public string? FavoriteExerciseName { get; set; }
    public int FavoriteExerciseCount { get; set; }

    public double BestProgressPercent { get; set; }
    public string? BestProgressExercise { get; set; }
    public int TotalAwardsCount { get; set; }

    public List<PersonalRecordItem> RecentRecords { get; set; } = [];
}

public class PersonalRecordItem
{
    public string MuscleGroupName { get; set; } = string.Empty;
    public string MuscleSubGroupName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public double MaxWeightKg { get; set; }
    public DateTime UpdatedAt { get; set; }
}
