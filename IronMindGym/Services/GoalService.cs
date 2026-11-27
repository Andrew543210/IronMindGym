using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class GoalService(IronMindDbContext db, AthleteCalendars calendars)
{
    private const int MaxActiveGoals = 20;

    public Task<List<GoalResponse>> ListAsync(int userId, CancellationToken cancellationToken) => db.UserGoals.AsNoTracking()
        .Where(g => g.UserId == userId)
        .OrderBy(g => g.IsCompleted).ThenBy(g => g.Deadline)
        .Select(g => new GoalResponse(g.Id, g.Title, g.Deadline, g.IsCompleted))
        .ToListAsync(cancellationToken);

    public async Task CreateAsync(int userId, string title, DateOnly deadline, CancellationToken cancellationToken)
    {
        var today = (await calendars.ForAsync(userId, cancellationToken)).Today;
        if (deadline < today || deadline > today.AddYears(5))
            throw new DomainValidationException("Choose a deadline between today and five years from now.", "Deadline");
        if (await db.UserGoals.CountAsync(g => g.UserId == userId && !g.IsCompleted, cancellationToken) >= MaxActiveGoals)
            throw new DomainValidationException($"Finish or remove an existing goal before adding more. You can have {MaxActiveGoals} active goals.");
        db.UserGoals.Add(new UserGoal { UserId = userId, Title = title.Trim(), Deadline = deadline });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SetCompletedAsync(int userId, int id, bool completed, CancellationToken cancellationToken) =>
        await db.UserGoals.Where(g => g.Id == id && g.UserId == userId)
            .ExecuteUpdateAsync(goal => goal.SetProperty(g => g.IsCompleted, completed), cancellationToken) > 0;

    public async Task<bool> DeleteAsync(int userId, int id, CancellationToken cancellationToken) =>
        await db.UserGoals.Where(g => g.Id == id && g.UserId == userId).ExecuteDeleteAsync(cancellationToken) > 0;
}
