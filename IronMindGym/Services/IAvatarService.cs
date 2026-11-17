using IronMindGym.Models;

namespace IronMindGym.Services;

public interface IAvatarService
{
    Avatar ResolveAvatar(IEnumerable<Avatar> avatars, InitialBenchmarks? benchmarks, Anthropometry? latestAnthropometry);
    double CalculateStrengthCoefficient(InitialBenchmarks? benchmarks, Anthropometry? latestAnthropometry);
}

