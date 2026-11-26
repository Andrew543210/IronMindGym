using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Contracts;

public sealed record NutritionDay(
    DateOnly Date,
    bool IsLogged,
    double CaloriesKcal,
    double ProteinG,
    double FatG,
    double CarbsG,
    NutritionAdvice Advice,
    List<NutritionLogEntry> RecentLogs);

public sealed record NutritionLogEntry(DateOnly Date, double CaloriesKcal, double ProteinG, double FatG, double CarbsG);

public sealed record NutritionEntryRequest(
    [Range(0, 10000, ErrorMessage = "Calories must be 0–10000 kcal.")] double CaloriesKcal,
    [Range(0, 500, ErrorMessage = "Protein must be 0–500 g.")] double ProteinG,
    [Range(0, 300, ErrorMessage = "Fat must be 0–300 g.")] double FatG,
    [Range(0, 1000, ErrorMessage = "Carbs must be 0–1000 g.")] double CarbsG);
