using System.Globalization;
using System.Security.Claims;
using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Api;

[Route("api/session")]
[AllowAnonymous]
public class SessionController(IronMindDbContext db, IAntiforgery antiforgery) : ApiController
{
    /// <summary>
    /// The token the client sends in the X-CSRF-TOKEN header with every change.
    /// It belongs to the signed-in user, so the client asks again after signing in or out.
    /// </summary>
    [HttpGet("csrf")]
    public CsrfTokenResponse Csrf() => new(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!);

    [HttpGet]
    public ActionResult<SessionResponse> Current() => User.GetUserId() is int userId ? Describe(userId) : NoContent();

    [HttpPost("login")]
    [EnableRateLimiting("authentication")]
    public async Task<ActionResult<SessionResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !PasswordHashUtility.Verify(request.Password, user.PasswordHash))
            return Rejected("Invalid email or password.");
        await SignIn(user, request.RememberMe);
        return Describe(user.Id);
    }

    [HttpPost("register")]
    [EnableRateLimiting("authentication")]
    public async Task<ActionResult<SessionResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim();
        var username = request.Username.Trim();
        if (db.Users.Any(u => u.Email == email))
            return Rejected("A user with this email already exists.", nameof(request.Email));
        if (db.Users.Any(u => u.Username == username))
            return Rejected("This username is already taken.", nameof(request.Username));

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = PasswordHashUtility.Hash(request.Password),
            Profile = new Profile
            {
                Age = request.Age,
                TimeZone = AthleteCalendar.TryFindZone(request.TimeZone, out _) ? request.TimeZone : null,
                Language = Localized.IsUkrainian ? "uk" : "en"
            }
        };
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // A parallel registration took the email or username between the checks above and the insert.
            return Rejected("Email or username is already registered.");
        }
        await SignIn(user, rememberMe: false);
        return Describe(user.Id);
    }

    [HttpPost("logout")]
    public async Task<NoContentResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private SessionResponse Describe(int userId)
    {
        var user = db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { u.Username, u.Email, Language = u.Profile == null ? null : u.Profile.Language }).Single();
        var calendar = AthleteCalendar.For(db, userId);
        return new SessionResponse(userId, user.Username, user.Email, calendar.IanaZoneId,
            calendar.Today, AthleteSetup.IsComplete(db, userId), user.Language);
    }

    private async Task SignIn(User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Username),
            // Changing the password changes the stamp, which signs out every other session.
            new("SecurityStamp", user.SecurityStamp)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null,
            AllowRefresh = true
        });
    }
}

public sealed record CsrfTokenResponse(string Token);

public sealed record SessionResponse(int Id, string Username, string Email, string TimeZone, DateOnly Today, bool SetupComplete, string? Language);
