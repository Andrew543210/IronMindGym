using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class GoalService(IronMindDbContext db, AthleteCalendars calendars)
{
    public const int MaxActiveGoals = 20;

    public Task<List<GoalResponse>> ListAsync(int userId, CancellationToken cancellationToken) => db.UserGoals.AsNoTracking()
        .Where(g => g.UserId == userId)
        .OrderBy(g => g.IsCompleted).ThenBy(g => g.Deadline)
        .Select(g => new GoalResponse(g.Id, g.Title, g.Deadline, g.IsCompleted))
        .ToListAsync(cancellationToken);

    public async Task CreateAsync(int userId, string title, DateOnly deadline, CancellationToken cancellationToken)
    {
        var today = (await calendars.ForAsync(userId, cancellationToken)).Today;
        if (deadline < today || deadline > today.AddYears(5))
            throw new DomainValidationException("Choose a deadline between today and five years from now.", nameof(GoalRequest.Deadline));
        // The count and the insert hold the athlete's lock, so parallel requests cannot pass the limit together.
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        await EnsureRoomForAnotherActiveGoalAsync(userId, cancellationToken);
        db.UserGoals.Add(new UserGoal { UserId = userId, Title = title.Trim(), Deadline = deadline });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> SetCompletedAsync(int userId, int id, bool completed, CancellationToken cancellationToken)
    {
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        var goal = db.UserGoals.Where(g => g.Id == id && g.UserId == userId);
        // Reopening a finished goal makes one more active goal.
        if (!completed && await goal.AnyAsync(g => g.IsCompleted, cancellationToken))
            await EnsureRoomForAnotherActiveGoalAsync(userId, cancellationToken);
        var changed = await goal.ExecuteUpdateAsync(update => update.SetProperty(g => g.IsCompleted, completed), cancellationToken) > 0;
        await transaction.CommitAsync(cancellationToken);
        return changed;
    }

    public async Task<bool> DeleteAsync(int userId, int id, CancellationToken cancellationToken) =>
        await db.UserGoals.Where(g => g.Id == id && g.UserId == userId).ExecuteDeleteAsync(cancellationToken) > 0;

    private async Task EnsureRoomForAnotherActiveGoalAsync(int userId, CancellationToken cancellationToken)
    {
        if (await db.UserGoals.CountAsync(g => g.UserId == userId && !g.IsCompleted, cancellationToken) >= MaxActiveGoals)
            throw new DomainValidationException($"Finish or remove an existing goal before adding more. You can have {MaxActiveGoals} active goals.");
    }
}
