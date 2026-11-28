using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public static class SplitTypeCode
{
    public const string FullBody = "FULL_BODY";
    public const string UpperLower = "UPPER_LOWER";
    public const string LegsBackChest = "LEGS_BACK_CHEST";
}

public class SplitType
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? NameUk { get; set; }

    public ICollection<SplitDay> Days { get; set; } = [];
}
