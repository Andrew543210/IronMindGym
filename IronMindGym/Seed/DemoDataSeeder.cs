using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Seed;

public static class DemoDataSeeder
{
    public static void Seed(IronMindDbContext db)
    {
        if (db.Users.Any())
            return;
        var splitType = db.SplitTypes.Single(s => s.Name == "Upper-Lower");
        var workingSetType = db.SetTypes.Single(s => s.Name == "Working");
        var exercises = db.Exercises.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
        var user = new User
        {
            Username = "demo_athlete",
            Email = "demo@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("DemoTraining123!"),
            Profile = new Profile { Age = 24, AccountCreatedAt = DateTime.UtcNow.AddDays(-40) },
            Benchmarks = new InitialBenchmarks { BenchPressKg = 100, SquatKg = 140, DeadliftKg = 170 }
        };

        var friend = new User
        {
            Username = "fit_friend",
            Email = "friend@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("FriendTraining123!"),
            Profile = new Profile { Age = 23, AccountCreatedAt = DateTime.UtcNow.AddDays(-20) },
            Benchmarks = new InitialBenchmarks { BenchPressKg = 65, SquatKg = 95, DeadliftKg = 120 }
        };

        db.Users.AddRange(user, friend);

        db.AnthropometryLogs.AddRange(
            new Anthropometry { User = user, WeightKg = 82, HeightCm = 180, RecordedAt = DateTime.UtcNow.Date },
            new Anthropometry { User = friend, WeightKg = 62, HeightCm = 168, RecordedAt = DateTime.UtcNow.Date });

        db.WorkoutPlans.Add(new WorkoutPlan
        {
            User = user,
            WorkoutsPerWeek = 4,
            IsActive = true,
            SplitType = splitType
        });

        var session = new WorkoutSession
        {
            User = user,
            Name = "Upper Day",
            SessionDate = DateTime.UtcNow.AddDays(-1),
            DurationMinutes = 70,
            SplitType = splitType
        };

        var performed = new PerformedExercise
        {
            WorkoutSession = session,
            Exercise = FindExerciseByAnyName(exercises, "Flat Bench Press"),
            OrderInWorkout = 1
        };

        performed.Sets.Add(new WorkoutSet { SetNumber = 1, SetType = workingSetType, WeightKg = 80, Reps = 8 });
        performed.Sets.Add(new WorkoutSet { SetNumber = 2, SetType = workingSetType, WeightKg = 85, Reps = 6 });
        db.PerformedExercises.Add(performed);

        db.WorkoutReviews.Add(new WorkoutReview { WorkoutSession = session, Rating = 5, Comment = "Strong training session" });

        db.DailyNutritions.Add(new DailyNutrition
        {
            User = user,
            Date = DateTime.UtcNow.Date,
            CaloriesKcal = 2550,
            ProteinG = 165,
            FatG = 70,
            CarbsG = 280
        });

        var seededBenchAchievement = db.Achievements.FirstOrDefault(a => a.Code == "BENCH_100");
        if (seededBenchAchievement != null)
        {
            db.UserAchievements.Add(new UserAchievement { User = user, Achievement = seededBenchAchievement, AchievedAt = DateTime.UtcNow.AddDays(-5) });
        }

        db.PersonalRecords.AddRange(
            new PersonalRecord { User = user, Exercise = FindExerciseByAnyName(exercises, "Bench Press", "Жим лежачи"), MaxWeightKg = 105, UpdatedAt = DateTime.UtcNow.AddDays(-10) },
            new PersonalRecord { User = user, Exercise = FindExerciseByAnyName(exercises, "Barbell Back Squat"), MaxWeightKg = 155, UpdatedAt = DateTime.UtcNow.AddDays(-12) });

        db.Friendships.Add(new Friendship { User1 = user, User2 = friend, Status = "Accepted" });

        db.SaveChanges();
    }

    private static Exercise FindExerciseByAnyName(IReadOnlyDictionary<string, Exercise> exercises, params string[] names)
    {
        foreach (var name in names)
        {
            if (exercises.TryGetValue(name, out var exercise))
            {
                return exercise;
            }
        }

        return exercises.Values.First();
    }

}
