using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Seed;

// The demo athlete's records and awards are derived from its workouts, like anyone else's.
public static class DemoDataSeeder
{
    public static async Task SeedAsync(IronMindDbContext db, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return;
        var upperLower = await db.SplitTypes.SingleAsync(s => s.Code == SplitTypeCode.UpperLower, cancellationToken);
        var upper = await db.SplitDays.SingleAsync(d => d.Code == "UPPER", cancellationToken);
        var lower = await db.SplitDays.SingleAsync(d => d.Code == "LOWER", cancellationToken);
        var working = await db.SetTypes.SingleAsync(s => s.Code == SetTypeCode.Working, cancellationToken);
        var exercises = await db.Exercises.ToDictionaryAsync(e => e.Name, cancellationToken);
        var now = clock.UtcNow();
        Exercise Find(string name) => exercises.TryGetValue(name, out var exercise)
            ? exercise
            : throw new InvalidOperationException($"The demo data needs the catalog exercise \"{name}\".");

        var athlete = new User
        {
            Username = "demo_athlete",
            Email = "demo@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("DemoTraining123!"),
            CreatedAt = now.AddDays(-40),
            Profile = new Profile { Age = 24, HeightCm = 180 },
            Benchmarks = new InitialBenchmarks { BenchPressKg = 90, SquatKg = 130, DeadliftKg = 160 }
        };
        var friend = new User
        {
            Username = "fit_friend",
            Email = "friend@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("FriendTraining123!"),
            CreatedAt = now.AddDays(-20),
            Profile = new Profile { Age = 23, HeightCm = 168 },
            Benchmarks = new InitialBenchmarks { BenchPressKg = 65, SquatKg = 95, DeadliftKg = 120 }
        };
        db.Users.AddRange(athlete, friend);
        var today = DateOnly.FromDateTime(now);
        db.WeightLogs.AddRange(
            new WeightLog { User = athlete, Date = today.AddDays(-40), ValueKg = 83 },
            new WeightLog { User = athlete, Date = today.AddDays(-10), ValueKg = 82 },
            new WeightLog { User = friend, Date = today.AddDays(-20), ValueKg = 62 });
        db.WorkoutPlans.Add(new WorkoutPlan { User = athlete, WorkoutsPerWeek = 4, SplitType = upperLower });
        db.DailyNutritions.Add(new DailyNutrition
        {
            User = athlete,
            Date = today,
            CaloriesKcal = 2550,
            ProteinG = 165,
            FatG = 70,
            CarbsG = 280
        });

        Workout(athlete, upper, daysAgo: 8, minutes: 65, working,
            (Find("Flat Bench Press"), [(80, 8), (90, 5), (95, 3)]),
            (Find("Bent-Over Barbell Row"), [(60, 10), (70, 8)]));
        Workout(athlete, lower, daysAgo: 6, minutes: 70, working,
            (Find("Barbell Back Squat"), [(120, 6), (140, 3), (150, 1)]),
            (Find("Romanian Deadlift"), [(100, 8), (110, 6)]));
        Workout(athlete, upper, daysAgo: 1, minutes: 70, working,
            (Find("Flat Bench Press"), [(85, 6), (100, 2)]),
            (Find("Bent-Over Barbell Row"), [(72.5, 8)]));
        await db.SaveChangesAsync(cancellationToken);

        db.Friendships.Add(new Friendship
        {
            User1Id = Math.Min(athlete.Id, friend.Id),
            User2Id = Math.Max(athlete.Id, friend.Id),
            RequestedByUserId = friend.Id,
            Status = FriendshipStatus.Accepted
        });
        var records = new PersonalRecordService(db);
        foreach (var user in new[] { athlete, friend })
            await records.RecalculateAsync(user.Id, exercises.Values.Select(e => e.Id), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        var achievements = new AchievementService(db, new AthleteCalendars(db, clock));
        await achievements.AwardEarnedAsync(athlete.Id, cancellationToken);
        await achievements.AwardEarnedAsync(friend.Id, cancellationToken);

        void Workout(User user, SplitDay day, int daysAgo, int minutes, SetType setType,
            params (Exercise Exercise, (double Kg, int Reps)[] Sets)[] blocks)
        {
            var session = new WorkoutSession
            {
                User = user,
                SplitDay = day,
                SessionDate = now.AddDays(-daysAgo),
                DurationMinutes = minutes,
                Rating = 5,
                Notes = daysAgo == 1 ? "Strong training session" : null
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
