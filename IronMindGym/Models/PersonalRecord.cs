namespace IronMindGym.Models;

public class PersonalRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public double MaxWeightKg { get; set; }

    /// <summary>When the record was first reached: the date of the earliest workout with this load.</summary>
    public DateTime AchievedAt { get; set; }
}
