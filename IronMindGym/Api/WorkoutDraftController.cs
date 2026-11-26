using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/workouts/draft")]
public class WorkoutDraftController(WorkoutDraftService drafts) : ApiController
{
    [HttpGet]
    public ActionResult<WorkoutDraftResponse> Get() => drafts.Get(UserId) is { } draft ? draft : NoContent();

    [HttpPut]
    [RequestSizeLimit(150_000)]
    public ActionResult Save(WorkoutDraftRequest request)
    {
        drafts.Save(UserId, request.DraftId!.Value, request.Content!.Value);
        return NoContent();
    }

    [HttpDelete]
    public ActionResult Discard(Guid draftId)
    {
        drafts.Discard(UserId, draftId);
        return NoContent();
    }
}
