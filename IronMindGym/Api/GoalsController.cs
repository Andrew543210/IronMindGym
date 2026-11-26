using IronMindGym.Contracts;
using IronMindGym.Models;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/goals")]
public class GoalsController(GoalService goals) : ApiController
{
    [HttpGet]
    public List<GoalResponse> List() => goals.List(UserId).Select(GoalResponse.From).ToList();

    [HttpPost]
    public NoContentResult Create(GoalRequest request)
    {
        goals.Create(UserId, request.Title, request.Deadline);
        return NoContent();
    }

    [HttpPut("{id:int}/completed")]
    public ActionResult SetCompleted(int id, GoalCompletionRequest request) =>
        goals.SetCompleted(UserId, id, request.Completed) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id) => goals.Delete(UserId, id) ? NoContent() : NotFound();
}

public sealed record GoalCompletionRequest(bool Completed);

public sealed record GoalResponse(int Id, string Title, DateOnly Deadline, bool IsCompleted)
{
    public static GoalResponse From(UserGoal goal) =>
        new(goal.Id, goal.GoalType, goal.Deadline, goal.IsCompleted);
}
