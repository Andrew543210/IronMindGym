using System.Globalization;
using System.Security.Claims;
using IronMindGym.Contracts;
using IronMindGym.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IronMindGym.Api;

[Route("api/session")]
[AllowAnonymous]
public class SessionController(AccountService accounts, IAntiforgery antiforgery, TimeProvider clock) : ApiController
{
    /// <summary>
    /// The token the client sends in the X-CSRF-TOKEN header with every change.
    /// It belongs to the signed-in user, so the client asks again after signing in or out.
    /// </summary>
    [HttpGet("csrf")]
    public CsrfTokenResponse Csrf() => new(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!);

    [HttpGet]
    public async Task<ActionResult<SessionResponse>> Current(CancellationToken cancellationToken) =>
        User.GetUserId() is int userId ? await accounts.DescribeAsync(userId, cancellationToken) : NoContent();

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<ActionResult<SessionResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await accounts.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        await SignIn(user, request.RememberMe);
        return await accounts.DescribeAsync(user.Id, cancellationToken);
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<ActionResult<SessionResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await accounts.RegisterAsync(request, cancellationToken);
        await SignIn(user, rememberMe: false);
        return await accounts.DescribeAsync(user.Id, cancellationToken);
    }

    [HttpPost("logout")]
    public async Task<NoContentResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private async Task SignIn(User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Username),
            // Changing the password changes the stamp, which signs out every other session.
            new(SessionClaims.SecurityStamp, user.SecurityStamp)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? clock.GetUtcNow().AddDays(30) : null,
            AllowRefresh = true
        });
    }
}
