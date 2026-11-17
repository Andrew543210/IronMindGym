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

    // New interesting stats
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

    // New statistics
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

    // Additional useful statistics
    public double TotalVolume
    {
        get; set;
    } // kg × reps for all exercises
    public int UniquExercisesCount
    {
        get; set;
    } // Number of different exercises
    public int ExercisesThisMonth
    {
        get; set;
    } // Different exercises in last 30 days
    public double AvgWeeklyWorkouts
    {
        get; set;
    } // Workouts per week
    public int TotalSetsCount
    {
        get; set;
    } // Total sets performed
    public double AvgSetsPerWorkout
    {
        get; set;
    } // Average sets per session
    public string FavoriteExerciseName { get; set; } = "N/A"; // Most frequent exercise
    public int FavoriteExerciseCount
    {
        get; set;
    } // Times performed
    public double WeeklyProgress
    {
        get; set;
    } // Progress in last 7 days
    public double MonthlyProgress
    {
        get; set;
    } // Progress in last 30 days
    public string Last7DaysInfo { get; set; } = "N/A"; // Last week detail
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

