using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Data;

public static class AthleteQueries
{
    public static Task<WeightLog?> LatestWeighInAsync(this IQueryable<WeightLog> weighIns, int userId, CancellationToken cancellationToken) =>
        weighIns.AsNoTracking().Where(w => w.UserId == userId).OrderByDescending(w => w.Date).FirstOrDefaultAsync(cancellationToken);

    public static Task<WorkoutPlan?> PlanOfAsync(this IQueryable<WorkoutPlan> plans, int userId, CancellationToken cancellationToken) =>
        plans.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public static Task<int> WorkoutsPerWeekAsync(this IQueryable<WorkoutPlan> plans, int userId, CancellationToken cancellationToken) =>
        plans.Where(p => p.UserId == userId).Select(p => p.WorkoutsPerWeek).FirstOrDefaultAsync(cancellationToken);

    public static Task<int> WorkoutsThisWeekAsync(this IQueryable<WorkoutSession> sessions, int userId, AthleteCalendar calendar,
        CancellationToken cancellationToken)
    {
        var weekStart = calendar.CurrentWeekStartUtc;
        return sessions.CountAsync(s => s.UserId == userId && s.SessionDate >= weekStart, cancellationToken);
    }
}
