using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Tests.Domain;

public class AchievementServiceTests
{
    private static AchievementService Awards(IronMindDbContext db) => new(db, new AthleteCalendars(db, TimeProvider.System));

    private static int Lift(IronMindDbContext db, KeyLift lift) => db.Exercises.Single(e => e.KeyLift == lift).Id;

    private static int[] Users(IronMindDbContext db) => db.Users.OrderBy(u => u.Id).Select(u => u.Id).ToArray();

    private static async Task<TestApplication> WithAthletes(params string[] others)
    {
        var app = new TestApplication();
        using (var api = app.Api())
            await api.RegisterWithSetup();
        foreach (var username in others)
        {
            using var api = app.Api();
            await api.Register(username);
        }
        return app;
    }

    [Fact]
    public async Task EveryMetricCountsOnlyTheAthletesOwnData()
    {
        using var app = await WithAthletes("friend", "stranger");
        var (bench, squat, deadlift) = app.Query(db => (Lift(db, KeyLift.BenchPress), Lift(db, KeyLift.Squat), Lift(db, KeyLift.Deadlift)));
        var now = DateTime.UtcNow;
        // Three days in a row, two of them at least an hour long, and one workout outside the 30-day tonnage window.
        var oldest = Journal.AddWorkout(app, now.AddDays(-40), 30, (deadlift, 120, 1, 0));
        Journal.AddWorkout(app, now.AddDays(-2), 60, (bench, 60, 5, 90), (bench, 60, 5, 90));
        Journal.AddWorkout(app, now.AddDays(-1), 45, (squat, 100, 5, 90));
        Journal.AddWorkout(app, now, 75, (bench, 40, 10, 60));
        app.Query(db =>
        {
            var users = Users(db);
            db.WorkoutSessions.Single(s => s.Id == oldest).Notes = "Heavy pull";
            db.WorkoutSessions.Add(new WorkoutSession { UserId = users[1], SessionDate = now, DurationMinutes = 90, Notes = "Not mine" });
            db.DailyNutritions.AddRange(
                new DailyNutrition { UserId = users[0], Date = new DateOnly(2026, 11, 1), CaloriesKcal = 2500 },
                new DailyNutrition { UserId = users[0], Date = new DateOnly(2026, 11, 2), CaloriesKcal = 2400 },
                new DailyNutrition { UserId = users[1], Date = new DateOnly(2026, 11, 2), CaloriesKcal = 2000 });
            db.Friendships.AddRange(
                new Friendship { User1Id = users[0], User2Id = users[1], RequestedByUserId = users[0], Status = FriendshipStatus.Accepted },
                new Friendship { User1Id = users[0], User2Id = users[2], RequestedByUserId = users[2], Status = FriendshipStatus.Pending });
            return db.SaveChanges();
        });

        var awards = app.Query(db => Awards(db).Progress(Users(db)[0])).ToDictionary(a => a.Code);

        Assert.Equal(4, awards["WORKOUT_005"].CurrentValue);
        Assert.Equal(5, awards["SETS_050"].CurrentValue);
        Assert.Equal(60 * 5 * 2 + 100 * 5 + 40 * 10, awards["TONNAGE_5K"].CurrentValue);
        Assert.Equal(60, awards["BENCH_80"].CurrentValue);
        Assert.Equal(100, awards["SQUAT_140"].CurrentValue);
        Assert.Equal(120, awards["DEADLIFT_180"].CurrentValue);
        Assert.Equal(2, awards["NUTRITION_007"].CurrentValue);
        Assert.Equal(1, awards["FRIENDS_003"].CurrentValue);
        Assert.Equal(3, awards["STREAK_007"].CurrentValue);
        Assert.Equal(2, awards["LONG_005"].CurrentValue);
        Assert.Equal((2.0, 5.0, 40.0), (awards["LONG_005"].CurrentValue, awards["LONG_005"].TargetValue, awards["LONG_005"].ProgressPercent));
    }

    [Fact]
    public async Task AwardsAreEarnedAtTheirTargetAndStayEarned()
    {
        using var app = await WithAthletes();
        var bench = app.Query(db => Lift(db, KeyLift.BenchPress));
        var workout = Journal.AddWorkout(app, DateTime.UtcNow, 40, (bench, 40, 5, 90));

        app.Query(db =>
        {
            Awards(db).AwardEarned(Users(db)[0]);
            return 0;
        });
        Assert.Equal(["BENCH_40", "PROFILE_SETUP", "WORKOUT_001"], Earned(app));

        app.Query(db =>
        {
            db.WorkoutSessions.Where(s => s.Id == workout).ExecuteDelete();
            db.PersonalRecords.ExecuteDelete();
            Awards(db).AwardEarned(Users(db)[0]);
            return 0;
        });
        Assert.Equal(["BENCH_40", "PROFILE_SETUP", "WORKOUT_001"], Earned(app));
    }

    [Fact]
    public async Task YesOrNoAwardsShowNoCount()
    {
        using var app = await WithAthletes();

        var awards = app.Query(db =>
        {
            Awards(db).AwardEarned(Users(db)[0]);
            return Awards(db).Progress(Users(db)[0]).ToDictionary(a => a.Code);
        });

        var setup = awards["PROFILE_SETUP"];
        Assert.Equal((true, false, 0.0, 0.0, 100.0), (setup.IsUnlocked, setup.IsNumeric, setup.CurrentValue, setup.TargetValue, setup.ProgressPercent));
        var review = awards["REVIEW_FIRST"];
        Assert.Equal((false, false, 0.0), (review.IsUnlocked, review.IsNumeric, review.ProgressPercent));
    }

    private static List<string> Earned(TestApplication app) =>
        app.Query(db => db.UserAchievements.Select(a => a.Achievement.Code).ToList()).Order(StringComparer.Ordinal).ToList();
}
