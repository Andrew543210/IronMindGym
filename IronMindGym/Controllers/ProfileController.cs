using IronMindGym.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Rendering;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class ProfileController(IronMindDbContext db, IAvatarService avatarService, ProfileService profileService, StrengthService strengthService) : Controller
{
    public IActionResult Index()
    {
        var userId = User.GetUserId();
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

        var avatar = avatarService.ResolveAvatar(db.Avatars.AsNoTracking().ToList(), strengthService.Current(currentUserId, user.Benchmarks), latestAnthro);

        var vm = new ProfileViewModel
        {
            WeightHistory = db.WeightLogs.AsNoTracking().Where(w => w.UserId == currentUserId).OrderByDescending(w => w.Date).Take(30).ToList(),
            Username = user.Username,
            Email = user.Email,
            Age = user.Profile?.Age ?? 0,
            AvatarName = avatar.Name,
            WeightKg = latestAnthro?.WeightKg,
            HeightCm = latestAnthro?.HeightCm,
            AnthropometryRecordedAt = latestAnthro?.RecordedAt,
            BenchPressKg = user.Benchmarks?.BenchPressKg,
            SquatKg = user.Benchmarks?.SquatKg,
            DeadliftKg = user.Benchmarks?.DeadliftKg,
            Achievements = user.UserAchievements
                .OrderByDescending(a => a.AchievedAt)
                .Select(a => new ProfileAchievementViewModel
                {
                    Name = a.Achievement.Name,
                    ConditionDescription = a.Achievement.ConditionDescription,
                    AchievedAt = a.AchievedAt
                })
                .ToList()
        };

        return View(vm);
    }
    [HttpGet]
    public IActionResult Edit()
    {
        FillSplits();
        return View(profileService.BuildEdit(User.GetUserId()!.Value));
    }

    [HttpPost]
    public IActionResult Edit(EditProfileViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                profileService.Update(User.GetUserId()!.Value, model);
                TempData["Message"] = "Profile and training plan updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
            catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "Could not save your profile. Check that your username is available."); }
        }
        FillSplits();
        return View(model);
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                profileService.ChangePassword(User.GetUserId()!.Value, model);
                await HttpContext.SignOutAsync();
                TempData["Message"] = "Password changed. Sign in again. All previous sessions have been revoked.";
                return RedirectToAction("Login", "Auth");
            }
            catch (ValidationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        return View(model);
    }

    [HttpPost]
    public IActionResult LogWeight(LogWeightViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a weight between 30 and 250 kg and a valid date.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            profileService.LogWeight(User.GetUserId()!.Value, model);
            TempData["Message"] = "Weight logged.";
        }
        catch (ValidationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Export()
    {
        var id = User.GetUserId()!.Value;
        var data = new
        {
            ExportedAtUtc = DateTime.UtcNow,
            Account = db.Users.Where(u => u.Id == id).Select(u => new { u.Username, u.Email, Age = u.Profile == null ? 0 : u.Profile.Age }).Single(),
            Weight = db.WeightLogs.Where(w => w.UserId == id).Select(w => new { w.Date, w.ValueKg }).ToList(),
            Nutrition = db.DailyNutritions.Where(n => n.UserId == id).Select(n => new { n.Date, n.CaloriesKcal, n.ProteinG, n.FatG, n.CarbsG }).ToList(),
            Workouts = db.WorkoutSessions.Where(w => w.UserId == id).Select(w => new
            {
                w.Name,
                w.SessionDate,
                w.DurationMinutes,
                Exercises = w.PerformedExercises.Select(e => new { e.Exercise.Name, Sets = e.Sets.Select(s => new { s.SetNumber, s.WeightKg, s.Reps, s.DurationSeconds, s.Intensity, s.RestSeconds }) })
            }).ToList(),
            Progress = db.ExerciseProgress.Where(p => p.UserId == id).Select(p => new { p.Exercise.Name, p.InitialWeightKg, p.CurrentWeightKg, p.InitialReps, p.CurrentReps, p.InitialDurationSeconds, p.CurrentDurationSeconds, p.InitialIntensity, p.CurrentIntensity }).ToList(),
            Goals = db.UserGoals.Where(g => g.UserId == id).Select(g => new { g.GoalType, g.Deadline, g.IsCompleted }).ToList()
        };
        return File(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), "application/json", "ironmind-export.json");
    }

    private void FillSplits() => ViewBag.SplitTypes = new SelectList(db.SplitTypes.AsNoTracking().ToList(), "Id", "Name");
}
