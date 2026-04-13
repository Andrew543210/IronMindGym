using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Contracts;

public class SetupRequest
{
    [Range(30, 250)]
    public double WeightKg { get; set; }

    [Range(120, 230)]
    public double HeightCm { get; set; }

    [Range(0, 500)]
    public double BenchPressKg { get; set; }

    [Range(0, 500)]
    public double SquatKg { get; set; }

    [Range(0, 500)]
    public double DeadliftKg { get; set; }

    [Range(1, 7)]
    public int WorkoutsPerWeek { get; set; } = 3;

    [Required]
    public int SplitTypeId { get; set; }

    [MaxLength(64)]
    public string? TimeZone { get; set; }
}
