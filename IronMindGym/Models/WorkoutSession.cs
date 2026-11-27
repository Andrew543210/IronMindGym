using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime SessionDate { get; set; }

    /// <summary>The logger draft the workout was saved from, so a repeated save finds the first one.</summary>
    public Guid? DraftId { get; set; }
    public int DurationMinutes { get; set; }

    /// <summary>The split day the workout trained; none for a general training day. It also gives the title.</summary>
    public int? SplitDayId { get; set; }
    public SplitDay? SplitDay { get; set; }

    /// <summary>The athlete's rating of the workout, 1 to 5.</summary>
    public int? Rating { get; set; }

    [MaxLength(800)]
    public string? Notes { get; set; }

    public ICollection<PerformedExercise> PerformedExercises { get; set; } = [];
}
