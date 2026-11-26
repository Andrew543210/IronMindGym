using System.Net;

namespace IronMindGym.Tests.Api;

public sealed class HostingTests : IDisposable
{
    private readonly string webRoot = Directory.CreateTempSubdirectory("ironmind-client-").FullName;

    public HostingTests()
    {
        File.WriteAllText(Path.Combine(webRoot, "index.html"), "<!doctype html><div id=\"root\"></div>");
        Directory.CreateDirectory(Path.Combine(webRoot, "assets"));
        File.WriteAllText(Path.Combine(webRoot, "assets", "index-abc123.js"), "console.log('client');");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/login")]
    [InlineData("/workouts/42")]
    public async Task ClientRoutesServeTheReactAppToEveryone(string route)
    {
        using var app = new TestApplication(webRoot);
        using var browser = app.Browser();

        var response = await browser.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"root\"", await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.CacheControl?.NoCache);
    }

    [Fact]
    public async Task BundlesAreCachedForeverAndPagesCarrySecurityHeaders()
    {
        using var app = new TestApplication(webRoot);
        using var browser = app.Browser();

        var bundle = await browser.GetAsync("/assets/index-abc123.js");

        Assert.Equal(HttpStatusCode.OK, bundle.StatusCode);
        Assert.Equal("public, max-age=31536000, immutable", bundle.Headers.CacheControl?.ToString());
        Assert.Equal("nosniff", Assert.Single(bundle.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(bundle.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("strict-origin-when-cross-origin", Assert.Single(bundle.Headers.GetValues("Referrer-Policy")));
        var policy = Assert.Single(bundle.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("script-src 'self'", policy, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", policy, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownApiRoutesAreNotFoundInsteadOfThePage()
    {
        using var app = new TestApplication(webRoot);
        using var browser = app.Browser();

        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync("/api/does-not-exist")).StatusCode);
    }

    [Theory]
    [InlineData("/api/setup")]
    [InlineData("/api/dashboard")]
    [InlineData("/api/workouts")]
    [InlineData("/api/workouts/previous-set?exerciseId=1")]
    [InlineData("/api/progress")]
    [InlineData("/api/nutrition")]
    [InlineData("/api/statistics")]
    [InlineData("/api/awards")]
    [InlineData("/api/exercises")]
    [InlineData("/api/goals")]
    [InlineData("/api/friends")]
    [InlineData("/api/profile")]
    [InlineData("/api/profile/export")]
    public async Task PrivateEndpointsAnswerUnauthorizedWithoutASession(string route)
    {
        using var app = new TestApplication();
        using var browser = app.Browser();

        var response = await browser.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    public void Dispose() => Directory.Delete(webRoot, recursive: true);
}
