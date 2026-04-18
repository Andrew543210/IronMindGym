using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class PersonalRecord
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    [Range(0, 1000)]
    public double MaxWeightKg { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool TryUpdateIfHigher(double candidateWeight)
    {
        if (candidateWeight < 0)
        {
            throw new ArgumentException("Candidate weight cannot be negative.");
        }

        if (candidateWeight <= MaxWeightKg)
        {
            return false;
        }

        MaxWeightKg = candidateWeight;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public override string ToString() => $"PersonalRecord: ExerciseId={ExerciseId}, Max={MaxWeightKg}kg";
}
