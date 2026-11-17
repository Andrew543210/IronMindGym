using System.Net;
using IronMindGym.Models;
using IronMindGym.Services;
using static IronMindGym.Tests.TestApplication;

namespace IronMindGym.Tests;

public class ProductTests
{
    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/WorkoutLogger?startTraining=true")]
    [InlineData("/ExerciseLibrary?search=deadlift")]
    [InlineData("/ProgressTracker")]
    [InlineData("/ProgressTracker/TrackExercise")]
    [InlineData("/Nutrition")]
    [InlineData("/Statistics")]
    [InlineData("/Statistics/Records")]
    [InlineData("/Goals")]
    [InlineData("/Awards")]
    [InlineData("/Friends")]
    [InlineData("/Profile")]
    [InlineData("/Profile/Edit")]
    [InlineData("/Profile/ChangePassword")]
    public async Task EveryMainPageRendersForANewAccount(string route)
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route)).StatusCode);
    }

    [Fact]
    public async Task GoalsCannotBeChangedByAnotherUser()
    {
        using var app = new TestApplication();
        using var owner = app.Browser();
        using var other = app.Browser();
        await Register(owner);
        await Register(other, "other");
        var created = await Post(owner, "/Goals", "/Goals", new()
        {
            ["Title"] = "Train three times this week",
            ["Deadline"] = DateTime.UtcNow.Date.AddDays(7).ToString("yyyy-MM-dd")
        });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var id = app.Query(db => db.UserGoals.Single().Id);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(other, "/Goals", "/Goals/Toggle", new()
        {
            ["id"] = id.ToString()
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await Post(owner, "/Goals", "/Goals/Toggle", new()
        {
            ["id"] = id.ToString()
        })).StatusCode);
        Assert.True(app.Query(db => db.UserGoals.Single().IsCompleted));
    }

    [Fact]
    public void PersonalNutritionTargetsOverrideEstimatesAndCaloriesMatchMacroBudget()
    {
        var service = new NutritionAdviceService();
        var result = service.BuildAdvice(new DailyNutrition(), new NutritionProfileSnapshot
        {
            Age = 24,
            WeightKg = 80,
            HeightCm = 180,
            EnergyEquation = "Male",
            WorkoutsPerWeek = 3
        }, []);
        Assert.InRange(Math.Abs(result.TargetProteinG * 4 + result.TargetFatG * 9 + result.TargetCarbsG * 4 - result.TargetCaloriesKcal), 0, 4);
        var personal = service.BuildAdvice(new DailyNutrition(), new NutritionProfileSnapshot { CalorieTarget = 2400, ProteinTarget = 140, FatTarget = 70, CarbsTarget = 280 }, []);
        Assert.Equal(2400, personal.TargetCaloriesKcal);
        Assert.Equal(140, personal.TargetProteinG);
        var minor = service.BuildAdvice(new DailyNutrition(), new NutritionProfileSnapshot { Age = 15, WeightKg = 65, HeightCm = 170, EnergyEquation = "Male" }, []);
        Assert.Equal(0, minor.BasalMetabolicRate);
    }

    [Fact]
    public async Task AuthenticationEndpointEnforcesRateLimit()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        HttpResponseMessage? response = null;
        for (var i = 0; i < 11; i++)
            response = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.TooManyRequests, response!.StatusCode);
    }
}
