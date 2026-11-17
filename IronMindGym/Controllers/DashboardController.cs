using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class DashboardController(IronMindDbContext db, DashboardService dashboard) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(dashboard.Build(User.GetUserId()!.Value));

    [HttpGet]
    public IActionResult Details(int id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var item = db.ActivityFeeds
            .AsNoTracking()
            .Include(x => x.ResearchItem)
            .Where(x => x.Id == id)
            .Select(x => new DashboardResearchItemViewModel
            {
                FeedId = x.Id,
                PublishedAt = x.PublishedAt,
                Title = x.ResearchItem.Title,
                ThesisText = x.ResearchItem.ThesisText,
                Source = DashboardService.BuildSourceName(x.ResearchItem.Source),
                SourceUrl = DashboardService.BuildSourceUrl(x.ResearchItem.Source)
            })
            .FirstOrDefault();

        if (item == null)
        {
            return NotFound();
        }

        return View(new DashboardResearchDetailsViewModel { Item = item });
    }

}
