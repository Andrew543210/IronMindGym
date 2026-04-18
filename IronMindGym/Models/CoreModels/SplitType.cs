using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class SplitType
{
    private string _name = string.Empty;

    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Name
    {
        get => _name;
        set => _name = (value ?? string.Empty).Trim();
    }

    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();

    public void Rename(string newName) => Name = newName;

    public override string ToString() => $"SplitType: {Name}";
}
