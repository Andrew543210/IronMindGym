using IronMindGym.Data;
using IronMindGym.Models;
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
        var avatars = db.Avatars.AsNoTracking().OrderBy(a => a.MinStrengthCoefficient).ToList();
        var avatar = avatarService.ResolveAvatar(avatars, user.Benchmarks, latestAnthropometry);
        var avatarScalePoints = BuildAvatarScalePoints(avatars);
        foreach (var point in avatarScalePoints)
        {
            point.IsCurrent = point.Name.Equals(avatar.Name, StringComparison.OrdinalIgnoreCase);
        }

        var vm = new DashboardViewModel
        {
            Username = user.Username,
            AvatarName = avatar.Name,
            StrengthCoefficient = strengthCoefficient,
            CurrentAvatarPositionPercent = CalculateEqualScalePosition(strengthCoefficient, avatars),
            PlannedWorkoutsPerWeek = activePlan?.WorkoutsPerWeek ?? 0,
            CompletedThisWeek = completedThisWeek,
            AvatarScalePoints = avatarScalePoints,
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

    private static List<AvatarScalePointViewModel> BuildAvatarScalePoints(List<Avatar> avatars)
    {
        if (avatars.Count == 0)
        {
            return new List<AvatarScalePointViewModel>();
        }

        var maxIndex = avatars.Count - 1;
        return avatars
            .Select((a, index) => new AvatarScalePointViewModel
            {
                Name = a.Name,
                MinStrengthCoefficient = a.MinStrengthCoefficient,
                PositionPercent = maxIndex == 0 ? 0 : index * 100.0 / maxIndex,
                RangeLabel = BuildRangeLabel(index, avatars),
                IsCurrent = false
            })
            .ToList();
    }

    private static string BuildRangeLabel(int index, List<Avatar> avatars)
    {
        var start = avatars[index].MinStrengthCoefficient;
        if (index == avatars.Count - 1)
        {
            return $"{start:0.0} - 5.0";
        }

        var nextStart = avatars[index + 1].MinStrengthCoefficient;
        var end = Math.Round(nextStart - 0.1, 1);
        return $"{start:0.0} - {end:0.0}";
    }

    private static double CalculateEqualScalePosition(double coefficient, List<Avatar> avatars)
    {
        if (avatars.Count <= 1)
        {
            return 0;
        }

        var sorted = avatars.OrderBy(a => a.MinStrengthCoefficient).ToList();
        if (coefficient <= sorted[0].MinStrengthCoefficient)
        {
            return 0;
        }

        var maxIndex = sorted.Count - 1;
        for (var i = 0; i < sorted.Count - 1; i++)
        {
            var left = sorted[i].MinStrengthCoefficient;
            var right = sorted[i + 1].MinStrengthCoefficient;
            if (coefficient <= right)
            {
                var segmentPercent = (double)i / maxIndex;
                var segmentSize = 1.0 / maxIndex;
                var ratio = right <= left ? 0 : (coefficient - left) / (right - left);
                return Math.Clamp((segmentPercent + ratio * segmentSize) * 100.0, 0, 100);
            }
        }

        return 100;
    }
}
