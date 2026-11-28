using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected int UserId => User.GetUserId() ?? throw new InvalidOperationException("This endpoint requires a signed-in user.");
}

