using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class DailyNutrition
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Date { get; set; }

    [Range(0, 10000)]
    public double CaloriesKcal { get; set; }

    [Range(0, 500)]
    public double ProteinG { get; set; }

    [Range(0, 300)]
    public double FatG { get; set; }

    [Range(0, 1000)]
    public double CarbsG { get; set; }
}
