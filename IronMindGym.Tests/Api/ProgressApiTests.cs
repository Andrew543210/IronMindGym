using System.Net;
using IronMindGym.Contracts;
using IronMindGym.Models;

namespace IronMindGym.Tests.Api;

public class ProgressApiTests
{
    [Fact]
    public async Task SavingBothColumnsIsAtomicAndAnInvalidCurrentResultKeepsTheOldValues()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        Task<HttpResponseMessage> Save(double initial, double currentReps) =>
            api.Put($"/api/progress/{exercise}", new { initialValue1 = initial, initialValue2 = 8, currentValue1 = 60, currentValue2 = currentReps });

        Assert.Contains("Reps must be between 1 and 500.", await ApiClient.Errors(await Save(40, 501)));
        Assert.Equal(0, app.Query(db => db.ExerciseProgress.Count()));
        Assert.Equal(HttpStatusCode.NoContent, (await api.Http.GetAsync($"/api/progress/{exercise}")).StatusCode);
        await ApiClient.Ok(await Save(40, 8));
        Assert.NotEmpty(await ApiClient.Errors(await Save(50, 1.5)));
        Assert.NotEmpty(await ApiClient.Errors(await api.Put($"/api/progress/{exercise}", new { initialValue1 = 50 })));

        var saved = await api.Get<ProgressResultsRequest>($"/api/progress/{exercise}");
        Assert.Equal((40, 8, 60, 8), (saved.InitialValue1!.Value, saved.InitialValue2!.Value, saved.CurrentValue1!.Value, saved.CurrentValue2!.Value));
    }

    [Fact]
    public async Task ListShowsRepsForBodyweightAndEffortForCardioAndCurrentResultsCanBeReplaced()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var pushUps = app.Query(db => db.Exercises.Single(e => e.Name == "Push-Ups").Id);
        var cardio = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.Cardio).Id);
        await ApiClient.Ok(await api.Put($"/api/progress/{pushUps}", new { initialValue1 = 0, initialValue2 = 10, currentValue1 = 0, currentValue2 = 15 }));
        await ApiClient.Ok(await api.Put($"/api/progress/{cardio}", new { initialValue1 = 60, initialValue2 = 5, currentValue1 = 60, currentValue2 = 10 }));

        await ApiClient.Ok(await api.Put($"/api/progress/{cardio}/current", new { durationSeconds = 90, intensity = 7 }));
        var entries = await api.Get<List<ExerciseProgressEntry>>("/api/progress");

        var bodyweight = entries.Single(e => e.ExerciseId == pushUps);
        Assert.Equal((5, 0.0), (bodyweight.RepsProgressCount, bodyweight.WeightProgressKg));
        var run = entries.Single(e => e.ExerciseId == cardio);
        Assert.Equal((30, 2), (run.CardioDurationProgressSeconds, run.CardioIntensityProgressCount));
        var other = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining && e.Id != pushUps).Id);
        Assert.Contains("Save initial results first.", await ApiClient.Errors(await api.Put($"/api/progress/{other}/current", new { weightKg = 50, reps = 5 })));
    }

    [Fact]
    public async Task OnlyTheOwnerDeletesAndOnlyFriendsSeeTheTracker()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var other = app.Api();
        await owner.RegisterWithSetup("owner");
        await other.Register("other");
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        await ApiClient.Ok(await owner.Put($"/api/progress/{exercise}", new { initialValue1 = 40, initialValue2 = 8, currentValue1 = 45, currentValue2 = 8 }));
        var ownerId = app.Query(db => db.Users.Single(u => u.Username == "owner").Id);
        var otherId = app.Query(db => db.Users.Single(u => u.Username == "other").Id);

        Assert.Equal(HttpStatusCode.NotFound, (await other.Delete($"/api/progress/{exercise}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Http.GetAsync($"/api/friends/{ownerId}/progress")).StatusCode);
        await ApiClient.Ok(await other.Post($"/api/friends/{ownerId}/request", new { }));
        await ApiClient.Ok(await owner.Post($"/api/friends/{otherId}/accept", new { }));
        var friend = await other.Get<FriendProgressResponse>($"/api/friends/{ownerId}/progress");
        Assert.Equal("owner", friend.FriendUsername);
        Assert.Equal(5, Assert.Single(friend.Exercises).WeightProgressKg);
        Assert.True(friend.CurrentStrengthCoefficient > 0);

        await ApiClient.Ok(await owner.Delete($"/api/progress/{exercise}"));
        Assert.Equal(0, app.Query(db => db.ExerciseProgress.Count()));
    }
}
