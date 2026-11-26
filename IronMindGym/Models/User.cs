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

    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public Profile? Profile { get; set; }
    public InitialBenchmarks? Benchmarks { get; set; }
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = [];
    public ICollection<DailyNutrition> DailyNutritionLogs { get; set; } = [];
    public ICollection<Friendship> FriendshipsAsUser1 { get; set; } = [];
    public ICollection<Friendship> FriendshipsAsUser2 { get; set; } = [];
    public ICollection<UserAchievement> UserAchievements { get; set; } = [];
    public ICollection<WeightLog> WeightLogs { get; set; } = [];
    public ICollection<ActivityLike> ActivityLikes { get; set; } = [];
    public ICollection<FriendComment> FriendComments { get; set; } = [];
    public ICollection<ExerciseProgress> ExerciseProgress { get; set; } = [];
}
