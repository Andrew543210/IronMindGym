using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/workouts")]
public class WorkoutsController(WorkoutService workouts, WorkoutSocialService social) : ApiController
{
    [HttpGet]
    public WorkoutHistoryResponse History(int page = 1) => workouts.History(UserId, page);

    [HttpGet("logger")]
    public WorkoutLoggerResponse Logger() => workouts.LoggerOptions(UserId);

    [HttpGet("previous-set")]
    public ActionResult<PreviousSetResponse> PreviousSet(int exerciseId) =>
        workouts.PreviousSet(UserId, exerciseId) is { } previous ? previous : NoContent();

    [HttpPost]
    [RequestSizeLimit(150_000)]
    public SavedWorkoutResponse Save(SaveWorkoutRequest request) => new(workouts.Save(UserId, request));

    [HttpGet("{id:int}")]
    public ActionResult<WorkoutDetailsResponse> Details(int id) =>
        workouts.Details(UserId, id, UserId) is { } details ? details : NotFound();

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id) => workouts.Delete(UserId, id) ? NoContent() : NotFound();

    [HttpPost("{id:int}/comments")]
    public ActionResult AddComment(int id, CommentRequest request) =>
        social.AddComment(UserId, UserId, id, request.Text) ? NoContent() : NotFound();
}
