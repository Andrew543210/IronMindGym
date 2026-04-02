using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class MuscleGroup
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? NameUk { get; set; }

    public ICollection<MuscleSubGroup> SubGroups { get; set; } = [];
}
