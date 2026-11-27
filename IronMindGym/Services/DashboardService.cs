using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class DashboardService(IronMindDbContext db, AthleteCalendars calendars, StrengthService strength)
{
    public async Task<DashboardResponse> BuildAsync(int userId, CancellationToken cancellationToken)
    {
        var username = await db.Users.Where(u => u.Id == userId).Select(u => u.Username).SingleAsync(cancellationToken);
        var calendar = await calendars.ForAsync(userId, cancellationToken);
        var today = calendar.Today;
        var rank = await strength.RankAsync(userId, cancellationToken);
        var scale = rank.Ladder.Select(step => new AvatarScalePoint
        {
            Name = Localized.Pick(step.Name, step.NameUk),
            MinStrengthCoefficient = step.MinStrengthCoefficient,
            IsCurrent = step.Name == rank.Rank.Name
        }).ToList();

        var nutrition = await db.DailyNutritions.AsNoTracking().FirstOrDefaultAsync(n => n.UserId == userId && n.Date == today, cancellationToken);
        return new DashboardResponse(
            username,
            !await AthleteSetup.IsCompleteAsync(db, userId, cancellationToken),
            rank.BodyweightKg,
            await db.WorkoutSessions.CountAsync(w => w.UserId == userId, cancellationToken),
            nutrition is null ? null : new NutritionTotalsResponse(nutrition.CaloriesKcal, nutrition.ProteinG, nutrition.FatG, nutrition.CarbsG),
            await db.UserGoals.AsNoTracking().Where(g => g.UserId == userId && !g.IsCompleted).OrderBy(g => g.Deadline).Take(3)
                .Select(g => new GoalResponse(g.Id, g.Title, g.Deadline, g.IsCompleted)).ToListAsync(cancellationToken),
            await db.WorkoutSessions.AsNoTracking().Where(w => w.UserId == userId).OrderByDescending(w => w.SessionDate).Take(3)
                .Select(WorkoutService.ListItem).ToListAsync(cancellationToken),
            Localized.Pick(rank.Rank.Name, rank.Rank.NameUk),
            rank.Coefficient,
            StrengthRanks.ScalePosition(rank.Coefficient, rank.Ladder),
            scale,
            await db.WorkoutPlans.WorkoutsPerWeekAsync(userId, cancellationToken),
            await db.WorkoutSessions.WorkoutsThisWeekAsync(userId, calendar, cancellationToken),
            await ResearchNotes(db.ResearchItems.OrderByDescending(r => r.PublishedAt).ThenBy(r => r.Id).Take(4)).ToListAsync(cancellationToken));
    }

    public Task<DashboardResearchItem?> ResearchNoteAsync(int id, CancellationToken cancellationToken) =>
        ResearchNotes(db.ResearchItems.Where(r => r.Id == id)).SingleOrDefaultAsync(cancellationToken);

    /// <summary>Research notes in the request language.</summary>
    private static IQueryable<DashboardResearchItem> ResearchNotes(IQueryable<ResearchItem> items) => items.AsNoTracking()
        .Select(r => new DashboardResearchItem
        {
            Id = r.Id,
            PublishedAt = r.PublishedAt,
            Title = Localized.Pick(r.Title, r.TitleUk),
            ThesisText = Localized.Pick(r.ThesisText, r.ThesisTextUk),
            Source = r.SourceName,
            SourceUrl = r.SourceUrl
        });
}
