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
                            WorkoutSessionId = s.Id,
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

                    var achievements = db.UserAchievements
                        .AsNoTracking()
                        .Include(ua => ua.Achievement)
                        .Where(ua => ua.UserId == friend.Id)
                        .OrderByDescending(ua => ua.AchievedAt)
                        .Select(ua => new FriendAchievementPreviewViewModel
                        {
                            Name = ua.Achievement.Name,
                            ConditionDescription = ua.Achievement.ConditionDescription,
                            AchievedAt = ua.AchievedAt
                        })
                        .ToList();

                    return new FriendCardViewModel
                    {
                    UserId = friend.Id,
                    Username = friend.Username,
                    Status = isAcceptedFriend ? "Friend" : "Found user",
                    LastFiveWorkoutsCount = workouts.Count,
                    AverageKcal = nutritionLogs.Select(n => n.CaloriesKcal).DefaultIfEmpty(0).Average(),
                    LastFiveWorkouts = workouts,
                    LastNutritionLogs = nutritionLogs,
                    PersonalRecords = records,
                    Achievements = achievements
                };})
                .ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult WorkoutDetails(int friendId, int workoutSessionId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var session = db.WorkoutSessions
            .AsNoTracking()
            .Include(s => s.PerformedExercises)
            .ThenInclude(pe => pe.Exercise)
            .Include(s => s.PerformedExercises)
            .ThenInclude(pe => pe.Sets)
            .ThenInclude(set => set.SetType)
            .Include(s => s.Review)
            .FirstOrDefault(s => s.Id == workoutSessionId && s.UserId == friendId);

        if (session == null)
        {
            return NotFound();
        }

        var stats = session.PerformedExercises
            .Select(pe =>
            {
                var peakWeight = pe.Sets.Max(set => set.WeightKg);
                var previousRecordWeight = db.PerformedExercises
                    .AsNoTracking()
                    .Where(x => x.ExerciseId == pe.ExerciseId && x.WorkoutSession.UserId == friendId && x.WorkoutSessionId != session.Id)
                    .SelectMany(x => x.Sets)
                    .Select(x => (double?)x.WeightKg)
                    .Max();

                var setLoadPercents = (previousRecordWeight.HasValue && previousRecordWeight.Value > 0)
                    ? pe.Sets.Select(set => set.WeightKg / previousRecordWeight.Value * 100.0).ToList()
                    : new List<double>();

                return new ExerciseWorkoutSummaryViewModel
                {
                    ExerciseName = pe.Exercise.Name,
                    SetsCount = pe.Sets.Count,
                    AverageLoad = setLoadPercents.Count == 0 ? (double?)null : Math.Round(setLoadPercents.Average(), 1),
                    PeakLoad = setLoadPercents.Count == 0 ? (double?)null : Math.Round(setLoadPercents.Max(), 1),
                    PeakWeightKg = peakWeight,
                    PercentToPersonalRecord = setLoadPercents.Count == 0 ? (double?)null : Math.Round(setLoadPercents.Max(), 1),
                    IsNewRecord = !previousRecordWeight.HasValue || peakWeight > previousRecordWeight.Value
                };
            })
            .ToList();

        var detailsVm = new FriendWorkoutDetailsViewModel
        {
            FriendUsername = db.Users.AsNoTracking().Where(x => x.Id == friendId).Select(x => x.Username).FirstOrDefault() ?? "Friend",
            Details = new WorkoutDetailsViewModel
            {
                Session = session,
                Summary = new WorkoutSummaryViewModel
                {
                    WorkoutSessionId = session.Id,
                    SessionName = session.Name,
                    AverageRestSeconds = session.PerformedExercises.SelectMany(pe => pe.Sets).Any()
                        ? Math.Round(session.PerformedExercises.SelectMany(pe => pe.Sets).Average(set => set.RestSeconds), 1)
                        : 0,
                    RemainingWorkoutsThisWeek = 0,
                    ExerciseStats = stats
                }
            }
        };

        return View(detailsVm);
    }
}
