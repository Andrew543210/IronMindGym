using System.ComponentModel.DataAnnotations;
using IronMindGym.Models;

namespace IronMindGym.Contracts;

public sealed record GoalRequest
{
    [Required, StringLength(80, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    public DateOnly Deadline { get; init; }
}

public sealed record GoalCompletionRequest(bool Completed);

public sealed record GoalResponse(int Id, string Title, DateOnly Deadline, bool IsCompleted)
{
    public static GoalResponse From(UserGoal goal) =>
        new(goal.Id, goal.Title, goal.Deadline, goal.IsCompleted);
}
