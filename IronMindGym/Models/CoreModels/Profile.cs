using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Profile
{
    public int Id { get; set; }

    [Range(0, 120)]
    public int Age { get; set; }

    public DateTime AccountCreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Validation Methods
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
