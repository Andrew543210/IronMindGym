using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Data;

/// <summary>Lookups that several features share, so each rule is written once.</summary>
public static class AthleteQueries
{
    /// <summary>The weigh-in with the latest date; its value is the athlete's current bodyweight.</summary>
    public static WeightLog? LatestWeighIn(this IQueryable<WeightLog> weighIns, int userId) => weighIns.AsNoTracking()
        .Where(w => w.UserId == userId).OrderByDescending(w => w.Date).FirstOrDefault();
}
