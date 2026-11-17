using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class NutritionAssessment
{
    private string _systemVerdict = string.Empty;

    public int Id
    {
        get; set;
    }
    public int DailyNutritionId
    {
        get; set;
    }
    public DailyNutrition DailyNutrition { get; set; } = null!;

    [MaxLength(120)]
    public string SystemVerdict
    {
        get => _systemVerdict;
        set => _systemVerdict = (value ?? string.Empty).Trim();
    }

    public int? AdviceId
    {
        get; set;
    }
    public NutritionAdvice? Advice
    {
        get; set;
    }

    public void AttachAdvice(int? adviceId) => AdviceId = adviceId;

    public override string ToString() => $"NutritionAssessment: {SystemVerdict}";
}
