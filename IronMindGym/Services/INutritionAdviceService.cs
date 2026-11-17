using IronMindGym.Models;

namespace IronMindGym.Services;

public interface INutritionAdviceService
{
    NutritionAdviceResult BuildAdvice(
        DailyNutrition nutrition,
        NutritionProfileSnapshot profile,
        IReadOnlyList<DailyNutrition> recentLogs);
}

public class NutritionProfileSnapshot
{
    public int? CalorieTarget
    {
        get; set;
    }
    public int? ProteinTarget
    {
        get; set;
    }
    public int? FatTarget
    {
        get; set;
    }
    public int? CarbsTarget
    {
        get; set;
    }
    public string EnergyEquation { get; set; } = "None";

    public double? WeightKg
    {
        get; set;
    }
    public double? HeightCm
    {
        get; set;
    }
    public int? Age
    {
        get; set;
    }
    public int WorkoutsPerWeek
    {
        get; set;
    }
}

public class NutritionAdviceResult
{
    public string Verdict { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int Score
    {
        get; set;
    }
    public List<string> Tips { get; set; } = new();
    public double TargetCaloriesKcal
    {
        get; set;
    }
    public double TargetProteinG
    {
        get; set;
    }
    public double TargetFatG
    {
        get; set;
    }
    public double TargetCarbsG
    {
        get; set;
    }
    public double BasalMetabolicRate
    {
        get; set;
    }
    public double ActivityMultiplier
    {
        get; set;
    }
    public string BmrFormulaText { get; set; } = string.Empty;
    public string CaloriesFormulaText { get; set; } = string.Empty;
    public string ProteinFormulaText { get; set; } = string.Empty;
    public string FatFormulaText { get; set; } = string.Empty;
    public string CarbsFormulaText { get; set; } = string.Empty;
}

