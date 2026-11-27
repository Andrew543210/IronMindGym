using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/progress")]
public class ProgressController(ExerciseProgressService progress, ExerciseCatalog catalog) : ApiController
{
    [HttpGet]
    public Task<List<ExerciseProgressEntry>> List(CancellationToken cancellationToken) => progress.ListAsync(UserId, cancellationToken);

    [HttpGet("catalog")]
    public Task<List<ExerciseCatalogItem>> Catalog(CancellationToken cancellationToken) => catalog.ItemsAsync(cancellationToken);

    [HttpGet("{exerciseId:int}")]
    public async Task<ActionResult<ProgressPair>> Results(int exerciseId, CancellationToken cancellationToken) =>
        await progress.ResultsAsync(UserId, exerciseId, cancellationToken) is { } pair ? pair : NoContent();

    [HttpPut("{exerciseId:int}")]
    public async Task<ActionResult> SaveResults(int exerciseId, ProgressResultsRequest request, CancellationToken cancellationToken)
    {
        await progress.SaveResultsAsync(UserId, new ProgressPair(exerciseId,
            request.InitialValue1!.Value, request.InitialValue2!.Value, request.CurrentValue1!.Value, request.CurrentValue2!.Value), cancellationToken);
        return NoContent();
    }

    [HttpPut("{exerciseId:int}/current")]
    public async Task<ActionResult> SaveCurrent(int exerciseId, CurrentResultRequest request, CancellationToken cancellationToken)
    {
        await progress.SaveCurrentAsync(UserId, exerciseId, request.WeightKg, request.Reps, request.DurationSeconds, request.Intensity, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{exerciseId:int}")]
    public async Task<ActionResult> Delete(int exerciseId, CancellationToken cancellationToken) =>
        await progress.DeleteAsync(UserId, exerciseId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("/api/friends/{friendId:int}/progress")]
    public async Task<ActionResult<FriendProgressResponse>> Friend(int friendId, CancellationToken cancellationToken) =>
        await progress.OfFriendAsync(UserId, friendId, cancellationToken) is { } friend ? friend : NotFound();
}
