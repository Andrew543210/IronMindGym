using IronMindGym.Data;

namespace IronMindGym.Services;

public static class AthleteSetup
{
    public static bool IsComplete(IronMindDbContext db, int userId) =>
        db.InitialBenchmarks.Any(b => b.UserId == userId) && db.AnthropometryLogs.Any(a => a.UserId == userId);
}
