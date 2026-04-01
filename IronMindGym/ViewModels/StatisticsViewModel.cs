namespace IronMindGym.ViewModels;

public class StatisticsViewModel
{
    public int WorkoutsCompleted { get; set; }
    public double MonthlyTotalTonnage { get; set; }
    public double AvgWeeklyCalories { get; set; }
    public double ProteinGoalAdherencePercent { get; set; }
    public List<(string ExerciseName, double MaxWeight)> PersonalRecords { get; set; } = new();
}

