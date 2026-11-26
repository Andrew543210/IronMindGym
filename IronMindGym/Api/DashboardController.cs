using IronMindGym.Contracts;
using IronMindGym.Data;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

public class DashboardController(IronMindDbContext db, DashboardService dashboard) : ApiController
{
    [HttpGet("api/dashboard")]
    public DashboardResponse Overview() => dashboard.Build(UserId);

    [HttpGet("api/feed/{id:int}")]
    public ActionResult<DashboardResearchItem> FeedItem(int id) =>
        DashboardService.ResearchNotes(db.ResearchItems.Where(r => r.Id == id)).SingleOrDefault() is { } note ? note : NotFound();
}
