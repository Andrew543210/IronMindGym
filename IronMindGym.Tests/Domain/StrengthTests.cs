using IronMindGym.Models;
using IronMindGym.Services;

namespace IronMindGym.Tests.Domain;

public class StrengthTests
{
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

        Assert.Equal(5.25, StrengthRanks.Coefficient(lifts, 80));
        Assert.Equal(0, StrengthRanks.Coefficient(lifts, null));
        Assert.Equal(0, StrengthRanks.Coefficient(null, 80));
        Assert.Equal(0, StrengthRanks.Coefficient(lifts, 0));
    }

    [Theory]
    [InlineData(0, "Noodle Nomad")]
    [InlineData(1.99, "Noodle Nomad")]
    [InlineData(2, "Iron Squire")]
    [InlineData(4.5, "Titanium Overlord")]
    public void TheRankIsTheHighestReachedStep(double coefficient, string rank) =>
        Assert.Equal(rank, StrengthRanks.RankOf(Ladder, coefficient).Name);

    [Fact]
    public void AnEmptyLadderStillGivesTheStartingRank() =>
        Assert.Equal("Noodle Nomad", StrengthRanks.RankOf([], 3).Name);

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 25)]
    [InlineData(2, 50)]
    [InlineData(3, 75)]
    [InlineData(4, 100)]
    [InlineData(6, 100)]
    public void EveryStepTakesTheSameWidthOnTheScale(double coefficient, double percent) =>
        Assert.Equal(percent, StrengthRanks.ScalePosition(coefficient, Ladder), 6);

    [Fact]
    public void AnUnevenLadderStillMovesEvenlyWithinEachStep()
    {
        Avatar[] uneven = [new() { MinStrengthCoefficient = 0 }, new() { MinStrengthCoefficient = 0.5 }, new() { MinStrengthCoefficient = 3 }];

        Assert.Equal(25, StrengthRanks.ScalePosition(0.25, uneven), 6);
        Assert.Equal(75, StrengthRanks.ScalePosition(1.75, uneven), 6);
        Assert.Equal(0, StrengthRanks.ScalePosition(2, [new() { MinStrengthCoefficient = 0 }]));
    }
}
