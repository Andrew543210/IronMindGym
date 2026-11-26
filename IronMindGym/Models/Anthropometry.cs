namespace IronMindGym.Models;

public class Anthropometry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public double WeightKg { get; set; }
    public double HeightCm { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
