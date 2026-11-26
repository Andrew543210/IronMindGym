namespace IronMindGym.Models;

public class DailyNutrition
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public double CaloriesKcal { get; set; }
    public double ProteinG { get; set; }
    public double FatG { get; set; }
    public double CarbsG { get; set; }
}
