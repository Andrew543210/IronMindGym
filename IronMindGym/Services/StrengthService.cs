using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>An athlete's current lifts, bodyweight, strength coefficient and rank, with the whole ladder.</summary>
public sealed record StrengthRank(double? BodyweightKg, double Coefficient, Avatar Rank, IReadOnlyList<Avatar> Ladder);

public class StrengthService(IronMindDbContext db)
{
    /// <summary>
    /// The rank counts each key lift at the higher of the setup result and the personal record, against the
    /// latest weigh-in.
    /// </summary>
    public async Task<StrengthRank> RankAsync(int userId, CancellationToken cancellationToken)
    {
        var baseline = await db.InitialBenchmarks.AsNoTracking().FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);
        var lifts = await CurrentAsync(userId, baseline, cancellationToken);
        var bodyweightKg = (await db.WeightLogs.LatestWeighInAsync(userId, cancellationToken))?.ValueKg;
        var coefficient = StrengthRanks.Coefficient(lifts, bodyweightKg);
        var ladder = await db.Avatars.AsNoTracking().OrderBy(a => a.MinStrengthCoefficient).ToListAsync(cancellationToken);
        return new StrengthRank(bodyweightKg, coefficient, StrengthRanks.RankOf(ladder, coefficient), ladder);
    }

    private async Task<InitialBenchmarks> CurrentAsync(int userId, InitialBenchmarks? baseline, CancellationToken cancellationToken)
    {
        var records = await db.PersonalRecords.AsNoTracking().Where(r => r.UserId == userId && r.Exercise.KeyLift != null)
            .Select(r => new { Lift = r.Exercise.KeyLift!.Value, r.MaxWeightKg })
            .ToDictionaryAsync(r => r.Lift, r => r.MaxWeightKg, cancellationToken);
        double Best(KeyLift lift, double initial) => Math.Max(initial, records.GetValueOrDefault(lift));
        return new InitialBenchmarks
        {
            BenchPressKg = Best(KeyLift.BenchPress, baseline?.BenchPressKg ?? 0),
            SquatKg = Best(KeyLift.Squat, baseline?.SquatKg ?? 0),
            DeadliftKg = Best(KeyLift.Deadlift, baseline?.DeadliftKg ?? 0)
        };
    }
}
