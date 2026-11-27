using System.Globalization;
using System.Net;
using System.Text.Json;
using IronMindGym.Contracts;

namespace IronMindGym.Tests.Api;

public class DashboardApiTests
{
    [Fact]
    public async Task NewAthleteIsAskedToCompleteSetup()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();

        var overview = await api.Get<DashboardResponse>("/api/dashboard");

        Assert.True(overview.NeedsSetup);
        Assert.Equal("athlete", overview.Username);
        Assert.Empty(overview.RecentWorkouts);
    }

    [Fact]
    public async Task TheRankScaleMarksTheCurrentStepAndThisWeeksWorkouts()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        // 60 + 80 + 100 kg at 80 kg bodyweight is a coefficient of 3.
        await api.RegisterWithSetup();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        await ApiClient.Ok(await api.Post("/api/workouts", Journal.Workout((bench, 50, 5))));

        var overview = await api.Get<DashboardResponse>("/api/dashboard");

        Assert.Equal(3, overview.StrengthCoefficient);
        var current = Assert.Single(overview.AvatarScalePoints, p => p.IsCurrent);
        Assert.Equal(overview.AvatarName, current.Name);
        Assert.InRange(overview.CurrentAvatarPositionPercent, 0, 100);
        Assert.Equal(0, overview.AvatarScalePoints[0].MinStrengthCoefficient);
        Assert.Equal(5, overview.AvatarScalePoints[^1].MinStrengthCoefficient);
        Assert.Equal((3, 1), (overview.PlannedWorkoutsPerWeek, overview.CompletedThisWeek));
    }

    [Fact]
    public async Task OverviewShowsPlanGoalsAndResearchNotesWithoutAccountSecrets()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.RegisterWithSetup();
        var deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2));
        await ApiClient.Ok(await api.Post("/api/goals", new { title = "Squat 120 kg", deadline = deadline.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }));

        var response = await api.Http.GetAsync("/api/dashboard");
        var json = await response.Content.ReadAsStringAsync();
        var overview = JsonSerializer.Deserialize<DashboardResponse>(json, ApiClient.Json)!;

        Assert.False(overview.NeedsSetup);
        Assert.Equal(3, overview.PlannedWorkoutsPerWeek);
        Assert.Equal(deadline, Assert.Single(overview.Goals).Deadline);
        Assert.NotEmpty(overview.FeedItems);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);

        var note = await api.Get<JsonElement>($"/api/feed/{overview.FeedItems[0].Id}");
        Assert.Equal(overview.FeedItems[0].Title, note.GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await api.Http.GetAsync("/api/feed/999999")).StatusCode);
    }
}
