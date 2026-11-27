using System.ComponentModel.DataAnnotations;
using IronMindGym.Models;

namespace IronMindGym.Contracts;

public sealed record ProfileSettings : SetupRequest
{
    [Required(ErrorMessage = "Enter a username."), StringLength(50, MinimumLength = 2, ErrorMessage = "Use 2–50 letters, numbers, underscores, dots or hyphens.")]
    [RegularExpression("^[A-Za-z0-9_.-]{2,50}$", ErrorMessage = "Use 2–50 letters, numbers, underscores, dots or hyphens.")]
    public string Username { get; init; } = string.Empty;

    [Range(12, 100, ErrorMessage = "Age must be between 12 and 100.")]
    public int Age { get; init; }

    [Range(500, 10000, ErrorMessage = "The calorie target must be between 500 and 10000 kcal.")]
    public int? CalorieTarget { get; init; }

    [Range(1, 1000, ErrorMessage = "The protein target must be between 1 and 1000 g.")]
    public int? ProteinTarget { get; init; }

    [Range(1, 1000, ErrorMessage = "The fat target must be between 1 and 1000 g.")]
    public int? FatTarget { get; init; }

    [Range(1, 2000, ErrorMessage = "The carbohydrate target must be between 1 and 2000 g.")]
    public int? CarbsTarget { get; init; }

    [EnumDataType(typeof(EnergyEquation), ErrorMessage = "Choose an energy equation: none, male or female.")]
    public EnergyEquation EnergyEquation { get; init; } = EnergyEquation.None;
}

public sealed record ChangePasswordRequest
{
    [Required(ErrorMessage = "Enter your current password."), MaxLength(128, ErrorMessage = "Passwords are limited to 128 characters.")]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter a new password.")]
    [MinLength(10, ErrorMessage = "Use a password of at least 10 characters."), MaxLength(128, ErrorMessage = "Passwords are limited to 128 characters.")]
    public string NewPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Repeat the password."), Compare(nameof(NewPassword), ErrorMessage = "Passwords must match.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
