using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Data;

/// <summary>Lookups that several features share, so each rule is written once.</summary>
public static class AthleteQueries
{
    /// <summary>The weigh-in with the latest date; its value is the athlete's current bodyweight.</summary>
    public static WeightLog? LatestWeighIn(this IQueryable<WeightLog> weighIns, int userId) => weighIns.AsNoTracking()
        .Where(w => w.UserId == userId).OrderByDescending(w => w.Date).FirstOrDefault();

    /// <summary>The athlete's training plan; there is at most one, and none before setup.</summary>
    public static WorkoutPlan? PlanOf(this IQueryable<WorkoutPlan> plans, int userId) =>
        plans.AsNoTracking().FirstOrDefault(p => p.UserId == userId);

    /// <summary>The workouts the athlete plans per week, or 0 before setup.</summary>
    public static int WorkoutsPerWeek(this IQueryable<WorkoutPlan> plans, int userId) =>
        plans.Where(p => p.UserId == userId).Select(p => p.WorkoutsPerWeek).FirstOrDefault();

    /// <summary>The workouts saved since the start of the athlete's current Monday-based week.</summary>
    public static int WorkoutsThisWeek(this IQueryable<WorkoutSession> sessions, int userId, AthleteCalendar calendar)
    {
        var weekStart = calendar.CurrentWeekStartUtc;
        return sessions.Count(s => s.UserId == userId && s.SessionDate >= weekStart);
    }
}
