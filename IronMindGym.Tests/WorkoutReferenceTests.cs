using System.Net;
using System.Text.Json;
using IronMindGym.Models;
using static IronMindGym.Tests.TestApplication;

namespace IronMindGym.Tests;

public class WorkoutReferenceTests
{
    [Fact]
    public async Task PreviousSetReturnsLastSetFromOwnLatestWorkoutOnly()
    {
        using var app = new TestApplication();
        using var owner = app.Browser();
        using var other = app.Browser();
        await Register(owner);
        await Register(other, "other");
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        var setType = app.Query(db => db.SetTypes.First().Id);
        var route = $"/WorkoutLogger/PreviousSet?exerciseId={exercise}";
        Assert.Equal(HttpStatusCode.NoContent, (await owner.GetAsync(route)).StatusCode);
        var response = await Post(owner, "/WorkoutLogger?startTraining=true", "/WorkoutLogger/Finish", new()
        {
            ["SplitDayLabel"] = "Upper",
            ["DurationMinutes"] = "40",
            ["ReviewRating"] = "4",
            ["DraftPayloadJson"] = JsonSerializer.Serialize(new[]
            {
                new { exerciseId = exercise, setTypeId = setType, weightKg = 50, reps = 10, restSeconds = 90 },
                new { exerciseId = exercise, setTypeId = setType, weightKg = 45, reps = 12, restSeconds = 120 }
            })
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var result = JsonDocument.Parse(await owner.GetStringAsync(route));
        Assert.Equal(45, result.RootElement.GetProperty("weightKg").GetDouble());
        Assert.Equal(12, result.RootElement.GetProperty("reps").GetInt32());
        Assert.Equal(120, result.RootElement.GetProperty("restSeconds").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await other.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.GetAsync("/WorkoutLogger/PreviousSet?exerciseId=999999")).StatusCode);
    }

    [Fact]
    public async Task PreviousSetRequiresAuthentication()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        var response = await client.GetAsync("/WorkoutLogger/PreviousSet?exerciseId=1");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/Login", response.Headers.Location!.ToString());
    }
}
