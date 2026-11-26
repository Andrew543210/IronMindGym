namespace IronMindGym.Models;

public class WorkoutPlan
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int WorkoutsPerWeek { get; set; }
    public bool IsActive { get; set; }
    public int SplitTypeId { get; set; }
    public SplitType SplitType { get; set; } = null!;
}
