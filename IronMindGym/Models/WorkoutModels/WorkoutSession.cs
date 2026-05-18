using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutSession
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public DateTime SessionDate { get; set; } = DateTime.UtcNow;

    [Range(0, 480)]
    public int DurationMinutes { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? SplitTypeId { get; set; }
    public SplitType? SplitType { get; set; }

    public ICollection<PerformedExercise> PerformedExercises { get; set; } = new List<PerformedExercise>();
    public WorkoutReview? Review { get; set; }
    public ICollection<ActivityLike> Likes { get; set; } = new List<ActivityLike>();
    public ICollection<FriendComment> Comments { get; set; } = new List<FriendComment>();

    // Validation Methods
    public bool IsValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 120;
    public bool IsValidDuration(int minutes) => minutes >= 0 && minutes <= 480;

    public void ValidateAndSetName(string name)
    {
        if (!IsValidName(name))
            throw new ArgumentException("Workout session name must be between 1 and 120 characters.");
        Name = name.Trim();
    }

    public void Rename(string newName) => ValidateAndSetName(newName);

    public void SetDuration(int durationMinutes)
    {
        if (!IsValidDuration(durationMinutes))
            throw new ArgumentException("Duration must be between 0 and 480 minutes.");
        DurationMinutes = durationMinutes;
    }

    public override string ToString() => $"WorkoutSession: {Name} ({SessionDate:yyyy-MM-dd})";
}
