using System.Net;
using System.Text.Json;
using IronMindGym.Api;

namespace IronMindGym.Tests.Api;

public class WorkoutDraftApiTests
{
    private const string Route = "/api/workouts/draft";

    private static object Draft(Guid draftId, int sets = 1)
    {
        var set = """{"exerciseId":7,"setTypeId":1,"setTypeName":"Робочий","weightKg":60,"reps":5}""";
        var content = $$$"""
            {"draftId":"{{{draftId}}}","startedAt":1700000000000,"sets":[{{{string.Join(",", Enumerable.Repeat(set, sets))}}}],
             "settings":{"splitDayLabel":"Upper","reviewComment":"Важкий день"}}
            """;
        return new { draftId, content = JsonDocument.Parse(content).RootElement };
    }

    [Fact]
    public async Task AnotherDeviceContinuesTheUnfinishedWorkout()
    {
        using var app = new TestApplication();
        using var phone = app.Api();
        await phone.Register();
        var draftId = Guid.NewGuid();

        await ApiClient.Ok(await phone.Put(Route, Draft(draftId, sets: 3)));
        using var laptop = app.Api();
        await laptop.SignIn("athlete@example.com");
        var draft = await laptop.Get<WorkoutDraftResponse>(Route);

        Assert.Equal(draftId, draft.DraftId);
        Assert.Equal(3, draft.Content.GetProperty("sets").GetArrayLength());
        Assert.Equal("Важкий день", draft.Content.GetProperty("settings").GetProperty("reviewComment").GetString());
        Assert.True(draft.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ANewerCopyReplacesTheOldOne()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();

        await ApiClient.Ok(await api.Put(Route, Draft(Guid.NewGuid(), sets: 1)));
        var newer = Guid.NewGuid();
        await ApiClient.Ok(await api.Put(Route, Draft(newer, sets: 2)));

        Assert.Equal(1, app.Query(db => db.WorkoutDrafts.Count()));
        Assert.Equal(newer, (await api.Get<WorkoutDraftResponse>(Route)).DraftId);
    }

    [Fact]
    public async Task SavingTheWorkoutRemovesItsDraftAndRefusesALateCopy()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var draftId = Guid.NewGuid();
        await ApiClient.Ok(await api.Put(Route, Draft(draftId)));

        await ApiClient.Ok(await api.Post("/api/workouts", new
        {
            splitDayLabel = "Full Body",
            durationMinutes = 45,
            reviewRating = 4,
            draftId,
            sets = new[]
            {
                new
                {
                    exerciseId = app.Query(db => db.Exercises.First(e => e.Type == Models.ExerciseType.StrengthTraining).Id),
                    setTypeId = app.Query(db => db.SetTypes.First().Id),
                    weightKg = 60,
                    reps = 5,
                    restSeconds = 90
                }
            }
        }));

        Assert.Equal(HttpStatusCode.NoContent, (await api.Http.GetAsync(Route)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.Put(Route, Draft(draftId))).StatusCode);
        Assert.Equal(0, app.Query(db => db.WorkoutDrafts.Count()));
    }

    [Fact]
    public async Task DiscardRemovesOnlyTheNamedDraft()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var draftId = Guid.NewGuid();
        await ApiClient.Ok(await api.Put(Route, Draft(draftId)));

        await ApiClient.Ok(await api.Delete($"{Route}?draftId={Guid.NewGuid()}"));
        Assert.Equal(1, app.Query(db => db.WorkoutDrafts.Count()));

        await ApiClient.Ok(await api.Delete($"{Route}?draftId={draftId}"));
        Assert.Equal(0, app.Query(db => db.WorkoutDrafts.Count()));
    }

    [Fact]
    public async Task ADraftIsVisibleOnlyToItsAthlete()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        await owner.Register("owner");
        var draftId = Guid.NewGuid();
        await ApiClient.Ok(await owner.Put(Route, Draft(draftId)));
        using var other = app.Api();
        await other.Register("other");

        Assert.Equal(HttpStatusCode.NoContent, (await other.Http.GetAsync(Route)).StatusCode);
        await ApiClient.Ok(await other.Delete($"{Route}?draftId={draftId}"));
        Assert.Equal(1, app.Query(db => db.WorkoutDrafts.Count()));
        using var anonymous = app.Api();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.Http.GetAsync(Route)).StatusCode);
    }

    [Theory]
    [InlineData("""{"draftId":"0f8fad5b-d9cb-469f-a165-70867728950e","content":[]}""")]
    [InlineData("""{"draftId":"0f8fad5b-d9cb-469f-a165-70867728950e","content":"sets"}""")]
    [InlineData("""{"draftId":"0f8fad5b-d9cb-469f-a165-70867728950e","content":{"note":"a\u0000b"}}""")]
    [InlineData("""{"draftId":"0f8fad5b-d9cb-469f-a165-70867728950e"}""")]
    [InlineData("""{"content":{"sets":[]}}""")]
    public async Task InvalidCopiesAreRejected(string body)
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();

        var response = await api.Put(Route, JsonDocument.Parse(body).RootElement);

        Assert.NotEmpty(await ApiClient.Errors(response));
        Assert.Equal(0, app.Query(db => db.WorkoutDrafts.Count()));
    }

    [Fact]
    public async Task AnOversizedCopyIsRejected()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();

        var response = await api.Put(Route, new
        {
            draftId = Guid.NewGuid(),
            content = new { note = new string('x', WorkoutDraftController.MaxContentLength) }
        });

        Assert.NotEmpty(await ApiClient.Errors(response));
    }
}
