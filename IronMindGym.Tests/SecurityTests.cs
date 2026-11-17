using System.Net;
using IronMindGym.Security;
using static IronMindGym.Tests.TestApplication;

namespace IronMindGym.Tests;

public class SecurityTests
{
    [Fact]
    public void PasswordsUseSaltAndRejectWrongOrMalformedHashes()
    {
        var first = PasswordHashUtility.Hash("TrainingPassword123!");
        Assert.NotEqual(first, PasswordHashUtility.Hash("TrainingPassword123!"));
        Assert.True(PasswordHashUtility.Verify("TrainingPassword123!", first));
        Assert.False(PasswordHashUtility.Verify("wrong", first));
        Assert.False(PasswordHashUtility.Verify("TrainingPassword123!", "invalid hash"));
    }

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/WorkoutLogger")]
    [InlineData("/ProgressTracker/GetExerciseCatalog")]
    [InlineData("/Profile/Export")]
    public async Task PrivatePagesRequireAuthentication(string route)
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task RegistrationAndAuthenticatedPostsRejectMissingCsrfTokens()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        var response = await client.PostAsync("/Auth/Register", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await Register(client);
        response = await client.PostAsync("/ProgressTracker/DeleteProgress", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmailIsNormalizedAndCaseVariantsCannotRegisterTwice()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var response = await Post(client, "/Auth/Register", "/Auth/Register", new()
        {
            ["Username"] = "another",
            ["Email"] = "ATHLETE@EXAMPLE.COM",
            ["Age"] = "24",
            ["Password"] = "TrainingPassword123!",
            ["ConfirmPassword"] = "TrainingPassword123!"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, app.Query(db => db.Users.Count()));
    }

    [Fact]
    public async Task PasswordChangeRevokesOtherSessions()
    {
        using var app = new TestApplication();
        using var first = app.Browser();
        using var second = app.Browser();
        await Register(first);
        var login = await Post(second, "/Auth/Login", "/Auth/Login", new()
        {
            ["Email"] = "athlete@example.com",
            ["Password"] = "TrainingPassword123!"
        });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var changed = await Post(first, "/Profile/ChangePassword", "/Profile/ChangePassword", new()
        {
            ["CurrentPassword"] = "TrainingPassword123!",
            ["NewPassword"] = "NewTrainingPassword123!",
            ["ConfirmPassword"] = "NewTrainingPassword123!"
        });
        Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);
        var response = await second.GetAsync("/Profile");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/Login", response.Headers.Location!.ToString());
    }
}
