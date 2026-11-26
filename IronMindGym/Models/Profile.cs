using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

/// <summary>The athlete's personal settings; one per user, keyed by the user id.</summary>
public class Profile
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int Age { get; set; }
    public double? HeightCm { get; set; }

    [MaxLength(64)]
    public string? TimeZone { get; set; }

    /// <summary>Empty until the athlete picks one; the client then follows the browser.</summary>
    public Language? Language { get; set; }

    public int? CalorieTarget { get; set; }
    public int? ProteinTarget { get; set; }
    public int? FatTarget { get; set; }
    public int? CarbsTarget { get; set; }

    public EnergyEquation EnergyEquation { get; set; } = EnergyEquation.None;
}
