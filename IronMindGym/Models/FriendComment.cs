using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class FriendComment
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    [Required, MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
