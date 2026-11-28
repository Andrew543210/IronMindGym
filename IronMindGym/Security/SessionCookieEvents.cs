using IronMindGym.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Security;

// Answers 401 and 403 instead of redirecting, and ends a session once the security stamp changes.
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
