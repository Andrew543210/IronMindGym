namespace IronMindGym.Contracts;

public sealed record FriendCard
{
    public bool IsFriend { get; init; }
    public bool RequestSent { get; init; }
    public bool RequestReceived { get; init; }
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public double AverageKcal { get; init; }
    public List<WorkoutSessionListItem> LastFiveWorkouts { get; init; } = new();
    public List<NutritionLogEntry> LastNutritionLogs { get; init; } = new();
    public List<FriendRecordPreview> PersonalRecords { get; init; } = new();
    public List<ProfileAchievement> Achievements { get; init; } = new();
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

public enum FriendRequestResult { Sent, AlreadyRequested, Connected, AlreadyConnected }

public sealed record FriendRequestResponse(FriendRequestResult Result);

public sealed record FriendWorkoutResponse(string FriendUsername, WorkoutDetailsResponse Workout);

public sealed record LikeRequest(bool Liked);
