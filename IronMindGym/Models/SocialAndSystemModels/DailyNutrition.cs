using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class DailyNutrition
{
    public int Id
    {
        get; set;
    }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    [Range(0, 20000)]
    public double CaloriesKcal
    {
        get; set;
    }

    [Range(0, 1000)]
    public double ProteinG
    {
        get; set;
    }

    [Range(0, 1000)]
    public double FatG
    {
        get; set;
    }

    [Range(0, 2000)]
    public double CarbsG
    {
        get; set;
    }

    public int UserId
    {
        get; set;
    }
    public User User { get; set; } = null!;

    public NutritionAssessment? Assessment
    {
        get; set;
    }

    public void UpdateMacros(double kcal, double protein, double fat, double carbs)
    {
        CaloriesKcal = kcal;
        ProteinG = protein;
        FatG = fat;
        CarbsG = carbs;
    }

    public override string ToString() => $"DailyNutrition: {Date:yyyy-MM-dd}, {CaloriesKcal} kcal";
}
