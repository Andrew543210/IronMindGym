namespace IronMindGym.ViewModels;

public class ProfileViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int Age { get; set; }
    public string AvatarName { get; set; } = string.Empty;
    public List<ProfileAchievementViewModel> Achievements { get; set; } = new();
}

public class ProfileAchievementViewModel
{
    public string Name { get; set; } = string.Empty;
    public string ConditionDescription { get; set; } = string.Empty;
    public DateTime AchievedAt { get; set; }
}

