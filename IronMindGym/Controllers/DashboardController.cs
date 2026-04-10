using IronMindGym.Data;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class DashboardController(IronMindDbContext db, IAvatarService avatarService) : Controller
{
    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;

        var user = db.Users
            .Include(u => u.Benchmarks)
            .FirstOrDefault(u => u.Id == currentUserId);

        if (user == null)
        {
            HttpContext.Session.Remove("UserId");
            return RedirectToAction("Login", "Auth");
        }

        var latestAnthropometry = db.AnthropometryLogs
            .Where(a => a.UserId == currentUserId)
            .OrderByDescending(a => a.RecordedAt)
            .FirstOrDefault();

        var activePlan = db.WorkoutPlans
            .FirstOrDefault(p => p.UserId == currentUserId && p.IsActive);

        var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.Date.DayOfWeek + (int)DayOfWeek.Monday);
        if (startOfWeek > DateTime.UtcNow.Date)
        {
            startOfWeek = startOfWeek.AddDays(-7);
        }

        var completedThisWeek = db.WorkoutSessions.Count(s => s.UserId == currentUserId && s.SessionDate >= startOfWeek);

        var feed = db.ActivityFeeds
            .Include(f => f.ResearchItem)
            .OrderByDescending(f => f.PublishedAt)
            .Take(24)
            .ToList();

        var strengthCoefficient = avatarService.CalculateStrengthCoefficient(user.Benchmarks, latestAnthropometry);
        var avatar = avatarService.ResolveAvatar(db.Avatars.AsNoTracking().ToList(), user.Benchmarks, latestAnthropometry);

        var vm = new DashboardViewModel
        {
            Username = user.Username,
            AvatarName = avatar.Name,
            StrengthCoefficient = strengthCoefficient,
            PlannedWorkoutsPerWeek = activePlan?.WorkoutsPerWeek ?? 0,
            CompletedThisWeek = completedThisWeek,
            FeedItems = feed
                .Select(f => new DashboardResearchItemViewModel
                {
                    FeedId = f.Id,
                    PublishedAt = f.PublishedAt,
                    Title = f.ResearchItem.Title,
                    ThesisText = f.ResearchItem.ThesisText,
                    Source = BuildSourceName(f.ResearchItem.Source),
                    SourceUrl = BuildSourceUrl(f.ResearchItem.Source)
                })
                .ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
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
                Source = BuildSourceName(x.ResearchItem.Source),
                SourceUrl = BuildSourceUrl(x.ResearchItem.Source)
            })
            .FirstOrDefault();

        if (item == null)
        {
            return NotFound();
        }

        return View(new DashboardResearchDetailsViewModel { Item = item });
    }

    private static string BuildSourceName(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return "Research Source";
        }

        var parts = source.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? source : parts[0];
    }

    private static string BuildSourceUrl(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var parts = source.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            return parts[1];
        }

        return Uri.TryCreate(source, UriKind.Absolute, out _) ? source : string.Empty;
    }
}
