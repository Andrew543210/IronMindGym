using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Profile
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int Age { get; set; }
    public double? HeightCm { get; set; }

    [MaxLength(64)]
    public string? TimeZone { get; set; }

    public Language? Language { get; set; }

    public int? CalorieTarget { get; set; }
    public int? ProteinTarget { get; set; }
    public int? FatTarget { get; set; }
    public int? CarbsTarget { get; set; }

    public EnergyEquation EnergyEquation { get; set; } = EnergyEquation.None;
}
