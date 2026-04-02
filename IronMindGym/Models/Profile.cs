using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Profile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Range(0, 120)]
    public int Age { get; set; }

    public DateTime AccountCreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? TimeZone { get; set; }

    [MaxLength(8)]
    public string? Language { get; set; }

    [Range(500, 10000)]
    public int? CalorieTarget { get; set; }

    [Range(1, 1000)]
    public int? ProteinTarget { get; set; }

    [Range(1, 1000)]
    public int? FatTarget { get; set; }

    [Range(1, 2000)]
    public int? CarbsTarget { get; set; }

    [MaxLength(8)]
    public string EnergyEquation { get; set; } = "None";
}
