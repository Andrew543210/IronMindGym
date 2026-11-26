using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/friends")]
public class FriendsController(FriendshipService friendships, WorkoutSocialService social) : ApiController
{
    [HttpGet]
    public FriendsOverview Index(string? search) => friendships.BuildIndex(UserId, search);

    [HttpPost("{userId:int}/request")]
    public FriendRequestResponse SendRequest(int userId) => new(friendships.SendRequest(UserId, userId));

    [HttpPost("{userId:int}/accept")]
    public ActionResult Accept(int userId) => friendships.Accept(UserId, userId) ? NoContent() : NotFound();

    [HttpDelete("{userId:int}")]
    public ActionResult Remove(int userId) => friendships.Remove(UserId, userId) ? NoContent() : NotFound();

    [HttpGet("{friendId:int}/workouts/{sessionId:int}")]
    public ActionResult<FriendWorkoutResponse> Workout(int friendId, int sessionId) =>
        social.FriendWorkout(UserId, friendId, sessionId) is { } workout ? workout : NotFound();

    [HttpPut("{friendId:int}/workouts/{sessionId:int}/like")]
    public ActionResult SetLike(int friendId, int sessionId, LikeRequest request) =>
        social.SetLike(UserId, friendId, sessionId, request.Liked) ? NoContent() : NotFound();

    [HttpPost("{friendId:int}/workouts/{sessionId:int}/comments")]
    public ActionResult AddComment(int friendId, int sessionId, CommentRequest request) =>
        social.AddComment(UserId, friendId, sessionId, request.Text) ? NoContent() : NotFound();
}
