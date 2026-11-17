using System.ComponentModel.DataAnnotations;
using IronMindGym.Models;

namespace IronMindGym.ViewModels;

public class GoalViewModel
{
    [Required, StringLength(80, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime Deadline { get; set; } = DateTime.UtcNow.Date.AddMonths(1);

    public List<UserGoal> Goals { get; set; } = new();
}
