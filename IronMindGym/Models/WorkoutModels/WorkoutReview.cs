using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutReview
{
    private string _comment = string.Empty;

    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(800)]
    public string Comment
    {
        get => _comment;
        set => _comment = value.Trim();
    }

    public void UpdateComment(string comment) => Comment = comment;

    public override string ToString() => $"WorkoutReview: Rating={Rating}";
}
