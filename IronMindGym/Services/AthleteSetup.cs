using IronMindGym.Data;

namespace IronMindGym.Services;

/// <summary>Setup is complete once the athlete has a height, starting lifts and at least one weigh-in.</summary>
public static class AthleteSetup
{
    public static bool IsComplete(IronMindDbContext db, int userId) => db.Users.Any(u =>
        u.Id == userId && u.Benchmarks != null && u.Profile != null && u.Profile.HeightCm != null && u.WeightLogs.Any());
}
