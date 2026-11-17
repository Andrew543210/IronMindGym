namespace IronMindGym.ViewModels;

public class ProfileViewModel
{
    public List<IronMindGym.Models.WeightLog> WeightHistory { get; set; } = new();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age
    {
        get; set;
    }
    public string AvatarName { get; set; } = string.Empty;
    public double? WeightKg
    {
        get; set;
    }
    public double? HeightCm
    {
        get; set;
    }
    public DateTime? AnthropometryRecordedAt
    {
        get; set;
    }
    public double? BenchPressKg
    {
        get; set;
    }
    public double? SquatKg
    {
        get; set;
    }
    public double? DeadliftKg
    {
        get; set;
    }
    public List<ProfileAchievementViewModel> Achievements { get; set; } = new();
}

public class ProfileAchievementViewModel
{
    public string Name { get; set; } = string.Empty;
    public string ConditionDescription { get; set; } = string.Empty;
    public DateTime AchievedAt
    {
        get; set;
    }
}

