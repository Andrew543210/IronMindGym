using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using IronMindGym.Api;
using IronMindGym.Data;
using IronMindGym.Localization;
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
                // Every contract states [Required] with its own message. The implicit one would only repeat an
                // unreadable body as "The request field is required." in English.
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                // The binder's own messages quote the value and the parameter name in English, such as a page
                // number that is not a number; the athlete gets the generic, translated one.
                var binding = options.ModelBindingMessageProvider;
                binding.SetAttemptedValueIsInvalidAccessor((_, _) => ServerMessages.InvalidInput);
                binding.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => ServerMessages.InvalidInput);
                binding.SetUnknownValueIsInvalidAccessor(_ => ServerMessages.InvalidInput);
                binding.SetNonPropertyUnknownValueIsInvalidAccessor(() => ServerMessages.InvalidInput);
                binding.SetValueIsInvalidAccessor(_ => ServerMessages.InvalidInput);
                binding.SetValueMustBeANumberAccessor(_ => ServerMessages.InvalidInput);
                binding.SetNonPropertyValueMustBeANumberAccessor(() => ServerMessages.InvalidInput);
                binding.SetValueMustNotBeNullAccessor(_ => ServerMessages.InvalidInput);
                binding.SetMissingBindRequiredValueAccessor(_ => ServerMessages.InvalidInput);
                binding.SetMissingKeyOrValueAccessor(() => ServerMessages.InvalidInput);
                binding.SetMissingRequestBodyRequiredValueAccessor(() => "A non-empty request body is required.");
            })
            .AddJsonOptions(options =>
            {
                // Choices travel as their names; a number is refused instead of becoming an undefined value.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
                // The serializer's own messages name .NET types; the athlete gets the generic, translated one.
                options.AllowInputFormatterExceptionMessages = false;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                var refuse = options.InvalidModelStateResponseFactory;
                options.InvalidModelStateResponseFactory = context =>
                {
                    ServerMessages.TranslateErrors(context.ModelState);
                    return refuse(context);
                };
            });
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
        services.AddScoped<AccountService>();
        services.AddScoped<SetupService>();
        services.AddScoped<WorkoutDraftService>();
        services.AddScoped<ExerciseCatalog>();
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
