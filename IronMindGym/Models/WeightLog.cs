using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WeightLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Range(0, 500)]
    public double ValueKg { get; set; }

    public DateTime Date { get; set; }
}
