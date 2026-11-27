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
    public async Task SeedingTwiceKeepsOneConsistentCatalog()
    {
        using var app = new TestApplication();
        await app.QueryAsync(async db =>
        {
            var counts = (db.Exercises.Count(), db.Avatars.Count(), db.ResearchItems.Count(), db.Achievements.Count());

            await DataSeeder.SeedAsync(db, includeDemoData: false, TimeProvider.System, CancellationToken.None);

            Assert.Equal(counts, (db.Exercises.Count(), db.Avatars.Count(), db.ResearchItems.Count(), db.Achievements.Count()));
            Assert.Equal(ExerciseCatalogSeeder.Definitions().Count, db.Exercises.Count());
            Assert.Equal(AchievementCatalog.All.Count, db.Achievements.Count());
            Assert.Equal(DataSeeder.AvatarLadder.Length, db.Avatars.Count());
            Assert.Equal(DataSeeder.ResearchNotes.Length, db.ResearchItems.Count());
            Assert.All(db.ResearchItems.ToList(), r => Assert.StartsWith("https://", r.SourceUrl, StringComparison.Ordinal));
            Assert.Equal("Hamstrings", db.Exercises.Include(e => e.MuscleSubGroup).Single(e => e.Name == "Romanian Deadlift").MuscleSubGroup.Name);
            Assert.Equal(ExerciseType.Timed, db.Exercises.Single(e => e.Name == "Weighted Plank").Type);
            Assert.Equal(
                ["Flat Bench Press", "Deadlift", "Barbell Back Squat"],
                db.Exercises.Where(e => e.KeyLift != null).OrderBy(e => e.KeyLift).Select(e => e.Name).ToList());
            Assert.Equal(DataSeeder.SplitTypes.Select(s => s.Code), db.SplitTypes.OrderBy(s => s.Id).Select(s => s.Code));
            Assert.Equal(DataSeeder.SetTypes.Select(s => s.Code), db.SetTypes.OrderBy(s => s.Id).Select(s => s.Code));
            Assert.Equal(0, db.Users.Count());
            return true;
        });
    }

    [Fact]
    public async Task DemoDataAddsTwoConnectedAthletesOnlyToAnEmptyDatabase()
    {
        using var app = new TestApplication();
        await app.QueryAsync(async db =>
        {
            await DataSeeder.SeedAsync(db, includeDemoData: true, TimeProvider.System, CancellationToken.None);
            await DataSeeder.SeedAsync(db, includeDemoData: true, TimeProvider.System, CancellationToken.None);

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

    [Theory]
    [InlineData(SplitTypeCode.LegsBackChest, new[] { "Legs", "Back", "Chest" })]
    [InlineData(SplitTypeCode.UpperLower, new[] { "Upper", "Lower" })]
    [InlineData(SplitTypeCode.FullBody, new[] { "Full Body" })]
    public void EverySplitHasItsTrainingDaysInOrder(string split, string[] days)
    {
        using var app = new TestApplication();

        Assert.Equal(days, app.Query(db => db.SplitDays.Where(d => d.SplitType.Code == split).OrderBy(d => d.Position).Select(d => d.Name).ToList()));
    }

    [Fact]
    public async Task ReferenceRowsAddedToTheCatalogReachAnExistingDatabase()
    {
        using var app = new TestApplication();
        await app.QueryAsync(async db =>
        {
            db.Exercises.Where(e => e.MuscleSubGroup.Name == "Calves").ExecuteDelete();
            db.MuscleSubGroups.Where(s => s.Name == "Calves").ExecuteDelete();
            db.SplitDays.Where(d => d.Code == "CHEST").ExecuteDelete();
            db.Avatars.Single(a => a.Name == "Iron Squire").MinStrengthCoefficient = 9;
            db.SaveChanges();

            await DataSeeder.SeedAsync(db, includeDemoData: false, TimeProvider.System, CancellationToken.None);

            var calves = ExerciseCatalogSeeder.Definitions().Where(d => d.Subgroup == "Calves").Select(d => d.Name).Order().ToList();
            Assert.NotEmpty(calves);
            Assert.Equal(calves, db.Exercises.Where(e => e.MuscleSubGroup.Name == "Calves" && e.MuscleSubGroup.MuscleGroup.Name == "Legs")
                .Select(e => e.Name).Order().ToList());
            Assert.Equal("Литки", db.MuscleSubGroups.Single(s => s.Name == "Calves").NameUk);
            Assert.Equal(3, db.SplitDays.Single(d => d.Code == "CHEST").Position);
            Assert.Equal(2, db.Avatars.Single(a => a.Name == "Iron Squire").MinStrengthCoefficient);
            return true;
        });
    }

    [Fact]
    public async Task ReferenceDataGetsTheTranslationsItIsMissing()
    {
        using var app = new TestApplication();
        await app.QueryAsync(async db =>
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

            await DataSeeder.SeedAsync(db, includeDemoData: false, TimeProvider.System, CancellationToken.None);

            Assert.Equal(0, db.Exercises.Count(e => e.NameUk == null));
            Assert.Equal("Станова тяга", db.Exercises.Single(e => e.Name == "Deadlift").NameUk);
            Assert.Null(db.Exercises.Single(e => e.Name == "Deadlift").DescriptionUk);
            Assert.Equal("Груди", db.MuscleGroups.Single(g => g.Name == "Chest").NameUk);
            Assert.Equal("Робочий", db.SetTypes.Single(s => s.Code == SetTypeCode.Working).NameUk);
            Assert.Equal(0, db.Avatars.Count(a => a.NameUk == null));
            Assert.Equal(ExerciseCatalogSeeder.Definitions().Count, db.Exercises.Count());
            return true;
        });
    }
}
