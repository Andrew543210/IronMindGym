using IronMindGym.Models;

namespace IronMindGym.Services;

public class AvatarService
{
    public Avatar ResolveAvatar(IEnumerable<Avatar> avatars, InitialBenchmarks? benchmarks, double? bodyweightKg)
    {
        var coefficient = CalculateStrengthCoefficient(benchmarks, bodyweightKg);
        return avatars
            .OrderByDescending(a => a.MinStrengthCoefficient)
            .FirstOrDefault(a => coefficient >= a.MinStrengthCoefficient)
            ?? new Avatar { Name = "Noodle Nomad", MinStrengthCoefficient = 0 };
    }

    public double CalculateStrengthCoefficient(InitialBenchmarks? benchmarks, double? bodyweightKg)
    {
        if (benchmarks is null || bodyweightKg is not > 0)
        {
            return 0;
        }

        var total = benchmarks.BenchPressKg + benchmarks.SquatKg + benchmarks.DeadliftKg;
        return Math.Round(total / bodyweightKg.Value, 2);
    }
}
