using System.Net;
using System.Net.Http.Json;
using IronMindGym.Contracts;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Tests.Api;

public class FriendsApiTests
{
    [Fact]
    public async Task FriendDataRequiresAnAcceptedRequestAndRemovalRevokesAccess()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var partner = app.Api();
        await owner.Register("owner");
        await partner.Register("partner");
        var (ownerId, partnerId) = (UserId(app, "owner"), UserId(app, "partner"));
        var sessionId = AddWorkout(app, ownerId);
        var route = $"/api/friends/{ownerId}/workouts/{sessionId}";

        Assert.Equal(HttpStatusCode.NotFound, (await partner.Http.GetAsync(route)).StatusCode);
        var search = await partner.Get<FriendsOverview>("/api/friends?search=OWN");
        var card = Assert.Single(search.Friends);
        Assert.Equal(("owner", false, false, false), (card.Username, card.IsFriend, card.RequestSent, card.RequestReceived));
        Assert.Empty(card.LastFiveWorkouts);

        Assert.Equal(FriendRequestResult.Sent, await Request(partner, ownerId));
        Assert.Equal(HttpStatusCode.NotFound, (await partner.Http.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await partner.Post($"/api/friends/{ownerId}/accept", new { })).StatusCode);
        Assert.True((await owner.Get<FriendsOverview>("/api/friends")).Friends.Single().RequestReceived);

        await ApiClient.Ok(await owner.Post($"/api/friends/{partnerId}/accept", new { }));
        var friendWorkout = await partner.Get<FriendWorkoutResponse>(route);
        Assert.Equal(("owner", "Legs day", false), (friendWorkout.FriendUsername, friendWorkout.Workout.Name, friendWorkout.Workout.IsOwner));
        Assert.Null(friendWorkout.Workout.RemainingWorkoutsThisWeek);

