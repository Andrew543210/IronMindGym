namespace IronMindGym.Contracts;

public sealed record NutritionDay(
    DateTime Date,
    bool IsLogged,
    double CaloriesKcal,
    double ProteinG,
    double FatG,
    double CarbsG,
    NutritionAdvice Advice,
    List<NutritionLogEntry> RecentLogs);

public sealed record NutritionLogEntry(DateTime Date, double CaloriesKcal, double ProteinG, double FatG, double CarbsG);
