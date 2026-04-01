using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class DailyNutrition
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public double CaloriesKcal { get; set; }
    public double ProteinG { get; set; }
    public double FatG { get; set; }
    public double CarbsG { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NutritionAssessment? Assessment { get; set; }
}

public class NutritionAdvice
{
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string AdviceText { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Type { get; set; } = string.Empty;
}

public class NutritionAssessment
{
    public int Id { get; set; }
    public int DailyNutritionId { get; set; }
    public DailyNutrition DailyNutrition { get; set; } = null!;

    [MaxLength(120)]
    public string SystemVerdict { get; set; } = string.Empty;

    public int? AdviceId { get; set; }
    public NutritionAdvice? Advice { get; set; }
}

public class Friendship
{
    public int Id { get; set; }

    public int User1Id { get; set; }
    public User User1 { get; set; } = null!;

    public int User2Id { get; set; }
    public User User2 { get; set; } = null!;

    [MaxLength(30)]
    public string Status { get; set; } = "Pending";
}

public class ResearchItem
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(1200)]
    public string ThesisText { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Source { get; set; } = string.Empty;

    public ICollection<ActivityFeed> FeedItems { get; set; } = new List<ActivityFeed>();
}

public class ActivityFeed
{
    public int Id { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public int ResearchItemId { get; set; }
    public ResearchItem ResearchItem { get; set; } = null!;
}

public class Achievement
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ConditionDescription { get; set; } = string.Empty;

    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}

public class UserAchievement
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int AchievementId { get; set; }
    public Achievement Achievement { get; set; } = null!;

    public DateTime AchievedAt { get; set; } = DateTime.UtcNow;
}

public class WeightLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public double ValueKg { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
}

public class UserGoal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(80)]
    public string GoalType { get; set; } = string.Empty;

    public DateTime Deadline { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(300)]
    public string Text { get; set; } = string.Empty;

    public bool ReadStatus { get; set; }
}

public class UITheme
{
    public int Id { get; set; }

    [MaxLength(30)]
    public string Type { get; set; } = "Light";
}

public class Settings
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(10)]
    public string Language { get; set; } = "uk";

    public int UIThemeId { get; set; }
    public UITheme UITheme { get; set; } = null!;
}

