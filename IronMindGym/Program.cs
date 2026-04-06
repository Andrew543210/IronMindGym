using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using IronMindGym.Api;
using IronMindGym.Data;
using IronMindGym.Seed;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<CsrfProtectionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddDbContext<IronMindDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException(
        "Set ConnectionStrings:DefaultConnection to a PostgreSQL database. See README for the local Docker database."),
    npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddAuthorization(options => options.FallbackPolicy =
    new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context => Status(context.Response, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context => Status(context.Response, StatusCodes.Status403Forbidden);
        // A changed password changes the security stamp, which ends every other session of the account.
        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?.GetUserId();
            var db = context.HttpContext.RequestServices.GetRequiredService<IronMindDbContext>();
            var stamp = userId is null ? null : await db.Users.Where(u => u.Id == userId).Select(u => u.SecurityStamp).FirstOrDefaultAsync();
            if (stamp is null || context.Principal?.FindFirst("SecurityStamp")?.Value != stamp)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentCalendar>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IronMindDbContext>();
    DatabaseMigrations.Apply(db);
    DataSeeder.Seed(db);
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();
// Only the wording follows Accept-Language; numbers and dates are always read in the invariant English form.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US", "en"),
    SupportedCultures = [new CultureInfo("en-US")],
    SupportedUICultures = [new CultureInfo("en"), new CultureInfo("uk")],
    RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()]
});
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static Task Status(HttpResponse response, int statusCode)
{
    response.StatusCode = statusCode;
    return Task.CompletedTask;
}

public partial class Program;
