using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class UserGoal
{
    private string _goalType = string.Empty;

    public int Id
    {
        get; set;
    }
    public int UserId
    {
        get; set;
    }
    public User User { get; set; } = null!;

    [MaxLength(80)]
    public string GoalType
    {
        get => _goalType;
        set => _goalType = (value ?? string.Empty).Trim();
    }

    public bool IsCompleted
    {
        get; set;
    }

    public DateTime Deadline
    {
        get; set;
    }

    public bool IsExpired(DateTime now) => now > Deadline;

    public override string ToString() => $"UserGoal: {GoalType}, Deadline={Deadline:yyyy-MM-dd}";
}
