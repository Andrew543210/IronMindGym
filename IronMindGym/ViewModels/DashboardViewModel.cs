namespace IronMindGym.ViewModels;

public class DashboardViewModel
{
    public string Username { get; set; } = string.Empty;
    public string AvatarName { get; set; } = string.Empty;
    public double StrengthCoefficient { get; set; }
    public int PlannedWorkoutsPerWeek { get; set; }
    public int CompletedThisWeek { get; set; }
    public string HeroImagePath { get; set; } = "/images/photo.jpg";
    public List<DashboardResearchItemViewModel> FeedItems { get; set; } = new();
}

public class DashboardResearchItemViewModel
{
    public int FeedId { get; set; }
    public DateTime PublishedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ThesisText { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}

public class DashboardResearchDetailsViewModel
{
    public DashboardResearchItemViewModel Item { get; set; } = new();
}

