namespace IronMindGym.ViewModels;

public class DashboardViewModel
{
    public bool NeedsSetup
    {
        get; set;
    }
    public double? WeightKg
    {
        get; set;
    }
    public int TotalWorkouts
    {
        get; set;
    }
    public IronMindGym.Models.DailyNutrition? TodayNutrition
    {
        get; set;
    }
    public List<IronMindGym.Models.UserGoal> Goals { get; set; } = new();
    public List<IronMindGym.Models.WorkoutSession> RecentWorkouts { get; set; } = new();
    public string Username { get; set; } = string.Empty;
    public string AvatarName { get; set; } = string.Empty;
    public double StrengthCoefficient
    {
        get; set;
    }
    public int PlannedWorkoutsPerWeek
    {
        get; set;
    }
    public int CompletedThisWeek
    {
        get; set;
    }
    public string HeroImagePath { get; set; } = "/images/photo.jpg";
    public List<AvatarScalePointViewModel> AvatarScalePoints { get; set; } = new();
    public double CurrentAvatarPositionPercent
    {
        get; set;
    }
    public List<DashboardResearchItemViewModel> FeedItems { get; set; } = new();
}

public class AvatarScalePointViewModel
{
    public string Name { get; set; } = string.Empty;
    public double MinStrengthCoefficient
    {
        get; set;
    }
    public double PositionPercent
    {
        get; set;
    }
    public string RangeLabel { get; set; } = string.Empty;
    public bool IsCurrent
    {
        get; set;
    }
}

public class DashboardResearchItemViewModel
{
    public int FeedId
    {
        get; set;
    }
    public DateTime PublishedAt
    {
        get; set;
    }
    public string Title { get; set; } = string.Empty;
    public string ThesisText { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}

public class DashboardResearchDetailsViewModel
{
    public DashboardResearchItemViewModel Item { get; set; } = new();
}

