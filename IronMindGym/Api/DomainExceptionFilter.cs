using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace IronMindGym.Api;

/// <summary>
/// The one place that turns a refusal into a response. Services and controllers throw a
/// <see cref="DomainValidationException"/> or <see cref="DomainConflictException"/>; any other exception is a
/// server error and is left to the exception handler.
/// </summary>
public sealed class DomainExceptionFilter(ProblemDetailsFactory problems) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case DomainValidationException refused:
                context.ModelState.AddModelError(refused.Field, refused.Message);
                context.Result = new BadRequestObjectResult(problems.CreateValidationProblemDetails(context.HttpContext, context.ModelState));
                break;
            case DomainConflictException conflict:
                context.Result = new ConflictObjectResult(problems.CreateProblemDetails(
                    context.HttpContext, StatusCodes.Status409Conflict, conflict.Message));
                break;
            default:
                return;
        }
        context.ExceptionHandled = true;
    }
}
