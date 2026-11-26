using IronMindGym.Contracts;
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
