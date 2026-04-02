using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Anthropometry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Range(0, 500)]
    public double WeightKg { get; set; }

    [Range(0, 300)]
    public double HeightCm { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
