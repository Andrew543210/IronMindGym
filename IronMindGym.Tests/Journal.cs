using IronMindGym.Models;
using IronMindGym.Services;

namespace IronMindGym.Tests;

/// <summary>Builds training history for tests: workouts in the past go straight to the database, with their records.</summary>
internal static class Journal
{
    public static Task<int> AddWorkoutAsync(TestApplication app, DateTime date, int minutes,
        params (int ExerciseId, double Kg, int Reps, int RestSeconds)[] sets) => app.QueryAsync(async db =>
    {
        var setType = db.SetTypes.Single(t => t.Code == SetTypeCode.Working).Id;
        var session = new WorkoutSession { UserId = db.Users.OrderBy(u => u.Id).First().Id, SessionDate = date, DurationMinutes = minutes };
        foreach (var group in sets.GroupBy(s => s.ExerciseId))
        {
            var performed = new PerformedExercise { ExerciseId = group.Key, OrderInWorkout = session.PerformedExercises.Count + 1 };
            var number = 1;
            foreach (var set in group)
                performed.Sets.Add(new WorkoutSet { WeightKg = set.Kg, Reps = set.Reps, RestSeconds = set.RestSeconds, SetNumber = number++, SetTypeId = setType });
            session.PerformedExercises.Add(performed);
        }
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync();
        await new PersonalRecordService(db).RecalculateAsync(session.UserId, sets.Select(s => s.ExerciseId), CancellationToken.None);
        await db.SaveChangesAsync();
        return session.Id;
    });

    /// <summary>A request for POST /api/workouts with working sets of strength exercises.</summary>
    public static object Workout(params (int ExerciseId, double Kg, int Reps)[] sets) => new
    {
        durationMinutes = 45,
        reviewRating = 4,
        sets = sets.Select(s => new { exerciseId = s.ExerciseId, setTypeId = 1, weightKg = s.Kg, reps = s.Reps, restSeconds = 90 })
    };
}
