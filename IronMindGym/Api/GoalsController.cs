using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/goals")]
public class GoalsController(GoalService goals) : ApiController
{
    [HttpGet]
    public Task<List<GoalResponse>> List(CancellationToken cancellationToken) => goals.ListAsync(UserId, cancellationToken);

    [HttpPost]
    public async Task<NoContentResult> Create(GoalRequest request, CancellationToken cancellationToken)
    {
        await goals.CreateAsync(UserId, request.Title, request.Deadline, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/completed")]
    public async Task<ActionResult> SetCompleted(int id, GoalCompletionRequest request, CancellationToken cancellationToken) =>
        await goals.SetCompletedAsync(UserId, id, request.Completed, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await goals.DeleteAsync(UserId, id, cancellationToken) ? NoContent() : NotFound();
}
