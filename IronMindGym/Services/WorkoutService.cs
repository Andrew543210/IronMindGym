using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class WorkoutService(IronMindDbContext db)
{
    public int Save(int currentUserId, WorkoutLoggerViewModel model)
    {
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
        var safeDraftSets = ParseDraft(model.DraftPayloadJson);
        var exercises = db.Exercises.AsNoTracking().Where(e => safeDraftSets.Select(s => s.ExerciseId).Contains(e.Id)).ToDictionary(e => e.Id);
        var setTypeIds = db.SetTypes.Select(s => s.Id).ToHashSet();
        foreach (var set in safeDraftSets)
        {
            if (!exercises.TryGetValue(set.ExerciseId, out var exercise) || !setTypeIds.Contains(set.SetTypeId))
                throw new ValidationException("Choose valid exercises and set types.");
            if (set.RestSeconds is < 0 or > 3600)
                throw new ValidationException("Rest must be between 0 and 3600 seconds.");
            if (exercise.Type != ExerciseType.StrengthTraining)
            {
                if (set.DurationSeconds is < 1 or > 86400 || (exercise.Type == ExerciseType.Cardio && set.Intensity is < 1 or > 10) || !double.IsFinite(set.WeightKg) || set.WeightKg is < 0 or > 500)
                    throw new ValidationException("Cardio requires a duration of 1–86400 seconds and intensity of 1–10.");
                if (exercise.Type == ExerciseType.Cardio)
                    set.WeightKg = 0;
                else
                    set.Intensity = 0;
                set.Reps = 0;
            }
            else
            {
                if (!double.IsFinite(set.WeightKg) || set.WeightKg is < 0 or > 500 || set.Reps is < 1 or > 500)
                    throw new ValidationException("Strength sets require 0–500 kg and 1–500 reps.");
                set.DurationSeconds = 0;
                set.Intensity = 0;
            }
        }
        if (model.SplitTypeId.HasValue && !db.SplitTypes.Any(s => s.Id == model.SplitTypeId))
            throw new ValidationException("Choose a valid training split.");
        using var transaction = db.Database.BeginTransaction();
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
                    DurationSeconds = set.DurationSeconds,
                    Intensity = set.Intensity,
                    RestSeconds = set.RestSeconds
                });
            }

            db.PerformedExercises.Add(performedExercise);

            var peakWeight = exerciseGroup.Max(x => x.WeightKg);
            var existingRecord = db.PersonalRecords.FirstOrDefault(pr => pr.UserId == currentUserId && pr.ExerciseId == exerciseGroup.Key);

            var isStrength = exercises[exerciseGroup.Key].Type == ExerciseType.StrengthTraining;
            var isNewRecord = isStrength && peakWeight > 0 && (existingRecord == null || peakWeight > existingRecord.MaxWeightKg);
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

        }

        db.WorkoutReviews.Add(new WorkoutReview
        {
            WorkoutSession = session,
            Rating = model.ReviewRating,
            Comment = model.ReviewComment?.Trim() ?? string.Empty
        });

        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, currentUserId);
        transaction.Commit();
        return session.Id;

    }

    private static List<DraftSetItemViewModel> ParseDraft(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 100000)
            throw new ValidationException("Workout data is missing or too large.");
        try
        {
            var sets = JsonSerializer.Deserialize<List<DraftSetItemViewModel>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (sets is null || sets.Count is < 1 or > 200 || sets.Any(s => s is null))
                throw new ValidationException("Add between 1 and 200 sets before saving.");
            return sets;
        }
        catch (JsonException)
        {
            throw new ValidationException("Workout data is invalid. Please review your sets.");
        }
    }

    public bool Delete(int userId, int sessionId)
    {
        var session = db.WorkoutSessions.Include(s => s.PerformedExercises)
            .FirstOrDefault(s => s.Id == sessionId && s.UserId == userId);
        if (session is null)
            return false;
        var exerciseIds = session.PerformedExercises.Select(e => e.ExerciseId).Distinct().ToList();
        using var transaction = db.Database.BeginTransaction();
        db.WorkoutSessions.Remove(session);
        db.SaveChanges();
        foreach (var exerciseId in exerciseIds)
        {
            var record = db.PersonalRecords.FirstOrDefault(r => r.UserId == userId && r.ExerciseId == exerciseId);
            if (record is null)
                continue;
            var best = db.Sets.Where(s => s.PerformedExercise.ExerciseId == exerciseId && s.PerformedExercise.WorkoutSession.UserId == userId)
                .OrderByDescending(s => s.WeightKg).Select(s => new { s.WeightKg, s.PerformedExercise.WorkoutSession.SessionDate }).FirstOrDefault();
            if (best is null)
                db.PersonalRecords.Remove(record);
            else
            {
                record.MaxWeightKg = best.WeightKg;
                record.UpdatedAt = best.SessionDate;
            }
        }
        db.SaveChanges();
        transaction.Commit();
        return true;
    }
}
