using IronMindGym.Models;

namespace IronMindGym.Services;

/// <summary>
/// The rank arithmetic. The strength coefficient is the bench press, squat and deadlift total divided by the
/// bodyweight; the rank is the highest step of the ladder the coefficient reaches. On the rank scale every step
/// takes the same width, whatever its coefficient range.
/// </summary>
public static class StrengthRanks
{
    /// <summary>The top of the last step's range on the scale.</summary>
    public const double ScaleTop = 5.0;

    private static readonly Avatar StartingRank = new() { Name = "Noodle Nomad", NameUk = "Макаронний мандрівник", MinStrengthCoefficient = 0 };

    public static double Coefficient(InitialBenchmarks? lifts, double? bodyweightKg) =>
        lifts is null || bodyweightKg is not > 0
            ? 0
            : Math.Round((lifts.BenchPressKg + lifts.SquatKg + lifts.DeadliftKg) / bodyweightKg.Value, 2);

    public static Avatar RankOf(IEnumerable<Avatar> ladder, double coefficient) =>
        ladder.OrderByDescending(a => a.MinStrengthCoefficient).FirstOrDefault(a => coefficient >= a.MinStrengthCoefficient)
        ?? StartingRank;

    /// <summary>Where the coefficient sits on the scale, from 0 to 100, moving evenly within its step.</summary>
    public static double ScalePosition(double coefficient, IReadOnlyList<Avatar> ladder)
    {
        if (ladder.Count <= 1)
            return 0;
        var sorted = ladder.OrderBy(a => a.MinStrengthCoefficient).ToList();
        if (coefficient <= sorted[0].MinStrengthCoefficient)
            return 0;
        var steps = sorted.Count - 1;
        for (var i = 0; i < steps; i++)
        {
            var left = sorted[i].MinStrengthCoefficient;
            var right = sorted[i + 1].MinStrengthCoefficient;
            if (coefficient <= right)
            {
                var withinStep = right <= left ? 0 : (coefficient - left) / (right - left);
                return Math.Clamp((i + withinStep) / steps * 100.0, 0, 100);
            }
        }
        return 100;
    }
}
