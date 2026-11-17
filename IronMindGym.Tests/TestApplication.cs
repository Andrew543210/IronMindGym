using System.Net;
using System.Text.RegularExpressions;
using IronMindGym.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IronMindGym.Tests;

public sealed class TestApplication : WebApplicationFactory<Program>
{
    private readonly string database = Path.Combine(Path.GetTempPath(), $"ironmind-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={database};Pooling=False");
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Error));
    }

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public T Query<T>(Func<IronMindDbContext, T> query)
    {
        using var scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<IronMindDbContext>());
    }

    public static async Task<string> Token(HttpClient client, string page)
    {
        var response = await client.GetAsync(page);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, $"Missing anti-forgery token on {page}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static async Task<HttpResponseMessage> Post(HttpClient client, string page, string action, Dictionary<string, string> values)
    {
        values["__RequestVerificationToken"] = await Token(client, page);
        return await client.PostAsync(action, new FormUrlEncodedContent(values));
    }

    public static async Task Register(HttpClient client, string username = "athlete")
    {
        var response = await Post(client, "/Auth/Register", "/Auth/Register", new()
        {
            ["Username"] = username,
            ["Email"] = $"{username}@example.com",
            ["Age"] = "24",
            ["Password"] = "TrainingPassword123!",
            ["ConfirmPassword"] = "TrainingPassword123!"
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(database))
            File.Delete(database);
    }
}
