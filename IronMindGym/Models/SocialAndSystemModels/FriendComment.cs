using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class FriendComment
{
    private string _text = string.Empty;

    public int Id
    {
        get; set;
    }

    [Required, MaxLength(500)]
    public string Text
    {
        get => _text;
        set => _text = (value ?? string.Empty).Trim();
    }

    public int AuthorId
    {
        get; set;
    }
    public User Author { get; set; } = null!;

    public int WorkoutSessionId
    {
        get; set;
    }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    public DateTime Date { get; set; } = DateTime.UtcNow;
}

