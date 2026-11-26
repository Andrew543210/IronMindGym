using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

/// <summary>The codes of the set types the code relies on.</summary>
public static class SetTypeCode
{
    public const string Working = "WORKING";
    public const string WarmUp = "WARM_UP";
    public const string Failure = "FAILURE";
}

public class SetType
{
    public int Id { get; set; }

    /// <summary>The stable key; the names are only shown.</summary>
    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? NameUk { get; set; }
}
