using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

// Records are recalculated from the logged sets whenever workouts change, instead of being adjusted step by step.
public sealed class PersonalRecordService(IronMindDbContext db)
{
    public async Task RecalculateAsync(int userId, IEnumerable<int> exerciseIds, CancellationToken cancellationToken)
    {
        var ids = exerciseIds.Distinct().ToArray();
        var strengthSets = db.Sets.Where(s => s.PerformedExercise.WorkoutSession.UserId == userId
            && ids.Contains(s.PerformedExercise.ExerciseId)
            && s.PerformedExercise.Exercise.Type == ExerciseType.StrengthTraining
            && s.WeightKg > 0);
        var heaviest = strengthSets
            .GroupBy(s => s.PerformedExercise.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, WeightKg = g.Max(s => s.WeightKg) });
        var best = await strengthSets
            .Join(heaviest,
                s => new { s.PerformedExercise.ExerciseId, s.WeightKg },
                h => new { h.ExerciseId, h.WeightKg },
                (s, h) => new { h.ExerciseId, h.WeightKg, s.PerformedExercise.WorkoutSession.SessionDate })
            .GroupBy(x => new { x.ExerciseId, x.WeightKg })
            .Select(g => new { g.Key.ExerciseId, g.Key.WeightKg, AchievedAt = g.Min(x => x.SessionDate) })
            .ToDictionaryAsync(x => x.ExerciseId, cancellationToken);

        var records = await db.PersonalRecords.Where(r => r.UserId == userId && ids.Contains(r.ExerciseId)).ToListAsync(cancellationToken);
        db.PersonalRecords.RemoveRange(records.Where(r => !best.ContainsKey(r.ExerciseId)));
        foreach (var (exerciseId, set) in best)
        {
            var record = records.FirstOrDefault(r => r.ExerciseId == exerciseId);
            if (record is null)
                db.PersonalRecords.Add(record = new PersonalRecord { UserId = userId, ExerciseId = exerciseId });
            record.MaxWeightKg = set.WeightKg;
            record.AchievedAt = set.AchievedAt;
        }
    }
}
