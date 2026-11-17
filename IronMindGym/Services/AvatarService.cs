using IronMindGym.Models;

namespace IronMindGym.Services;

public class AvatarService : IAvatarService
{
    public Avatar ResolveAvatar(IEnumerable<Avatar> avatars, InitialBenchmarks? benchmarks, Anthropometry? latestAnthropometry)
    {
        var coefficient = CalculateStrengthCoefficient(benchmarks, latestAnthropometry);
        return avatars
            .OrderByDescending(a => a.MinStrengthCoefficient)
            .FirstOrDefault(a => coefficient >= a.MinStrengthCoefficient)
            ?? new Avatar { Name = "Noodle Nomad", MinStrengthCoefficient = 0, IconPath = "/images/avatar-novice.png" };
    }

    public double CalculateStrengthCoefficient(InitialBenchmarks? benchmarks, Anthropometry? latestAnthropometry)
    {
        if (benchmarks is null || latestAnthropometry is null || latestAnthropometry.WeightKg <= 0)
        {
            return 0;
        }

        var total = benchmarks.BenchPressKg + benchmarks.SquatKg + benchmarks.DeadliftKg;
        return Math.Round(total / latestAnthropometry.WeightKg, 2);
    }
}

