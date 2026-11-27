namespace IronMindGym.Contracts;

public sealed record AvatarScalePoint
{
    public string Name { get; init; } = string.Empty;
    public double MinStrengthCoefficient { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed record DashboardResearchItem
{
    public int Id { get; init; }
    public DateOnly PublishedAt { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ThesisText { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string SourceUrl { get; init; } = string.Empty;
}

public sealed record DashboardResponse(
    string Username,
    bool NeedsSetup,
    double? WeightKg,
    int TotalWorkouts,
    NutritionTotalsResponse? TodayNutrition,
    List<GoalResponse> Goals,
    List<WorkoutSessionListItem> RecentWorkouts,
    string AvatarName,
    double StrengthCoefficient,
    double CurrentAvatarPositionPercent,
    List<AvatarScalePoint> AvatarScalePoints,
    int PlannedWorkoutsPerWeek,
    int CompletedThisWeek,
    List<DashboardResearchItem> FeedItems);

public sealed record NutritionTotalsResponse(double CaloriesKcal, double ProteinG, double FatG, double CarbsG);
