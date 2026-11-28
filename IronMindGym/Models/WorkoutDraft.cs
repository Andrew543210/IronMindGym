namespace IronMindGym.Models;

public class WorkoutDraft
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid DraftId { get; set; }
    public string Content { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; }
}
