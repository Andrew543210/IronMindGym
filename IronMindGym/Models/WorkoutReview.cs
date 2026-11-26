using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutReview
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public int Rating { get; set; }

    [MaxLength(800)]
    public string Comment { get; set; } = string.Empty;
}
