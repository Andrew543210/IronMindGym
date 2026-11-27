using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/setup")]
public class SetupController(SetupService setup) : ApiController
{
    [HttpGet]
    public Task<SetupFormResponse> Form(CancellationToken cancellationToken) => setup.FormAsync(UserId, cancellationToken);

    [HttpPost]
    public async Task<ActionResult> Save(SetupRequest request, CancellationToken cancellationToken)
    {
        await setup.SaveAsync(UserId, request, cancellationToken);
        return NoContent();
    }
}
