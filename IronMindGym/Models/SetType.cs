using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public static class SetTypeCode
{
    public const string Working = "WORKING";
    public const string WarmUp = "WARM_UP";
    public const string Failure = "FAILURE";
}

public class SetType
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? NameUk { get; set; }
}
