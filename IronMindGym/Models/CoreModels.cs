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
}

public class Profile
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string Gender { get; set; } = "Unknown";

    public int Age { get; set; }
    public DateTime AccountCreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Anthropometry
{
    public int Id { get; set; }
    public double WeightKg { get; set; }
    public double HeightCm { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class InitialBenchmarks
{
    public int Id { get; set; }
    public double BenchPressKg { get; set; }
    public double SquatKg { get; set; }
    public double DeadliftKg { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Avatar
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    public double MinStrengthCoefficient { get; set; }

    [MaxLength(255)]
    public string IconPath { get; set; } = string.Empty;
}

public class WorkoutPlan
{
    public int Id { get; set; }
    public int WorkoutsPerWeek { get; set; }
    public bool IsActive { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SplitTypeId { get; set; }
    public SplitType SplitType { get; set; } = null!;

    public ICollection<ScheduledDay> ScheduledDays { get; set; } = new List<ScheduledDay>();
}

public class SplitType
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
}

public class WeekDay
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ScheduledDay> ScheduledDays { get; set; } = new List<ScheduledDay>();
}

public class ScheduledDay
{
    public int Id { get; set; }
    public int WorkoutPlanId { get; set; }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;

    public int WeekDayId { get; set; }
    public WeekDay WeekDay { get; set; } = null!;
}

