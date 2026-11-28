using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public static class AthleteSetup
{
    public static Task<bool> IsCompleteAsync(IronMindDbContext db, int userId, CancellationToken cancellationToken) => db.Users.AnyAsync(u =>
        u.Id == userId && u.Benchmarks != null && u.Profile != null && u.Profile.HeightCm != null && u.WeightLogs.Any(), cancellationToken);

    public static async Task EnsureSplitExistsAsync(IronMindDbContext db, int splitTypeId, CancellationToken cancellationToken)
    {
        if (!await db.SplitTypes.AnyAsync(s => s.Id == splitTypeId, cancellationToken))
            throw new DomainValidationException("Choose a valid training split.", nameof(AthleteBasics.SplitTypeId));
    }

    // The caller holds the athlete's lock and saves; the bodyweight is a weigh-in the caller dates.
    public static async Task ApplyAsync(IronMindDbContext db, User user, AthleteBasics basics, CancellationToken cancellationToken)
    {
        user.Profile ??= new Profile { UserId = user.Id };
        user.Profile.HeightCm = basics.HeightCm;
        user.Benchmarks ??= new InitialBenchmarks { UserId = user.Id };
        user.Benchmarks.BenchPressKg = basics.BenchPressKg;
        user.Benchmarks.SquatKg = basics.SquatKg;
        user.Benchmarks.DeadliftKg = basics.DeadliftKg;
        var plan = await db.WorkoutPlans.FindAsync([user.Id], cancellationToken);
        if (plan is null)
            db.WorkoutPlans.Add(plan = new WorkoutPlan { UserId = user.Id });
        plan.WorkoutsPerWeek = basics.WorkoutsPerWeek;
        plan.SplitTypeId = basics.SplitTypeId;
    }

    public static async Task SaveWeighInAsync(IronMindDbContext db, int userId, double weightKg, DateOnly date, CancellationToken cancellationToken)
    {
        var weighIn = await db.WeightLogs.FirstOrDefaultAsync(w => w.UserId == userId && w.Date == date, cancellationToken);
        if (weighIn is null)
            db.WeightLogs.Add(new WeightLog { UserId = userId, Date = date, ValueKg = weightKg });
        else
            weighIn.ValueKg = weightKg;
    }
}
