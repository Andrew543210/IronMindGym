using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class FriendsController(IronMindDbContext db, FriendshipService friendshipService) : Controller
{
    [HttpGet]
    public IActionResult Index(string? searchUsername) => View(friendshipService.BuildIndex(User.GetUserId()!.Value, searchUsername));

    [HttpPost]
    public IActionResult SendRequest(int userId)
    {
        try
        {
            friendshipService.SendRequest(User.GetUserId()!.Value, userId);
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex) { return BadRequest(ex.Message); }
        TempData["Message"] = "Friend request sent.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Accept(int userId)
    {
        if (!friendshipService.Accept(User.GetUserId()!.Value, userId))
            return NotFound();
        TempData["Message"] = "You're now connected. Your friend can see your workouts, nutrition, records and progress.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Remove(int userId)
    {
        if (!friendshipService.Remove(User.GetUserId()!.Value, userId))
            return NotFound();
        TempData["Message"] = "Connection removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult WorkoutDetails(int friendId, int workoutSessionId)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        if (!friendshipService.AreFriends(currentUserId, friendId))
            return NotFound();

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
                    .Where(x => x.ExerciseId == pe.ExerciseId && x.WorkoutSession.UserId == friendId && x.WorkoutSession.SessionDate < session.SessionDate)
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
                    AverageLoad = setLoadPercents.Count == 0 ? null : Math.Round(setLoadPercents.Average(), 1),
                    PeakLoad = setLoadPercents.Count == 0 ? null : Math.Round(setLoadPercents.Max(), 1),
                    PeakWeightKg = peakWeight,
                    PercentToPersonalRecord = setLoadPercents.Count == 0 ? null : Math.Round(setLoadPercents.Max(), 1),
                    IsNewRecord = pe.Exercise.Type == IronMindGym.Models.ExerciseType.StrengthTraining && (!previousRecordWeight.HasValue || peakWeight > previousRecordWeight.Value)
                };
            })
            .ToList();

        var detailsVm = new FriendWorkoutDetailsViewModel
        {
            FriendUsername = db.Users.AsNoTracking().Where(x => x.Id == friendId).Select(x => x.Username).FirstOrDefault() ?? "Friend",
            WorkoutSessionId = session.Id,
            LikesCount = db.ActivityLikes.Count(x => x.WorkoutSessionId == session.Id),
            IsLikedByCurrentUser = db.ActivityLikes.Any(x => x.WorkoutSessionId == session.Id && x.UserId == currentUserId),
            Comments = db.FriendComments
                .AsNoTracking()
                .Where(x => x.WorkoutSessionId == session.Id)
                .Include(x => x.Author)
                .OrderByDescending(x => x.Date)
                .Select(x => new FriendWorkoutCommentViewModel
                {
                    AuthorUsername = x.Author.Username,
                    Text = x.Text,
                    Date = x.Date
                })
                .ToList(),
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleLike(int friendId, int workoutSessionId)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        if (!friendshipService.AreFriends(currentUserId, friendId))
            return NotFound();

        var exists = db.WorkoutSessions.AsNoTracking()
            .Any(s => s.Id == workoutSessionId && s.UserId == friendId);
        if (!exists)
        {
            return NotFound();
        }

        var existingLike = db.ActivityLikes
            .FirstOrDefault(x => x.WorkoutSessionId == workoutSessionId && x.UserId == currentUserId);

        if (existingLike == null)
        {
            db.ActivityLikes.Add(new ActivityLike
            {
                WorkoutSessionId = workoutSessionId,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            db.ActivityLikes.Remove(existingLike);
        }

        db.SaveChanges();
        return RedirectToAction(nameof(WorkoutDetails), new
        {
            friendId,
            workoutSessionId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddComment(int friendId, int workoutSessionId, string? commentText)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!friendshipService.AreFriends(userId.Value, friendId))
            return NotFound();
        var text = (commentText ?? string.Empty).Trim();
        if (text.Length > 500)
            return BadRequest("Comments are limited to 500 characters.");
        if (text.Length == 0)
        {
            return RedirectToAction(nameof(WorkoutDetails), new
            {
                friendId,
                workoutSessionId
            });
        }

        var exists = db.WorkoutSessions.AsNoTracking()
            .Any(s => s.Id == workoutSessionId && s.UserId == friendId);
        if (!exists)
        {
            return NotFound();
        }

        db.FriendComments.Add(new FriendComment
        {
            WorkoutSessionId = workoutSessionId,
            AuthorId = userId.Value,
            Text = text,
            Date = DateTime.UtcNow
        });

        db.SaveChanges();
        return RedirectToAction(nameof(WorkoutDetails), new
        {
            friendId,
            workoutSessionId
        });
    }
}
