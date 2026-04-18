namespace IronMindGym.ViewModels;

public class AwardsViewModel
{
    public List<AwardProgressViewModel> Awards { get; set; } = new();
}

public class AwardProgressViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConditionDescription { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public bool IsNumeric { get; set; }
    public double CurrentValue { get; set; }
    public double TargetValue { get; set; }
    public double ProgressPercent { get; set; }
}

