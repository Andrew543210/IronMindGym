using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class HeartRateLog
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    [Range(0, 260)]
    public int HeartRate { get; set; }

    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;

    public bool IsAboveThreshold(int threshold) => HeartRate >= threshold;

    public override string ToString() => $"HeartRateLog: {HeartRate} bpm";
}
