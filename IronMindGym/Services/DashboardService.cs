using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class DashboardService(IronMindDbContext db, AthleteCalendars calendars, StrengthService strength)
{
    public DashboardResponse Build(int currentUserId)
    {
        var username = db.Users.Where(u => u.Id == currentUserId).Select(u => u.Username).Single();
        var calendar = calendars.For(currentUserId);
        var today = calendar.Today;
        var rank = strength.Rank(currentUserId);
        var rankName = Localized.Pick(rank.Rank.Name, rank.Rank.NameUk);
        var scale = rank.Ladder.Select((step, index) => new AvatarScalePoint
        {
            Name = Localized.Pick(step.Name, step.NameUk),
            MinStrengthCoefficient = step.MinStrengthCoefficient,
            PositionPercent = rank.Ladder.Count == 1 ? 0 : index * 100.0 / (rank.Ladder.Count - 1),
            RangeLabel = StrengthRanks.RangeLabel(rank.Ladder, index),
            IsCurrent = step.Name == rank.Rank.Name
        }).ToList();

        var nutrition = db.DailyNutritions.AsNoTracking().FirstOrDefault(n => n.UserId == currentUserId && n.Date == today);
        return new DashboardResponse(
            username,
            !AthleteSetup.IsComplete(db, currentUserId),
            rank.BodyweightKg,
            db.WorkoutSessions.Count(w => w.UserId == currentUserId),
            nutrition is null ? null : new NutritionTotalsResponse(nutrition.CaloriesKcal, nutrition.ProteinG, nutrition.FatG, nutrition.CarbsG),
            db.UserGoals.AsNoTracking().Where(g => g.UserId == currentUserId && !g.IsCompleted).OrderBy(g => g.Deadline).Take(3)
                .AsEnumerable().Select(GoalResponse.From).ToList(),
            db.WorkoutSessions.AsNoTracking().Where(w => w.UserId == currentUserId).OrderByDescending(w => w.SessionDate).Take(3)
                .Select(w => new WorkoutSessionListItem
                {
                    Id = w.Id,
                    Name = WorkoutService.SessionName(w.SplitDay!.WorkoutName, w.SplitDay.WorkoutNameUk),
                    SessionDate = w.SessionDate,
                    DurationMinutes = w.DurationMinutes
                }).ToList(),
            rankName,
            rank.Coefficient,
            StrengthRanks.ScalePosition(rank.Coefficient, rank.Ladder),
            scale,
            db.WorkoutPlans.WorkoutsPerWeek(currentUserId),
            db.WorkoutSessions.WorkoutsThisWeek(currentUserId, calendar),
            ResearchNotes(db.ResearchItems.OrderByDescending(r => r.PublishedAt).ThenBy(r => r.Id).Take(4)));
    }

    /// <summary>Research notes in the request language.</summary>
    public static List<DashboardResearchItem> ResearchNotes(IQueryable<ResearchItem> items) => items.AsNoTracking()
        .Select(r => new DashboardResearchItem
        {
            Id = r.Id,
            PublishedAt = r.PublishedAt,
            Title = Localized.Pick(r.Title, r.TitleUk),
            ThesisText = Localized.Pick(r.ThesisText, r.ThesisTextUk),
            Source = r.SourceName,
            SourceUrl = r.SourceUrl
        })
        .ToList();
}
