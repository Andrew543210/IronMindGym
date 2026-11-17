using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Friendship
{
    private string _status = "Pending";

    public int Id
    {
        get; set;
    }

    public int RequestedByUserId
    {
        get; set;
    }

    public int User1Id
    {
        get; set;
    }
    public User User1 { get; set; } = null!;

    public int User2Id
    {
        get; set;
    }
    public User User2 { get; set; } = null!;

    [MaxLength(30)]
    public string Status
    {
        get => _status;
        set => _status = string.IsNullOrWhiteSpace(value) ? "Pending" : value.Trim();
    }

    public void Accept() => Status = "Accepted";
    public void Decline() => Status = "Declined";

    public override string ToString() => $"Friendship: {User1Id}<->{User2Id}, {Status}";
}
