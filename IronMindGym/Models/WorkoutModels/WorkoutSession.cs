using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutSession
{
    private string _name = string.Empty;

    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Workout session name is required.");
            }

            _name = value.Trim();
        }
    }

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

    public void Rename(string newName) => Name = newName;

    public void SetDuration(int durationMinutes)
    {
        if (durationMinutes < 0)
        {
            throw new ArgumentException("Duration cannot be negative.");
        }

        DurationMinutes = durationMinutes;
    }

    public override string ToString() => $"WorkoutSession: {Name} ({SessionDate:yyyy-MM-dd})";
}
