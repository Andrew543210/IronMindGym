using IronMindGym.Models;

namespace IronMindGym.Tests;

/// <summary>Builds training history for tests: workouts in the past go straight to the database.</summary>
internal static class Journal
{
    public static int AddWorkout(TestApplication app, DateTime date, int minutes,
        params (int ExerciseId, double Kg, int Reps, int RestSeconds)[] sets) => app.Query(db =>
    {
        var setType = db.SetTypes.Single(t => t.Name == "Working").Id;
        var session = new WorkoutSession { UserId = db.Users.OrderBy(u => u.Id).First().Id, Name = "Full Body day", SessionDate = date, DurationMinutes = minutes };
        foreach (var group in sets.GroupBy(s => s.ExerciseId))
        {
            var performed = new PerformedExercise { ExerciseId = group.Key, OrderInWorkout = session.PerformedExercises.Count + 1 };
            var number = 1;
            foreach (var set in group)
                performed.Sets.Add(new WorkoutSet { WeightKg = set.Kg, Reps = set.Reps, RestSeconds = set.RestSeconds, SetNumber = number++, SetTypeId = setType });
            session.PerformedExercises.Add(performed);
        }
        db.WorkoutSessions.Add(session);
        db.SaveChanges();
        return session.Id;
    });

    /// <summary>A request for POST /api/workouts with working sets of strength exercises.</summary>
    public static object Workout(params (int ExerciseId, double Kg, int Reps)[] sets) => new
    {
        splitDayLabel = "Full Body",
        durationMinutes = 45,
        reviewRating = 4,
        sets = sets.Select(s => new { exerciseId = s.ExerciseId, setTypeId = 1, weightKg = s.Kg, reps = s.Reps, restSeconds = 90 })
    };
}
