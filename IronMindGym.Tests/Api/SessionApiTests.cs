using System.Net;
using System.Net.Http.Json;
using IronMindGym.Api;

namespace IronMindGym.Tests.Api;

public class SessionApiTests
{
    [Fact]
    public async Task ASignedOutClientHasNoSession()
    {
        using var app = new TestApplication();
        using var api = app.Api();

        Assert.Equal(HttpStatusCode.NoContent, (await api.Http.GetAsync("/api/session")).StatusCode);
    }

    [Fact]
    public async Task RegistrationSignsInAndReportsTheAthletesCalendar()
    {
        using var app = new TestApplication();
        using var api = app.Api();

        await api.Register(timeZone: "Europe/Kyiv");

        var session = await api.Get<SessionResponse>("/api/session");
        Assert.Equal("athlete", session.Username);
        Assert.Equal("Europe/Kyiv", session.TimeZone);
        Assert.False(session.SetupComplete);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", session.Today);
    }

    [Fact]
    public async Task ChangesWithoutTheCsrfHeaderAreRejected()
    {
        using var app = new TestApplication();
        using var api = app.Api();

        var anonymous = await api.Http.PostAsJsonAsync("/api/session/register", new { username = "athlete" });
        Assert.Equal(HttpStatusCode.BadRequest, anonymous.StatusCode);
        await api.Register();
        var signedIn = await api.Http.PostAsync("/api/session/logout", null);
        Assert.Equal(HttpStatusCode.BadRequest, signedIn.StatusCode);
        Assert.Equal("athlete", (await api.Get<SessionResponse>("/api/session")).Username);
    }

    [Fact]
    public async Task EmailIsNormalizedAndCaseVariantsCannotRegisterTwice()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();

        using var other = app.Api();
        var response = await other.Post("/api/session/register", new
        {
            username = "another",
            email = "ATHLETE@EXAMPLE.COM",
            age = 24,
            password = ApiClient.Password,
            confirmPassword = ApiClient.Password
        });

        Assert.Contains("A user with this email already exists.", await ApiClient.Errors(response));
        Assert.Equal(1, app.Query(db => db.Users.Count()));
    }

    [Fact]
    public async Task WrongPasswordIsRejectedAndSignOutEndsTheSession()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        await api.Post("/api/session/logout");

        var wrong = await api.Post("/api/session/login", new { email = "athlete@example.com", password = "WrongPassword123!" });
        Assert.Contains("Invalid email or password.", await ApiClient.Errors(wrong));

        await api.SignIn("Athlete@Example.com");
        Assert.Equal("athlete", (await api.Get<SessionResponse>("/api/session")).Username);
        await ApiClient.Ok(await api.Post("/api/session/logout"));
        Assert.Equal(HttpStatusCode.NoContent, (await api.Http.GetAsync("/api/session")).StatusCode);
    }

    [Fact]
    public async Task SignInAttemptsAreRateLimited()
    {
        using var app = new TestApplication();
        using var api = app.Api();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 11; attempt++)
            statuses.Add((await api.Post("/api/session/login", new { email = "nobody@example.com", password = "WrongPassword123!" })).StatusCode);

        Assert.All(statuses.Take(10), status => Assert.Equal(HttpStatusCode.BadRequest, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }

    [Fact]
    public async Task SetupIsSavedOnceAndCompletesTheSession()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var form = await api.Get<SetupFormResponse>("/api/setup");
        Assert.NotEmpty(form.SplitTypes);
        Assert.False(form.AlreadyCompleted);

        var invalid = await api.Post("/api/setup", new { weightKg = 80, heightCm = 180, workoutsPerWeek = 3, splitTypeId = 9999 });
        Assert.Contains("Choose a valid training split.", await ApiClient.Errors(invalid));
        var setup = new { weightKg = 80.5, heightCm = 180, benchPressKg = 60, squatKg = 80, deadliftKg = 100, workoutsPerWeek = 3, splitTypeId = form.SplitTypes[0].Id };
        await ApiClient.Ok(await api.Post("/api/setup", setup));
        await ApiClient.Ok(await api.Post("/api/setup", setup));

        Assert.True((await api.Get<SessionResponse>("/api/session")).SetupComplete);
        Assert.Equal(1, app.Query(db => db.AnthropometryLogs.Count()));
        Assert.Equal(1, app.Query(db => db.WorkoutPlans.Count(p => p.IsActive)));
    }
}
