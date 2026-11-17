using System.Net;
using IronMindGym.Models;
using IronMindGym.Services;
using IronMindGym.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using static IronMindGym.Tests.TestApplication;

namespace IronMindGym.Tests;

public class AthleteWorkflowTests
{
    [Fact]
    public async Task CircuitRetainsSetOrderAndCountsEachExerciseOncePerWorkout()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var ids = app.Query(db => db.Exercises.Where(e => e.Type == ExerciseType.StrengthTraining).Take(2).Select(e => e.Id).ToArray());
        var sessionId = Save(app, [(ids[0], 20, 8, 0), (ids[1], 30, 10, 0), (ids[0], 25, 6, 0), (ids[1], 35, 8, 0)]);
        app.Query(db =>
        {
            var session = db.WorkoutSessions.Include(s => s.PerformedExercises).ThenInclude(p => p.Exercise)
                .Include(s => s.PerformedExercises).ThenInclude(p => p.Sets).Single(s => s.Id == sessionId);
            var blocks = session.PerformedExercises.OrderBy(p => p.OrderInWorkout).ToList();
            Assert.Equal(new[] { ids[0], ids[1], ids[0], ids[1] }, blocks.Select(p => p.ExerciseId));
            Assert.Equal(new[] { 1, 1, 2, 2 }, blocks.SelectMany(p => p.Sets).Select(s => s.SetNumber));
            var stats = new WorkoutService(db).BuildExerciseStats(session);
            Assert.Equal(2, stats.Count);
            Assert.All(stats, s => Assert.Equal(2, s.SetsCount));
            Assert.Equal(2, db.PersonalRecords.Count());
            Assert.Equal(1, new StatisticsService(db).Build(session.UserId).FavoriteExerciseCount);
            return true;
        });
    }

    [Fact]
    public async Task BodyweightDoesNotCreateLoadRecordAndDeletingLastWeightedWorkoutRemovesRecord()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var exercise = app.Query(db => db.Exercises.Single(e => e.Name == "Push-Ups").Id);
        var bodyweightId = Save(app, [(exercise, 0, 20, 0)]);
        var weightedId = Save(app, [(exercise, 10, 8, 0)]);
        app.Query(db =>
        {
            var service = new WorkoutService(db);
            var session = db.WorkoutSessions.Include(s => s.PerformedExercises).ThenInclude(p => p.Exercise)
                .Include(s => s.PerformedExercises).ThenInclude(p => p.Sets).Single(s => s.Id == bodyweightId);
            Assert.False(service.BuildExerciseStats(session).Single().IsNewRecord);
            Assert.True(service.Delete(session.UserId, weightedId));
            Assert.Empty(db.PersonalRecords);
            return true;
        });
        var html = await client.GetStringAsync($"/WorkoutLogger/Details/{bodyweightId}");
        Assert.DoesNotContain("New record", html);
    }

    [Fact]
    public async Task HistoricalRecordComparisonIgnoresLaterWorkoutsAndShowsFailureSetType()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var exercise = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        var first = Save(app, [(exercise, 40, 8, 0)]);
        var second = Save(app, [(exercise, 50, 5, 0)]);
        var later = Save(app, [(exercise, 60, 3, 0)]);
        app.Query(db =>
        {
            var date = DateTime.UtcNow.Date;
            foreach (var session in db.WorkoutSessions)
                session.SessionDate = date;
            var failure = db.SetTypes.Single(t => t.Name == "Failure").Id;
            db.Sets.Single(s => s.PerformedExercise.WorkoutSessionId == second).SetTypeId = failure;
            db.SaveChanges();
            var middle = db.WorkoutSessions.Include(s => s.PerformedExercises).ThenInclude(p => p.Exercise)
                .Include(s => s.PerformedExercises).ThenInclude(p => p.Sets).Single(s => s.Id == second);
            var result = new WorkoutService(db).BuildExerciseStats(middle).Single();
            Assert.True(result.IsNewRecord);
            Assert.Equal(125, result.PeakLoad);
            return true;
        });
        var html = await client.GetStringAsync($"/WorkoutLogger/Details/{second}");
        Assert.Contains("Failure", html);
        Assert.Contains("125%", html);
    }

    [Fact]
    public async Task TimedLoadsNeverUseStrengthRecordPercentages()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var exercise = app.Query(db => db.Exercises.Single(e => e.Name == "Weighted Plank").Id);
        Save(app, [(exercise, 10, 0, 60)]);
        var id = Save(app, [(exercise, 20, 0, 60)]);
        app.Query(db =>
        {
            var session = db.WorkoutSessions.Include(s => s.PerformedExercises).ThenInclude(p => p.Exercise)
                .Include(s => s.PerformedExercises).ThenInclude(p => p.Sets).Single(s => s.Id == id);
            var result = new WorkoutService(db).BuildExerciseStats(session).Single();
            Assert.Null(result.AverageLoad);
            Assert.Null(result.PeakLoad);
            Assert.False(result.IsNewRecord);
            return true;
        });
    }

    [Fact]
    public async Task AtomicProgressSaveRollsBackBothNewAndExistingBaselinesWhenCurrentResultIsInvalid()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var exercise = app.Query(db => db.Exercises.First(e => e.Type == ExerciseType.StrengthTraining).Id);
        async Task<HttpResponseMessage> SaveProgress(string initial, string currentReps) => await Post(client,
            "/ProgressTracker/TrackExercise", "/ProgressTracker/SaveResults", new()
            {
                ["ExerciseId"] = exercise.ToString(),
                ["InitialValue1"] = initial,
                ["InitialValue2"] = "8",
                ["CurrentValue1"] = "60",
                ["CurrentValue2"] = currentReps
            });
        Assert.Equal(HttpStatusCode.BadRequest, (await SaveProgress("40", "501")).StatusCode);
        Assert.Equal(0, app.Query(db => db.ExerciseProgress.Count()));
        Assert.Equal(HttpStatusCode.OK, (await SaveProgress("40", "8")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SaveProgress("50", "1.5")).StatusCode);
        Assert.Equal(40, app.Query(db => db.ExerciseProgress.Single().InitialWeightKg));
        Assert.Equal(8, app.Query(db => db.ExerciseProgress.Single().CurrentReps));
    }

    [Fact]
    public async Task ProgressDisplaysRepsForBodyweightAndSeparateCardioEffortChanges()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        app.Query(db =>
        {
            var userId = db.Users.Single().Id;
            var service = new ExerciseProgressService(db);
            var pushup = db.Exercises.Single(e => e.Name == "Push-Ups").Id;
            var cardio = db.Exercises.First(e => e.Type == ExerciseType.Cardio).Id;
            service.SaveResults(userId, new SaveProgressViewModel { ExerciseId = pushup, InitialValue1 = 0, InitialValue2 = 10, CurrentValue1 = 0, CurrentValue2 = 15 });
            service.SaveResults(userId, new SaveProgressViewModel { ExerciseId = cardio, InitialValue1 = 60, InitialValue2 = 5, CurrentValue1 = 60, CurrentValue2 = 10 });
            return true;
        });
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/ProgressTracker"));
        Assert.Contains("+5 reps", html);
        Assert.Contains("+5 effort", html);
        Assert.DoesNotContain("100.0%", html);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(100, null)]
    [InlineData(null, 60)]
    public void PartialPersonalTargetsDoNotInventCarbohydrateTarget(int? protein, int? fat)
    {
        var result = new NutritionAdviceService().BuildAdvice(new DailyNutrition(), new NutritionProfileSnapshot
        {
            CalorieTarget = 2000,
            ProteinTarget = protein,
            FatTarget = fat,
            EnergyEquation = "None"
        }, []);
        Assert.Equal(0, result.TargetCarbsG);
        Assert.Contains("Set a carbohydrate target", result.CarbsFormulaText);
    }

    [Fact]
    public void CompletePersonalMacroInputsDeriveRemainingEnergyAndExplainOverBudget()
    {
        var service = new NutritionAdviceService();
        var profile = new NutritionProfileSnapshot { CalorieTarget = 2000, ProteinTarget = 100, FatTarget = 80 };
        Assert.Equal(220, service.BuildAdvice(new DailyNutrition(), profile, []).TargetCarbsG);
        profile.CalorieTarget = 500;
        var result = service.BuildAdvice(new DailyNutrition(), profile, []);
        Assert.Equal(0, result.TargetCarbsG);
        Assert.Contains(result.Tips, tip => tip.Contains("already exceed"));
    }

    [Fact]
    public async Task CatalogMigrationPreservesSavedExerciseIdsAndDurations()
    {
        using var app = new TestApplication();
        using var client = app.Browser();
        await Register(client);
        var exercise = app.Query(db => db.Exercises.Single(e => e.Name == "Comprehensive full-body mobilization").Id);
        Save(app, [(exercise, 0, 0, 120)]);
        app.Query(db =>
        {
            var migrator = db.GetService<IMigrator>();
            migrator.Migrate("20261117125932_InitialCreate");
            db.PersonalRecords.Add(new PersonalRecord
            {
                UserId = db.Users.Single().Id,
                ExerciseId = db.Exercises.Single(e => e.Name == "Push-Ups").Id,
                MaxWeightKg = 0,
                UpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
            migrator.Migrate();
            db.ChangeTracker.Clear();
            var updated = db.Exercises.Include(e => e.MuscleSubGroup).ThenInclude(s => s.MuscleGroup).Single(e => e.Id == exercise);
            Assert.Equal(ExerciseType.Timed, updated.Type);
            Assert.Equal("Mobility", updated.MuscleSubGroup.MuscleGroup.Name);
            Assert.Equal(120, db.Sets.Single().DurationSeconds);
            Assert.Equal(exercise, db.PerformedExercises.Single().ExerciseId);
            Assert.Empty(db.PersonalRecords);
            return true;
        });
    }

    private static int Save(TestApplication app, (int Exercise, double Weight, int Reps, int Duration)[] sets) => app.Query(db =>
        new WorkoutService(db).Save(db.Users.Single().Id, new WorkoutLoggerViewModel
        {
            SplitDayLabel = "Full Body",
            DurationMinutes = 45,
            DraftPayloadJson = System.Text.Json.JsonSerializer.Serialize(sets.Select(s => new DraftSetItemViewModel
            {
                ExerciseId = s.Exercise,
                WeightKg = s.Weight,
                Reps = s.Reps,
                DurationSeconds = s.Duration,
                Intensity = 5,
                SetTypeId = db.SetTypes.First().Id
            }))
        }));
}
