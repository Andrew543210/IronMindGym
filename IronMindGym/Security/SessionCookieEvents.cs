using IronMindGym.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Security;

/// <summary>
/// The API answers 401 and 403 instead of redirecting to a sign-in page, and a session ends as soon as the
/// account's security stamp changes, which happens when the password is changed.
/// </summary>
public sealed class SessionCookieEvents(IronMindDbContext db) : CookieAuthenticationEvents
{
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userId = context.Principal?.GetUserId();
        var stamp = userId is null ? null : await db.Users.Where(u => u.Id == userId)
            .Select(u => u.SecurityStamp).FirstOrDefaultAsync(context.HttpContext.RequestAborted);
        if (stamp is null || context.Principal?.FindFirst(SessionClaims.SecurityStamp)?.Value != stamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
