using System.Globalization;
using System.Net;
using IronMindGym.Contracts;
using IronMindGym.Models;

namespace IronMindGym.Tests.Api;

public class GoalsApiTests
{
    private static string InTwoMonths() => DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [Fact]
    public async Task GoalsCanBeCompletedReopenedAndDeleted()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        await ApiClient.Ok(await api.Post("/api/goals", new { title = "  Bench 80 kg for 5 reps ", deadline = InTwoMonths() }));
        var goal = Assert.Single(await api.Get<List<GoalResponse>>("/api/goals"));
        Assert.Equal("Bench 80 kg for 5 reps", goal.Title);

        await ApiClient.Ok(await api.Put($"/api/goals/{goal.Id}/completed", new { completed = true }));
        await ApiClient.Ok(await api.Put($"/api/goals/{goal.Id}/completed", new { completed = true }));
        Assert.True(Assert.Single(await api.Get<List<GoalResponse>>("/api/goals")).IsCompleted);
        await ApiClient.Ok(await api.Put($"/api/goals/{goal.Id}/completed", new { completed = false }));
        Assert.False(app.Query(db => db.UserGoals.Single().IsCompleted));

        await ApiClient.Ok(await api.Delete($"/api/goals/{goal.Id}"));
        Assert.Empty(await api.Get<List<GoalResponse>>("/api/goals"));
    }

    [Fact]
    public async Task ADoubleTapOnDeleteRemovesTheGoalOnceAndFindsNothingTheSecondTime()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        await ApiClient.Ok(await api.Post("/api/goals", new { title = "Run 10 km", deadline = InTwoMonths() }));
        var goal = Assert.Single(await api.Get<List<GoalResponse>>("/api/goals"));

        var responses = await Task.WhenAll(api.Delete($"/api/goals/{goal.Id}"), api.Delete($"/api/goals/{goal.Id}"));

        Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NotFound], responses.Select(r => r.StatusCode).Order());
        Assert.Equal(HttpStatusCode.NotFound, (await api.Put($"/api/goals/{goal.Id}/completed", new { completed = true })).StatusCode);
    }

    [Fact]
    public async Task GoalsNeedATitleAndADeadlineWithinFiveYears()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();

        Assert.NotEmpty(await ApiClient.Errors(await api.Post("/api/goals", new { title = "ab", deadline = InTwoMonths() })));
        var past = await api.Post("/api/goals", new { title = "Run 5 km", deadline = "2000-01-01" });
        Assert.Contains("Choose a deadline between today and five years from now.", await ApiClient.Errors(past));
        Assert.Equal(0, app.Query(db => db.UserGoals.Count()));
    }

    [Fact]
    public async Task AnAthleteHasAtMostTwentyActiveGoals()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        app.Query(db =>
        {
            var userId = db.Users.Single().Id;
            db.UserGoals.AddRange(Enumerable.Range(1, 20).Select(n => new UserGoal { UserId = userId, Title = $"Goal {n}", Deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1) }));
            return db.SaveChanges();
        });

        var refused = await api.Post("/api/goals", new { title = "One more goal", deadline = InTwoMonths() });
        Assert.Contains("Finish or remove an existing goal before adding more. You can have 20 active goals.", await ApiClient.Errors(refused));

        var first = app.Query(db => db.UserGoals.OrderBy(g => g.Id).First().Id);
        await ApiClient.Ok(await api.Put($"/api/goals/{first}/completed", new { completed = true }));
        await ApiClient.Ok(await api.Post("/api/goals", new { title = "One more goal", deadline = InTwoMonths() }));
        Assert.Equal(21, app.Query(db => db.UserGoals.Count()));

        // Reopening the finished goal would make 21 active goals.
        var reopened = await api.Put($"/api/goals/{first}/completed", new { completed = false });
        Assert.Contains("Finish or remove an existing goal before adding more. You can have 20 active goals.", await ApiClient.Errors(reopened));
        Assert.Equal(20, app.Query(db => db.UserGoals.Count(g => !g.IsCompleted)));
    }

    [Fact]
    public async Task ParallelRequestsCannotPassTheGoalLimit()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        app.Query(db =>
        {
            var userId = db.Users.Single().Id;
            db.UserGoals.AddRange(Enumerable.Range(1, 19).Select(n => new UserGoal { UserId = userId, Title = $"Goal {n}", Deadline = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1) }));
            return db.SaveChanges();
        });

        await Task.WhenAll(Enumerable.Range(1, 4).Select(n => api.Post("/api/goals", new { title = $"Parallel goal {n}", deadline = InTwoMonths() })));

        Assert.Equal(20, app.Query(db => db.UserGoals.Count(g => !g.IsCompleted)));
    }

    [Fact]
    public async Task GoalsCannotBeChangedByAnotherUser()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var other = app.Api();
        await owner.Register("owner");
        await other.Register("other");
        await ApiClient.Ok(await owner.Post("/api/goals", new { title = "Squat 120 kg", deadline = InTwoMonths() }));
        var id = app.Query(db => db.UserGoals.Single().Id);

        Assert.Equal(HttpStatusCode.NotFound, (await other.Put($"/api/goals/{id}/completed", new { completed = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Delete($"/api/goals/{id}")).StatusCode);
        Assert.Empty(await other.Get<List<GoalResponse>>("/api/goals"));
        Assert.False(app.Query(db => db.UserGoals.Single().IsCompleted));
    }
}
