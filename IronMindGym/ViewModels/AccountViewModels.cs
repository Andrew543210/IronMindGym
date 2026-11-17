using System.ComponentModel.DataAnnotations;

namespace IronMindGym.ViewModels;

public class EditProfileViewModel : SetupViewModel
{
    [Range(500, 10000)]
    public int? CalorieTarget
    {
        get; set;
    }
    [Range(1, 1000)]
    public int? ProteinTarget
    {
        get; set;
    }
    [Range(1, 1000)]
    public int? FatTarget
    {
        get; set;
    }
    [Range(1, 2000)]
    public int? CarbsTarget
    {
        get; set;
    }
    [RegularExpression("^(None|Male|Female)$")]
    public string EnergyEquation { get; set; } = "None";

    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9_.-]{2,50}$", ErrorMessage = "Use 2–50 letters, numbers, underscores, dots or hyphens.")]
    public string Username { get; set; } = string.Empty;

    [Range(12, 100)]
    public int Age
    {
        get; set;
    }
}

public class ChangePasswordViewModel
{
    [Required, MaxLength(128), DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(10), MaxLength(128), DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LogWeightViewModel
{
    [Range(30, 250)]
    public double WeightKg
    {
        get; set;
    }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
}
