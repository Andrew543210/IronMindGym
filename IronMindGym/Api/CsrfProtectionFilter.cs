using IronMindGym.Localization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace IronMindGym.Api;

/// <summary>
/// Requires the antiforgery token in the X-CSRF-TOKEN header for every change. The session is a cookie the
/// browser attaches by itself, so without the token another site could send changes on the athlete's behalf.
/// A refused request gets a 400 with the <see cref="ProblemType"/> problem type, which tells the client to fetch
/// a fresh token and retry once.
/// </summary>
public sealed class CsrfProtectionFilter(IAntiforgery antiforgery, ProblemDetailsFactory problems) : IAsyncAuthorizationFilter
{
    public const string ProblemType = "urn:ironmind:problem:csrf-token";

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
            var problem = problems.CreateProblemDetails(context.HttpContext, StatusCodes.Status400BadRequest,
                title: ServerMessages.Translate(ServerMessages.UnverifiedRequest), type: ProblemType);
            context.Result = new BadRequestObjectResult(problem);
        }
    }
}
