using IronMindGym.Localization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace IronMindGym.Api;

// The browser sends the session cookie by itself, so every change must also carry the antiforgery token.
public sealed class CsrfProtectionFilter(IAntiforgery antiforgery, ProblemDetailsFactory problems) : IAsyncAuthorizationFilter
{
    public const string HeaderName = "X-CSRF-TOKEN";
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
