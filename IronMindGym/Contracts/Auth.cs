using System.ComponentModel.DataAnnotations;
using IronMindGym.Models;

namespace IronMindGym.Contracts;

public sealed record LoginRequest
{
    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; }
}

public sealed record RegisterRequest
{
    [Required, MaxLength(50)]
    [RegularExpression("^[A-Za-z0-9_.-]{2,50}$", ErrorMessage = "Use 2–50 letters, numbers, underscores, dots or hyphens.")]
    public string Username { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; init; } = string.Empty;

    [Range(12, 100)]
    public int Age { get; init; } = 19;

    [Required, MinLength(10), MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords must match")]
    public string ConfirmPassword { get; init; } = string.Empty;

    [MaxLength(64)]
    public string? TimeZone { get; init; }
}

public sealed record CsrfTokenResponse(string Token);

public sealed record SessionResponse(int Id, string Username, string Email, string TimeZone, DateOnly Today, bool SetupComplete, Language? Language);
