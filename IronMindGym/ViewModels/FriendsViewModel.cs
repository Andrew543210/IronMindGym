namespace IronMindGym.ViewModels;

public class FriendCardViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LastFiveWorkoutsCount { get; set; }
    public double AverageKcal { get; set; }
    public List<FriendWorkoutPreviewViewModel> LastFiveWorkouts { get; set; } = new();
    public List<FriendNutritionPreviewViewModel> LastNutritionLogs { get; set; } = new();
    public List<FriendRecordPreviewViewModel> PersonalRecords { get; set; } = new();
    public List<FriendAchievementPreviewViewModel> Achievements { get; set; } = new();
}

public class FriendWorkoutPreviewViewModel
{
    public int WorkoutSessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public int DurationMinutes { get; set; }
}

public class FriendAchievementPreviewViewModel
{
    public string Name { get; set; } = string.Empty;
    public string ConditionDescription { get; set; } = string.Empty;
    public DateTime AchievedAt { get; set; }
}

public class FriendNutritionPreviewViewModel
{
    public DateTime Date { get; set; }
    public double CaloriesKcal { get; set; }
    public double ProteinG { get; set; }
    public double FatG { get; set; }
    public double CarbsG { get; set; }
}

public class FriendRecordPreviewViewModel
{
    public string ExerciseName { get; set; } = string.Empty;
    public double MaxWeightKg { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FriendsViewModel
{
    public string SearchUsername { get; set; } = string.Empty;
    public List<FriendCardViewModel> Friends { get; set; } = new();
}

public class FriendWorkoutDetailsViewModel
{
    public string FriendUsername { get; set; } = string.Empty;
    public WorkoutDetailsViewModel Details { get; set; } = new();
}

