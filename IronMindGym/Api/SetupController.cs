using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

[Route("api/setup")]
public class SetupController(SetupService setup) : ApiController
{
    [HttpGet]
    public SetupFormResponse Form() => setup.Form(UserId);

    [HttpPost]
    public ActionResult Save(SetupRequest request)
    {
        setup.Save(UserId, request);
        return NoContent();
    }
}
