using IronMindGym.Data;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class ProfileController(IronMindDbContext db, IAvatarService avatarService) : Controller
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
            .Include(u => u.Profile)
            .Include(u => u.Benchmarks)
            .Include(u => u.UserAchievements)
            .ThenInclude(ua => ua.Achievement)
            .First(u => u.Id == currentUserId);

        var latestAnthro = db.AnthropometryLogs
            .Where(a => a.UserId == currentUserId)
            .OrderByDescending(a => a.RecordedAt)
            .FirstOrDefault();

        var avatar = avatarService.ResolveAvatar(db.Avatars.AsNoTracking().ToList(), user.Benchmarks, latestAnthro);

        var vm = new ProfileViewModel
        {
            Username = user.Username,
            Email = user.Email,
            Gender = user.Profile?.Gender ?? "Unknown",
            Age = user.Profile?.Age ?? 0,
            AvatarName = avatar.Name,
            Achievements = user.UserAchievements.Select(a => a.Achievement.Name).ToList()
        };

        return View(vm);
    }
}
