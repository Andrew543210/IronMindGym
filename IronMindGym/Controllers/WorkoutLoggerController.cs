using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IronMindGym.Controllers;

public class WorkoutLoggerController(IronMindDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var vm = BuildIndexViewModel(userId.Value);
        FillSelections(vm);
        return View(vm);
    }

    [HttpPost]
    public IActionResult Finish(WorkoutLoggerViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        FillSelections(model);

        List<DraftSetItemViewModel>? draftSets;
        try
        {
            draftSets = JsonSerializer.Deserialize<List<DraftSetItemViewModel>>(
                model.DraftPayloadJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            draftSets = null;
        }

        if (draftSets == null || draftSets.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Додайте хоча б один підхід перед завершенням тренування.");
        }

        if (!ModelState.IsValid)
        {
            model.RecentSessions = BuildRecentSessions(currentUserId);
            return View(model);
        }

        var safeDraftSets = draftSets!;

        var splitTypeName = db.SplitTypes.AsNoTracking().FirstOrDefault(s => s.Id == model.SplitTypeId)?.Name;
        var sessionName = string.IsNullOrWhiteSpace(model.SessionName)
            ? "Силове тренування"
            : model.SessionName.Trim();

        var session = new WorkoutSession
        {
            UserId = currentUserId,
            Name = sessionName,
            SessionDate = DateTime.UtcNow,
            DurationMinutes = model.DurationMinutes,
            SplitTypeId = model.SplitTypeId
        };

        var groupedByExercise = safeDraftSets
            .GroupBy(x => x.ExerciseId)
            .ToList();

        if (groupedByExercise.Any(g => g.Key <= 0) || groupedByExercise.Any(g => !db.Exercises.Any(e => e.Id == g.Key)))
        {
            ModelState.AddModelError(string.Empty, "Некоректні дані вправ у тренуванні. Додайте підходи ще раз.");
            model.RecentSessions = BuildRecentSessions(currentUserId);
            return View("Index", model);
        }

        var summaryRows = new List<ExerciseWorkoutSummaryViewModel>();
        var order = 1;

        foreach (var exerciseGroup in groupedByExercise)
        {
            var performedExercise = new PerformedExercise
            {
                ExerciseId = exerciseGroup.Key,
                WorkoutSession = session,
                OrderInWorkout = order++
            };

            var setNumber = 1;
            foreach (var set in exerciseGroup)
            {
                performedExercise.Sets.Add(new WorkoutSet
                {
                    SetNumber = setNumber++,
                    WeightKg = set.WeightKg,
                    Reps = set.Reps,
                    SetTypeId = set.SetTypeId,
                    RestSeconds = set.RestSeconds
                });
            }

            db.PerformedExercises.Add(performedExercise);

            var exerciseName = db.Exercises.AsNoTracking().First(e => e.Id == exerciseGroup.Key).Name;
            var loads = exerciseGroup.Select(x => x.WeightKg * x.Reps).ToList();
            var peakWeight = exerciseGroup.Max(x => x.WeightKg);
            var existingRecord = db.PersonalRecords.FirstOrDefault(pr => pr.UserId == currentUserId && pr.ExerciseId == exerciseGroup.Key);

            var isNewRecord = existingRecord == null || peakWeight > existingRecord.MaxWeightKg;
            var percentToRecord = existingRecord == null
                ? (double?)null
                : Math.Round(peakWeight / existingRecord.MaxWeightKg * 100.0, 1);

            if (isNewRecord)
            {
                if (existingRecord == null)
                {
                    db.PersonalRecords.Add(new PersonalRecord
                    {
                        UserId = currentUserId,
                        ExerciseId = exerciseGroup.Key,
                        MaxWeightKg = peakWeight,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existingRecord.MaxWeightKg = peakWeight;
                    existingRecord.UpdatedAt = DateTime.UtcNow;
                }
            }

            summaryRows.Add(new ExerciseWorkoutSummaryViewModel
            {
                ExerciseName = exerciseName,
                SetsCount = exerciseGroup.Count(),
                AverageLoad = Math.Round(loads.Average(), 1),
                PeakLoad = Math.Round(loads.Max(), 1),
                PeakWeightKg = peakWeight,
                PercentToPersonalRecord = percentToRecord,
                IsNewRecord = isNewRecord
            });
        }

        db.WorkoutReviews.Add(new WorkoutReview
        {
            WorkoutSession = session,
            Rating = model.ReviewRating,
            Comment = model.ReviewComment
        });

        db.SaveChanges();

        AssignAchievements(currentUserId);
        db.SaveChanges();

        var workoutsPerWeek = db.WorkoutPlans
            .Where(w => w.UserId == currentUserId && w.IsActive)
            .Select(w => w.WorkoutsPerWeek)
            .FirstOrDefault();

        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
        if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
        {
            weekStart = DateTime.UtcNow.Date.AddDays(-6);
        }

        var doneThisWeek = db.WorkoutSessions.Count(s => s.UserId == currentUserId && s.SessionDate >= weekStart);

        var summary = new WorkoutSummaryViewModel
        {
            WorkoutSessionId = session.Id,
            SessionName = string.IsNullOrWhiteSpace(splitTypeName) ? session.Name : $"{session.Name} ({splitTypeName})",
            AverageRestSeconds = Math.Round(safeDraftSets.Average(x => x.RestSeconds), 1),
            RemainingWorkoutsThisWeek = Math.Max(workoutsPerWeek - doneThisWeek, 0),
            ExerciseStats = summaryRows
        };

        TempData["Message"] = "Тренування збережено.";
        TempData["WorkoutSummary"] = JsonSerializer.Serialize(summary);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var session = db.WorkoutSessions
            .Include(s => s.PerformedExercises)
            .ThenInclude(pe => pe.Exercise)
            .Include(s => s.PerformedExercises)
            .ThenInclude(pe => pe.Sets)
            .ThenInclude(set => set.SetType)
            .Include(s => s.Review)
            .FirstOrDefault(s => s.UserId == currentUserId && s.Id == id);

        if (session == null)
        {
            return NotFound();
        }

        var stats = session.PerformedExercises
            .Select(pe =>
            {
                var loads = pe.Sets.Select(set => set.WeightKg * set.Reps).ToList();
                var peakWeight = pe.Sets.Max(set => set.WeightKg);
                var currentRecord = db.PersonalRecords.AsNoTracking().FirstOrDefault(pr => pr.UserId == currentUserId && pr.ExerciseId == pe.ExerciseId);

                return new ExerciseWorkoutSummaryViewModel
                {
                    ExerciseName = pe.Exercise.Name,
                    SetsCount = pe.Sets.Count,
                    AverageLoad = Math.Round(loads.Average(), 1),
                    PeakLoad = Math.Round(loads.Max(), 1),
                    PeakWeightKg = peakWeight,
                    PercentToPersonalRecord = currentRecord == null ? null : Math.Round(peakWeight / currentRecord.MaxWeightKg * 100.0, 1),
                    IsNewRecord = currentRecord != null && peakWeight >= currentRecord.MaxWeightKg
                };
            })
            .ToList();

        var activePlanTarget = db.WorkoutPlans.Where(w => w.UserId == currentUserId && w.IsActive).Select(w => w.WorkoutsPerWeek).FirstOrDefault();
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
        if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
        {
            weekStart = DateTime.UtcNow.Date.AddDays(-6);
        }

        var doneThisWeek = db.WorkoutSessions.Count(s => s.UserId == currentUserId && s.SessionDate >= weekStart);

        var detailsVm = new WorkoutDetailsViewModel
        {
            Session = session,
            Summary = new WorkoutSummaryViewModel
            {
                WorkoutSessionId = session.Id,
                SessionName = session.Name,
                AverageRestSeconds = session.PerformedExercises.SelectMany(pe => pe.Sets).Any()
                    ? Math.Round(session.PerformedExercises.SelectMany(pe => pe.Sets).Average(set => set.RestSeconds), 1)
                    : 0,
                RemainingWorkoutsThisWeek = Math.Max(activePlanTarget - doneThisWeek, 0),
                ExerciseStats = stats
            }
        };

        return View(detailsVm);
    }

    private WorkoutLoggerViewModel BuildIndexViewModel(int currentUserId)
    {
        var vm = new WorkoutLoggerViewModel
        {
            RecentSessions = BuildRecentSessions(currentUserId)
        };

        if (TempData["WorkoutSummary"] is string summaryJson)
        {
            vm.LastSummary = JsonSerializer.Deserialize<WorkoutSummaryViewModel>(summaryJson);
        }

        return vm;
    }

    private List<WorkoutSessionListItemViewModel> BuildRecentSessions(int currentUserId)
    {
        return db.WorkoutSessions
            .Where(s => s.UserId == currentUserId)
            .OrderByDescending(s => s.SessionDate)
            .Take(5)
            .Select(s => new WorkoutSessionListItemViewModel
            {
                Id = s.Id,
                SessionDate = s.SessionDate,
                Name = s.Name,
                DurationMinutes = s.DurationMinutes
            })
            .ToList();
    }

    private void FillSelections(WorkoutLoggerViewModel model)
    {
        var groups = db.MuscleGroups.AsNoTracking().OrderBy(g => g.Name).ToList();
        var subGroups = db.MuscleSubGroups.AsNoTracking().OrderBy(s => s.Name).ToList();
        var exercises = db.Exercises
            .AsNoTracking()
            .Include(e => e.MuscleSubGroup)
            .ThenInclude(s => s.MuscleGroup)
            .OrderBy(e => e.Name)
            .ToList();

        ViewBag.SplitTypes = new SelectList(db.SplitTypes.AsNoTracking().OrderBy(s => s.Name), "Id", "Name", model.SplitTypeId);
        ViewBag.MuscleGroups = new SelectList(groups, "Id", "Name", model.SelectedMuscleGroupId);
        ViewBag.MuscleSubGroups = new SelectList(subGroups, "Id", "Name", model.SelectedMuscleSubGroupId);
        ViewBag.Exercises = new SelectList(exercises, "Id", "Name", model.ExerciseId);
        ViewBag.SetTypes = new SelectList(db.SetTypes.AsNoTracking().OrderBy(s => s.Name), "Id", "Name", model.SetTypeId);

        model.ExerciseCatalog = exercises.Select(e => new ExerciseCatalogItemViewModel
        {
            ExerciseId = e.Id,
            ExerciseName = e.Name,
            MuscleGroupId = e.MuscleSubGroup.MuscleGroupId,
            MuscleGroupName = e.MuscleSubGroup.MuscleGroup.Name,
            MuscleSubGroupId = e.MuscleSubGroupId,
            MuscleSubGroupName = e.MuscleSubGroup.Name
        }).ToList();
    }

    private void AssignAchievements(int userId)
    {
        var unlocked = db.UserAchievements.Where(ua => ua.UserId == userId).Select(ua => ua.Achievement.Code).ToHashSet();
        var all = db.Achievements.AsNoTracking().ToDictionary(a => a.Code, a => a.Id);

        void Grant(string code)
        {
            if (!all.TryGetValue(code, out var achievementId) || unlocked.Contains(code))
            {
                return;
            }

            db.UserAchievements.Add(new UserAchievement
            {
                UserId = userId,
                AchievementId = achievementId,
                AchievedAt = DateTime.UtcNow
            });
            unlocked.Add(code);
        }

        var workoutsCount = db.WorkoutSessions.Count(s => s.UserId == userId);
        if (workoutsCount >= 1) Grant("FIRST_WORKOUT");
        if (workoutsCount >= 5) Grant("WORKOUTS_5");
        if (workoutsCount >= 20) Grant("WORKOUTS_20");

        var records = db.PersonalRecords
            .Where(pr => pr.UserId == userId)
            .Include(pr => pr.Exercise)
            .AsNoTracking()
            .ToList();

        if (records.Any(pr => pr.Exercise.Name.Contains("Жим лежачи", StringComparison.OrdinalIgnoreCase) && pr.MaxWeightKg >= 100))
            Grant("BENCH_100");
        if (records.Any(pr => pr.Exercise.Name.Contains("Присідання", StringComparison.OrdinalIgnoreCase) && pr.MaxWeightKg >= 140))
            Grant("SQUAT_140");
        if (records.Any(pr => pr.Exercise.Name.Contains("Станова тяга", StringComparison.OrdinalIgnoreCase) && pr.MaxWeightKg >= 180))
            Grant("DEADLIFT_180");

        var monthlyTonnage = db.WorkoutSessions
            .Where(s => s.UserId == userId && s.SessionDate >= DateTime.UtcNow.AddDays(-30))
            .Include(s => s.PerformedExercises)
            .ThenInclude(pe => pe.Sets)
            .AsEnumerable()
            .SelectMany(s => s.PerformedExercises)
            .SelectMany(pe => pe.Sets)
            .Sum(set => set.WeightKg * set.Reps);

        if (monthlyTonnage >= 20000)
            Grant("MONTHLY_TONNAGE_20K");
    }
}
