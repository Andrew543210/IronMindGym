using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WorkoutPlan
{
    public int Id { get; set; }

    [Range(0, 14)]
    public int WorkoutsPerWeek { get; set; }

    public bool IsActive { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SplitTypeId { get; set; }
    public SplitType SplitType { get; set; } = null!;

    public ICollection<ScheduledDay> ScheduledDays { get; set; } = new List<ScheduledDay>();

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public override string ToString() => $"WorkoutPlan: {WorkoutsPerWeek}/week, Active={IsActive}";
}
