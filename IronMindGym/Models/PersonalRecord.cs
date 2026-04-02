using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class PersonalRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    [Range(0, 500)]
    public double MaxWeightKg { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
