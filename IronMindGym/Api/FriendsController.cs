using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/friends")]
public class FriendsController(FriendshipService friendships, WorkoutSocialService social) : ApiController
{
    [HttpGet]
    public Task<FriendsOverview> Index(string? search, CancellationToken cancellationToken) =>
        friendships.BuildIndexAsync(UserId, search, cancellationToken);

    [HttpPost("{userId:int}/request")]
    public async Task<FriendRequestResponse> SendRequest(int userId, CancellationToken cancellationToken) =>
        new(await friendships.SendRequestAsync(UserId, userId, cancellationToken));

    [HttpPost("{userId:int}/accept")]
    public async Task<ActionResult> Accept(int userId, CancellationToken cancellationToken) =>
        await friendships.AcceptAsync(UserId, userId, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{userId:int}")]
    public async Task<ActionResult> Remove(int userId, CancellationToken cancellationToken) =>
        await friendships.RemoveAsync(UserId, userId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{friendId:int}/workouts/{sessionId:int}")]
    public async Task<ActionResult<FriendWorkoutResponse>> Workout(int friendId, int sessionId, CancellationToken cancellationToken) =>
        await social.FriendWorkoutAsync(UserId, friendId, sessionId, cancellationToken) is { } workout ? workout : NotFound();

    [HttpPut("{friendId:int}/workouts/{sessionId:int}/like")]
    public async Task<ActionResult> SetLike(int friendId, int sessionId, LikeRequest request, CancellationToken cancellationToken) =>
        await social.SetLikeAsync(UserId, friendId, sessionId, request.Liked, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{friendId:int}/workouts/{sessionId:int}/comments")]
    public async Task<ActionResult> AddComment(int friendId, int sessionId, CommentRequest request, CancellationToken cancellationToken) =>
        await social.AddCommentAsync(UserId, friendId, sessionId, request.Text, cancellationToken) ? NoContent() : NotFound();
}
