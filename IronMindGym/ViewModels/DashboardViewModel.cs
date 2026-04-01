using IronMindGym.Models;

namespace IronMindGym.ViewModels;

public class DashboardViewModel
{
    public string Username { get; set; } = string.Empty;
    public string AvatarName { get; set; } = string.Empty;
    public double StrengthCoefficient { get; set; }
    public int PlannedWorkoutsPerWeek { get; set; }
    public int CompletedThisWeek { get; set; }
    public List<ActivityFeed> FeedItems { get; set; } = new();
}

