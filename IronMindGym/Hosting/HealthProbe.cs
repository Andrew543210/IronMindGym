using IronMindGym.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IronMindGym.Hosting;

/// <summary>The application is healthy when it can reach its database.</summary>
public sealed class DatabaseHealthCheck(IronMindDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database cannot be reached.");
}

/// <summary>
/// Run as <c>dotnet IronMindGym.dll --check-health</c> by the container health check: the runtime image has no
/// curl, so the application asks its own <c>/healthz</c> endpoint and exits with 0 when it is healthy.
/// </summary>
public static class HealthProbe
{
    public const string Argument = "--check-health";
    public const string Path = "/healthz";

    public static async Task<int> Run()
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';', ',')[0] ?? "8080";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"http://127.0.0.1:{port}{Path}"));
        // Host filtering refuses an unknown host, so the probe presents the first allowed one.
        var allowedHost = Environment.GetEnvironmentVariable("AllowedHosts")?.Split(';')
            .FirstOrDefault(host => !string.IsNullOrWhiteSpace(host) && !host.Contains('*', StringComparison.Ordinal));
        if (allowedHost is not null)
            request.Headers.Host = allowedHost.Trim();
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
}
