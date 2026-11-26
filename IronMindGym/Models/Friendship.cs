namespace IronMindGym.Models;

public enum FriendshipStatus
{
    Pending,
    Accepted
}

/// <summary>
/// One row per pair of athletes, stored with the smaller id first; the requester is one of the two.
/// Check constraints in the database hold both rules.
/// </summary>
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
