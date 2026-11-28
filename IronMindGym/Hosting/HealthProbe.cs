using IronMindGym.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IronMindGym.Hosting;

public sealed class DatabaseHealthCheck(IronMindDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database cannot be reached.");
}

// The runtime image has no curl, so the container health check runs the app with --check-health.
public static class HealthProbe
{
    public const string Argument = "--check-health";
    public const string Path = "/healthz";

    public static async Task<int> Run()
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';', ',')[0] ?? "8080";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"http://127.0.0.1:{port}{Path}"));
        // Host filtering refuses an unknown host, so the probe presents one the application allows.
        if (HostFor(Configuration()["AllowedHosts"]) is { } host)
            request.Headers.Host = host;
        try
        {
            using var response = await http.SendAsync(request);
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }

    public static string? HostFor(string? allowedHosts)
    {
        var hosts = (allowedHosts ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hosts.Length == 0 || hosts.Contains("*"))
            return null;
        return hosts.FirstOrDefault(host => !host.StartsWith("*.", StringComparison.Ordinal))
            ?? "health" + hosts[0][1..];
    }

    private static IConfiguration Configuration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Production;
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables("ASPNETCORE_")
            .AddEnvironmentVariables()
            .Build();
    }
}
