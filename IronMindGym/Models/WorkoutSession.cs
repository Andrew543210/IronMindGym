using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime SessionDate { get; set; }

    public Guid? DraftId { get; set; }
    public int DurationMinutes { get; set; }

    public int? SplitDayId { get; set; }
    public SplitDay? SplitDay { get; set; }

    public int? Rating { get; set; }

    [MaxLength(800)]
    public string? Notes { get; set; }

    public ICollection<PerformedExercise> PerformedExercises { get; set; } = [];
}
