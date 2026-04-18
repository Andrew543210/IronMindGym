using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Avatar
{
    private string _name = string.Empty;
    private string _iconPath = string.Empty;

    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Name
    {
        get => _name;
        set => _name = (value ?? string.Empty).Trim();
    }

    [Range(0, 100)]
    public double MinStrengthCoefficient { get; set; }

    [MaxLength(255)]
    public string IconPath
    {
        get => _iconPath;
        set => _iconPath = (value ?? string.Empty).Trim();
    }

    public bool Matches(double coefficient) => coefficient >= MinStrengthCoefficient;

    public override string ToString() => $"Avatar: {Name} ({MinStrengthCoefficient:F2}+ )";
}
