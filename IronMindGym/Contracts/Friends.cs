namespace IronMindGym.Contracts;

public sealed record FriendCard
{
    public bool IsFriend { get; init; }
    public bool RequestSent { get; init; }
    public bool RequestReceived { get; init; }
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public int LastFiveWorkoutsCount { get; init; }
    public double AverageKcal { get; init; }
    public List<FriendWorkoutPreview> LastFiveWorkouts { get; init; } = new();
    public List<FriendNutritionPreview> LastNutritionLogs { get; init; } = new();
    public List<FriendRecordPreview> PersonalRecords { get; init; } = new();
    public List<FriendAchievementPreview> Achievements { get; init; } = new();
}

public sealed record FriendWorkoutPreview
{
    public int WorkoutSessionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime SessionDate { get; init; }
    public int DurationMinutes { get; init; }
}

public sealed record FriendAchievementPreview
{
    public string Name { get; init; } = string.Empty;
    public string ConditionDescription { get; init; } = string.Empty;
    public DateTime AchievedAt { get; init; }
}

public sealed record FriendNutritionPreview
{
    public DateOnly Date { get; init; }
    public double CaloriesKcal { get; init; }
    public double ProteinG { get; init; }
    public double FatG { get; init; }
    public double CarbsG { get; init; }
}

public sealed record FriendRecordPreview
{
    public string ExerciseName { get; init; } = string.Empty;
    public double MaxWeightKg { get; init; }
    public DateTime AchievedAt { get; init; }
}

public sealed record FriendsOverview
{
    public List<FriendCard> Friends { get; init; } = new();
}

public sealed record FriendRequestResponse(FriendshipService.RequestResult Result);

public sealed record FriendWorkoutResponse(string FriendUsername, WorkoutDetailsResponse Workout);

public sealed record LikeRequest(bool Liked);
