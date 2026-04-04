using IronMindGym.Models;

namespace IronMindGym.Services;

public interface INutritionAdviceService
{
    NutritionAdviceResult BuildAdvice(DailyNutrition nutrition, double? bodyWeightKg, IReadOnlyList<DailyNutrition> recentLogs);
}

public class NutritionAdviceResult
{
    public string Verdict { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<string> Tips { get; set; } = new();
}

