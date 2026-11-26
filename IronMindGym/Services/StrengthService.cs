using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

/// <summary>An athlete's current lifts, bodyweight, strength coefficient and rank, with the whole ladder.</summary>
public sealed record StrengthRank(InitialBenchmarks Lifts, double? BodyweightKg, double Coefficient, Avatar Rank, IReadOnlyList<Avatar> Ladder);

public class StrengthService(IronMindDbContext db)
{
    /// <summary>
    /// The rank counts each key lift at the higher of the setup result and the personal record, against the
    /// latest weigh-in.
    /// </summary>
    public StrengthRank Rank(int userId)
    {
        var baseline = db.InitialBenchmarks.AsNoTracking().FirstOrDefault(b => b.UserId == userId);
        var lifts = Current(userId, baseline);
        var bodyweightKg = db.WeightLogs.LatestWeighIn(userId)?.ValueKg;
        var coefficient = StrengthRanks.Coefficient(lifts, bodyweightKg);
        var ladder = db.Avatars.AsNoTracking().OrderBy(a => a.MinStrengthCoefficient).ToList();
        return new StrengthRank(lifts, bodyweightKg, coefficient, StrengthRanks.RankOf(ladder, coefficient), ladder);
    }

    private InitialBenchmarks Current(int userId, InitialBenchmarks? baseline)
    {
        var records = db.PersonalRecords.AsNoTracking().Where(r => r.UserId == userId && r.Exercise.KeyLift != null)
            .Select(r => new { Lift = r.Exercise.KeyLift!.Value, r.MaxWeightKg })
            .ToDictionary(r => r.Lift, r => r.MaxWeightKg);
        double Best(KeyLift lift, double initial) => Math.Max(initial, records.GetValueOrDefault(lift));
        return new InitialBenchmarks
        {
            BenchPressKg = Best(KeyLift.BenchPress, baseline?.BenchPressKg ?? 0),
            SquatKg = Best(KeyLift.Squat, baseline?.SquatKg ?? 0),
            DeadliftKg = Best(KeyLift.Deadlift, baseline?.DeadliftKg ?? 0)
        };
    }
}
