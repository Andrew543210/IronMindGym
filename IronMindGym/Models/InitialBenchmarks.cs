using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class InitialBenchmarks
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Range(0, 500)]
    public double BenchPressKg { get; set; }

    [Range(0, 600)]
    public double SquatKg { get; set; }

    [Range(0, 600)]
    public double DeadliftKg { get; set; }
}
