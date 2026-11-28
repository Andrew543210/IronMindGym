using IronMindGym.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace IronMindGym.Api;

public sealed class DomainExceptionFilter(ProblemDetailsFactory problems) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case DomainValidationException refused:
                context.ModelState.AddModelError(refused.Field, ServerMessages.Translate(refused.Message));
                context.Result = new BadRequestObjectResult(problems.CreateValidationProblemDetails(context.HttpContext, context.ModelState));
                break;
            case DomainConflictException conflict:
                context.Result = new ConflictObjectResult(problems.CreateProblemDetails(
                    context.HttpContext, StatusCodes.Status409Conflict, ServerMessages.Translate(conflict.Message)));
                break;
            default:
                return;
        }
        context.ExceptionHandled = true;
    }
}
