using IronMindGym.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace IronMindGym.Api;

public class DashboardController(DashboardService dashboard) : ApiController
{
    [HttpGet("api/dashboard")]
    public Task<DashboardResponse> Overview(CancellationToken cancellationToken) => dashboard.BuildAsync(UserId, cancellationToken);

    [HttpGet("api/feed/{id:int}")]
    public async Task<ActionResult<DashboardResearchItem>> FeedItem(int id, CancellationToken cancellationToken) =>
        await dashboard.ResearchNoteAsync(id, cancellationToken) is { } note ? note : NotFound();
}
