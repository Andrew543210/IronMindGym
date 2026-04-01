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
}

