using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Achievement
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ConditionDescription { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NameUk { get; set; }

    [MaxLength(500)]
    public string? ConditionDescriptionUk { get; set; }
}
