using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Contracts;

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterRequest
{
    [Required, MaxLength(50)]
    [RegularExpression("^[A-Za-z0-9_.-]{2,50}$", ErrorMessage = "Use 2–50 letters, numbers, underscores, dots or hyphens.")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Range(12, 100)]
    public int Age { get; set; } = 19;

    [Required, MinLength(10), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords must match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? TimeZone { get; set; }
}
