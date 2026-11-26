using IronMindGym.Contracts;
using IronMindGym.Data;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/progress")]
public class ProgressController(
    IronMindDbContext db,
    ExerciseProgressService progress,
    FriendshipService friendships,
    StrengthService strength) : ApiController
{
    [HttpGet]
    public List<ExerciseProgressEntry> List() => progress.List(UserId);

    [HttpGet("catalog")]
    public List<ExerciseCatalogItem> Catalog() => ExerciseCatalog.Items(db.Exercises);

    [HttpGet("{exerciseId:int}")]
    public ActionResult<ProgressPair> Results(int exerciseId) => progress.Results(UserId, exerciseId) is { } pair ? pair : NoContent();

    [HttpPut("{exerciseId:int}")]
    public ActionResult SaveResults(int exerciseId, ProgressResultsRequest request)
    {
        progress.SaveResults(UserId, new ProgressPair(exerciseId,
            request.InitialValue1!.Value, request.InitialValue2!.Value, request.CurrentValue1!.Value, request.CurrentValue2!.Value));
        return NoContent();
    }

    [HttpPut("{exerciseId:int}/current")]
    public ActionResult SaveCurrent(int exerciseId, CurrentResultRequest request)
    {
        progress.SaveCurrent(UserId, exerciseId, request.WeightKg, request.Reps, request.DurationSeconds, request.Intensity);
        return NoContent();
    }

    [HttpDelete("{exerciseId:int}")]
    public ActionResult Delete(int exerciseId) => progress.Delete(UserId, exerciseId) ? NoContent() : NotFound();

    [HttpGet("/api/friends/{friendId:int}/progress")]
    public ActionResult<FriendProgressResponse> Friend(int friendId)
    {
        if (!friendships.AreFriends(UserId, friendId))
            return NotFound();
        return new FriendProgressResponse(
            db.Users.Where(u => u.Id == friendId).Select(u => u.Username).Single(),
            strength.Rank(friendId).Coefficient,
            progress.List(friendId));
    }
}
