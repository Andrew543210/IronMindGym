namespace IronMindGym.Models;

/// <summary>The lifts entered at setup; one per user, keyed by the user id.</summary>
public class InitialBenchmarks
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public double BenchPressKg { get; set; }
    public double SquatKg { get; set; }
    public double DeadliftKg { get; set; }
}
