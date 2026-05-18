using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Security;
using IronMindGym.Services;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Seed;

public static class DataSeeder
{
    public static void Seed(IronMindDbContext db)
    {
        EnsureSocialTables(db);
        EnsureSpecificUsersDemographics(db);
        EnsureEnglishReferenceData(db);
        EnsureAvatarLadder(db);
        EnsureResearchFeed(db);
        AchievementEngine.EnsureCatalog(db);

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
            new SetType { Name = "Working" },
            new SetType { Name = "Warm-up" },
            new SetType { Name = "Failure" }
        };
        db.SetTypes.AddRange(setTypes);

        var avatars = new[]
        {
            new Avatar { Name = "Noodle Nomad", MinStrengthCoefficient = 0.0, IconPath = "/images/avatar-novice.png" },
            new Avatar { Name = "Couch Crusader", MinStrengthCoefficient = 0.5, IconPath = "/images/avatar-novice.png" },
            new Avatar { Name = "Barbell Intern", MinStrengthCoefficient = 1.0, IconPath = "/images/avatar-novice.png" },
            new Avatar { Name = "Dumbbell Bard", MinStrengthCoefficient = 1.5, IconPath = "/images/avatar-novice.png" },
            new Avatar { Name = "Iron Squire", MinStrengthCoefficient = 2.0, IconPath = "/images/avatar-athlete.png" },
            new Avatar { Name = "Steel Disciple", MinStrengthCoefficient = 2.5, IconPath = "/images/avatar-athlete.png" },
            new Avatar { Name = "Gym Centurion", MinStrengthCoefficient = 3.0, IconPath = "/images/avatar-athlete.png" },
            new Avatar { Name = "Berserker Prime", MinStrengthCoefficient = 3.5, IconPath = "/images/avatar-athlete.png" },
            new Avatar { Name = "Titanium Overlord", MinStrengthCoefficient = 4.0, IconPath = "/images/avatar-titan.png" },
            new Avatar { Name = "Celestial Colossus", MinStrengthCoefficient = 4.5, IconPath = "/images/avatar-titan.png" },
            new Avatar { Name = "Omnipotent Origin", MinStrengthCoefficient = 5.0, IconPath = "/images/avatar-titan.png" }
        };
        db.Avatars.AddRange(avatars);

        var exercises = ExerciseCatalogSeeder.Seed(db);

        var user = new User
        {
            Username = "demo_athlete",
            Email = "demo@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("demo123"),
            Profile = new Profile { Age = 24, AccountCreatedAt = DateTime.UtcNow.AddDays(-40) },
            Benchmarks = new InitialBenchmarks { BenchPressKg = 100, SquatKg = 140, DeadliftKg = 170 }
        };

        var friend = new User
        {
            Username = "fit_friend",
            Email = "friend@ironmind.app",
            PasswordHash = PasswordHashUtility.Hash("friend123"),
            Profile = new Profile { Age = 23, AccountCreatedAt = DateTime.UtcNow.AddDays(-20) },
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
            Exercise = FindExerciseByAnyName(exercises, "Bench Press", "Жим лежачи"),
            OrderInWorkout = 1
        };

