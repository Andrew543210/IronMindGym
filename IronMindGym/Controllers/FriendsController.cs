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
        var search = (searchUsername ?? string.Empty).Trim();

        var acceptedFriendIds = db.Friendships
            .AsNoTracking()
            .Where(f => (f.User1Id == currentUserId || f.User2Id == currentUserId) && f.Status == "Accepted")
            .Select(f => f.User1Id == currentUserId ? f.User2Id : f.User1Id)
            .Distinct()
            .ToList();

        var usersQuery = db.Users
            .AsNoTracking()
            .Where(u => u.Id != currentUserId);

        if (string.IsNullOrWhiteSpace(search))
        {
            usersQuery = usersQuery.Where(u => acceptedFriendIds.Contains(u.Id));
        }
        else
        {
            var normalizedSearch = search.ToLower();
            usersQuery = usersQuery.Where(u => u.Username.ToLower().Contains(normalizedSearch));
        }

        var users = usersQuery
            .OrderBy(u => u.Username)
            .ToList();

        var vm = new FriendsViewModel
        {
            SearchUsername = search,
            Friends = users
                .Select(friend =>
                {
                    var isAcceptedFriend = acceptedFriendIds.Contains(friend.Id);

                    var workouts = db.WorkoutSessions
                        .AsNoTracking()
                        .Where(s => s.UserId == friend.Id)
                        .OrderByDescending(s => s.SessionDate)
                        .Take(5)
                        .Select(s => new FriendWorkoutPreviewViewModel
                        {
                            Name = s.Name,
                            SessionDate = s.SessionDate,
                            DurationMinutes = s.DurationMinutes
                        })
                        .ToList();

                    var nutritionLogs = db.DailyNutritions
                        .AsNoTracking()
                        .Where(n => n.UserId == friend.Id)
                        .OrderByDescending(n => n.Date)
                        .Take(10)
                        .Select(n => new FriendNutritionPreviewViewModel
                        {
                            Date = n.Date,
                            CaloriesKcal = n.CaloriesKcal,
                            ProteinG = n.ProteinG,
                            FatG = n.FatG,
                            CarbsG = n.CarbsG
                        })
                        .ToList();

                    var records = db.PersonalRecords
                        .AsNoTracking()
                        .Include(pr => pr.Exercise)
                        .Where(pr => pr.UserId == friend.Id)
                        .OrderByDescending(pr => pr.MaxWeightKg)
                        .Select(pr => new FriendRecordPreviewViewModel
                        {
                            ExerciseName = pr.Exercise.Name,
                            MaxWeightKg = pr.MaxWeightKg,
                            UpdatedAt = pr.UpdatedAt
                        })
                        .ToList();

                    return new FriendCardViewModel
                    {
                    UserId = friend.Id,
                    Username = friend.Username,
                    Status = isAcceptedFriend ? "Друг" : "Знайдений користувач",
                    LastFiveWorkoutsCount = workouts.Count,
                    AverageKcal = nutritionLogs.Select(n => n.CaloriesKcal).DefaultIfEmpty(0).Average(),
                    LastFiveWorkouts = workouts,
                    LastNutritionLogs = nutritionLogs,
                    PersonalRecords = records
                };})
                .ToList()
        };

        return View(vm);
    }
}
