using System.ComponentModel.DataAnnotations;
using IronMindGym.Models;

namespace IronMindGym.Contracts;

public sealed record LoginRequest
{
    [Required(ErrorMessage = "Enter your email."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(120, ErrorMessage = "Emails are limited to 120 characters.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter your password."), MaxLength(128, ErrorMessage = "Passwords are limited to 128 characters.")]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; }
}

public sealed record RegisterRequest
{
    [Required(ErrorMessage = "Enter a username."), MaxLength(50, ErrorMessage = "Use 2–50 letters, numbers, underscores, dots or hyphens.")]
    [RegularExpression("^[A-Za-z0-9_.-]{2,50}$", ErrorMessage = "Use 2–50 letters, numbers, underscores, dots or hyphens.")]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "Enter your email."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(120, ErrorMessage = "Emails are limited to 120 characters.")]
    public string Email { get; init; } = string.Empty;

    [Range(12, 100, ErrorMessage = "Age must be between 12 and 100.")]
    public int Age { get; init; } = 19;

    [Required(ErrorMessage = "Enter a password.")]
    [MinLength(10, ErrorMessage = "Use a password of at least 10 characters."), MaxLength(128, ErrorMessage = "Passwords are limited to 128 characters.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Repeat the password."), Compare(nameof(Password), ErrorMessage = "Passwords must match.")]
    public string ConfirmPassword { get; init; } = string.Empty;

    [MaxLength(64, ErrorMessage = "Time zone names are limited to 64 characters.")]
    public string? TimeZone { get; init; }
}

public sealed record CsrfTokenResponse(string Token);

public sealed record SessionResponse(int Id, string Username, string Email, string TimeZone, DateOnly Today, bool SetupComplete, Language? Language);
