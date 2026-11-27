using IronMindGym.Contracts;
using IronMindGym.Models;
using IronMindGym.Services;

namespace IronMindGym.Tests.Api;

public class StatisticsApiTests
{
    [Fact]
    public async Task SummaryAggregatesTheLoggedTraining()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var (squat, row) = app.Query(db =>
        {
            var strength = db.Exercises.Where(e => e.Type == ExerciseType.StrengthTraining).OrderBy(e => e.Id).Take(2).ToList();
            return (strength[0].Id, strength[1].Id);
        });
        var noon = DateTime.UtcNow.Date.AddHours(12);
        await Journal.AddWorkoutAsync(app, noon.AddDays(-2), 60, (squat, 100, 5, 120), (squat, 100, 5, 180), (row, 50, 10, 60));
        await Journal.AddWorkoutAsync(app, noon.AddDays(-3), 30, (squat, 90, 5, 90));
        await Journal.AddWorkoutAsync(app, noon.AddDays(-40), 45, (squat, 40, 10, 60));

        var stats = await api.Get<TrainingStatistics>("/api/statistics");

        Assert.Equal((3, 135, 45.0), (stats.WorkoutsCompleted, stats.TotalWorkoutMinutes, stats.AvgWorkoutDurationMinutes));
        Assert.Equal((5, 1.7, 102), (stats.TotalSetsCount, stats.AvgSetsPerWorkout, stats.AvgRestSeconds));
        Assert.Equal((2350.0, 1950.0, 1950.0), (stats.TotalVolume, stats.MonthlyTotalTonnage, stats.WeeklyProgress));
        Assert.Equal((2, 90), (stats.Last7DaysWorkouts, stats.Last7DaysMinutes));
        Assert.Equal((2, 2, 3), (stats.UniqueExercisesCount, stats.ExercisesThisMonth, stats.FavoriteExerciseCount));
        Assert.NotNull(stats.FavoriteExerciseName);
        Assert.Equal(2, stats.LongestStreak);
        Assert.InRange(stats.AvgWeeklyWorkouts, 0.5, 0.6);
    }

    [Fact]
    public async Task AnEmptyJournalHasNoStatistics()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();

        var stats = await api.Get<TrainingStatistics>("/api/statistics");

        Assert.Equal((0, 0, 0, 0), (stats.WorkoutsCompleted, stats.TotalSetsCount, stats.AvgRestSeconds, stats.LongestStreak));
        Assert.Null(stats.FavoriteExerciseName);
        Assert.Null(stats.BestProgressExercise);
        Assert.Equal((0, 0), (stats.Last7DaysWorkouts, stats.Last7DaysMinutes));
        Assert.Empty(await api.Get<List<PersonalRecordItem>>("/api/statistics/records"));
    }

    [Fact]
    public async Task BestProgressComparesLoadsAtTheSameRepetitions()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        var squat = app.Query(db => db.Exercises.Single(e => e.Name == "Barbell Back Squat").Id);
        await ApiClient.Ok(await api.Put($"/api/progress/{bench}", new { initialValue1 = 50, initialValue2 = 8, currentValue1 = 60, currentValue2 = 8 }));
        await ApiClient.Ok(await api.Put($"/api/progress/{squat}", new { initialValue1 = 60, initialValue2 = 8, currentValue1 = 100, currentValue2 = 3 }));

        var stats = await api.Get<TrainingStatistics>("/api/statistics");

        Assert.Equal(("Flat Bench Press", 20.0), (stats.BestProgressExercise, stats.BestProgressPercent));
    }

    [Fact]
    public async Task RecordsAreGroupedByMuscleAndTranslated()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        var squat = app.Query(db => db.Exercises.Single(e => e.Name == "Barbell Back Squat").Id);
        await ApiClient.Ok(await api.Post("/api/workouts", Journal.Workout((bench, 80, 5), (squat, 110, 5))));

        var records = await api.Get<List<PersonalRecordItem>>("/api/statistics/records");
        api.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uk");
        var translated = await api.Get<List<PersonalRecordItem>>("/api/statistics/records");

        Assert.Equal(["Chest", "Legs"], records.Select(r => r.MuscleGroupName));
        Assert.Equal((80.0, 110.0), (records[0].MaxWeightKg, records[1].MaxWeightKg));
        Assert.Contains(translated, r => r.ExerciseName == "Жим штанги лежачи");
        Assert.True((await api.Get<TrainingStatistics>("/api/statistics")).TotalAwardsCount > 0);
    }

    [Fact]
    public async Task AwardsShowProgressTowardsEachTarget()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.RegisterWithSetup();
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        await ApiClient.Ok(await api.Post("/api/workouts", Journal.Workout((bench, 50, 5))));

        var awards = await api.Get<List<AwardProgressItem>>("/api/awards");

        Assert.Equal(AchievementCatalog.All.Count, awards.Count);
        Assert.True(awards.Single(a => a.Code == "PROFILE_SETUP").IsUnlocked);
        var firstWorkout = awards.Single(a => a.Code == "WORKOUT_001");
        Assert.Equal((true, 100.0), (firstWorkout.IsUnlocked, firstWorkout.ProgressPercent));
        var tenWorkouts = awards.Single(a => a.Code == "WORKOUT_010");
        Assert.Equal((false, 1.0, 10.0, 10.0), (tenWorkouts.IsUnlocked, tenWorkouts.CurrentValue, tenWorkouts.TargetValue, tenWorkouts.ProgressPercent));
        api.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uk");
        Assert.Equal("Повна готовність", (await api.Get<List<AwardProgressItem>>("/api/awards")).Single(a => a.Code == "PROFILE_SETUP").Name);
    }
}
