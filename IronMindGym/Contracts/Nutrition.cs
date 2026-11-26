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
