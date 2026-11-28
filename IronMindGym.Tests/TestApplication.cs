using IronMindGym.Data;
using IronMindGym.Tests.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IronMindGym.Tests;

public sealed class TestApplication(string? webRoot = null) : WebApplicationFactory<Program>
{
    private readonly string connectionString = TestDatabase.Create();

    public TestClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        if (webRoot is not null)
            builder.UseWebRoot(webRoot);
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Error));
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public ApiClient Api() => new(Browser());

    public T Query<T>(Func<IronMindDbContext, T> query)
    {
        using var scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<IronMindDbContext>());
    }

    public async Task<T> QueryAsync<T>(Func<IronMindDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<IronMindDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            TestDatabase.Drop(connectionString);
    }
}
