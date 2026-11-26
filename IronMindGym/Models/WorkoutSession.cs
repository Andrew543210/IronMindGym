namespace IronMindGym.Models;

public class WorkoutSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime SessionDate { get; set; } = DateTime.UtcNow;

    /// <summary>The logger draft the workout was saved from, so a repeated save finds the first one.</summary>
    public Guid? DraftId { get; set; }
    public int DurationMinutes { get; set; }

    /// <summary>The split day the workout trained; none for a general training day. It also gives the title.</summary>
    public int? SplitDayId { get; set; }
    public SplitDay? SplitDay { get; set; }
    public WorkoutReview? Review { get; set; }
    public ICollection<PerformedExercise> PerformedExercises { get; set; } = [];
    public ICollection<ActivityLike> Likes { get; set; } = [];
    public ICollection<FriendComment> Comments { get; set; } = [];
}
