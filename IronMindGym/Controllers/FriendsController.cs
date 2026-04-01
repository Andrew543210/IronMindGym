using IronMindGym.Data;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class FriendsController(IronMindDbContext db) : Controller
{
    public IActionResult Index(string? searchUsername)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var query = db.Friendships
            .Include(f => f.User1)
            .Include(f => f.User2)
            .Where(f => (f.User1Id == currentUserId || f.User2Id == currentUserId) && f.Status == "Accepted");

        var vm = new FriendsViewModel
        {
            SearchUsername = searchUsername ?? string.Empty,
            Friends = query
                .AsEnumerable()
                .Select(f => f.User1Id == currentUserId ? f.User2 : f.User1)
                .Where(u => string.IsNullOrWhiteSpace(searchUsername) || u.Username.Contains(searchUsername, StringComparison.OrdinalIgnoreCase))
                .Select(friend => new FriendCardViewModel
                {
                    UserId = friend.Id,
                    Username = friend.Username,
                    Status = "Друг",
                    LastFiveWorkoutsCount = db.WorkoutSessions.Count(s => s.UserId == friend.Id),
                    AverageKcal = db.DailyNutritions.Where(n => n.UserId == friend.Id).Select(n => n.CaloriesKcal).DefaultIfEmpty(0).Average(),
                    LastFiveWorkouts = db.WorkoutSessions
                        .Where(s => s.UserId == friend.Id)
                        .OrderByDescending(s => s.SessionDate)
                        .Take(5)
                        .Select(s => new FriendWorkoutPreviewViewModel
                        {
                            Name = s.Name,
                            SessionDate = s.SessionDate,
                            DurationMinutes = s.DurationMinutes
                        })
                        .ToList()
                })
                .ToList()
        };

        return View(vm);
    }
}
