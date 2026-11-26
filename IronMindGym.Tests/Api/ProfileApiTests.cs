using System.Globalization;
using System.Net;
using System.Text.Json;
using IronMindGym.Api;
using IronMindGym.Contracts;
using IronMindGym.Services;
using IronMindGym.Models;

namespace IronMindGym.Tests.Api;

public class ProfileApiTests
{
    private static async Task<ProfileSettings> Settings(ApiClient api) =>
        (await api.Get<ProfileSettingsResponse>("/api/profile/settings")).Settings;

    private static string Day(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [Fact]
    public async Task OverviewShowsTheAccountBodyRankAndWeighIns()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.RegisterWithSetup();
        var today = (await api.Get<SessionResponse>("/api/session")).Today;
        await ApiClient.Ok(await api.Put($"/api/profile/weights/{Day(today.AddDays(-1))}", new { weightKg = 79.5 }));

        var profile = await api.Get<ProfileOverview>("/api/profile");

        Assert.Equal(("athlete", "athlete@example.com", 24), (profile.Username, profile.Email, profile.Age));
        Assert.Equal((80.0, 180.0), (profile.WeightKg!.Value, profile.HeightCm!.Value));
        Assert.Equal((60.0, 80.0, 100.0), (profile.BenchPressKg!.Value, profile.SquatKg!.Value, profile.DeadliftKg!.Value));
        Assert.Equal(3, profile.StrengthCoefficient);
        Assert.Equal("Gym Centurion", profile.AvatarName);
        Assert.Equal(today, profile.WeighedOn);
        Assert.Equal([80, 79.5], profile.WeightHistory.Select(w => w.ValueKg));
        Assert.Contains(profile.Achievements, a => a.Name == "Fully Set");
    }

    [Fact]
    public async Task EditingTargetsDoesNotInventAWeighIn()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.RegisterWithSetup();
        var settings = await Settings(api);
        var weighIns = app.Query(db => db.WeightLogs.Count());
        settings.CalorieTarget = 2600;

        await ApiClient.Ok(await api.Put("/api/profile/settings", settings));

