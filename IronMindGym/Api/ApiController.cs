using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IronMindGym.Api;

[ApiController]
[ValidationExceptionFilter]
public abstract class ApiController : ControllerBase
{
    protected int UserId => User.GetUserId() ?? throw new InvalidOperationException("This endpoint requires a signed-in user.");

    protected ActionResult Rejected(string message, string field = "")
    {
        ModelState.AddModelError(field, message);
        return ValidationProblem(ModelState);
    }
}

public sealed record OptionResponse(int Id, string Name);

public sealed class ValidationExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException exception)
            return;
        context.ModelState.AddModelError(string.Empty, exception.Message);
        context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState) { Status = StatusCodes.Status400BadRequest });
        context.ExceptionHandled = true;
    }
}
