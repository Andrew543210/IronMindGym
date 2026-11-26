using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

/// <summary>One training day of a split, such as the legs day of legs-back-chest.</summary>
public class SplitDay
{
    public int Id { get; set; }
    public int SplitTypeId { get; set; }
    public SplitType SplitType { get; set; } = null!;

    /// <summary>The stable key; the names are only shown.</summary>
    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    /// <summary>The place of the day in its split, starting at 1.</summary>
    public int Position { get; set; }

    /// <summary>The day as it is chosen in the logger, for example "Legs".</summary>
    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? NameUk { get; set; }

    /// <summary>The title of a workout logged on this day, for example "Legs day".</summary>
    [Required, MaxLength(80)]
    public string WorkoutName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? WorkoutNameUk { get; set; }

    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = [];
}
