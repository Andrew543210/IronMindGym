using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Tests.Domain;

public class WorkoutRecordTests
{
    private static WorkoutService Workouts(IronMindDbContext db)
    {
        var calendars = new AthleteCalendars(db, TimeProvider.System);
        return new(db, TimeProvider.System, calendars, new ExerciseCatalog(db), new PersonalRecordService(db), new AchievementService(db, calendars));
    }

    private static Task<int> SaveAsync(TestApplication app, params (int Exercise, double Weight, int Reps, int Duration)[] sets) => app.QueryAsync(db =>
        Workouts(db).SaveAsync(db.Users.Single().Id, new SaveWorkoutRequest
        {
            DurationMinutes = 45,
            Sets = sets.Select(s => new WorkoutSetInput
            {
                ExerciseId = s.Exercise,
                SetTypeId = db.SetTypes.Single(t => t.Code == SetTypeCode.Working).Id,
                WeightKg = s.Weight,
                Reps = s.Reps,
                DurationSeconds = s.Duration,
                Intensity = 5
            }).ToList()
        }, CancellationToken.None));

    private static WorkoutSession Load(IronMindDbContext db, int id) => db.WorkoutSessions
        .Include(s => s.PerformedExercises).ThenInclude(p => p.Exercise)
        .Include(s => s.PerformedExercises).ThenInclude(p => p.Sets)
        .Single(s => s.Id == id);

    private static async Task<TestApplication> WithAthlete()
    {
        var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        return app;
    }

    [Fact]
    public async Task ACircuitKeepsItsOrderAndCountsEachExerciseOncePerWorkout()
    {
        using var app = await WithAthlete();
        var ids = app.Query(db => db.Exercises.Where(e => e.Type == ExerciseType.StrengthTraining).OrderBy(e => e.Id).Take(2).Select(e => e.Id).ToArray());

        var id = await SaveAsync(app, (ids[0], 20, 8, 0), (ids[1], 30, 10, 0), (ids[0], 25, 6, 0), (ids[1], 35, 8, 0));

        await app.QueryAsync(async db =>
        {
            var blocks = Load(db, id).PerformedExercises.OrderBy(p => p.OrderInWorkout).ToList();
            Assert.Equal([ids[0], ids[1], ids[0], ids[1]], blocks.Select(p => p.ExerciseId));
            Assert.Equal([1, 1, 2, 2], blocks.SelectMany(p => p.Sets).Select(s => s.SetNumber));
            var stats = await Workouts(db).BuildExerciseStatsAsync(Load(db, id), CancellationToken.None);
            Assert.Equal(2, stats.Count);
            Assert.All(stats, s => Assert.Equal(2, s.SetsCount));
            Assert.Equal(2, db.PersonalRecords.Count());
            var statistics = new StatisticsService(db, new AthleteCalendars(db, TimeProvider.System));
            Assert.Equal(1, (await statistics.BuildAsync(db.Users.Single().Id, CancellationToken.None)).FavoriteExerciseCount);
            return true;
        });
    }

    [Fact]
    public async Task BodyweightSetsSetNoLoadRecordAndDeletingTheLastWeightedWorkoutRemovesIt()
    {
        using var app = await WithAthlete();
        var pushUps = app.Query(db => db.Exercises.Single(e => e.Name == "Push-Ups").Id);
        var bodyweight = await SaveAsync(app, (pushUps, 0, 20, 0));
        var weighted = await SaveAsync(app, (pushUps, 10, 8, 0));

        await app.QueryAsync(async db =>
        {
            var service = Workouts(db);
            Assert.False((await service.BuildExerciseStatsAsync(Load(db, bodyweight), CancellationToken.None)).Single().IsNewRecord);
            Assert.True(await service.DeleteAsync(db.Users.Single().Id, weighted, CancellationToken.None));
            Assert.Empty(db.PersonalRecords);
            Assert.False(await service.DeleteAsync(db.Users.Single().Id, weighted, CancellationToken.None));
            return true;
        });
    }

    [Fact]
    public async Task ARecordIsTheHeaviestRemainingSetDatedByTheFirstWorkoutThatReachedIt()
    {
        using var app = await WithAthlete();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        var first = await Journal.AddWorkoutAsync(app, new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc), 45, (bench, 70, 5, 90));
        await Journal.AddWorkoutAsync(app, new DateTime(2026, 9, 8, 18, 0, 0, DateTimeKind.Utc), 45, (bench, 70, 6, 90));
        var heaviest = await SaveAsync(app, (bench, 80, 3, 0));

        await app.QueryAsync(async db =>
        {
            Assert.Equal(80, db.PersonalRecords.Single().MaxWeightKg);
            Assert.True(await Workouts(db).DeleteAsync(db.Users.Single().Id, heaviest, CancellationToken.None));
            var record = db.PersonalRecords.AsNoTracking().Single();
            Assert.Equal((70, new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc)), (record.MaxWeightKg, record.AchievedAt));
            Assert.True(await Workouts(db).DeleteAsync(db.Users.Single().Id, first, CancellationToken.None));
            Assert.Equal(new DateTime(2026, 9, 8, 18, 0, 0, DateTimeKind.Utc), db.PersonalRecords.AsNoTracking().Single().AchievedAt);
            return true;
        });
    }

    [Fact]
    public async Task AHistoricalComparisonIgnoresLaterWorkouts()
    {
        using var app = await WithAthlete();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        await SaveAsync(app, (bench, 40, 8, 0));
        var second = await SaveAsync(app, (bench, 50, 5, 0));
        await SaveAsync(app, (bench, 60, 3, 0));

        await app.QueryAsync(async db =>
        {
            foreach (var session in db.WorkoutSessions)
                session.SessionDate = DateTime.UtcNow.Date;
            db.SaveChanges();
            var result = (await Workouts(db).BuildExerciseStatsAsync(Load(db, second), CancellationToken.None)).Single();
            Assert.True(result.IsNewRecord);
            Assert.Equal(125, result.PercentOfPreviousBest);
            return true;
        });
    }

    [Fact]
    public async Task TimedLoadsNeverUseStrengthRecordPercentages()
    {
        using var app = await WithAthlete();
        var plank = app.Query(db => db.Exercises.Single(e => e.Name == "Weighted Plank").Id);
        await SaveAsync(app, (plank, 10, 0, 60));
        var id = await SaveAsync(app, (plank, 20, 0, 60));

        await app.QueryAsync(async db =>
        {
            var result = (await Workouts(db).BuildExerciseStatsAsync(Load(db, id), CancellationToken.None)).Single();
            Assert.Null(result.PercentOfPreviousBest);
            Assert.False(result.IsNewRecord);
            Assert.False(result.TracksLoad);
            return true;
        });
    }
}
