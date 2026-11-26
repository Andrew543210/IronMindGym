namespace IronMindGym.Models;

public class WeightLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public double ValueKg { get; set; }
    public DateOnly Date { get; set; }
}
