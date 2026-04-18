using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Profile
{
    private string _gender = "Unknown";

    public int Id { get; set; }

    [MaxLength(20)]
    public string Gender
    {
        get => _gender;
        set => _gender = string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
    }

    [Range(0, 120)]
    public int Age { get; set; }

    public DateTime AccountCreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public void UpdateAge(int age) => Age = age;

    public override string ToString() => $"Profile: Gender={Gender}, Age={Age}";
}
