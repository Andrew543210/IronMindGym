using IronMindGym.Contracts;
using IronMindGym.Models;

namespace IronMindGym.Services;

public sealed class NutritionProfileSnapshot
{
    public int? CalorieTarget { get; init; }
    public int? ProteinTarget { get; init; }
    public int? FatTarget { get; init; }
    public int? CarbsTarget { get; init; }
    public EnergyEquation EnergyEquation { get; init; } = EnergyEquation.None;
    public double? WeightKg { get; init; }
    public double? HeightCm { get; init; }
    public int? Age { get; init; }
    public int WorkoutsPerWeek { get; init; }
}

// Personal targets win. Adults with an energy equation get estimates: Mifflin–St Jeor BMR times an activity
// factor, 1.6 g/kg protein, 0.8 g/kg fat and carbohydrates from the rest. A target is matched within 10%.
public sealed class NutritionAdviceService
{
    private const double MatchTolerance = 0.1;
    private const double MacroEnergyTolerance = 150;

    public NutritionAdvice BuildAdvice(DailyNutrition day, NutritionProfileSnapshot profile, IReadOnlyList<DailyNutrition> recentLogs)
    {
        var canEstimate = profile is { Age: >= 18, WeightKg: > 0, HeightCm: > 0, EnergyEquation: EnergyEquation.Male or EnergyEquation.Female };
        var bmr = canEstimate
            ? 10 * profile.WeightKg!.Value + 6.25 * profile.HeightCm!.Value - 5 * profile.Age!.Value + (profile.EnergyEquation == EnergyEquation.Male ? 5 : -161)
            : 0;
        var activity = canEstimate ? ActivityFactor(profile.WorkoutsPerWeek) : 0;

        var calories = Pick(profile.CalorieTarget, canEstimate, () => Math.Round(bmr * activity));
        var protein = Pick(profile.ProteinTarget, canEstimate, () => Math.Round(profile.WeightKg!.Value * 1.6));
        var fat = Pick(profile.FatTarget, canEstimate, () => Math.Round(profile.WeightKg!.Value * 0.8));
        var remainingEnergy = calories.Value - protein.Value * 4 - fat.Value * 9;
        var canDeriveCarbs = calories.Value > 0 && protein.Value > 0 && fat.Value > 0;
        var carbs = profile.CarbsTarget is { } personalCarbs
            ? new NutritionTarget(personalCarbs, TargetSource.Personal)
            : canDeriveCarbs ? new NutritionTarget(Math.Max(0, Math.Round(remainingEnergy / 4)), TargetSource.Remainder)
            : new NutritionTarget(0, TargetSource.NotSet);

        var comparisons = new[] { (day.CaloriesKcal, calories), (day.ProteinG, protein), (day.FatG, fat), (day.CarbsG, carbs) }
            .Where(c => c.Item2.Value > 0).ToList();
        var matched = comparisons.Count(c => Math.Abs(c.Item1 - c.Item2.Value) <= c.Item2.Value * MatchTolerance);
        var macroEnergy = day.ProteinG * 4 + day.FatG * 9 + day.CarbsG * 4;
        var week = recentLogs.Where(l => l.Date <= day.Date && l.Date >= day.Date.AddDays(-6)).ToList();

        return new NutritionAdvice(
            calories, protein, fat, carbs,
            canEstimate ? profile.EnergyEquation : null,
            Math.Round(bmr),
            activity,
            comparisons.Count,
            matched,
            comparisons.Count == 0 ? 0 : matched * 100 / comparisons.Count,
            canDeriveCarbs && remainingEnergy < 0 && profile.CarbsTarget is null,
            Math.Round(macroEnergy),
            Math.Abs(macroEnergy - day.CaloriesKcal) > MacroEnergyTolerance,
            week.Count,
            week.Count > 0 ? Math.Round(week.Average(l => l.CaloriesKcal)) : 0);
    }

    private static NutritionTarget Pick(int? personal, bool canEstimate, Func<double> estimate) =>
        personal is { } value ? new(value, TargetSource.Personal)
        : canEstimate ? new(estimate(), TargetSource.Estimate)
        : new(0, TargetSource.NotSet);

    private static double ActivityFactor(int workoutsPerWeek) => workoutsPerWeek switch
    {
        <= 1 => 1.3,
        <= 3 => 1.5,
        <= 5 => 1.6,
        _ => 1.7
    };
}
