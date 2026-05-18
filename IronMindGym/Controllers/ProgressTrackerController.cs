using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class ProgressTrackerController(IronMindDbContext db) : Controller
{
    private readonly IronMindDbContext _db = db;

    [HttpGet]
    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return RedirectToAction("Login", "Auth");

        var model = new ProgressTrackerViewModel();

        try
        {
            var progressList = _db.ExerciseProgress
                .AsNoTracking()
                .Include(ep => ep.Exercise)
                .ThenInclude(e => e.MuscleSubGroup)
                .ThenInclude(msg => msg.MuscleGroup)
                .Where(ep => ep.UserId == userId.Value)
                .OrderBy(ep => ep.Exercise.MuscleSubGroup.MuscleGroup.Name)
                .ThenBy(ep => ep.Exercise.MuscleSubGroup.Name)
                .ThenBy(ep => ep.Exercise.Name)
                .ToList();

            foreach (var progress in progressList)
            {
                var entry = new ExerciseProgressEntry
                {
                    Id = progress.Id,
                    ExerciseProgressId = progress.Id,
                    ExerciseName = progress.Exercise.Name,
                    MuscleSubGroup = progress.Exercise.MuscleSubGroup.Name,
                    MuscleGroup = progress.Exercise.MuscleSubGroup.MuscleGroup.Name,
                    ExerciseType = progress.Exercise.Type,

                    InitialWeightKg = progress.InitialWeightKg,
                    InitialReps = progress.InitialReps,
                    InitialDurationSeconds = progress.InitialDurationSeconds,
                    InitialIntensity = progress.InitialIntensity,
                    InitialRecordedAt = progress.InitialRecordedAt,

                    CurrentWeightKg = progress.CurrentWeightKg,
                    CurrentReps = progress.CurrentReps,
                    CurrentDurationSeconds = progress.CurrentDurationSeconds,
                    CurrentIntensity = progress.CurrentIntensity,
                    CurrentRecordedAt = progress.CurrentRecordedAt,

                    WeightProgressKg = progress.CalculateWeightProgressKg(),
                    WeightProgressPercent = progress.CalculateWeightProgressPercent(),
                    RepsProgressCount = progress.CalculateRepsProgressCount(),
                    RepsProgressPercent = progress.CalculateRepsProgressPercent(),
                    CardioDurationProgressSeconds = progress.CalculateCardioDurationProgress(),
                    CardioIntensityProgressCount = progress.CalculateCardioIntensityProgress(),
                    CardioEffortProgressPercent = progress.CalculateCardioEffortRatio()
                };

                model.Exercises.Add(entry);
            }
        }
        catch (Exception ex)
        {
            model.ErrorMessage = $"Error loading progress: {ex.Message}";
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult TrackExercise()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return RedirectToAction("Login", "Auth");

        return View();
    }

    [HttpGet("ProgressTracker/GetExerciseCatalog")]
    public IActionResult GetExerciseCatalog()
    {
        var exercises = _db.Exercises
            .AsNoTracking()
            .Include(e => e.MuscleSubGroup)
            .ThenInclude(sg => sg.MuscleGroup)
            .Select(e => new
            {
                exerciseId = e.Id,
                exerciseName = e.Name,
                muscleGroupName = e.MuscleSubGroup.MuscleGroup.Name,
                muscleSubGroupName = e.MuscleSubGroup.Name,
                exerciseType = (int)e.Type
            })
            .ToList();

        return Json(exercises);
    }

    [HttpPost]
    public IActionResult SetInitialResults(int exerciseId, double? weightKg = null, int? reps = null, int? durationSeconds = null, int? intensity = null)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return Json(new { success = false, message = "Not authorized" });

        var exercise = _db.Exercises
            .AsNoTracking()
            .FirstOrDefault(e => e.Id == exerciseId);

        if (exercise == null)
            return Json(new { success = false, message = "Exercise not found" });

        var existingProgress = _db.ExerciseProgress
            .FirstOrDefault(ep => ep.UserId == userId.Value && ep.ExerciseId == exerciseId);

        try
        {
            if (existingProgress == null)
            {
                var progress = new ExerciseProgress
                {
                    UserId = userId.Value,
                    ExerciseId = exerciseId,
                    InitialWeightKg = weightKg ?? 0,
                    InitialReps = reps ?? 0,
                    InitialDurationSeconds = durationSeconds ?? 0,
                    InitialIntensity = intensity ?? 0,
                    CurrentWeightKg = weightKg ?? 0,
                    CurrentReps = reps ?? 0,
                    CurrentDurationSeconds = durationSeconds ?? 0,
                    CurrentIntensity = intensity ?? 0
                };

                if (exercise.Type == ExerciseType.Cardio)
                    progress.SetInitialCardioResults(durationSeconds ?? 0, intensity ?? 0);
                else
                    progress.SetInitialStrengthResults(weightKg ?? 0, reps ?? 0);

                _db.ExerciseProgress.Add(progress);
            }
            else
            {
                if (exercise.Type == ExerciseType.Cardio)
                    existingProgress.SetInitialCardioResults(durationSeconds ?? 0, intensity ?? 0);
                else
                    existingProgress.SetInitialStrengthResults(weightKg ?? 0, reps ?? 0);

                _db.ExerciseProgress.Update(existingProgress);
            }

            _db.SaveChanges();
            return Json(new { success = true, message = "Initial results saved" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult SetCurrentResults(int exerciseId, double? weightKg = null, int? reps = null, int? durationSeconds = null, int? intensity = null)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return Json(new { success = false, message = "Not authorized" });

        var exercise = _db.Exercises
            .AsNoTracking()
            .FirstOrDefault(e => e.Id == exerciseId);

        if (exercise == null)
            return Json(new { success = false, message = "Exercise not found" });

        var progress = _db.ExerciseProgress
            .FirstOrDefault(ep => ep.UserId == userId.Value && ep.ExerciseId == exerciseId);

        if (progress == null)
            return Json(new { success = false, message = "Please set initial results first" });

        try
        {
            if (progress.Exercise.Type == ExerciseType.Cardio)
                progress.SetCurrentCardioResults(durationSeconds ?? 0, intensity ?? 0);
            else
                progress.SetCurrentStrengthResults(weightKg ?? 0, reps ?? 0);

            _db.ExerciseProgress.Update(progress);
            _db.SaveChanges();
            return Json(new { success = true, message = "Current results updated" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult ViewFriendProgress(int friendId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return RedirectToAction("Login", "Auth");

        var friend = _db.Users
            .AsNoTracking()
            .Include(u => u.Benchmarks)
            .FirstOrDefault(u => u.Id == friendId);

        if (friend == null)
            return NotFound();

        var model = new FriendProgressTrackerViewModel
        {
            FriendId = friendId,
            FriendUsername = friend.Username
        };

        try
        {
            if (friend.Benchmarks != null)
            {
                var avgBench = friend.Benchmarks.BenchPressKg;
                var avgSquat = friend.Benchmarks.SquatKg;
                var avgDL = friend.Benchmarks.DeadliftKg;

                var latestAnthro = _db.AnthropometryLogs
                    .AsNoTracking()
                    .Where(a => a.UserId == friendId)
                    .OrderByDescending(a => a.RecordedAt)
                    .FirstOrDefault();

                if (latestAnthro != null && latestAnthro.WeightKg > 0)
                {
                    var totalStrength = avgBench + avgSquat + avgDL;
                    model.CurrentStrengthCoefficient = totalStrength / latestAnthro.WeightKg;
                }
            }

            var progressList = _db.ExerciseProgress
                .AsNoTracking()
                .Include(ep => ep.Exercise)
                .ThenInclude(e => e.MuscleSubGroup)
                .ThenInclude(msg => msg.MuscleGroup)
                .Where(ep => ep.UserId == friendId)
                .OrderBy(ep => ep.Exercise.MuscleSubGroup.MuscleGroup.Name)
                .ThenBy(ep => ep.Exercise.MuscleSubGroup.Name)
                .ThenBy(ep => ep.Exercise.Name)
                .ToList();

            foreach (var progress in progressList)
            {
                var entry = new ExerciseProgressEntry
                {
                    Id = progress.Id,
                    ExerciseProgressId = progress.Id,
                    ExerciseName = progress.Exercise.Name,
                    MuscleSubGroup = progress.Exercise.MuscleSubGroup.Name,
                    MuscleGroup = progress.Exercise.MuscleSubGroup.MuscleGroup.Name,
                    ExerciseType = progress.Exercise.Type,

                    InitialWeightKg = progress.InitialWeightKg,
                    InitialReps = progress.InitialReps,
                    InitialDurationSeconds = progress.InitialDurationSeconds,
                    InitialIntensity = progress.InitialIntensity,
                    InitialRecordedAt = progress.InitialRecordedAt,

                    CurrentWeightKg = progress.CurrentWeightKg,
                    CurrentReps = progress.CurrentReps,
                    CurrentDurationSeconds = progress.CurrentDurationSeconds,
                    CurrentIntensity = progress.CurrentIntensity,
                    CurrentRecordedAt = progress.CurrentRecordedAt,

                    WeightProgressKg = progress.CalculateWeightProgressKg(),
                    WeightProgressPercent = progress.CalculateWeightProgressPercent(),
                    RepsProgressCount = progress.CalculateRepsProgressCount(),
                    RepsProgressPercent = progress.CalculateRepsProgressPercent(),
                    CardioDurationProgressSeconds = progress.CalculateCardioDurationProgress(),
                    CardioIntensityProgressCount = progress.CalculateCardioIntensityProgress(),
                    CardioEffortProgressPercent = progress.CalculateCardioEffortRatio()
                };

                model.Exercises.Add(entry);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error loading friend progress: {ex.Message}");
        }

        return View(model);
    }
}









