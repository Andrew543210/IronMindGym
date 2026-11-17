namespace IronMindGym.ViewModels;

public class StatisticsViewModel
{
    public int WorkoutsCompleted
    {
        get; set;
    }
    public double MonthlyTotalTonnage
    {
        get; set;
    }
    public double AvgWeeklyCalories
    {
        get; set;
    }
    public double ProteinGoalAdherencePercent
    {
        get; set;
    }
    public List<(string ExerciseName, double MaxWeight)> PersonalRecords { get; set; } = new();
    public List<PersonalRecordListItemViewModel> PersonalRecordItems { get; set; } = new();

    public string ExerciseOfWeekName { get; set; } = "N/A";
    public int ExerciseOfWeekSets
    {
        get; set;
    }
    public string MostVolumeExerciseName { get; set; } = "N/A";
    public double MostVolumeTonnage
    {
        get; set;
    }
    public int WorkoutStreak
    {
        get; set;
    }
    public string MaxSetsExerciseName { get; set; } = "N/A";
    public int MaxSetsCount
    {
        get; set;
    }

    public int TotalWorkoutMinutes
    {
        get; set;
    }
    public double AvgWorkoutDurationMinutes
    {
        get; set;
    }
    public int LongestStreak
    {
        get; set;
    }
    public double BestProgressPercent
    {
        get; set;
    }
    public string BestProgressExercise { get; set; } = "N/A";
    public int AvgRestSeconds
    {
        get; set;
    }
    public int TotalAwardsCount
    {
        get; set;
    }

    public double TotalVolume
    {
        get; set;
    }
    public int UniquExercisesCount
    {
        get; set;
    }
    public int ExercisesThisMonth
    {
        get; set;
    }
    public double AvgWeeklyWorkouts
    {
        get; set;
    }
    public int TotalSetsCount
    {
        get; set;
    }
    public double AvgSetsPerWorkout
    {
        get; set;
    }
    public string FavoriteExerciseName { get; set; } = "N/A";
    public int FavoriteExerciseCount
    {
        get; set;
    }
    public double WeeklyProgress
    {
        get; set;
    }
    public double MonthlyProgress
    {
        get; set;
    }
    public string Last7DaysInfo { get; set; } = "N/A";
}

public class PersonalRecordListItemViewModel
{
    public string MuscleGroupName { get; set; } = string.Empty;
    public string MuscleSubGroupName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public double MaxWeightKg
    {
        get; set;
    }
    public DateTime UpdatedAt
    {
        get; set;
    }
}

