using System.Globalization;
using System.Net;
using IronMindGym.Data;
using IronMindGym.Hosting;
using IronMindGym.Seed;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;

if (args is [HealthProbe.Argument])
    return await HealthProbe.Run();

var builder = WebApplication.CreateBuilder(args);

var keyDirectory = builder.Configuration["DataProtection:KeyDirectory"];
if (!string.IsNullOrWhiteSpace(keyDirectory))
    builder.Services.AddDataProtection().PersistKeysToFileSystem(Directory.CreateDirectory(keyDirectory));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if (IPAddress.TryParse(builder.Configuration["ReverseProxy:KnownProxy"], out var proxy))
        options.KnownProxies.Add(proxy);
});
builder.Services.AddIronMindServices(builder.Configuration);
builder.Services.AddSessionSecurity(builder.Environment);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IronMindDbContext>();
    await DatabaseMigrations.ApplyAsync(db, app.Lifetime.ApplicationStopping);
    var demoData = app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Seed:DemoData");
    await DataSeeder.SeedAsync(db, demoData, app.Services.GetRequiredService<TimeProvider>(), app.Lifetime.ApplicationStopping);
}

app.UseForwardedHeaders();
app.UseSecurityHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US", "en"),
    SupportedCultures = [new CultureInfo("en-US")],
    SupportedUICultures = [new CultureInfo("en"), new CultureInfo("uk")],
    RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()]
});

// Hashed bundles never change; index.html is revalidated on every visit.
var clientFiles = new StaticFileOptions
{
    OnPrepareResponse = file => file.Context.Response.Headers.CacheControl =
        file.Context.Request.Path.StartsWithSegments("/assets") ? "public, max-age=31536000, immutable" : "no-cache"
};
app.UseStaticFiles(clientFiles);
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks(HealthProbe.Path).AllowAnonymous();
app.MapFallback("/api/{**path}", () => Results.NotFound()).AllowAnonymous();
app.MapFallbackToFile("index.html", clientFiles).AllowAnonymous();
await app.RunAsync();
return 0;

public partial class Program;
