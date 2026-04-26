using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                Name = WorkoutService.SessionName(w.Name),
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
    public ActionResult<DashboardResearchItem> FeedItem(int id)
    {
        var item = db.ActivityFeeds.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.PublishedAt,
                Title = Localized.Pick(x.ResearchItem.Title, x.ResearchItem.TitleUk),
                ThesisText = Localized.Pick(x.ResearchItem.ThesisText, x.ResearchItem.ThesisTextUk),
                x.ResearchItem.Source
            })
            .FirstOrDefault();
        if (item is null)
            return NotFound();
        return new DashboardResearchItem
        {
            FeedId = item.Id,
            PublishedAt = item.PublishedAt,
            Title = item.Title,
            ThesisText = item.ThesisText,
            Source = DashboardService.BuildSourceName(item.Source),
            SourceUrl = DashboardService.BuildSourceUrl(item.Source)
        };
    }
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
