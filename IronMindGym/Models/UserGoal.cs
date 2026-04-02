using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class UserGoal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(80)]
    public string GoalType { get; set; } = string.Empty;

    public DateTime Deadline { get; set; }
    public bool IsCompleted { get; set; }
}
