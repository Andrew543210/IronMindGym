namespace IronMindGym.Models;

public enum FriendshipStatus
{
    Pending,
    Accepted
}

public class Friendship
{
    public int Id { get; set; }
    public int User1Id { get; set; }
    public User User1 { get; set; } = null!;
    public int User2Id { get; set; }
    public User User2 { get; set; } = null!;
    public int RequestedByUserId { get; set; }
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
}
