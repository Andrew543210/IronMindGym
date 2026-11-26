using System.ComponentModel.DataAnnotations;
using IronMindGym.Contracts;
using IronMindGym.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Api;

[Route("api/progress")]
public class ProgressController(
    IronMindDbContext db,
    ExerciseProgressService progress,
    FriendshipService friendships,
    StrengthService strength,
    AvatarService avatars) : ApiController
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
        try
        {
            progress.SaveResults(UserId, new ProgressPair(exerciseId,
                request.InitialValue1!.Value, request.InitialValue2!.Value, request.CurrentValue1!.Value, request.CurrentValue2!.Value));
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return Rejected(ex.Message);
        }
    }

    [HttpPut("{exerciseId:int}/current")]
    public ActionResult SaveCurrent(int exerciseId, CurrentResultRequest request)
    {
        try
        {
            progress.SaveCurrent(UserId, exerciseId, request.WeightKg, request.Reps, request.DurationSeconds, request.Intensity);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return Rejected(ex.Message);
        }
    }

    [HttpDelete("{exerciseId:int}")]
    public ActionResult Delete(int exerciseId) => progress.Delete(UserId, exerciseId) ? NoContent() : NotFound();

    [HttpGet("/api/friends/{friendId:int}/progress")]
    public ActionResult<FriendProgressResponse> Friend(int friendId)
    {
        if (!friendships.AreFriends(UserId, friendId))
            return NotFound();
        var friend = db.Users.AsNoTracking().Include(u => u.Benchmarks).Single(u => u.Id == friendId);
        return new FriendProgressResponse(
            friend.Username,
            avatars.CalculateStrengthCoefficient(strength.Current(friendId, friend.Benchmarks), db.WeightLogs.LatestWeighIn(friendId)?.ValueKg),
            progress.List(friendId));
    }
}

public sealed record ProgressResultsRequest(
    [Required] double? InitialValue1,
    [Required] double? InitialValue2,
    [Required] double? CurrentValue1,
    [Required] double? CurrentValue2);

public sealed record CurrentResultRequest(double? WeightKg, int? Reps, int? DurationSeconds, int? Intensity);

public sealed record FriendProgressResponse(string FriendUsername, double CurrentStrengthCoefficient, List<ExerciseProgressEntry> Exercises);
