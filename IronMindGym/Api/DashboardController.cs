using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

public class DashboardController(DashboardService dashboard) : ApiController
{
    [HttpGet("api/dashboard")]
    public DashboardResponse Overview() => dashboard.Build(UserId);

    [HttpGet("api/feed/{id:int}")]
    public ActionResult<DashboardResearchItem> FeedItem(int id) =>
        dashboard.ResearchNote(id) is { } note ? note : NotFound();
}
