using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class SplitDay
{
    public int Id { get; set; }
    public int SplitTypeId { get; set; }
    public SplitType SplitType { get; set; } = null!;

    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    public int Position { get; set; }

    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? NameUk { get; set; }

    [Required, MaxLength(80)]
    public string WorkoutName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? WorkoutNameUk { get; set; }
}
