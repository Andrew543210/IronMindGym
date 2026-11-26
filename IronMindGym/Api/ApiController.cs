using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

/// <summary>
/// The base of every API controller. Refusals are thrown as domain exceptions and answered by
/// <see cref="DomainExceptionFilter"/>, so every 400 and 409 has the same shape.
/// </summary>
[ApiController]
public abstract class ApiController : ControllerBase
{
    protected int UserId => User.GetUserId() ?? throw new InvalidOperationException("This endpoint requires a signed-in user.");
}

public sealed record OptionResponse(int Id, string Name);
