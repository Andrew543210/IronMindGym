using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IronMindGym.Controllers;

public class WorkoutLoggerController(IronMindDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index(bool startTraining = false)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var vm = BuildIndexViewModel(userId.Value, startTraining);
        FillSelections(vm, userId.Value);
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
        model.IsTrainingActive = true;
        FillSelections(model, currentUserId);

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
            ModelState.AddModelError(string.Empty, "Add at least one set before finishing the workout.");
        }

        if (!ModelState.IsValid)
        {
            model.RecentSessions = BuildRecentSessions(currentUserId);
            return View("Index", model);
        }

        var safeDraftSets = draftSets!;

        var splitTypeName = db.SplitTypes.AsNoTracking().FirstOrDefault(s => s.Id == model.SplitTypeId)?.Name;
        var splitDayLabel = string.IsNullOrWhiteSpace(model.SplitDayLabel)
            ? "Training"
            : model.SplitDayLabel.Trim();
        var sessionName = splitDayLabel.StartsWith("Day ", StringComparison.OrdinalIgnoreCase)
            ? splitDayLabel
            : $"Day {splitDayLabel}";

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
            ModelState.AddModelError(string.Empty, "Invalid exercise data in this workout. Please add sets again.");
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

            var rawExerciseName = db.Exercises.AsNoTracking().First(e => e.Id == exerciseGroup.Key).Name;
            var exerciseName = ExerciseLibraryController.TranslateExerciseNameForDisplay(rawExerciseName);
            var peakWeight = exerciseGroup.Max(x => x.WeightKg);
            var existingRecord = db.PersonalRecords.FirstOrDefault(pr => pr.UserId == currentUserId && pr.ExerciseId == exerciseGroup.Key);
            var baselineRecordWeight = existingRecord?.MaxWeightKg;

            var setLoadPercents = (baselineRecordWeight.HasValue && baselineRecordWeight.Value > 0)
                ? exerciseGroup.Select(x => x.WeightKg / baselineRecordWeight.Value * 100.0).ToList()
                : new List<double>();

            var isNewRecord = existingRecord == null || peakWeight > existingRecord.MaxWeightKg;
            var averagePercent = setLoadPercents.Count == 0 ? (double?)null : Math.Round(setLoadPercents.Average(), 1);
            var peakPercent = setLoadPercents.Count == 0 ? (double?)null : Math.Round(setLoadPercents.Max(), 1);

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
                AverageLoad = averagePercent,
                PeakLoad = peakPercent,
                PeakWeightKg = peakWeight,
                PercentToPersonalRecord = peakPercent,
                IsNewRecord = isNewRecord
            });
        }

        db.WorkoutReviews.Add(new WorkoutReview
        {
            WorkoutSession = session,
            Rating = model.ReviewRating,
            Comment = model.ReviewComment
        });

        try
        {
            db.SaveChanges();
            AchievementEngine.AssignAchievements(db, currentUserId);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Unable to save workout right now. Please try again.");
            model.RecentSessions = BuildRecentSessions(currentUserId);
            return View("Index", model);
        }

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

        TempData["Message"] = "Workout saved.";
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
                var peakWeight = pe.Sets.Max(set => set.WeightKg);
                var previousRecordWeight = db.PerformedExercises
                    .AsNoTracking()
                    .Where(x => x.ExerciseId == pe.ExerciseId && x.WorkoutSession.UserId == currentUserId && x.WorkoutSessionId != session.Id)
                    .SelectMany(x => x.Sets)
                    .Select(x => (double?)x.WeightKg)
                    .Max();

                var setLoadPercents = (previousRecordWeight.HasValue && previousRecordWeight.Value > 0)
                    ? pe.Sets.Select(set => set.WeightKg / previousRecordWeight.Value * 100.0).ToList()
                    : new List<double>();

                var averagePercent = setLoadPercents.Count == 0 ? (double?)null : Math.Round(setLoadPercents.Average(), 1);
                var peakPercent = setLoadPercents.Count == 0 ? (double?)null : Math.Round(setLoadPercents.Max(), 1);

                return new ExerciseWorkoutSummaryViewModel
                {
                    ExerciseName = ExerciseLibraryController.TranslateExerciseNameForDisplay(pe.Exercise.Name),
                    SetsCount = pe.Sets.Count,
                    AverageLoad = averagePercent,
                    PeakLoad = peakPercent,
                    PeakWeightKg = peakWeight,
                    PercentToPersonalRecord = peakPercent,
                    IsNewRecord = !previousRecordWeight.HasValue || peakWeight > previousRecordWeight.Value
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
            LikesCount = db.ActivityLikes.Count(x => x.WorkoutSessionId == session.Id),
            IsLikedByCurrentUser = db.ActivityLikes.Any(x => x.WorkoutSessionId == session.Id && x.UserId == currentUserId),
            Comments = db.FriendComments
                .AsNoTracking()
                .Where(x => x.WorkoutSessionId == session.Id)
                .Include(x => x.Author)
                .OrderByDescending(x => x.Date)
                .Select(x => new WorkoutCommentViewModel
                {
                    AuthorUsername = x.Author.Username,
                    Text = x.Text,
                    Date = x.Date
                })
                .ToList(),
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleLike(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var sessionExists = db.WorkoutSessions.AsNoTracking().Any(s => s.Id == id && s.UserId == currentUserId);
        if (!sessionExists)
        {
            return NotFound();
        }

        var existingLike = db.ActivityLikes.FirstOrDefault(x => x.WorkoutSessionId == id && x.UserId == currentUserId);
        if (existingLike == null)
        {
            db.ActivityLikes.Add(new ActivityLike
            {
                WorkoutSessionId = id,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            db.ActivityLikes.Remove(existingLike);
        }

        db.SaveChanges();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddComment(int id, string? commentText)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentUserId = userId.Value;
        var sessionExists = db.WorkoutSessions.AsNoTracking().Any(s => s.Id == id && s.UserId == currentUserId);
        if (!sessionExists)
        {
            return NotFound();
        }

        var text = (commentText ?? string.Empty).Trim();
        if (text.Length > 0)
        {
            db.FriendComments.Add(new FriendComment
            {
                WorkoutSessionId = id,
                AuthorId = currentUserId,
                Text = text,
                Date = DateTime.UtcNow
            });

            db.SaveChanges();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private WorkoutLoggerViewModel BuildIndexViewModel(int currentUserId, bool startTraining)
    {
        var vm = new WorkoutLoggerViewModel
        {
            IsTrainingActive = startTraining,
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

    private void FillSelections(WorkoutLoggerViewModel model, int currentUserId)
    {
        var activePlan = db.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.SplitType)
            .FirstOrDefault(p => p.UserId == currentUserId && p.IsActive);

        if (activePlan != null)
        {
            model.SplitTypeId = activePlan.SplitTypeId;
            model.ActiveSplitTypeName = activePlan.SplitType.Name;
        }

        var groups = db.MuscleGroups.AsNoTracking().OrderBy(g => g.Name).ToList();
        var subGroups = db.MuscleSubGroups.AsNoTracking().OrderBy(s => s.Name).ToList();
        var exercises = db.Exercises
            .AsNoTracking()
            .Include(e => e.MuscleSubGroup)
            .ThenInclude(s => s.MuscleGroup)
            .OrderBy(e => e.Name)
            .ToList();

        var groupOptions = groups
            .Select(g => new { g.Id, Name = ExerciseLibraryController.TranslateMuscleGroupForDisplay(g.Name) })
            .OrderBy(g => g.Name)
            .ToList();

        var subGroupOptions = subGroups
            .Select(s => new
            {
                s.Id,
                s.MuscleGroupId,
                Name = ExerciseLibraryController.TranslateMuscleSubGroupForDisplay(s.Name)
            })
            .OrderBy(s => s.Name)
            .ToList();

        var exerciseOptions = exercises
            .Select(e => new
            {
                e.Id,
                Name = ExerciseLibraryController.TranslateExerciseNameForDisplay(e.Name)
            })
            .OrderBy(e => e.Name)
            .ToList();

        var splitTypeItems = activePlan == null
            ? db.SplitTypes.AsNoTracking().OrderBy(s => s.Name).ToList()
            : new List<SplitType> { activePlan.SplitType };

        ViewBag.SplitTypes = new SelectList(splitTypeItems, "Id", "Name", model.SplitTypeId);

        var selectedSplitTypeName = splitTypeItems.FirstOrDefault(s => s.Id == model.SplitTypeId)?.Name;
        var splitDays = BuildSplitDayOptions(selectedSplitTypeName);
        if (!splitDays.Contains(model.SplitDayLabel, StringComparer.OrdinalIgnoreCase))
        {
            model.SplitDayLabel = splitDays.First();
        }

        ViewBag.SplitDays = new SelectList(splitDays, model.SplitDayLabel);
        ViewBag.MuscleGroups = new SelectList(groupOptions, "Id", "Name", model.SelectedMuscleGroupId);
        ViewBag.MuscleSubGroups = new SelectList(subGroupOptions, "Id", "Name", model.SelectedMuscleSubGroupId);
        ViewBag.Exercises = new SelectList(exerciseOptions, "Id", "Name", model.ExerciseId);

        var setTypes = db.SetTypes
            .AsNoTracking()
            .ToList();

        // Keep sports-friendly default for quick logging if user doesn't choose type manually.
        var defaultSetTypeId = setTypes
            .FirstOrDefault(s => s.Name.Equals("Working", StringComparison.OrdinalIgnoreCase))?.Id
            ?? setTypes.FirstOrDefault()?.Id
            ?? 0;

        if (model.SetTypeId <= 0)
        {
            model.SetTypeId = defaultSetTypeId;
        }

        var orderedSetTypes = setTypes
            .OrderBy(s => s.Id == defaultSetTypeId ? 0 : 1)
            .ThenBy(s => s.Name)
            .ToList();

        ViewBag.SetTypes = new SelectList(orderedSetTypes, "Id", "Name", model.SetTypeId);

        model.ExerciseCatalog = exercises.Select(e => new ExerciseCatalogItemViewModel
        {
            ExerciseId = e.Id,
            ExerciseName = ExerciseLibraryController.TranslateExerciseNameForDisplay(e.Name),
            MuscleGroupId = e.MuscleSubGroup.MuscleGroupId,
            MuscleGroupName = ExerciseLibraryController.TranslateMuscleGroupForDisplay(e.MuscleSubGroup.MuscleGroup.Name),
            MuscleSubGroupId = e.MuscleSubGroupId,
            MuscleSubGroupName = ExerciseLibraryController.TranslateMuscleSubGroupForDisplay(e.MuscleSubGroup.Name)
        }).ToList();
    }

    private static List<string> BuildSplitDayOptions(string? splitTypeName)
    {
        if (string.IsNullOrWhiteSpace(splitTypeName))
        {
            return new List<string> { "Training Day" };
        }

        var normalized = splitTypeName.Trim().ToLowerInvariant();

        if (normalized.Contains("legs-back-chest"))
        {
            return new List<string> { "Legs", "Back", "Chest" };
        }

        if (normalized.Contains("upper-lower"))
        {
            return new List<string> { "Upper", "Lower" };
        }

        if (normalized.Contains("full-body"))
        {
            return new List<string> { "Full Body" };
        }

        return new List<string> { "Training Day" };
    }

}

