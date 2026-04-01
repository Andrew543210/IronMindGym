namespace IronMindGym.ViewModels;

public class FriendCardViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LastFiveWorkoutsCount { get; set; }
    public double AverageKcal { get; set; }
    public List<FriendWorkoutPreviewViewModel> LastFiveWorkouts { get; set; } = new();
}

public class FriendWorkoutPreviewViewModel
{
    public string Name { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public int DurationMinutes { get; set; }
}

public class FriendsViewModel
{
    public string SearchUsername { get; set; } = string.Empty;
    public List<FriendCardViewModel> Friends { get; set; } = new();
}

