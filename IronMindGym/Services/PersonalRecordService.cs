using IronMindGym.Data;
using IronMindGym.Models;

namespace IronMindGym.Services;

/// <summary>
/// A personal record is the heaviest strength set an athlete has logged for an exercise, dated by the
/// first workout that reached it. The table is a summary of the logged sets, so it is recalculated from
/// them whenever workouts are saved or deleted instead of being adjusted step by step.
/// </summary>
public sealed class PersonalRecordService(IronMindDbContext db)
{
    /// <summary>Brings the records of the given exercises in line with the sets. The caller saves the changes.</summary>
    public void Recalculate(int userId, IEnumerable<int> exerciseIds)
    {
        var ids = exerciseIds.Distinct().ToArray();
        var strengthSets = db.Sets.Where(s => s.PerformedExercise.WorkoutSession.UserId == userId
            && ids.Contains(s.PerformedExercise.ExerciseId)
            && s.PerformedExercise.Exercise.Type == ExerciseType.StrengthTraining
            && s.WeightKg > 0);
        var heaviest = strengthSets
            .GroupBy(s => s.PerformedExercise.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, WeightKg = g.Max(s => s.WeightKg) });
        var best = strengthSets
            .Join(heaviest,
                s => new { s.PerformedExercise.ExerciseId, s.WeightKg },
                h => new { h.ExerciseId, h.WeightKg },
                (s, h) => new { h.ExerciseId, h.WeightKg, s.PerformedExercise.WorkoutSession.SessionDate })
            .GroupBy(x => new { x.ExerciseId, x.WeightKg })
            .Select(g => new { g.Key.ExerciseId, g.Key.WeightKg, AchievedAt = g.Min(x => x.SessionDate) })
            .ToDictionary(x => x.ExerciseId);

        var records = db.PersonalRecords.Where(r => r.UserId == userId && ids.Contains(r.ExerciseId)).ToList();
        db.PersonalRecords.RemoveRange(records.Where(r => !best.ContainsKey(r.ExerciseId)));
        foreach (var (exerciseId, set) in best)
        {
            var record = records.FirstOrDefault(r => r.ExerciseId == exerciseId);
            if (record is null)
                db.PersonalRecords.Add(record = new PersonalRecord { UserId = userId, ExerciseId = exerciseId });
            record.MaxWeightKg = set.WeightKg;
            record.UpdatedAt = set.AchievedAt;
        }
    }
}
