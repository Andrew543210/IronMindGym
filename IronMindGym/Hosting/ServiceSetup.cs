using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using IronMindGym.Api;
using IronMindGym.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Hosting;

public static class ServiceSetup
{
    /// <summary>Controllers with their filters, the database and the domain services.</summary>
    public static IServiceCollection AddIronMindServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
            {
                options.Filters.Add<CsrfProtectionFilter>();
                options.Filters.Add<DomainExceptionFilter>();
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddProblemDetails();
        services.AddDbContext<IronMindDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException(
                "Set ConnectionStrings:DefaultConnection to a PostgreSQL database. See README for the local Docker database."),
            npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AthleteCalendars>();
        services.AddScoped<DashboardService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<StrengthService>();
        services.AddScoped<GoalService>();
        services.AddScoped<ExerciseProgressService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<WorkoutService>();
        services.AddScoped<PersonalRecordService>();
        services.AddScoped<WorkoutSocialService>();
        services.AddScoped<FriendshipService>();
        services.AddScoped<NutritionJournalService>();
        services.AddScoped<AchievementService>();
        services.AddSingleton<NutritionAdviceService>();
        return services;
    }

    /// <summary>
    /// The session cookie, the CSRF token header, the signed-in requirement for every endpoint that does not
    /// opt out, and the rate limit for sign-in.
    /// </summary>
    public static IServiceCollection AddSessionSecurity(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        services.AddAuthorization(options => options.FallbackPolicy =
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddScoped<SessionCookieEvents>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                options.EventsType = typeof(SessionCookieEvents);
            });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Authentication, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }

    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    /// <summary>
    /// Headers every response carries. The client is one bundle from this origin, and Bootstrap's icons are data: images.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
        await next();
    });
}
