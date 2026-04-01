using IronMindGym.Models;
using System.ComponentModel.DataAnnotations;

namespace IronMindGym.ViewModels;

public class WorkoutLoggerViewModel
{
    [Required, MaxLength(120)]
    public string SessionName { get; set; } = "Силове тренування";

    [Range(10, 240)]
    public int DurationMinutes { get; set; } = 60;

    [Required]
    public int ExerciseId { get; set; }

    [Range(1, 12)]
    public int SetsCount { get; set; } = 3;

    [Range(0, 500)]
    public double WeightKg { get; set; } = 60;

    [Range(1, 50)]
    public int Reps { get; set; } = 8;

    [Required]
    public int SetTypeId { get; set; }

    public List<WorkoutSession> RecentSessions { get; set; } = new();
}

