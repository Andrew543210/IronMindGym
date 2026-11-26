using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IronMindGym.Api;
using IronMindGym.Services;
using IronMindGym.Models;

namespace IronMindGym.Tests.Api;

public class WorkoutsApiTests
{
    [Fact]
    public async Task SavedWorkoutUpdatesTheRecordAndShowsItsSets()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.RegisterWithSetup();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);

        var saved = await Save(app, api, [new { exerciseId = bench, setTypeId = SetType(app), weightKg = 75.5, reps = 5, restSeconds = 120 }], comment: "");

        Assert.Equal(75.5, app.Query(db => db.PersonalRecords.Single().MaxWeightKg));
        var review = app.Query(db => db.WorkoutSessions.Select(s => new { s.Rating, s.Notes }).Single());
        Assert.Equal<(int?, string?)>((4, null), (review.Rating, review.Notes));
        var response = await api.Http.GetAsync($"/api/workouts/{saved}");
        var json = await response.Content.ReadAsStringAsync();
        var details = JsonSerializer.Deserialize<WorkoutDetailsResponse>(json, ApiClient.Json)!;
        Assert.True(details.IsOwner);
        Assert.Null(details.Notes);
        Assert.Equal(75.5, Assert.Single(Assert.Single(details.Exercises).Sets).WeightKg);
        Assert.True(Assert.Single(details.ExerciseStats).IsNewRecord);
        // The plan asks for 3 workouts a week and one is done.
        Assert.Equal(2, details.RemainingWorkoutsThisWeek);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ExerciseType.Cardio)]
    [InlineData(ExerciseType.Timed)]
    public async Task DurationSetsPersistAndDoNotCreateStrengthRecords(ExerciseType type)
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == type).Id);

        await Save(app, api, [new { exerciseId = exercise, setTypeId = SetType(app), durationSeconds = 90, intensity = 6, weightKg = 10, reps = 4 }]);

        Assert.Equal(90, app.Query(db => db.Sets.Single().DurationSeconds));
        Assert.Equal(type == ExerciseType.Cardio ? 0 : 10, app.Query(db => db.Sets.Single().WeightKg));
        Assert.Equal(0, app.Query(db => db.Sets.Single().Reps));
        Assert.Equal(0, app.Query(db => db.PersonalRecords.Count()));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("[{\"exerciseId\":999999,\"setTypeId\":1,\"weightKg\":60,\"reps\":5}]")]
    [InlineData("[{\"exerciseId\":1,\"setTypeId\":999999,\"weightKg\":60,\"reps\":5}]")]
    [InlineData("[{\"exerciseId\":1,\"setTypeId\":1,\"weightKg\":-5,\"reps\":5}]")]
    public async Task InvalidSetsAreRejectedWithoutSavingAnything(string sets)
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        var body = JsonDocument.Parse($$"""{"durationMinutes":45,"reviewRating":4,"sets":{{sets.Replace("\"exerciseId\":1,", $"\"exerciseId\":{exercise},")}}}""");

        var response = await api.Post("/api/workouts", body.RootElement);

        Assert.NotEmpty(await ApiClient.Errors(response));
        Assert.Equal(0, app.Query(db => db.WorkoutSessions.Count()));
    }

    [Fact]
    public async Task RepeatedAndParallelSavesOfOneDraftKeepOneWorkout()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        var draftId = Guid.NewGuid();
        object[] sets = [new { exerciseId = exercise, setTypeId = SetType(app), weightKg = 60, reps = 5, restSeconds = 120 }];

        var first = await Save(app, api, sets, draftId: draftId);
        var retry = await Save(app, api, sets, draftId: draftId);
        var parallelDraft = Guid.NewGuid();
        var parallel = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Save(app, api, sets, draftId: parallelDraft)));
        await Save(app, api, sets, draftId: Guid.NewGuid());

        Assert.Equal(first, retry);
        Assert.Single(parallel.Distinct());
        Assert.Equal(3, app.Query(db => db.WorkoutSessions.Count()));
    }

    [Theory]
    [InlineData("LEGS", "Legs day")]
    [InlineData("FULL_BODY", "Full Body day")]
    [InlineData(null, "Training Day")]
    public async Task AWorkoutIsTitledAfterItsSplitDay(string? day, string expected)
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);

        var id = await Save(app, api, [new { exerciseId = exercise, setTypeId = SetType(app), weightKg = 60, reps = 5 }], day);

        Assert.Equal(expected, (await api.Get<WorkoutDetailsResponse>($"/api/workouts/{id}")).Name);
    }

    [Fact]
    public async Task AnUnknownSplitDayIsRefused()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);

        var response = await api.Post("/api/workouts", new
        {
            splitDayId = 999999,
            durationMinutes = 45,
            sets = new[] { new { exerciseId = exercise, setTypeId = SetType(app), weightKg = 60, reps = 5 } }
        });

        Assert.Contains("Choose a valid training day.", await ApiClient.Errors(response));
    }

    [Fact]
    public async Task DeletingWorkoutRecalculatesRecordsAndRejectsOtherOwners()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var stranger = app.Api();
        await owner.Register();
        await stranger.Register("stranger");
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        await Save(app, owner, [new { exerciseId = exercise, setTypeId = SetType(app), weightKg = 60, reps = 8 }]);
        var heavier = await Save(app, owner, [new { exerciseId = exercise, setTypeId = SetType(app), weightKg = 70, reps = 5 }]);

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Delete($"/api/workouts/{heavier}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Http.GetAsync($"/api/workouts/{heavier}")).StatusCode);
        var responses = await Task.WhenAll(owner.Delete($"/api/workouts/{heavier}"), owner.Delete($"/api/workouts/{heavier}"));

        Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NotFound], responses.Select(r => r.StatusCode).Order());
        Assert.Equal(60, app.Query(db => db.PersonalRecords.Single().MaxWeightKg));
    }

    [Fact]
    public async Task HistoryOffersAnOlderPageOnlyWhenOneExists()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var userId = app.Query(db => db.Users.Single().Id);
        AddSessions(app, userId, 20);

        Assert.False((await api.Get<WorkoutHistoryResponse>("/api/workouts")).HasOlderSessions);
        AddSessions(app, userId, 1);
        var first = await api.Get<WorkoutHistoryResponse>("/api/workouts");
        var second = await api.Get<WorkoutHistoryResponse>("/api/workouts?page=2");

        Assert.True(first.HasOlderSessions);
        Assert.Equal(20, first.Sessions.Count);
        Assert.False(second.HasOlderSessions);
        Assert.Single(second.Sessions);
    }

    [Fact]
    public async Task PreviousSetReturnsLastSetFromOwnLatestWorkoutOnly()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var other = app.Api();
        await owner.Register();
        await other.Register("other");
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        var route = $"/api/workouts/previous-set?exerciseId={exercise}";
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Http.GetAsync(route)).StatusCode);

        await Save(app, owner, [
            new { exerciseId = exercise, setTypeId = SetType(app), weightKg = 50, reps = 10, restSeconds = 90 },
            new { exerciseId = exercise, setTypeId = SetType(app), weightKg = 45, reps = 12, restSeconds = 120 }]);

        var previous = await owner.Get<PreviousSetResponse>(route);
        Assert.Equal((45, 12, 120), (previous.WeightKg, previous.Reps, previous.RestSeconds));
        Assert.Equal(AthleteCalendar.Utc.Today, previous.LocalDate);
        Assert.Equal(HttpStatusCode.NoContent, (await other.Http.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Http.GetAsync("/api/workouts/previous-set?exerciseId=999999")).StatusCode);
        using var anonymous = app.Api();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.Http.GetAsync(route)).StatusCode);
    }

    [Fact]
    public async Task LoggerOffersThePlanSplitItsDaysAndWorkingSetsFirst()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        var upperLower = app.Query(db => db.SplitTypes.Single(s => s.Code == SplitTypeCode.UpperLower).Id);
        await api.RegisterWithSetup(splitTypeId: upperLower);

        var logger = await api.Get<WorkoutLoggerResponse>("/api/workouts/logger");

        var split = Assert.Single(logger.SplitTypes);
        Assert.Equal(upperLower, logger.ActiveSplitTypeId);
        Assert.Equal(["Upper", "Lower"], split.Days.Select(day => day.Name));
        Assert.Equal("Working", logger.SetTypes[0].Name);
        Assert.Equal(logger.SetTypes[0].Id, logger.DefaultSetTypeId);
        Assert.Equal(app.Query(db => db.Exercises.Count()), logger.Exercises.Count);
    }

    [Fact]
    public async Task OwnerCanCommentButLongCommentsAndOtherAthletesAreRefused()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var stranger = app.Api();
        await owner.Register();
        await stranger.Register("stranger");
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        var id = await Save(app, owner, [new { exerciseId = exercise, setTypeId = SetType(app), weightKg = 60, reps = 5 }]);

        await ApiClient.Ok(await owner.Post($"/api/workouts/{id}/comments", new { text = "  Felt strong  " }));
        var tooLong = await owner.Post($"/api/workouts/{id}/comments", new { text = new string('a', 501) });
        Assert.Contains("Comments are limited to 500 characters.", await ApiClient.Errors(tooLong));
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Post($"/api/workouts/{id}/comments", new { text = "Hi" })).StatusCode);

        var comment = Assert.Single((await owner.Get<WorkoutDetailsResponse>($"/api/workouts/{id}")).Comments);
        Assert.Equal(("athlete", "Felt strong"), (comment.AuthorUsername, comment.Text));
    }

    private static int SetType(TestApplication app) => app.Query(db => db.SetTypes.First().Id);

    private static async Task<int> Save(TestApplication app, ApiClient api, object[] sets, string? day = "FULL_BODY", Guid? draftId = null, string comment = "Training log")
    {
        var response = await api.Post("/api/workouts", new
        {
            splitDayId = day is null ? (int?)null : app.Query(db => db.SplitDays.Single(d => d.Code == day).Id),
            durationMinutes = 45,
            reviewRating = 4,
            reviewComment = comment,
            draftId,
            sets
        });
        await ApiClient.Ok(response);
        return (await response.Content.ReadFromJsonAsync<SavedWorkoutResponse>(ApiClient.Json))!.Id;
    }

    private static void AddSessions(TestApplication app, int userId, int count) => app.Query(db =>
    {
        for (var i = 0; i < count; i++)
            db.WorkoutSessions.Add(new WorkoutSession { UserId = userId, SessionDate = DateTime.UtcNow.AddDays(-i - 1), DurationMinutes = 40 });
        return db.SaveChanges();
    });
}