        Assert.Equal(2600, app.Query(db => db.Profiles.Single().CalorieTarget));
        Assert.Equal(weighIns, app.Query(db => db.WeightLogs.Count()));
    }

    [Fact]
    public async Task ChangingBodyweightAndHeightRecordsTodaysWeighInAndTheHeight()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.RegisterWithSetup();
        var settings = await Settings(api);
        settings.WeightKg = 81.5;
        settings.HeightCm = 181;
        settings.SplitTypeId = app.Query(db => db.SplitTypes.Single(s => s.Code == SplitTypeCode.LegsBackChest).Id);
        settings.WorkoutsPerWeek = 5;

        await ApiClient.Ok(await api.Put("/api/profile/settings", settings));

        Assert.Equal(81.5, app.Query(db => db.WeightLogs.Single().ValueKg));
        Assert.Equal(181, app.Query(db => db.Profiles.Single().HeightCm));
        var plan = app.Query(db => db.WorkoutPlans.Single(p => p.IsActive));
        Assert.Equal((settings.SplitTypeId, 5), (plan.SplitTypeId, plan.WorkoutsPerWeek));
        Assert.Equal(81.5, (await Settings(api)).WeightKg);
    }

    [Fact]
    public async Task SettingsRejectUnknownZonesSplitsAndTakenUsernames()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        using var other = app.Api();
        await api.RegisterWithSetup();
        await other.Register("taken");
        var settings = await Settings(api);

        settings.TimeZone = "Mars/Olympus";
        Assert.Contains("Enter a time zone such as Europe/Kyiv, or leave it empty to use UTC.", await ApiClient.Errors(await api.Put("/api/profile/settings", settings)));
        settings.TimeZone = null;
        settings.SplitTypeId = 999;
        Assert.Contains("Choose a valid training split.", await ApiClient.Errors(await api.Put("/api/profile/settings", settings)));
        settings.SplitTypeId = app.Query(db => db.SplitTypes.First().Id);
        settings.Username = "TAKEN";
        Assert.Contains("This username is already taken.", await ApiClient.Errors(await api.Put("/api/profile/settings", settings)));
        settings.Username = "bad name!";
        Assert.NotEmpty(await ApiClient.Errors(await api.Put("/api/profile/settings", settings)));

        Assert.Null(app.Query(db => db.Profiles.Single(p => p.User.Username == "athlete").TimeZone));
    }

    [Fact]
    public async Task AnAthleteAheadOfUtcLogsWeightForTheirOwnToday()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.RegisterWithSetup();
        var settings = await Settings(api);
        settings.TimeZone = "Pacific/Kiritimati";
        await ApiClient.Ok(await api.Put("/api/profile/settings", settings));
        var localToday = AthleteCalendar.FromId("Pacific/Kiritimati").Today;

        await ApiClient.Ok(await api.Put($"/api/profile/weights/{Day(localToday)}", new { weightKg = 79.6 }));

        Assert.Equal(79.6, app.Query(db => db.WeightLogs.Single(w => w.Date == localToday).ValueKg));
        Assert.Equal(79.6, (await api.Get<ProfileOverview>("/api/profile")).WeightKg);
    }

    [Fact]
    public async Task WeighInsNeedACompletedProfileARealDayAndAPlausibleWeight()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var today = (await api.Get<SessionResponse>("/api/session")).Today;

        Assert.Contains("Complete your profile first.", await ApiClient.Errors(await api.Put($"/api/profile/weights/{Day(today)}", new { weightKg = 80 })));
        await ApiClient.Ok(await api.Post("/api/setup", new { weightKg = 80, heightCm = 180, benchPressKg = 0, squatKg = 0, deadliftKg = 0, workoutsPerWeek = 3, splitTypeId = 1 }));
        Assert.Contains("Choose a date within the last five years, up to today.", await ApiClient.Errors(await api.Put($"/api/profile/weights/{Day(today.AddDays(1))}", new { weightKg = 80 })));
        Assert.Contains("Enter a weight between 30 and 250 kg.", await ApiClient.Errors(await api.Put($"/api/profile/weights/{Day(today)}", new { weightKg = 500 })));

        await ApiClient.Ok(await api.Put($"/api/profile/weights/{Day(today.AddDays(-2))}", new { weightKg = 80 }));
        await ApiClient.Ok(await api.Put($"/api/profile/weights/{Day(today.AddDays(-2))}", new { weightKg = 79 }));
        Assert.Equal(79, app.Query(db => db.WeightLogs.Single(w => w.Date == today.AddDays(-2))).ValueKg);
    }

    [Fact]
    public async Task ChangingThePasswordEndsEverySession()
    {
        using var app = new TestApplication();
        using var first = app.Api();
        using var second = app.Api();
        await first.Register();
        await second.SignIn("athlete@example.com");

        Assert.Contains("Current password is incorrect.", await ApiClient.Errors(await first.Put("/api/profile/password",
            new { currentPassword = "WrongPassword123!", newPassword = "NewTrainingPassword123!", confirmPassword = "NewTrainingPassword123!" })));
        Assert.Contains("Passwords must match.", await ApiClient.Errors(await first.Put("/api/profile/password",
            new { currentPassword = ApiClient.Password, newPassword = "NewTrainingPassword123!", confirmPassword = "Different123!" })));
        await ApiClient.Ok(await first.Put("/api/profile/password",
            new { currentPassword = ApiClient.Password, newPassword = "NewTrainingPassword123!", confirmPassword = "NewTrainingPassword123!" }));

        Assert.Equal(HttpStatusCode.Unauthorized, (await first.Http.GetAsync("/api/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.Http.GetAsync("/api/profile")).StatusCode);
        await second.SignIn("athlete@example.com", "NewTrainingPassword123!");
        Assert.Equal(HttpStatusCode.OK, (await second.Http.GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task GuessingTheCurrentPasswordIsRateLimited()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            statuses.Add((await api.Put("/api/profile/password",
                new { currentPassword = $"WrongPassword{attempt}!", newPassword = "NewTrainingPassword123!", confirmPassword = "NewTrainingPassword123!" })).StatusCode);
        }

        Assert.All(statuses.Take(9), status => Assert.Equal(HttpStatusCode.BadRequest, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[9]);
    }

    [Fact]
    public async Task ExportContainsOnlyTheAthletesOwnRecordsAndNoCredentials()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var other = app.Api();
        await owner.RegisterWithSetup();
        await other.Register("other");
        var today = (await owner.Get<SessionResponse>("/api/session")).Today;
        await ApiClient.Ok(await owner.Put($"/api/nutrition/days/{Day(today)}", new { caloriesKcal = 2100, proteinG = 120, fatG = 60, carbsG = 250 }));
        await ApiClient.Ok(await other.Put($"/api/nutrition/days/{Day(today)}", new { caloriesKcal = 1500, proteinG = 90, fatG = 50, carbsG = 150 }));
        await ApiClient.Ok(await owner.Post("/api/goals", new { title = "Squat 120 kg", deadline = today.AddMonths(2) }));

        var response = await owner.Http.GetAsync("/api/profile/export");
        await ApiClient.Ok(response);
        var text = await response.Content.ReadAsStringAsync();
        var export = JsonDocument.Parse(text).RootElement;

        Assert.Equal("ironmind-export.json", response.Content.Headers.ContentDisposition?.FileName);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("athlete@example.com", export.GetProperty("account").GetProperty("email").GetString());
        Assert.Equal(2100, Assert.Single(export.GetProperty("nutrition").EnumerateArray()).GetProperty("caloriesKcal").GetDouble());
        Assert.Equal("Squat 120 kg", Assert.Single(export.GetProperty("goals").EnumerateArray()).GetProperty("title").GetString());
        Assert.DoesNotContain("other@example.com", text);
        Assert.DoesNotContain("passwordHash", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", text, StringComparison.OrdinalIgnoreCase);
    }
}
