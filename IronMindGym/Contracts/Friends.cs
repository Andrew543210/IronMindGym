namespace IronMindGym.Contracts;

public class FriendCard
{
    public bool IsFriend { get; set; }
    public bool RequestSent { get; set; }
    public bool RequestReceived { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int LastFiveWorkoutsCount { get; set; }
    public double AverageKcal { get; set; }
    public List<FriendWorkoutPreview> LastFiveWorkouts { get; set; } = new();
    public List<FriendNutritionPreview> LastNutritionLogs { get; set; } = new();
    public List<FriendRecordPreview> PersonalRecords { get; set; } = new();
    public List<FriendAchievementPreview> Achievements { get; set; } = new();
}

public class FriendWorkoutPreview
{
    public int WorkoutSessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public int DurationMinutes { get; set; }
}

public class FriendAchievementPreview
{
    public string Name { get; set; } = string.Empty;
    public string ConditionDescription { get; set; } = string.Empty;
    public DateTime AchievedAt { get; set; }
}

public class FriendNutritionPreview
{
    public DateTime Date { get; set; }
    public double CaloriesKcal { get; set; }
    public double ProteinG { get; set; }
    public double FatG { get; set; }
    public double CarbsG { get; set; }
}

public class FriendRecordPreview
{
    public string ExerciseName { get; set; } = string.Empty;
    public double MaxWeightKg { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FriendsOverview
{
    public List<FriendCard> Friends { get; set; } = new();
}
