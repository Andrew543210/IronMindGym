using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Profile
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

    public int Id
    {
        get; set;
    }

    [Range(0, 120)]
    public int Age
    {
        get; set;
    }

    public DateTime AccountCreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId
    {
        get; set;
    }
    public User User { get; set; } = null!;

    public bool IsValidAge(int age) => age >= 0 && age <= 120;

    public void ValidateAndSetAge(int age)
    {
        if (!IsValidAge(age))
            throw new ArgumentException("Age must be between 0 and 120.");
        Age = age;
    }

    public void UpdateAge(int age) => ValidateAndSetAge(age);

    public override string ToString() => $"Profile: Age={Age}";
}
