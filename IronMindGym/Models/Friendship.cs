using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public static class FriendshipStatus
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
}

/// <summary>One row per pair of athletes, stored with the smaller id first.</summary>
public class Friendship
{
    public int Id { get; set; }
    public int User1Id { get; set; }
    public User User1 { get; set; } = null!;
    public int User2Id { get; set; }
    public User User2 { get; set; } = null!;
    public int RequestedByUserId { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = FriendshipStatus.Pending;
}
