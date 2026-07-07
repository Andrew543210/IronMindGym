using IronMindGym.Models;
using IronMindGym.Services;

namespace IronMindGym.Tests.Domain;

public class NutritionAdviceTests
{
    private static readonly NutritionAdviceService Advice = new();

    private static NutritionAdvice For(NutritionProfileSnapshot profile, DailyNutrition? day = null, params DailyNutrition[] recent) =>
        Advice.BuildAdvice(day ?? new DailyNutrition { Date = new DateTime(2026, 11, 20) }, profile, recent);

    [Fact]
    public void AdultEstimatesFollowMifflinStJeorAndFitTheEnergyBudget()
    {
        var advice = For(new NutritionProfileSnapshot { Age = 24, WeightKg = 80, HeightCm = 180, EnergyEquation = "Male", WorkoutsPerWeek = 3 });

        Assert.Equal(1810, advice.BasalMetabolicRate);
        Assert.Equal(1.5, advice.ActivityMultiplier);
        Assert.Equal("Male", advice.EnergyEquation);
        Assert.Equal(new NutritionTarget(2715, TargetSource.Estimate), advice.Calories);
        Assert.Equal(new NutritionTarget(128, TargetSource.Estimate), advice.Protein);
        Assert.Equal(new NutritionTarget(64, TargetSource.Estimate), advice.Fat);
        Assert.Equal(TargetSource.Remainder, advice.Carbs.Source);
        Assert.InRange(Math.Abs(advice.Protein.Value * 4 + advice.Fat.Value * 9 + advice.Carbs.Value * 4 - advice.Calories.Value), 0, 4);
    }

    [Theory]
    [InlineData(0, 1.3)]
    [InlineData(2, 1.5)]
    [InlineData(5, 1.6)]
    [InlineData(7, 1.7)]
    public void TheActivityFactorFollowsThePlan(int workoutsPerWeek, double factor) =>
        Assert.Equal(factor, For(new NutritionProfileSnapshot { Age = 30, WeightKg = 60, HeightCm = 165, EnergyEquation = "Female", WorkoutsPerWeek = workoutsPerWeek }).ActivityMultiplier);

    [Fact]
    public void PersonalTargetsWinOverEstimates()
    {
        var advice = For(new NutritionProfileSnapshot
        {
            Age = 24, WeightKg = 80, HeightCm = 180, EnergyEquation = "Male",
            CalorieTarget = 2400, ProteinTarget = 140, FatTarget = 70, CarbsTarget = 280
        });

        Assert.Equal([new(2400, TargetSource.Personal), new(140, TargetSource.Personal), new(70, TargetSource.Personal), new NutritionTarget(280, TargetSource.Personal)],
            new[] { advice.Calories, advice.Protein, advice.Fat, advice.Carbs });
    }

    [Theory]
    [InlineData(15, "Male")]
    [InlineData(30, "None")]
    public void MinorsAndAthletesWithoutAnEquationGetNoEstimates(int age, string equation)
    {
        var advice = For(new NutritionProfileSnapshot { Age = age, WeightKg = 65, HeightCm = 170, EnergyEquation = equation });

        Assert.Equal((0.0, 0.0, null), (advice.BasalMetabolicRate, advice.ActivityMultiplier, advice.EnergyEquation));
        Assert.All(new[] { advice.Calories, advice.Protein, advice.Fat, advice.Carbs }, t => Assert.Equal(TargetSource.NotSet, t.Source));
        Assert.Equal((0, 0), (advice.TargetsSet, advice.Score));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(100, null)]
    [InlineData(null, 60)]
    public void PartialPersonalTargetsDoNotInventCarbohydrates(int? protein, int? fat)
    {
        var advice = For(new NutritionProfileSnapshot { CalorieTarget = 2000, ProteinTarget = protein, FatTarget = fat });

        Assert.Equal(new NutritionTarget(0, TargetSource.NotSet), advice.Carbs);
    }

    [Fact]
    public void ProteinAndFatAboveTheEnergyTargetAreFlagged()
    {
        Assert.Equal(new NutritionTarget(220, TargetSource.Remainder), For(new NutritionProfileSnapshot { CalorieTarget = 2000, ProteinTarget = 100, FatTarget = 80 }).Carbs);

        var overBudget = For(new NutritionProfileSnapshot { CalorieTarget = 500, ProteinTarget = 100, FatTarget = 80 });

        Assert.Equal(new NutritionTarget(0, TargetSource.Remainder), overBudget.Carbs);
        Assert.True(overBudget.TargetsExceedEnergy);
    }

    [Fact]
    public void TheDayMatchesTargetsWithinTenPercentAndChecksItsMacros()
    {
        var profile = new NutritionProfileSnapshot { CalorieTarget = 2000, ProteinTarget = 150, FatTarget = 70, CarbsTarget = 200 };
        var day = new DailyNutrition { Date = new DateTime(2026, 11, 20), CaloriesKcal = 2100, ProteinG = 120, FatG = 75, CarbsG = 200 };
        var earlier = new DailyNutrition { Date = new DateTime(2026, 11, 18), CaloriesKcal = 1700 };
        var tooOld = new DailyNutrition { Date = new DateTime(2026, 11, 1), CaloriesKcal = 4000 };

        var advice = For(profile, day, day, earlier, tooOld);

        Assert.Equal((4, 3, 75), (advice.TargetsSet, advice.TargetsMatched, advice.Score));
        Assert.Equal(1955, advice.MacroEnergyKcal);
        Assert.False(advice.MacrosDisagreeWithEnergy);
        Assert.Equal((2, 1900.0), (advice.WeekDaysLogged, advice.WeekAverageKcal));
        Assert.True(For(profile, new DailyNutrition { CaloriesKcal = 3000, ProteinG = 100 }).MacrosDisagreeWithEnergy);
    }
}
