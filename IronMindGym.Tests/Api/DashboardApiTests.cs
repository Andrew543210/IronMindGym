using System.Globalization;
using System.Net;
using System.Text.Json;
using IronMindGym.Api;

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
