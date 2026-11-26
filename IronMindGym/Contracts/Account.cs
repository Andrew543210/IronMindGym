using System.ComponentModel.DataAnnotations;
using IronMindGym.Models;

namespace IronMindGym.Contracts;

public sealed record ProfileSettings : SetupRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9_.-]{2,50}$", ErrorMessage = "Use 2–50 letters, numbers, underscores, dots or hyphens.")]
    public string Username { get; init; } = string.Empty;

    [Range(12, 100)]
    public int Age { get; init; }

    [Range(500, 10000)]
    public int? CalorieTarget { get; init; }

    [Range(1, 1000)]
    public int? ProteinTarget { get; init; }

    [Range(1, 1000)]
    public int? FatTarget { get; init; }

    [Range(1, 2000)]
    public int? CarbsTarget { get; init; }

    [EnumDataType(typeof(EnergyEquation), ErrorMessage = "Choose an energy equation: none, male or female.")]
    public EnergyEquation EnergyEquation { get; init; } = EnergyEquation.None;
}

public sealed record ChangePasswordRequest
{
    [Required, MaxLength(128)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required, MinLength(10), MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords must match.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
