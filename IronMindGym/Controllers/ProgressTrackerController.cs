using IronMindGym.Data;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class ProgressTrackerController(IronMindDbContext db, ExerciseProgressService progressService, FriendshipService friends, StrengthService strengthService, IAvatarService avatarService) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new ProgressTrackerViewModel { Exercises = progressService.List(User.GetUserId()!.Value) });

    [HttpGet]
    public IActionResult TrackExercise(int? exerciseId)
    {
        ViewBag.ExerciseId = exerciseId ?? 0;
        return View();
    }

    [HttpGet]
    public IActionResult GetExerciseCatalog() => Json(db.Exercises.AsNoTracking().Select(e => new
    {
        exerciseId = e.Id,
        exerciseName = e.Name,
        muscleGroupName = e.MuscleSubGroup.MuscleGroup.Name,
        muscleSubGroupName = e.MuscleSubGroup.Name,
        exerciseType = (int)e.Type
    }).ToList());

    [HttpPost]
    public IActionResult SetInitialResults(int exerciseId, double? weightKg, int? reps, int? durationSeconds, int? intensity)
        => Save(exerciseId, true, weightKg, reps, durationSeconds, intensity);

    [HttpPost]
    public IActionResult SetCurrentResults(int exerciseId, double? weightKg, int? reps, int? durationSeconds, int? intensity)
        => Save(exerciseId, false, weightKg, reps, durationSeconds, intensity);

    private IActionResult Save(int exerciseId, bool initial, double? weight, int? reps, int? duration, int? intensity)
    {
        if (!ModelState.IsValid)
            return BadRequest(new
            {
                success = false,
                message = "Enter valid numeric results."
            });
        try
        {
            progressService.Save(User.GetUserId()!.Value, exerciseId, initial, weight, reps, duration, intensity);
            return Json(new
            {
                success = true,
                message = "Results saved."
            });
        }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet]
    public IActionResult GetExerciseProgress(int exerciseId)
    {
        var userId = User.GetUserId()!.Value;
        var p = db.ExerciseProgress.AsNoTracking().Include(p => p.Exercise).FirstOrDefault(p => p.UserId == userId && p.ExerciseId == exerciseId);
        if (p is null)
            return Json(new
            {
                success = true,
                hasData = false
            });
        var cardio = p.Exercise.Type != IronMindGym.Models.ExerciseType.StrengthTraining;
        var timed = p.Exercise.Type == IronMindGym.Models.ExerciseType.Timed;
        return Json(new
        {
            success = true,
            hasData = true,
            initialValue1 = cardio ? p.InitialDurationSeconds : p.InitialWeightKg,
            initialValue2 = timed ? p.InitialWeightKg : cardio ? p.InitialIntensity : p.InitialReps,
            currentValue1 = cardio ? p.CurrentDurationSeconds : p.CurrentWeightKg,
            currentValue2 = timed ? p.CurrentWeightKg : cardio ? p.CurrentIntensity : p.CurrentReps
        });
    }

    [HttpPost]
    public IActionResult DeleteProgress(int exerciseProgressId) => progressService.Delete(User.GetUserId()!.Value, exerciseProgressId)
        ? Json(new
        {
            success = true
        }) : NotFound(new
        {
            success = false,
            message = "Progress not found."
        });

    [HttpGet]
    public IActionResult ViewFriendProgress(int friendId)
    {
        if (!friends.AreFriends(User.GetUserId()!.Value, friendId))
            return NotFound();
        var friend = db.Users.AsNoTracking().Include(u => u.Benchmarks).Single(u => u.Id == friendId);
        var body = db.AnthropometryLogs.AsNoTracking().Where(a => a.UserId == friendId).OrderByDescending(a => a.RecordedAt).FirstOrDefault();
        return View(new FriendProgressTrackerViewModel
        {
            FriendId = friendId,
            FriendUsername = friend.Username,
            CurrentStrengthCoefficient = avatarService.CalculateStrengthCoefficient(strengthService.Current(friendId, friend.Benchmarks), body),
            Exercises = progressService.List(friendId)
        });
    }
}
