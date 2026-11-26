using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Tests.Domain;

public class WorkoutRecordTests
{
    private static WorkoutService Workouts(IronMindDbContext db) => new(db, new PersonalRecordService(db));

    private static int Save(TestApplication app, params (int Exercise, double Weight, int Reps, int Duration)[] sets) => app.Query(db =>
        Workouts(db).Save(db.Users.Single().Id, new SaveWorkoutRequest
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
        }));

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

        var id = Save(app, (ids[0], 20, 8, 0), (ids[1], 30, 10, 0), (ids[0], 25, 6, 0), (ids[1], 35, 8, 0));

        app.Query(db =>
        {
            var blocks = Load(db, id).PerformedExercises.OrderBy(p => p.OrderInWorkout).ToList();
            Assert.Equal([ids[0], ids[1], ids[0], ids[1]], blocks.Select(p => p.ExerciseId));
            Assert.Equal([1, 1, 2, 2], blocks.SelectMany(p => p.Sets).Select(s => s.SetNumber));
            var stats = Workouts(db).BuildExerciseStats(Load(db, id));
            Assert.Equal(2, stats.Count);
            Assert.All(stats, s => Assert.Equal(2, s.SetsCount));
            Assert.Equal(2, db.PersonalRecords.Count());
            Assert.Equal(1, new StatisticsService(db).Build(db.Users.Single().Id).FavoriteExerciseCount);
            return true;
        });
    }

    [Fact]
    public async Task BodyweightSetsSetNoLoadRecordAndDeletingTheLastWeightedWorkoutRemovesIt()
    {
        using var app = await WithAthlete();
        var pushUps = app.Query(db => db.Exercises.Single(e => e.Name == "Push-Ups").Id);
        var bodyweight = Save(app, (pushUps, 0, 20, 0));
        var weighted = Save(app, (pushUps, 10, 8, 0));

        app.Query(db =>
        {
            var service = Workouts(db);
            Assert.False(service.BuildExerciseStats(Load(db, bodyweight)).Single().IsNewRecord);
            Assert.True(service.Delete(db.Users.Single().Id, weighted));
            Assert.Empty(db.PersonalRecords);
            Assert.False(service.Delete(db.Users.Single().Id, weighted));
            return true;
        });
    }

    [Fact]
    public async Task ARecordIsTheHeaviestRemainingSetDatedByTheFirstWorkoutThatReachedIt()
    {
        using var app = await WithAthlete();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        var first = Journal.AddWorkout(app, new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc), 45, (bench, 70, 5, 90));
        Journal.AddWorkout(app, new DateTime(2026, 9, 8, 18, 0, 0, DateTimeKind.Utc), 45, (bench, 70, 6, 90));
        var heaviest = Save(app, (bench, 80, 3, 0));

        app.Query(db =>
        {
            Assert.Equal(80, db.PersonalRecords.Single().MaxWeightKg);
            Assert.True(Workouts(db).Delete(db.Users.Single().Id, heaviest));
            var record = db.PersonalRecords.AsNoTracking().Single();
            Assert.Equal((70, new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc)), (record.MaxWeightKg, record.UpdatedAt));
            Assert.True(Workouts(db).Delete(db.Users.Single().Id, first));
            Assert.Equal(new DateTime(2026, 9, 8, 18, 0, 0, DateTimeKind.Utc), db.PersonalRecords.AsNoTracking().Single().UpdatedAt);
            return true;
        });
    }

    [Fact]
    public async Task AHistoricalComparisonIgnoresLaterWorkouts()
    {
        using var app = await WithAthlete();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        Save(app, (bench, 40, 8, 0));
        var second = Save(app, (bench, 50, 5, 0));
        Save(app, (bench, 60, 3, 0));

        app.Query(db =>
        {
            foreach (var session in db.WorkoutSessions)
                session.SessionDate = DateTime.UtcNow.Date;
            db.SaveChanges();
            var result = Workouts(db).BuildExerciseStats(Load(db, second)).Single();
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
        Save(app, (plank, 10, 0, 60));
        var id = Save(app, (plank, 20, 0, 60));

        app.Query(db =>
        {
            var result = Workouts(db).BuildExerciseStats(Load(db, id)).Single();
            Assert.Null(result.PercentOfPreviousBest);
            Assert.False(result.IsNewRecord);
            Assert.False(result.TracksLoad);
            return true;
        });
    }
}
