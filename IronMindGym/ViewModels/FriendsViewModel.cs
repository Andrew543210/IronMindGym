namespace IronMindGym.ViewModels;

public class FriendCardViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LastFiveWorkoutsCount { get; set; }
}

public class FriendsViewModel
{
    public string SearchUsername { get; set; } = string.Empty;
    public List<FriendCardViewModel> Friends { get; set; } = new();
}

