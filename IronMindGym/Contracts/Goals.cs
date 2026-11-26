using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Contracts;

public class GoalRequest
{
    [Required, StringLength(80, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    public DateOnly Deadline { get; set; }
}
