using IronMindGym.Contracts;
using IronMindGym.Data;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

public class DashboardController(IronMindDbContext db, DashboardService dashboard) : ApiController
{
    [HttpGet("api/dashboard")]
    public DashboardResponse Overview()
    {
        var summary = dashboard.Build(UserId);
        var nutrition = summary.TodayNutrition;
        return new DashboardResponse(
            summary.Username,
            summary.NeedsSetup,
            summary.WeightKg,
            summary.TotalWorkouts,
            nutrition is null ? null : new NutritionTotalsResponse(nutrition.CaloriesKcal, nutrition.ProteinG, nutrition.FatG, nutrition.CarbsG),
            summary.Goals.Select(GoalResponse.From).ToList(),
            summary.RecentWorkouts.Select(w => new WorkoutSessionListItem
            {
                Id = w.Id,
                Name = WorkoutService.SessionName(w.SplitDay?.WorkoutName, w.SplitDay?.WorkoutNameUk),
                SessionDate = w.SessionDate,
                DurationMinutes = w.DurationMinutes
            }).ToList(),
            summary.AvatarName,
            summary.StrengthCoefficient,
            summary.CurrentAvatarPositionPercent,
            summary.AvatarScalePoints,
            summary.PlannedWorkoutsPerWeek,
            summary.CompletedThisWeek,
            summary.FeedItems);
    }

    [HttpGet("api/feed/{id:int}")]
    public ActionResult<DashboardResearchItem> FeedItem(int id) =>
        DashboardService.ResearchNotes(db.ResearchItems.Where(r => r.Id == id)).SingleOrDefault() is { } note ? note : NotFound();
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
