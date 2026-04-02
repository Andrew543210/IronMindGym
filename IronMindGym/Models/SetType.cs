using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class SetType
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? NameUk { get; set; }

    public ICollection<WorkoutSet> Sets { get; set; } = [];
}
