using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class MuscleSubGroup
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? NameUk { get; set; }

    public int MuscleGroupId { get; set; }
    public MuscleGroup MuscleGroup { get; set; } = null!;
    public ICollection<Exercise> Exercises { get; set; } = [];
}
