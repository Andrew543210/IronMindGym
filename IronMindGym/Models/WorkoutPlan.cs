namespace IronMindGym.Models;

public class WorkoutPlan
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int WorkoutsPerWeek { get; set; }
    public int SplitTypeId { get; set; }
    public SplitType SplitType { get; set; } = null!;
}
