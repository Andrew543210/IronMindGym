namespace IronMindGym.Models;

public class InitialBenchmarks
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public double BenchPressKg { get; set; }
    public double SquatKg { get; set; }
    public double DeadliftKg { get; set; }
}
