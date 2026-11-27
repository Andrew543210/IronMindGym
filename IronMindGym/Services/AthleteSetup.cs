using IronMindGym.Data;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>Setup is complete once the athlete has a height, starting lifts and at least one weigh-in.</summary>
public static class AthleteSetup
{
    public static Task<bool> IsCompleteAsync(IronMindDbContext db, int userId, CancellationToken cancellationToken) => db.Users.AnyAsync(u =>
        u.Id == userId && u.Benchmarks != null && u.Profile != null && u.Profile.HeightCm != null && u.WeightLogs.Any(), cancellationToken);
}
