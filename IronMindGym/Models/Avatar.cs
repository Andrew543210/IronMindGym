using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Avatar
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? NameUk { get; set; }

    [Range(0, 100)]
    public double MinStrengthCoefficient { get; set; }
}
