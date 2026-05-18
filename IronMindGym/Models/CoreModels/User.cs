using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ProfilePhotoPath { get; set; }

    public Profile? Profile { get; set; }
    public ICollection<Anthropometry> AnthropometryLogs { get; set; } = new List<Anthropometry>();
    public InitialBenchmarks? Benchmarks { get; set; }
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
    public ICollection<DailyNutrition> DailyNutritionLogs { get; set; } = new List<DailyNutrition>();
    public ICollection<Friendship> FriendshipsAsUser1 { get; set; } = new List<Friendship>();
    public ICollection<Friendship> FriendshipsAsUser2 { get; set; } = new List<Friendship>();
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    public ICollection<WeightLog> WeightLogs { get; set; } = new List<WeightLog>();
    public ICollection<ActivityLike> ActivityLikes { get; set; } = new List<ActivityLike>();
    public ICollection<FriendComment> FriendComments { get; set; } = new List<FriendComment>();
    public ICollection<ExerciseProgress> ExerciseProgress { get; set; } = new List<ExerciseProgress>();

    // Validation Methods
    public bool IsValidUsername(string? username) => !string.IsNullOrWhiteSpace(username) && username.Length > 0 && username.Length <= 50;
    public bool IsValidEmail(string? email) => !string.IsNullOrWhiteSpace(email) && email.Length <= 120;
    public bool IsValidPassword(string? password) => !string.IsNullOrWhiteSpace(password);

    public void ValidateAndSetUsername(string username)
    {
        if (!IsValidUsername(username))
            throw new ArgumentException("Username must be between 1 and 50 characters.");
        Username = username.Trim();
    }

    public void ValidateAndSetEmail(string email)
    {
        if (!IsValidEmail(email))
            throw new ArgumentException("Email must be valid and not exceed 120 characters.");
        Email = email.Trim();
    }

    public void ValidateAndSetPassword(string password)
    {
        if (!IsValidPassword(password))
            throw new ArgumentException("Password cannot be empty.");
        PasswordHash = password.Trim();
    }

    public void ValidateAndSetProfilePhoto(string? photoPath)
    {
        ProfilePhotoPath = photoPath?.Trim();
    }

    public void UpdateEmail(string email) => ValidateAndSetEmail(email);
    public void UpdateUsername(string username) => ValidateAndSetUsername(username);

    public override string ToString() => $"User: {Username} ({Email})";
}
