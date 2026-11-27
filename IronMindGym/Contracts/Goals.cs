using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Contracts;

public sealed record GoalRequest
{
    [Required(ErrorMessage = "Enter a goal."), StringLength(80, MinimumLength = 3, ErrorMessage = "A goal must be 3–80 characters long.")]
    public string Title { get; init; } = string.Empty;

    public DateOnly Deadline { get; init; }
}

public sealed record GoalCompletionRequest(bool Completed);

public sealed record GoalResponse(int Id, string Title, DateOnly Deadline, bool IsCompleted);
