using IronMindGym.Models;
using IronMindGym.Security;
using IronMindGym.Seed;
using IronMindGym.Services;
using IronMindGym.Tests.Api;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IronMindGym.Tests.Domain;

public class SeedingTests
{
    [Fact]
    public void SeedingTwiceKeepsOneConsistentCatalog()
    {
        using var app = new TestApplication();
        app.Query(db =>
        {
            var counts = (db.Exercises.Count(), db.Avatars.Count(), db.ActivityFeeds.Count(), db.Achievements.Count());

            DataSeeder.Seed(db);

            Assert.Equal(counts, (db.Exercises.Count(), db.Avatars.Count(), db.ActivityFeeds.Count(), db.Achievements.Count()));
            Assert.Equal(ExerciseCatalogSeeder.Definitions().Count, db.Exercises.Count());
            Assert.Equal(AchievementCatalog.All.Count, db.Achievements.Count());
            Assert.Equal(DataSeeder.AvatarLadder.Length, db.Avatars.Count());
            Assert.Equal("Hamstrings", db.Exercises.Include(e => e.MuscleSubGroup).Single(e => e.Name == "Romanian Deadlift").MuscleSubGroup.Name);
            Assert.Equal(ExerciseType.Timed, db.Exercises.Single(e => e.Name == "Weighted Plank").Type);
            Assert.Equal(0, db.Users.Count());
            return true;
        });
    }

    [Fact]
    public void DemoDataAddsTwoConnectedAthletesOnlyToAnEmptyDatabase()
    {
        using var app = new TestApplication();
        app.Query(db =>
        {
            DataSeeder.Seed(db, includeDemoData: true);
            DataSeeder.Seed(db, includeDemoData: true);

            var demo = db.Users.Include(u => u.Profile).Single(u => u.Email == "demo@ironmind.app");
            Assert.Equal(2, db.Users.Count());
            Assert.True(PasswordHashUtility.Verify("DemoTraining123!", demo.PasswordHash));
            var friendship = db.Friendships.Single();
            Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
            Assert.Contains(friendship.RequestedByUserId, new[] { friendship.User1Id, friendship.User2Id });
            Assert.Equal(3, db.WorkoutSessions.Count(s => s.UserId == demo.Id));

            // Records and awards come from the logged sets, never from the seeder itself.
            var records = db.PersonalRecords.Where(r => r.UserId == demo.Id).ToDictionary(r => r.Exercise.Name, r => r.MaxWeightKg);
            Assert.Equal(100, records["Flat Bench Press"]);
            Assert.Equal(150, records["Barbell Back Squat"]);
            var awards = db.UserAchievements.Where(a => a.UserId == demo.Id).Select(a => a.Achievement.Code).ToList();
            Assert.Contains("BENCH_100", awards);
            Assert.DoesNotContain("BENCH_120", awards);
            Assert.Contains("FRIENDS_001", awards);
            return true;
        });
    }

    [Fact]
    public async Task ReferenceDataInUseCannotBeDeleted()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.RegisterWithSetup();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        await ApiClient.Ok(await api.Post("/api/workouts", Journal.Workout((bench, 60, 8))));

        app.Query(db =>
        {
            var groupId = db.Exercises.Where(e => e.Id == bench).Select(e => e.MuscleSubGroup.MuscleGroupId).Single();
            var refused = Assert.Throws<PostgresException>(() => db.MuscleGroups.Where(g => g.Id == groupId).ExecuteDelete());
            Assert.Equal(PostgresErrorCodes.RestrictViolation, refused.SqlState);
            Assert.Throws<PostgresException>(() => db.SetTypes.ExecuteDelete());
            Assert.Throws<PostgresException>(() => db.SplitTypes.ExecuteDelete());
            Assert.Throws<PostgresException>(() => db.Achievements.ExecuteDelete());
            Assert.Equal(1, db.WorkoutSessions.Count());
            return true;
        });
    }

    [Fact]
    public void ReferenceDataGetsTheTranslationsItIsMissing()
    {
        using var app = new TestApplication();
        app.Query(db =>
        {
            foreach (var exercise in db.Exercises)
                (exercise.NameUk, exercise.DescriptionUk) = (null, null);
            foreach (var group in db.MuscleGroups)
                group.NameUk = null;
            foreach (var setType in db.SetTypes)
                setType.NameUk = null;
            foreach (var avatar in db.Avatars)
                avatar.NameUk = null;
            db.Exercises.Single(e => e.Name == "Deadlift").Description = "Own notes";
            db.SaveChanges();

            DataSeeder.Seed(db);

            Assert.Equal(0, db.Exercises.Count(e => e.NameUk == null));
            Assert.Equal("Станова тяга", db.Exercises.Single(e => e.Name == "Deadlift").NameUk);
            Assert.Null(db.Exercises.Single(e => e.Name == "Deadlift").DescriptionUk);
            Assert.Equal("Груди", db.MuscleGroups.Single(g => g.Name == "Chest").NameUk);
            Assert.Equal("Робочий", db.SetTypes.Single(s => s.Name == "Working").NameUk);
            Assert.Equal(0, db.Avatars.Count(a => a.NameUk == null));
            Assert.Equal(ExerciseCatalogSeeder.Definitions().Count, db.Exercises.Count());
            return true;
        });
    }
}
