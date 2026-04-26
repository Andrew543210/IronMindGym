using IronMindGym.Models;
using IronMindGym.Services;

namespace IronMindGym.Tests.Domain;

public class StrengthTests
{
    private static readonly AvatarService Avatars = new();

    private static readonly Avatar[] Ladder =
    [
        new() { Name = "Noodle Nomad", MinStrengthCoefficient = 0 },
        new() { Name = "Iron Squire", MinStrengthCoefficient = 2 },
        new() { Name = "Titanium Overlord", MinStrengthCoefficient = 4 }
    ];

    [Fact]
    public void TheCoefficientIsTheBigThreeTotalPerKilogramOfBodyweight()
    {
        var lifts = new InitialBenchmarks { BenchPressKg = 100, SquatKg = 140, DeadliftKg = 180 };

        Assert.Equal(5.25, Avatars.CalculateStrengthCoefficient(lifts, new Anthropometry { WeightKg = 80 }));
        Assert.Equal(0, Avatars.CalculateStrengthCoefficient(lifts, null));
        Assert.Equal(0, Avatars.CalculateStrengthCoefficient(null, new Anthropometry { WeightKg = 80 }));
        Assert.Equal(0, Avatars.CalculateStrengthCoefficient(lifts, new Anthropometry { WeightKg = 0 }));
    }

    [Theory]
    [InlineData(0, "Noodle Nomad")]
    [InlineData(160, "Iron Squire")]
    [InlineData(320, "Titanium Overlord")]
    public void TheRankIsTheHighestReachedStep(double squatKg, string rank) =>
        Assert.Equal(rank, Avatars.ResolveAvatar(Ladder, new InitialBenchmarks { SquatKg = squatKg }, new Anthropometry { WeightKg = 80 }).Name);

    [Fact]
    public void AnEmptyLadderStillGivesTheStartingRank() =>
        Assert.Equal("Noodle Nomad", Avatars.ResolveAvatar([], null, null).Name);
}
