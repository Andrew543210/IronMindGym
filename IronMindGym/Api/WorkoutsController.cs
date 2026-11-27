using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/workouts")]
public class WorkoutsController(WorkoutService workouts, WorkoutSocialService social) : ApiController
{
    [HttpGet]
    public Task<WorkoutHistoryResponse> History(CancellationToken cancellationToken, int page = 1) =>
        workouts.HistoryAsync(UserId, page, cancellationToken);

    [HttpGet("logger")]
    public Task<WorkoutLoggerResponse> Logger(CancellationToken cancellationToken) => workouts.LoggerOptionsAsync(UserId, cancellationToken);

    [HttpGet("previous-set")]
    public async Task<ActionResult<PreviousSetResponse>> PreviousSet(int exerciseId, CancellationToken cancellationToken) =>
        await workouts.PreviousSetAsync(UserId, exerciseId, cancellationToken) is { } previous ? previous : NoContent();

    [HttpPost]
    [RequestSizeLimit(150_000)]
    public async Task<SavedWorkoutResponse> Save(SaveWorkoutRequest request, CancellationToken cancellationToken) =>
        new(await workouts.SaveAsync(UserId, request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkoutDetailsResponse>> Details(int id, CancellationToken cancellationToken) =>
        await workouts.DetailsAsync(UserId, id, UserId, cancellationToken) is { } details ? details : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await workouts.DeleteAsync(UserId, id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult> AddComment(int id, CommentRequest request, CancellationToken cancellationToken) =>
        await social.AddCommentAsync(UserId, UserId, id, request.Text, cancellationToken) ? NoContent() : NotFound();
}
