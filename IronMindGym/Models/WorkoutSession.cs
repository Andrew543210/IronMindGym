using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public DateTime SessionDate { get; set; } = DateTime.UtcNow;

    /// <summary>The logger draft the workout was saved from, so a repeated save finds the first one.</summary>
    public Guid? DraftId { get; set; }
    public int DurationMinutes { get; set; }
    public int? SplitTypeId { get; set; }
    public SplitType? SplitType { get; set; }
    public WorkoutReview? Review { get; set; }
    public ICollection<PerformedExercise> PerformedExercises { get; set; } = [];
    public ICollection<ActivityLike> Likes { get; set; } = [];
    public ICollection<FriendComment> Comments { get; set; } = [];
}
