using IronMindGym.Models;

namespace IronMindGym.Services;

public class NutritionAdviceService : INutritionAdviceService
{
    public NutritionAdviceResult BuildAdvice(DailyNutrition nutrition, NutritionProfileSnapshot profile, IReadOnlyList<DailyNutrition> recentLogs)
    {
        var canEstimate = profile.Age >= 18 && profile.WeightKg > 0 && profile.HeightCm > 0 && profile.EnergyEquation is "Male" or "Female";
        var bmr = canEstimate ? 10 * profile.WeightKg!.Value + 6.25 * profile.HeightCm!.Value - 5 * profile.Age!.Value + (profile.EnergyEquation == "Male" ? 5 : -161) : 0;
        var pal = profile.WorkoutsPerWeek switch
        {
            <= 1 => 1.3,
            <= 3 => 1.5,
            <= 5 => 1.6,
            _ => 1.7
        };
        var calories = profile.CalorieTarget ?? (canEstimate ? Math.Round(bmr * pal) : 0);
        var protein = profile.ProteinTarget ?? (canEstimate ? Math.Round(profile.WeightKg!.Value * 1.6) : 0);
        var fat = profile.FatTarget ?? (canEstimate ? Math.Round(profile.WeightKg!.Value * 0.8) : 0);
        var carbs = profile.CarbsTarget ?? (calories > 0 ? Math.Max(0, Math.Round((calories - protein * 4 - fat * 9) / 4)) : 0);
        var targets = new[] { ("Energy", nutrition.CaloriesKcal, calories), ("Protein", nutrition.ProteinG, protein), ("Fat", nutrition.FatG, fat), ("Carbs", nutrition.CarbsG, carbs) };
        var configured = targets.Where(t => t.Item3 > 0).ToList();
        var matched = configured.Count(t => Math.Abs(t.Item2 - t.Item3) <= t.Item3 * 0.1);
        var tips = new List<string>();
        if (configured.Count == 0)
            tips.Add("Set your daily targets in Edit profile, or choose an adult energy equation to see an estimate.");
        foreach (var target in configured)
            tips.Add($"{target.Item1}: {target.Item2:0} logged / {target.Item3:0} target.");
        var derived = nutrition.ProteinG * 4 + nutrition.FatG * 9 + nutrition.CarbsG * 4;
        if (Math.Abs(derived - nutrition.CaloriesKcal) > 150)
            tips.Add($"Macros add up to about {derived:0} kcal. Check serving sizes if this differs from your energy total.");
        var logs = recentLogs.Where(l => l.Date >= nutrition.Date.Date.AddDays(-6) && l.Date <= nutrition.Date.Date).ToList();
        if (logs.Count > 1)
            tips.Add($"Average across {logs.Count} logged days: {logs.Average(l => l.CaloriesKcal):0} kcal.");
        return new NutritionAdviceResult
        {
            Verdict = configured.Count == 0 ? "Targets not set" : $"{matched} of {configured.Count} targets within 10%",
            Summary = "A comparison with your chosen targets, not a health assessment.",
            Score = configured.Count == 0 ? 0 : matched * 100 / configured.Count,
            Tips = tips,
            TargetCaloriesKcal = calories,
            TargetProteinG = protein,
            TargetFatG = fat,
            TargetCarbsG = carbs,
            BasalMetabolicRate = Math.Round(bmr),
            ActivityMultiplier = canEstimate ? pal : 0,
            BmrFormulaText = canEstimate ? $"Mifflin–St Jeor ({profile.EnergyEquation}): 10 × weight + 6.25 × height − 5 × age + {(profile.EnergyEquation == "Male" ? "5" : "(−161)")}." : "Automatic estimates are off. Set personal targets in your profile.",
            CaloriesFormulaText = profile.CalorieTarget.HasValue ? "Your personal energy target." : "Adult estimate: BMR × activity factor. Actual needs vary.",
            ProteinFormulaText = profile.ProteinTarget.HasValue ? "Your personal protein target." : "Optional estimate: 1.6 × bodyweight (kg).",
            FatFormulaText = profile.FatTarget.HasValue ? "Your personal fat target." : "Optional estimate: 0.8 × bodyweight (kg).",
            CarbsFormulaText = profile.CarbsTarget.HasValue ? "Your personal carbohydrate target." : "Remaining energy: (calories − protein × 4 − fat × 9) / 4."
        };
    }
}
