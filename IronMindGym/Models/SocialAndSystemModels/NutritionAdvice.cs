using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class NutritionAdvice
{
    private string _adviceText = string.Empty;
    private string _type = string.Empty;

    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string AdviceText
    {
        get => _adviceText;
        set => _adviceText = (value ?? string.Empty).Trim();
    }

    [MaxLength(40)]
    public string Type
    {
        get => _type;
        set => _type = (value ?? string.Empty).Trim();
    }

    public void Update(string adviceText, string type)
    {
        AdviceText = adviceText;
        Type = type;
    }

    public override string ToString() => $"NutritionAdvice: {Type}";
}
