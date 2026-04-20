using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class User
{
    private string _username = string.Empty;
    private string _email = string.Empty;
    private string _passwordHash = string.Empty;
    private string? _profilePhotoPath;

    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username
    {
        get => _username;
        set => _username = (value ?? string.Empty).Trim();
    }

    [Required, MaxLength(120)]
    public string Email
    {
        get => _email;
        set => _email = (value ?? string.Empty).Trim();
    }

    [Required]
    public string PasswordHash
    {
        get => _passwordHash;
        set => _passwordHash = (value ?? string.Empty).Trim();
    }

    [MaxLength(255)]
    public string? ProfilePhotoPath
    {
        get => _profilePhotoPath;
        set => _profilePhotoPath = value?.Trim();
    }

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

    public void UpdateEmail(string email) => Email = email;
    public void UpdateUsername(string username) => Username = username;

    public override string ToString() => $"User: {Username} ({Email})";
}
