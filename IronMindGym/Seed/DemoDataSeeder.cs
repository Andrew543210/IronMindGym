using IronMindGym.Data;
using IronMindGym.Models;

namespace IronMindGym.Seed;

/// <summary>
/// Two connected athletes for trying the app locally. The demo athlete's records and awards are derived
/// from its workouts the same way as for anyone else, so the demo never shows a result it did not log.
/// </summary>
public static class DemoDataSeeder
{
    public static void Seed(IronMindDbContext db)
    {
        if (db.Users.Any())
            return;
        var upperLower = db.SplitTypes.Single(s => s.Name == "Upper-Lower");
        var working = db.SetTypes.Single(s => s.Name == "Working");
        var exercises = db.Exercises.ToDictionary(e => e.Name);
        Exercise Find(string name) => exercises.TryGetValue(name, out var exercise)
            ? exercise
            : throw new InvalidOperationException($"The demo data needs the catalog exercise \"{name}\".");

        var athlete = new User
        {
            Username = "demo_athlete",
            Email = "demo@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("DemoTraining123!"),
            Profile = new Profile { Age = 24, AccountCreatedAt = DateTime.UtcNow.AddDays(-40) },
            Benchmarks = new InitialBenchmarks { BenchPressKg = 90, SquatKg = 130, DeadliftKg = 160 }
        };
        var friend = new User
        {
            Username = "fit_friend",
            Email = "friend@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("FriendTraining123!"),
            Profile = new Profile { Age = 23, AccountCreatedAt = DateTime.UtcNow.AddDays(-20) },
            Benchmarks = new InitialBenchmarks { BenchPressKg = 65, SquatKg = 95, DeadliftKg = 120 }
        };
        db.Users.AddRange(athlete, friend);
        db.AnthropometryLogs.AddRange(
            new Anthropometry { User = athlete, WeightKg = 82, HeightCm = 180, RecordedAt = DateTime.UtcNow.AddDays(-40) },
            new Anthropometry { User = friend, WeightKg = 62, HeightCm = 168, RecordedAt = DateTime.UtcNow.AddDays(-20) });
        db.WorkoutPlans.Add(new WorkoutPlan { User = athlete, WorkoutsPerWeek = 4, IsActive = true, SplitType = upperLower });
        db.DailyNutritions.Add(new DailyNutrition
        {
            User = athlete,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            CaloriesKcal = 2550,
            ProteinG = 165,
            FatG = 70,
            CarbsG = 280
        });

        Workout(athlete, "Upper day", daysAgo: 8, minutes: 65, upperLower, working,
            (Find("Flat Bench Press"), [(80, 8), (90, 5), (95, 3)]),
            (Find("Bent-Over Barbell Row"), [(60, 10), (70, 8)]));
        Workout(athlete, "Lower day", daysAgo: 6, minutes: 70, upperLower, working,
            (Find("Barbell Back Squat"), [(120, 6), (140, 3), (150, 1)]),
            (Find("Romanian Deadlift"), [(100, 8), (110, 6)]));
        Workout(athlete, "Upper day", daysAgo: 1, minutes: 70, upperLower, working,
            (Find("Flat Bench Press"), [(85, 6), (100, 2)]),
            (Find("Bent-Over Barbell Row"), [(72.5, 8)]));
        db.SaveChanges();

        db.Friendships.Add(new Friendship
        {
            User1Id = Math.Min(athlete.Id, friend.Id),
            User2Id = Math.Max(athlete.Id, friend.Id),
            RequestedByUserId = friend.Id,
            Status = FriendshipStatus.Accepted
        });
        var records = new PersonalRecordService(db);
        foreach (var user in new[] { athlete, friend })
            records.Recalculate(user.Id, exercises.Values.Select(e => e.Id));
        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, athlete.Id);
        AchievementEngine.AssignAchievements(db, friend.Id);

        void Workout(User user, string name, int daysAgo, int minutes, SplitType split, SetType setType,
            params (Exercise Exercise, (double Kg, int Reps)[] Sets)[] blocks)
        {
            var session = new WorkoutSession
            {
                User = user,
                Name = name,
                SessionDate = DateTime.UtcNow.AddDays(-daysAgo),
                DurationMinutes = minutes,
                SplitType = split,
                Review = new WorkoutReview { Rating = 5, Comment = daysAgo == 1 ? "Strong training session" : string.Empty }
            };
            foreach (var (exercise, sets) in blocks)
            {
                var performed = new PerformedExercise { Exercise = exercise, OrderInWorkout = session.PerformedExercises.Count + 1 };
                foreach (var (kg, reps) in sets)
                    performed.Sets.Add(new WorkoutSet { SetNumber = performed.Sets.Count + 1, SetType = setType, WeightKg = kg, Reps = reps, RestSeconds = 120 });
                session.PerformedExercises.Add(performed);
            }
            db.WorkoutSessions.Add(session);
        }
    }
}
