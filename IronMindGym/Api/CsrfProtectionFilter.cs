using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IronMindGym.Api;

/// <summary>
/// Requires the antiforgery token in the X-CSRF-TOKEN header for every change. The session is a cookie the
/// browser attaches by itself, so without the token another site could send changes on the athlete's behalf.
/// A refused request gets an empty 400, which tells the client to fetch a fresh token and retry once.
/// </summary>
public sealed class CsrfProtectionFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
            return;
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new ContentResult { StatusCode = StatusCodes.Status400BadRequest };
        }
    }
}
