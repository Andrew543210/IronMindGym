using System.ComponentModel.DataAnnotations;

namespace IronMindGym.ViewModels;

public class NutritionViewModel
{
    [Range(500, 8000)]
    public double CaloriesKcal { get; set; }

    [Range(0, 500)]
    public double ProteinG { get; set; }

    [Range(0, 300)]
    public double FatG { get; set; }

    [Range(0, 1000)]
    public double CarbsG { get; set; }

    public string SystemAdvice { get; set; } = string.Empty;
    public string SystemVerdict { get; set; } = string.Empty;
    public int SystemScore { get; set; }
    public List<string> AdviceItems { get; set; } = new();
    public double TargetCaloriesKcal { get; set; }
    public double TargetProteinG { get; set; }
    public double TargetFatG { get; set; }
    public double TargetCarbsG { get; set; }
    public double BasalMetabolicRate { get; set; }
    public double ActivityMultiplier { get; set; }
    public string BmrFormulaText { get; set; } = string.Empty;
    public string CaloriesFormulaText { get; set; } = string.Empty;
    public string ProteinFormulaText { get; set; } = string.Empty;
    public string FatFormulaText { get; set; } = string.Empty;
    public string CarbsFormulaText { get; set; } = string.Empty;
    public List<NutritionLogListItemViewModel> RecentLogs { get; set; } = new();
}

public class NutritionLogListItemViewModel
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double CaloriesKcal { get; set; }
    public double ProteinG { get; set; }
    public double FatG { get; set; }
    public double CarbsG { get; set; }
}

public class NutritionDetailsViewModel
{
    public NutritionLogListItemViewModel Log { get; set; } = new();
    public string SystemAdvice { get; set; } = string.Empty;
    public string SystemVerdict { get; set; } = string.Empty;
    public int SystemScore { get; set; }
    public List<string> AdviceItems { get; set; } = new();
    public double TargetCaloriesKcal { get; set; }
    public double TargetProteinG { get; set; }
    public double TargetFatG { get; set; }
    public double TargetCarbsG { get; set; }
    public double BasalMetabolicRate { get; set; }
    public double ActivityMultiplier { get; set; }
    public string BmrFormulaText { get; set; } = string.Empty;
    public string CaloriesFormulaText { get; set; } = string.Empty;
    public string ProteinFormulaText { get; set; } = string.Empty;
    public string FatFormulaText { get; set; } = string.Empty;
    public string CarbsFormulaText { get; set; } = string.Empty;
    public List<NutritionLogListItemViewModel> RecentLogs { get; set; } = new();
}