        performed.Sets.Add(new WorkoutSet { SetNumber = 1, SetType = setTypes[0], WeightKg = 80, Reps = 8 });
        performed.Sets.Add(new WorkoutSet { SetNumber = 2, SetType = setTypes[0], WeightKg = 85, Reps = 6 });
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
            new PersonalRecord { User = user, Exercise = FindExerciseByAnyName(exercises, "Barbell Back Squat", "Присідання зі штангою"), MaxWeightKg = 155, UpdatedAt = DateTime.UtcNow.AddDays(-12) });

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

    private static void EnsureSpecificUsersDemographics(IronMindDbContext db)
    {
        var targetUsernames = new HashSet<string>(StringComparer.Ordinal)
        {
            "AndrewKhrushch19",
            "PavloKhrushch"
        };

        var users = db.Users
            .Include(u => u.Profile)
            .Where(u => targetUsernames.Contains(u.Username))
            .ToList();

        if (users.Count == 0)
        {
            return;
        }

        foreach (var user in users)
        {
            user.Profile ??= new Profile { UserId = user.Id, AccountCreatedAt = DateTime.UtcNow };
            user.Profile.Age = 14;
        }

        db.SaveChanges();
    }

    private static void EnsureEnglishReferenceData(IronMindDbContext db)
    {
        var setTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Робочий"] = "Working",
            ["Розминка"] = "Warm-up",
            ["Відмова"] = "Failure"
        };

        foreach (var setType in db.SetTypes)
        {
            if (setTypeMap.TryGetValue(setType.Name, out var mappedName))
            {
                setType.Name = mappedName;
            }
        }

        var avatarMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Новачок"] = "Novice",
            ["Атлет"] = "Athlete",
            ["Титан"] = "Titan"
        };

        foreach (var avatar in db.Avatars)
        {
            if (avatarMap.TryGetValue(avatar.Name, out var mappedName))
            {
                avatar.Name = mappedName;
            }
        }

        AchievementEngine.EnsureCatalog(db);

        db.SaveChanges();
    }

    private static void EnsureAvatarLadder(IronMindDbContext db)
    {
        var targetAvatars = new[]
        {
            ("Noodle Nomad", 0.0, "/images/avatar-novice.png"),
            ("Couch Crusader", 0.5, "/images/avatar-novice.png"),
            ("Barbell Intern", 1.0, "/images/avatar-novice.png"),
            ("Dumbbell Bard", 1.5, "/images/avatar-novice.png"),
            ("Iron Squire", 2.0, "/images/avatar-athlete.png"),
            ("Steel Disciple", 2.5, "/images/avatar-athlete.png"),
            ("Gym Centurion", 3.0, "/images/avatar-athlete.png"),
            ("Berserker Prime", 3.5, "/images/avatar-athlete.png"),
            ("Titanium Overlord", 4.0, "/images/avatar-titan.png"),
            ("Celestial Colossus", 4.5, "/images/avatar-titan.png"),
            ("Omnipotent Origin", 5.0, "/images/avatar-titan.png")
        };

        var existingAvatars = db.Avatars.ToList();
        if (existingAvatars.Count > 0)
        {
            db.Avatars.RemoveRange(existingAvatars);
            db.SaveChanges();
        }

        db.Avatars.AddRange(targetAvatars.Select(x => new Avatar
        {
            Name = x.Item1,
            MinStrengthCoefficient = x.Item2,
            IconPath = x.Item3
        }));

        db.SaveChanges();
    }

    private static void EnsureResearchFeed(IronMindDbContext db)
    {
        var studies = new (string Title, string Thesis, string Source)[]
        {
            ("Resistance Training Volume and Hypertrophy", "Meta-analysis shows that higher weekly set volume generally improves muscle hypertrophy up to an individual recovery limit.", "Sports Medicine (Schoenfeld et al.)|https://pubmed.ncbi.nlm.nih.gov/27433992/"),
            ("Protein Intake for Strength Athletes", "A practical range of 1.6-2.2 g/kg/day supports recovery and muscle adaptation during resistance training.", "Journal of the International Society of Sports Nutrition|https://pubmed.ncbi.nlm.nih.gov/28698222/"),
            ("Creatine and Performance", "Creatine monohydrate consistently improves strength, repeated high-intensity effort, and lean mass over time.", "Nutrients Review|https://pubmed.ncbi.nlm.nih.gov/33526090/"),
            ("Sleep and Athletic Recovery", "Insufficient sleep reduces recovery quality, reaction speed, and can impair strength performance.", "Sports Medicine Review|https://pubmed.ncbi.nlm.nih.gov/27400005/"),
            ("Training Frequency and Strength", "When weekly volume is equated, frequency can be distributed across days to improve fatigue management.", "Sports Medicine Meta-analysis|https://pubmed.ncbi.nlm.nih.gov/30558493/"),
            ("Velocity Loss and Fatigue", "Higher velocity loss in sets increases fatigue and can alter adaptation; autoregulation helps maintain quality.", "Scandinavian Journal of Medicine and Science in Sports|https://pubmed.ncbi.nlm.nih.gov/31399902/"),
            ("Rest Intervals and Hypertrophy", "Longer rest intervals often maintain training quality and can improve hypertrophy outcomes in compound lifts.", "Journal of Strength and Conditioning Research|https://pubmed.ncbi.nlm.nih.gov/26605807/"),
            ("Energy Availability in Athletes", "Low energy availability can compromise performance, endocrine health, and recovery in both sexes.", "IOC Consensus Statement|https://pubmed.ncbi.nlm.nih.gov/30587314/"),
            ("Carbohydrates and Strength Training", "Sufficient carbohydrate availability supports training intensity, session quality, and total work output.", "Nutrients|https://pubmed.ncbi.nlm.nih.gov/32899481/"),
            ("Concurrent Training Interference", "Combining endurance and strength is possible, but scheduling and fatigue management are key to reduce interference.", "Sports Medicine|https://pubmed.ncbi.nlm.nih.gov/22002517/"),
            ("Deloads and Fatigue Management", "Planned reductions in volume/intensity can restore readiness and support long-term progression.", "Strength and Conditioning Journal|https://pubmed.ncbi.nlm.nih.gov/34567420/"),
            ("Women and Resistance Training", "Women gain significant strength and hypertrophy from progressive resistance training with sex-specific periodization considerations.", "Sports Medicine|https://pubmed.ncbi.nlm.nih.gov/29423684/")
        };

        foreach (var study in studies)
        {
            var existing = db.ResearchItems.FirstOrDefault(x => x.Title == study.Title);
            if (existing == null)
            {
                existing = new ResearchItem
                {
                    Title = study.Title,
                    ThesisText = study.Thesis,
                    Source = study.Source
                };
                db.ResearchItems.Add(existing);
                db.SaveChanges();
            }

            var existsInFeed = db.ActivityFeeds.Any(f => f.ResearchItemId == existing.Id);
            if (!existsInFeed)
            {
                db.ActivityFeeds.Add(new ActivityFeed
                {
                    ResearchItemId = existing.Id,
                    PublishedAt = DateTime.UtcNow.AddDays(-db.ActivityFeeds.Count())
                });
            }
        }

        db.SaveChanges();
    }

    private static void EnsureSocialTables(IronMindDbContext db)
    {
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ActivityLikes (
    Id INTEGER NOT NULL CONSTRAINT PK_ActivityLikes PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    WorkoutSessionId INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    CONSTRAINT FK_ActivityLikes_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ActivityLikes_WorkoutSessions_WorkoutSessionId FOREIGN KEY (WorkoutSessionId) REFERENCES WorkoutSessions (Id) ON DELETE CASCADE
);");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS FriendComments (
    Id INTEGER NOT NULL CONSTRAINT PK_FriendComments PRIMARY KEY AUTOINCREMENT,
    Text TEXT NOT NULL,
    AuthorId INTEGER NOT NULL,
    WorkoutSessionId INTEGER NOT NULL,
    Date TEXT NOT NULL,
    CONSTRAINT FK_FriendComments_Users_AuthorId FOREIGN KEY (AuthorId) REFERENCES Users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_FriendComments_WorkoutSessions_WorkoutSessionId FOREIGN KEY (WorkoutSessionId) REFERENCES WorkoutSessions (Id) ON DELETE CASCADE
);");

        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_ActivityLikes_UserId_WorkoutSessionId ON ActivityLikes (UserId, WorkoutSessionId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ActivityLikes_WorkoutSessionId ON ActivityLikes (WorkoutSessionId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FriendComments_WorkoutSessionId ON FriendComments (WorkoutSessionId);");
    }
}
