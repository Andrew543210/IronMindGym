using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class SplitType
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? NameUk { get; set; }

    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = [];
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = [];
}
