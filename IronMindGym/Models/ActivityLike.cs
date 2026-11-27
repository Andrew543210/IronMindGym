namespace IronMindGym.Models;

public class ActivityLike
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
