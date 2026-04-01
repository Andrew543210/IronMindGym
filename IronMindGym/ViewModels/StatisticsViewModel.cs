namespace IronMindGym.ViewModels;

public class StatisticsViewModel
{
    public int WorkoutsCompleted { get; set; }
    public double MonthlyTotalTonnage { get; set; }
    public double AvgWeeklyCalories { get; set; }
    public double ProteinGoalAdherencePercent { get; set; }
    public List<(string ExerciseName, double MaxWeight)> PersonalRecords { get; set; } = new();
    public List<PersonalRecordListItemViewModel> PersonalRecordItems { get; set; } = new();
}

public class PersonalRecordListItemViewModel
{
    public string MuscleGroupName { get; set; } = string.Empty;
    public string MuscleSubGroupName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public double MaxWeightKg { get; set; }
    public DateTime UpdatedAt { get; set; }
}

