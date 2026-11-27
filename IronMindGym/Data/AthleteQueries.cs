using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Data;

/// <summary>Lookups that several features share, so each rule is written once.</summary>
public static class AthleteQueries
{
    /// <summary>The weigh-in with the latest date; its value is the athlete's current bodyweight.</summary>
    public static Task<WeightLog?> LatestWeighInAsync(this IQueryable<WeightLog> weighIns, int userId, CancellationToken cancellationToken) =>
        weighIns.AsNoTracking().Where(w => w.UserId == userId).OrderByDescending(w => w.Date).FirstOrDefaultAsync(cancellationToken);

    /// <summary>The athlete's training plan; there is at most one, and none before setup.</summary>
    public static Task<WorkoutPlan?> PlanOfAsync(this IQueryable<WorkoutPlan> plans, int userId, CancellationToken cancellationToken) =>
        plans.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    /// <summary>The workouts the athlete plans per week, or 0 before setup.</summary>
    public static Task<int> WorkoutsPerWeekAsync(this IQueryable<WorkoutPlan> plans, int userId, CancellationToken cancellationToken) =>
        plans.Where(p => p.UserId == userId).Select(p => p.WorkoutsPerWeek).FirstOrDefaultAsync(cancellationToken);

    /// <summary>The workouts saved since the start of the athlete's current Monday-based week.</summary>
    public static Task<int> WorkoutsThisWeekAsync(this IQueryable<WorkoutSession> sessions, int userId, AthleteCalendar calendar,
        CancellationToken cancellationToken)
    {
        var weekStart = calendar.CurrentWeekStartUtc;
        return sessions.CountAsync(s => s.UserId == userId && s.SessionDate >= weekStart, cancellationToken);
    }
}
