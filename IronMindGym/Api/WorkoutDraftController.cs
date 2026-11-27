using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/workouts/draft")]
public class WorkoutDraftController(WorkoutDraftService drafts) : ApiController
{
    [HttpGet]
    public async Task<ActionResult<WorkoutDraftResponse>> Get(CancellationToken cancellationToken) =>
        await drafts.GetAsync(UserId, cancellationToken) is { } draft ? draft : NoContent();

    [HttpPut]
    [RequestSizeLimit(150_000)]
    public async Task<ActionResult> Save(WorkoutDraftRequest request, CancellationToken cancellationToken)
    {
        await drafts.SaveAsync(UserId, request.DraftId!.Value, request.Content!.Value, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<ActionResult> Discard(Guid draftId, CancellationToken cancellationToken)
    {
        await drafts.DiscardAsync(UserId, draftId, cancellationToken);
        return NoContent();
    }
}
