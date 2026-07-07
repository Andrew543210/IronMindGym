using System.Net;
using IronMindGym.Contracts;
using IronMindGym.Services;

namespace IronMindGym.Tests.Api;

public class NutritionApiTests
{
    private static object Entry(double calories = 2000) => new { caloriesKcal = calories, proteinG = 120, fatG = 60, carbsG = 250 };

    [Fact]
    public async Task SavingADayTwiceKeepsOneEntryAndOnlyTheOwnerSeesIt()
    {
        using var app = new TestApplication();
        using var owner = app.Api();
        using var other = app.Api();
        await owner.Register();
        await other.Register("other");
        var day = DateOnly.FromDateTime((await owner.Get<NutritionDay>("/api/nutrition")).Date);

        await ApiClient.Ok(await owner.Put($"/api/nutrition/days/{day:yyyy-MM-dd}", Entry(2000)));
        await ApiClient.Ok(await owner.Put($"/api/nutrition/days/{day:yyyy-MM-dd}", Entry(2001)));

        Assert.Equal(2001, app.Query(db => db.DailyNutritions.Single()).CaloriesKcal);
        var journal = await owner.Get<NutritionDay>($"/api/nutrition?date={day:yyyy-MM-dd}");
        Assert.True(journal.IsLogged);
        Assert.Equal(2001, journal.CaloriesKcal);
        Assert.Equal(2001, Assert.Single(journal.RecentLogs).CaloriesKcal);
        var stranger = await other.Get<NutritionDay>($"/api/nutrition?date={day:yyyy-MM-dd}");
        Assert.False(stranger.IsLogged);
        Assert.Empty(stranger.RecentLogs);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Delete($"/api/nutrition/days/{day:yyyy-MM-dd}")).StatusCode);

        await ApiClient.Ok(await owner.Delete($"/api/nutrition/days/{day:yyyy-MM-dd}"));
        Assert.Equal(0, app.Query(db => db.DailyNutritions.Count()));
    }

    [Fact]
    public async Task MissedDaysCanBeLoggedButNotFutureDaysOrImpossibleAmounts()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        var today = DateOnly.FromDateTime((await api.Get<NutritionDay>("/api/nutrition")).Date);

        await ApiClient.Ok(await api.Put($"/api/nutrition/days/{today.AddDays(-3):yyyy-MM-dd}", Entry(1800)));
        var future = await api.Put($"/api/nutrition/days/{today.AddDays(1):yyyy-MM-dd}", Entry());
        var tooMuch = await api.Put($"/api/nutrition/days/{today:yyyy-MM-dd}", Entry(20000));

        Assert.Contains("Choose a date within the last five years, up to today.", await ApiClient.Errors(future));
        Assert.Contains("Calories must be 0–10000 kcal.", await ApiClient.Errors(tooMuch));
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put("/api/nutrition/days/not-a-date", Entry())).StatusCode);
        Assert.Equal(today.AddDays(-3), DateOnly.FromDateTime(app.Query(db => db.DailyNutritions.Single().Date)));
    }

    [Fact]
    public async Task TheDayIsComparedWithPersonalTargetsAndTheWeek()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.RegisterWithSetup();
        app.Query(db =>
        {
            var profile = db.Profiles.Single();
            (profile.CalorieTarget, profile.ProteinTarget, profile.FatTarget) = (2400, 150, 70);
            return db.SaveChanges();
        });
        var today = DateOnly.FromDateTime((await api.Get<NutritionDay>("/api/nutrition")).Date);
        await ApiClient.Ok(await api.Put($"/api/nutrition/days/{today.AddDays(-1):yyyy-MM-dd}", new { caloriesKcal = 2000, proteinG = 120, fatG = 60, carbsG = 250 }));
        await ApiClient.Ok(await api.Put($"/api/nutrition/days/{today:yyyy-MM-dd}", new { caloriesKcal = 2400, proteinG = 150, fatG = 70, carbsG = 290 }));

        var advice = (await api.Get<NutritionDay>("/api/nutrition")).Advice;

        Assert.Equal(new NutritionTarget(2400, TargetSource.Personal), advice.Calories);
        Assert.Equal(new NutritionTarget(292, TargetSource.Remainder), advice.Carbs);
        Assert.Equal((4, 4, 100), (advice.TargetsSet, advice.TargetsMatched, advice.Score));
        Assert.Equal((2, 2200.0), (advice.WeekDaysLogged, advice.WeekAverageKcal));
        Assert.False(advice.MacrosDisagreeWithEnergy);
    }
}
