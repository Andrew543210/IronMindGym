namespace IronMindGym.Models;

/// <summary>The athlete's weekly plan; one per user, keyed by the user id.</summary>
public class WorkoutPlan
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int WorkoutsPerWeek { get; set; }
    public int SplitTypeId { get; set; }
    public SplitType SplitType { get; set; } = null!;
}