        await ApiClient.Ok(await owner.Delete($"/api/friends/{partnerId}"));
        Assert.Equal(HttpStatusCode.NotFound, (await partner.Http.GetAsync(route)).StatusCode);
        Assert.Empty((await partner.Get<FriendsOverview>("/api/friends")).Friends);
        Assert.Equal(HttpStatusCode.NotFound, (await partner.Delete($"/api/friends/{ownerId}")).StatusCode);
    }

    [Fact]
    public async Task SearchMatchesUnderscoresLiterally()
    {
        using var app = new TestApplication();
        using var searcher = app.Api();
        using var other = app.Api();
        using var similar = app.Api();
        await searcher.Register("searcher");
        await other.Register("iron_lifter");
        await similar.Register("ironXlifter");

        var found = await searcher.Get<FriendsOverview>("/api/friends?search=N_L");

        Assert.Equal(["iron_lifter"], found.Friends.Select(f => f.Username));
    }

    [Fact]
    public async Task FriendCardShowsTheLatestFiveWorkoutsNewestFirst()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var partner = app.Api();
        await owner.Register("owner");
        await partner.Register("partner");
        await Connect(app, owner, partner);
        var ownerId = UserId(app, "owner");
        var newestFirst = Enumerable.Range(1, 7).Select(day => AddWorkout(app, ownerId, DateTime.UtcNow.AddDays(-day))).ToList();

        var card = (await partner.Get<FriendsOverview>("/api/friends")).Friends.Single();

        Assert.True(card.IsFriend);
        Assert.Equal(newestFirst.Take(5), card.LastFiveWorkouts.Select(w => w.Id));
    }

    [Fact]
    public async Task RequestsInBothDirectionsConnectAndParallelRequestsKeepOneRow()
    {
        using var app = new TestApplication();
        using var first = app.Api();
        using var second = app.Api();
        await first.Register("first");
        await second.Register("second");

        var parallel = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Request(first, UserId(app, "second"))));
        Assert.Equal(1, app.Query(db => db.Friendships.Count()));
        Assert.Contains(FriendRequestResult.Sent, parallel);
        Assert.Equal(FriendRequestResult.Connected, await Request(second, UserId(app, "first")));
        Assert.Equal(FriendRequestResult.AlreadyConnected, await Request(second, UserId(app, "first")));

        Assert.Equal(FriendshipStatus.Accepted, app.Query(db => db.Friendships.Single().Status));
        var self = await first.Post($"/api/friends/{UserId(app, "first")}/request", new { });
        Assert.Contains("Choose another registered user.", await ApiClient.Errors(self));
    }

    [Fact]
    public async Task FriendsLikeOnceAndCommentWhileTheOwnerCannotLike()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var partner = app.Api();
        await owner.Register("owner");
        await partner.Register("partner");
        await Connect(app, owner, partner);
        var ownerId = UserId(app, "owner");
        var sessionId = AddWorkout(app, ownerId);
        var route = $"/api/friends/{ownerId}/workouts/{sessionId}";

        await ApiClient.Ok(await partner.Put($"{route}/like", new { liked = true }));
        await ApiClient.Ok(await partner.Put($"{route}/like", new { liked = true }));
        Assert.Equal(1, app.Query(db => db.ActivityLikes.Count()));
        Assert.True((await partner.Get<FriendWorkoutResponse>(route)).Workout.IsLikedByCurrentUser);
        await ApiClient.Ok(await partner.Put($"{route}/like", new { liked = false }));
        Assert.Equal(0, app.Query(db => db.ActivityLikes.Count()));
        Assert.Equal(HttpStatusCode.NotFound, (await owner.Put($"{route}/like", new { liked = true })).StatusCode);

        Assert.Equal(["Write a comment first."], await ApiClient.Errors(await partner.Post($"{route}/comments", new { text = "   " })));
        await ApiClient.Ok(await partner.Post($"{route}/comments", new { text = "Strong session" }));
        var comments = (await owner.Get<WorkoutDetailsResponse>($"/api/workouts/{sessionId}")).Comments;
        Assert.Equal(("partner", "Strong session"), (comments.Single().AuthorUsername, comments.Single().Text));

        await ApiClient.Ok(await owner.Delete($"/api/friends/{UserId(app, "partner")}"));
        Assert.Equal(HttpStatusCode.NotFound, (await partner.Post($"{route}/comments", new { text = "Still here?" })).StatusCode);
        Assert.Equal(1, app.Query(db => db.FriendComments.Count()));
    }

    [Fact]
    public async Task RemovingAnAthleteTakesTheirFriendshipsLikesCommentsAndWorkoutsWithThem()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var partner = app.Api();
        using var third = app.Api();
        await owner.Register("owner");
        await partner.Register("partner");
        await third.Register("third");
        await Connect(app, owner, partner);
        // The partner is the second athlete of one pair and the requester of the other.
        Assert.Equal(FriendRequestResult.Sent, await Request(partner, UserId(app, "third")));
        var (ownerId, partnerId) = (UserId(app, "owner"), UserId(app, "partner"));
        var ownerWorkout = $"/api/friends/{ownerId}/workouts/{AddWorkout(app, ownerId)}";
        AddWorkout(app, partnerId);
        await ApiClient.Ok(await partner.Put($"{ownerWorkout}/like", new { liked = true }));
        await ApiClient.Ok(await partner.Post($"{ownerWorkout}/comments", new { text = "Strong session" }));

        app.Query(db => db.Users.Where(u => u.Id == partnerId).ExecuteDelete());

        Assert.Equal((0, 0, 0, 0, 1), app.Query(db => (db.Friendships.Count(), db.ActivityLikes.Count(), db.FriendComments.Count(),
            db.WorkoutSessions.Count(s => s.UserId == partnerId), db.WorkoutSessions.Count())));
    }

    private static async Task Connect(TestApplication app, ApiClient owner, ApiClient partner)
    {
        Assert.Equal(FriendRequestResult.Sent, await Request(partner, UserId(app, "owner")));
        await ApiClient.Ok(await owner.Post($"/api/friends/{UserId(app, "partner")}/accept", new { }));
    }

    private static async Task<FriendRequestResult> Request(ApiClient api, int userId)
    {
        var response = await api.Post($"/api/friends/{userId}/request", new { });
        await ApiClient.Ok(response);
        return (await response.Content.ReadFromJsonAsync<FriendRequestResponse>(ApiClient.Json))!.Result;
    }

    private static int UserId(TestApplication app, string username) =>
        app.Query(db => db.Users.Single(u => u.Username == username).Id);

    private static int AddWorkout(TestApplication app, int userId, DateTime? date = null) => app.Query(db =>
    {
        var legs = db.SplitDays.Single(d => d.Code == "LEGS").Id;
        var session = new WorkoutSession { UserId = userId, SplitDayId = legs, SessionDate = date ?? DateTime.UtcNow.AddHours(-1), DurationMinutes = 40 };
        db.WorkoutSessions.Add(session);
        db.SaveChanges();
        return session.Id;
    });
}
