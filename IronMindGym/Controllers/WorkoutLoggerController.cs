using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IronMindGym.Controllers;

public class WorkoutLoggerController(IronMindDbContext db, WorkoutService workoutService, ILogger<WorkoutLoggerController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index(bool startTraining = false, int page = 1)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        ViewBag.Page = Math.Clamp(page, 1, 10000);
        var vm = BuildIndexViewModel(userId.Value, startTraining);
        vm.RecentSessions = BuildRecentSessions(userId.Value, (int)ViewBag.Page);
        FillSelections(vm, userId.Value);
        return View(vm);
    }

    [HttpPost]
    [RequestSizeLimit(150000)]
    public IActionResult Finish([Bind("DurationMinutes,SplitTypeId,SplitDayLabel,DraftPayloadJson,ReviewRating,ReviewComment")] WorkoutLoggerViewModel model)
    {
        var userId = User.GetUserId()!.Value;
        if (ModelState.IsValid)
        {
            try
            {
                var id = workoutService.Save(userId, model);
                TempData["ClearWorkoutDraft"] = true;
                TempData["Message"] = "Workout saved. Keep showing up.";
                return RedirectToAction(nameof(Details), new
                {
                    id
                });
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Could not save workout for user {UserId}", userId);
                ModelState.AddModelError(string.Empty, "Could not save this workout. Please try again.");
            }
        }
        model.IsTrainingActive = true;
        FillSelections(model, userId);
        model.RecentSessions = BuildRecentSessions(userId);
        return View("Index", model);
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        if (!workoutService.Delete(User.GetUserId()!.Value, id))
            return NotFound();
        TempData["Message"] = "Workout deleted. Personal records were recalculated. Earned awards are kept.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var userId = User.GetUserId();
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
                    .Where(x => x.ExerciseId == pe.ExerciseId && x.WorkoutSession.UserId == currentUserId && x.WorkoutSession.SessionDate < session.SessionDate)
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
                    ExerciseName = pe.Exercise.Name,
                    SetsCount = pe.Sets.Count,
                    AverageLoad = averagePercent,
                    PeakLoad = peakPercent,
                    PeakWeightKg = peakWeight,
                    PercentToPersonalRecord = peakPercent,
                    IsNewRecord = pe.Exercise.Type == ExerciseType.StrengthTraining && (!previousRecordWeight.HasValue || peakWeight > previousRecordWeight.Value)
                };
            })
            .ToList();

        var activePlanTarget = db.WorkoutPlans.Where(w => w.UserId == currentUserId && w.IsActive).Select(w => w.WorkoutsPerWeek).FirstOrDefault();
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
        if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
        {
            weekStart = DateTime.UtcNow.Date.AddDays(-6);
        }

        var doneThisWeek = db.WorkoutSessions.Count(s => s.UserId == currentUserId && s.SessionDate >= weekStart && s.SessionDate < weekStart.AddDays(7));

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
        var userId = User.GetUserId();
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
        return RedirectToAction(nameof(Details), new
        {
            id
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddComment(int id, string? commentText)
    {
        var userId = User.GetUserId();
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
        if (text.Length > 500)
            return BadRequest("Comments are limited to 500 characters.");
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

        return RedirectToAction(nameof(Details), new
        {
            id
        });
    }

    private WorkoutLoggerViewModel BuildIndexViewModel(int currentUserId, bool startTraining)
    {
        var vm = new WorkoutLoggerViewModel
        {
            IsTrainingActive = startTraining,
            RecentSessions = BuildRecentSessions(currentUserId)
        };

        return vm;
    }

    private List<WorkoutSessionListItemViewModel> BuildRecentSessions(int currentUserId, int page = 1)
    {
        return db.WorkoutSessions
            .Where(s => s.UserId == currentUserId)
            .OrderByDescending(s => s.SessionDate)
            .Skip((page - 1) * 20)
            .Take(20)
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
            .Select(g => new { g.Id, Name = g.Name })
            .OrderBy(g => g.Name)
            .ToList();

        var subGroupOptions = subGroups
            .Select(s => new
            {
                s.Id,
                s.MuscleGroupId,
                Name = s.Name
            })
            .OrderBy(s => s.Name)
            .ToList();

        var exerciseOptions = exercises
            .Select(e => new
            {
                e.Id,
                Name = e.Name
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
            ExerciseName = e.Name,
            ExerciseType = e.Type,
            MuscleGroupId = e.MuscleSubGroup.MuscleGroupId,
            MuscleGroupName = e.MuscleSubGroup.MuscleGroup.Name,
            MuscleSubGroupId = e.MuscleSubGroupId,
            MuscleSubGroupName = e.MuscleSubGroup.Name
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

