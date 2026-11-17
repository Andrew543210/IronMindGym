using System.Net;
using System.Text.Json;
using IronMindGym.Models;
using IronMindGym.Seed;
using Microsoft.EntityFrameworkCore;
using static IronMindGym.Tests.TestApplication;

namespace IronMindGym.Tests;

public class WorkflowTests
{
    [Fact]
    public async Task WorkoutCanBeSavedWithoutAnOptionalReviewComment()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        var response = await Post(client, "/WorkoutLogger?startTraining=true", "/WorkoutLogger/Finish", new()
        {
            ["SplitDayLabel"] = "Full Body",
            ["DurationMinutes"] = "30",
            ["ReviewRating"] = "4",
            ["ReviewComment"] = "",
            ["ExerciseId"] = "",
            ["SetTypeId"] = "",
            ["WeightKg"] = "",
            ["DraftPayloadJson"] = JsonSerializer.Serialize(new[] { new { exerciseId = exercise, setTypeId = app.Query(db => db.SetTypes.First().Id), weightKg = 25, reps = 10 } })
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(string.Empty, app.Query(db => db.WorkoutReviews.Single().Comment));
    }

    [Fact]
    public async Task NewAccountCanSetUpTrainAndSeeUpdatedStrength()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var split = app.Query(db => db.SplitTypes.First().Id);
        var setup = await Post(client, "/Setup", "/Setup", new()
        {
            ["WeightKg"] = "80.5",
            ["HeightCm"] = "180",
            ["BenchPressKg"] = "60",
            ["SquatKg"] = "80",
            ["DeadliftKg"] = "100",
            ["WorkoutsPerWeek"] = "3",
            ["SplitTypeId"] = split.ToString()
        });
        Assert.Equal(HttpStatusCode.Redirect, setup.StatusCode);
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        var response = await SaveWorkout(app, client, new[] { new { exerciseId = bench, setTypeId = app.Query(db => db.SetTypes.First().Id), weightKg = 75.5, reps = 5, restSeconds = 120 } });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(75.5, app.Query(db => db.PersonalRecords.Single().MaxWeightKg));
        var details = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        Assert.Contains("75.5", await details.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Statistics")).StatusCode);
    }

    [Theory]
    [InlineData(ExerciseType.Cardio)]
    [InlineData(ExerciseType.Timed)]
    public async Task DurationSetsPersistAndDoNotCreateStrengthRecords(ExerciseType type)
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == type).Id);
        var response = await SaveWorkout(app, client, new[] { new { exerciseId = exercise, setTypeId = app.Query(db => db.SetTypes.First().Id), durationSeconds = 90, intensity = 6, weightKg = 10 } });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(90, app.Query(db => db.Sets.Single().DurationSeconds));
        Assert.Equal(type == ExerciseType.Cardio ? 0 : 10, app.Query(db => db.Sets.Single().WeightKg));
        Assert.Equal(0, app.Query(db => db.PersonalRecords.Count()));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"exerciseId\":999999,\"setTypeId\":1,\"weightKg\":60,\"reps\":5}]")]
    [InlineData("[{\"exerciseId\":1,\"setTypeId\":999999,\"weightKg\":60,\"reps\":5}]")]
    public async Task InvalidWorkoutDraftsNeverWritePartialSessions(string json)
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var response = await SaveWorkout(app, client, json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, app.Query(db => db.WorkoutSessions.Count()));
    }

    [Fact]
    public async Task DeletingWorkoutRecalculatesRecordsAndRejectsOtherOwners()
    {
        using var app = new TestApplication();
        using var owner = app.Browser();
        using var stranger = app.Browser();
        await Register(owner);
        await Register(stranger, "stranger");
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        var setType = app.Query(db => db.SetTypes.First().Id);
        await SaveWorkout(app, owner, new[] { new { exerciseId = exercise, setTypeId = setType, weightKg = 60, reps = 8, restSeconds = 120 } });
        await SaveWorkout(app, owner, new[] { new { exerciseId = exercise, setTypeId = setType, weightKg = 70, reps = 5, restSeconds = 120 } });
        var id = app.Query(db => db.WorkoutSessions.OrderByDescending(s => s.Id).First().Id);
        var forbidden = await Post(stranger, "/WorkoutLogger", "/WorkoutLogger/Delete", new()
        {
            ["id"] = id.ToString()
        });
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
        var deleted = await Post(owner, "/WorkoutLogger", "/WorkoutLogger/Delete", new()
        {
            ["id"] = id.ToString()
        });
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.Equal(60, app.Query(db => db.PersonalRecords.Single().MaxWeightKg));
    }

    [Fact]
    public async Task FriendDataRequiresAcceptedRequestAndRemovalRevokesAccess()
    {
        using var app = new TestApplication();
        using var owner = app.Browser();
        using var other = app.Browser();
        await Register(owner, "owner");
        await Register(other, "partner");
        var ownerId = app.Query(db => db.Users.Single(u => u.Username == "owner").Id);
        var otherId = app.Query(db => db.Users.Single(u => u.Username == "partner").Id);
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        await SaveWorkout(app, owner, new[] { new { exerciseId = exercise, setTypeId = app.Query(db => db.SetTypes.First().Id), weightKg = 50, reps = 8, restSeconds = 60 } });
        var sessionId = app.Query(db => db.WorkoutSessions.Single().Id);
        var route = $"/Friends/WorkoutDetails?friendId={ownerId}&workoutSessionId={sessionId}";
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(route)).StatusCode);
        var search = await other.GetStringAsync("/Friends?searchUsername=owner");
        Assert.DoesNotContain("Last 5 workouts", search);
        await Post(other, "/Friends", "/Friends/SendRequest", new()
        {
            ["userId"] = ownerId.ToString()
        });
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(other, "/Friends", "/Friends/Accept", new()
        {
            ["userId"] = ownerId.ToString()
        })).StatusCode);
        await Post(owner, "/Friends", "/Friends/Accept", new()
        {
            ["userId"] = otherId.ToString()
        });
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync(route)).StatusCode);
        await Post(owner, "/Friends", "/Friends/Remove", new()
        {
            ["userId"] = otherId.ToString()
        });
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(route)).StatusCode);
    }

    [Fact]
    public async Task NutritionUpsertsTodaysEntryAndExportContainsOnlyOwnData()
    {
        using var app = new TestApplication();
        using var owner = app.Browser();
        using var other = app.Browser();
        await Register(owner);
        await Register(other, "other");
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Redirect, (await Post(owner, "/Nutrition", "/Nutrition", new()
            {
                ["CaloriesKcal"] = (2000 + i).ToString(),
                ["ProteinG"] = "120",
                ["FatG"] = "60",
                ["CarbsG"] = "250"
            })).StatusCode);
        Assert.Equal(1, app.Query(db => db.DailyNutritions.Count()));
        var id = app.Query(db => db.DailyNutritions.Single().Id);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/Nutrition/Details/{id}")).StatusCode);
        var export = await owner.GetStringAsync("/Profile/Export");
        Assert.Contains("athlete@example.com", export);
        Assert.DoesNotContain("other@example.com", export);
        Assert.DoesNotContain("PasswordHash", export);
        Assert.DoesNotContain("SecurityStamp", export);
    }

    [Fact]
    public void SeedingIsIdempotentAndCatalogIsConsistent()
    {
        using var app = new TestApplication();
        app.Query(db =>
        {
            var counts = (db.Exercises.Count(), db.Avatars.Count(), db.ActivityFeeds.Count());
            DataSeeder.Seed(db);
            Assert.Equal(counts, (db.Exercises.Count(), db.Avatars.Count(), db.ActivityFeeds.Count()));
            Assert.Equal(db.Exercises.Count(), db.Exercises.Select(e => e.Name).Distinct().Count());
            Assert.Equal("Hamstrings", db.Exercises.Include(e => e.MuscleSubGroup).Single(e => e.Name == "Romanian Deadlift").MuscleSubGroup.Name);
            Assert.Equal(ExerciseType.Timed, db.Exercises.Single(e => e.Name == "Weighted Plank").Type);
            Assert.Equal(0, db.Users.Count());
            return true;
        });
    }

    private static Task<HttpResponseMessage> SaveWorkout(TestApplication app, HttpClient client, object sets) =>
        Post(client, "/WorkoutLogger?startTraining=true", "/WorkoutLogger/Finish", new()
        {
            ["SplitTypeId"] = app.Query(db => db.SplitTypes.First().Id).ToString(),
            ["SplitDayLabel"] = "Full Body",
            ["DurationMinutes"] = "45",
            ["ReviewRating"] = "4",
            ["ReviewComment"] = "Training log",
            ["DraftPayloadJson"] = sets is string json ? json : JsonSerializer.Serialize(sets)
        });
}
