using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Contracts;

public sealed record GoalRequest
{
    [Required, StringLength(80, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    public DateOnly Deadline { get; init; }
}

public sealed record GoalCompletionRequest(bool Completed);

public sealed record GoalResponse(int Id, string Title, DateOnly Deadline, bool IsCompleted);
