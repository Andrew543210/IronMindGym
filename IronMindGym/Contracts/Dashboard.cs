namespace IronMindGym.Contracts;

public class DashboardSummary
{
    public bool NeedsSetup { get; set; }
    public double? WeightKg { get; set; }
    public int TotalWorkouts { get; set; }
    public IronMindGym.Models.DailyNutrition? TodayNutrition { get; set; }
    public List<IronMindGym.Models.UserGoal> Goals { get; set; } = new();
    public List<IronMindGym.Models.WorkoutSession> RecentWorkouts { get; set; } = new();
    public string Username { get; set; } = string.Empty;
    public string AvatarName { get; set; } = string.Empty;
    public double StrengthCoefficient { get; set; }
    public int PlannedWorkoutsPerWeek { get; set; }
    public int CompletedThisWeek { get; set; }
    public List<AvatarScalePoint> AvatarScalePoints { get; set; } = new();
    public double CurrentAvatarPositionPercent { get; set; }
    public List<DashboardResearchItem> FeedItems { get; set; } = new();
}

public class AvatarScalePoint
{
    public string Name { get; set; } = string.Empty;
    public double MinStrengthCoefficient { get; set; }
    public double PositionPercent { get; set; }
    public string RangeLabel { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}

public class DashboardResearchItem
{
    public int Id { get; set; }
    public DateOnly PublishedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ThesisText { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}
