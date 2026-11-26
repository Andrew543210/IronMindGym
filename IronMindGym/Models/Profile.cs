using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Profile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int Age { get; set; }
    public double? HeightCm { get; set; }
    public DateTime AccountCreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? TimeZone { get; set; }

    [MaxLength(8)]
    public string? Language { get; set; }

    public int? CalorieTarget { get; set; }
    public int? ProteinTarget { get; set; }
    public int? FatTarget { get; set; }
    public int? CarbsTarget { get; set; }

    [MaxLength(8)]
    public string EnergyEquation { get; set; } = "None";
}
