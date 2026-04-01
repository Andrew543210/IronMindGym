using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Security;

namespace IronMindGym.Seed;

public static class DataSeeder
{
    public static void Seed(IronMindDbContext db)
    {
        if (db.Users.Any())
        {
            return;
        }

        var splitTypes = new[]
        {
            new SplitType { Name = "Full-body" },
            new SplitType { Name = "Upper-Lower" },
            new SplitType { Name = "Legs-Back-Chest" }
        };
        db.SplitTypes.AddRange(splitTypes);

        var weekDays = new[]
        {
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
        }.Select(name => new WeekDay { Name = name }).ToList();
        db.WeekDays.AddRange(weekDays);

        var setTypes = new[]
        {
            new SetType { Name = "Робочий" },
            new SetType { Name = "Розминка" },
            new SetType { Name = "Відмова" }
        };
        db.SetTypes.AddRange(setTypes);

        var avatars = new[]
        {
            new Avatar { Name = "Новачок", MinStrengthCoefficient = 0.0, IconPath = "/images/avatar-novice.png" },
            new Avatar { Name = "Атлет", MinStrengthCoefficient = 3.0, IconPath = "/images/avatar-athlete.png" },
            new Avatar { Name = "Титан", MinStrengthCoefficient = 4.5, IconPath = "/images/avatar-titan.png" }
        };
        db.Avatars.AddRange(avatars);

        var exercises = ExerciseCatalogSeeder.Seed(db);

        var user = new User
        {
            Username = "demo_athlete",
            Email = "demo@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("demo123"),
            Profile = new Profile { Gender = "Male", Age = 24, AccountCreatedAt = DateTime.UtcNow.AddDays(-40) },
            Benchmarks = new InitialBenchmarks { BenchPressKg = 100, SquatKg = 140, DeadliftKg = 170 }
        };

        var friend = new User
        {
            Username = "fit_friend",
            Email = "friend@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("friend123"),
            Profile = new Profile { Gender = "Female", Age = 23, AccountCreatedAt = DateTime.UtcNow.AddDays(-20) },
            Benchmarks = new InitialBenchmarks { BenchPressKg = 65, SquatKg = 95, DeadliftKg = 120 }
        };

        db.Users.AddRange(user, friend);

        db.AnthropometryLogs.AddRange(
            new Anthropometry { User = user, WeightKg = 82, HeightCm = 180, RecordedAt = DateTime.UtcNow.Date },
            new Anthropometry { User = friend, WeightKg = 62, HeightCm = 168, RecordedAt = DateTime.UtcNow.Date });

        var plan = new WorkoutPlan
        {
            User = user,
            WorkoutsPerWeek = 4,
            IsActive = true,
            SplitType = splitTypes[1]
        };
        db.WorkoutPlans.Add(plan);

        db.ScheduledDays.AddRange(
            new ScheduledDay { WorkoutPlan = plan, WeekDay = weekDays[0] },
            new ScheduledDay { WorkoutPlan = plan, WeekDay = weekDays[2] },
            new ScheduledDay { WorkoutPlan = plan, WeekDay = weekDays[4] },
            new ScheduledDay { WorkoutPlan = plan, WeekDay = weekDays[5] });

        var session = new WorkoutSession
        {
            User = user,
            Name = "Upper Day",
            SessionDate = DateTime.UtcNow.AddDays(-1),
            DurationMinutes = 70,
            SplitType = splitTypes[1]
        };

        var performed = new PerformedExercise
        {
            WorkoutSession = session,
            Exercise = exercises["Жим лежачи"],
            OrderInWorkout = 1
        };

        performed.Sets.Add(new WorkoutSet { SetNumber = 1, SetType = setTypes[0], WeightKg = 80, Reps = 8 });
        performed.Sets.Add(new WorkoutSet { SetNumber = 2, SetType = setTypes[0], WeightKg = 85, Reps = 6 });
        db.PerformedExercises.Add(performed);

        db.WorkoutReviews.Add(new WorkoutReview { WorkoutSession = session, Rating = 5, Comment = "Сильне тренування" });

        db.DailyNutritions.Add(new DailyNutrition
        {
            User = user,
            Date = DateTime.UtcNow.Date,
            CaloriesKcal = 2550,
            ProteinG = 165,
            FatG = 70,
            CarbsG = 280
        });

        var research1 = new ResearchItem
        {
            Title = "Силовий обсяг і гіпертрофія",
            ThesisText = "Збільшення тренувального обсягу до помірної межі корелює з ростом м'язової маси.",
            Source = "Sports Medicine Journal"
        };

        var research2 = new ResearchItem
        {
            Title = "Білок для силових спортсменів",
            ThesisText = "Діапазон 1.6-2.2 г білка/кг маси тіла є ефективним для відновлення.",
            Source = "International Society of Sports Nutrition"
        };

        db.ResearchItems.AddRange(research1, research2);
        db.ActivityFeeds.AddRange(
            new ActivityFeed { ResearchItem = research1, PublishedAt = DateTime.UtcNow.AddDays(-1) },
            new ActivityFeed { ResearchItem = research2, PublishedAt = DateTime.UtcNow });

        var achievement = new Achievement
        {
            Name = "Перші 100 кг у жимі",
            ConditionDescription = "Досягнути жиму лежачи 100 кг"
        };
        db.Achievements.Add(achievement);
        db.UserAchievements.Add(new UserAchievement { User = user, Achievement = achievement, AchievedAt = DateTime.UtcNow.AddDays(-5) });

        db.PersonalRecords.AddRange(
            new PersonalRecord { User = user, Exercise = exercises["Жим лежачи"], MaxWeightKg = 105 },
            new PersonalRecord { User = user, Exercise = exercises["Присідання зі штангою"], MaxWeightKg = 155 });

        db.Friendships.Add(new Friendship { User1 = user, User2 = friend, Status = "Accepted" });

        db.SaveChanges();
    }
}
