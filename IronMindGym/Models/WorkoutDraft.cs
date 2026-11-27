namespace IronMindGym.Models;

/// <summary>The unfinished workout of an athlete, kept on the server so another device can continue it.</summary>
public class WorkoutDraft
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid DraftId { get; set; }
    public string Content { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; }
}
