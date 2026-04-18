using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Notification
{
    private string _text = string.Empty;

    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(300)]
    public string Text
    {
        get => _text;
        set => _text = (value ?? string.Empty).Trim();
    }

    public bool ReadStatus { get; set; }

    public void MarkAsRead() => ReadStatus = true;

    public override string ToString() => $"Notification: {(ReadStatus ? "Read" : "Unread")}";
}
