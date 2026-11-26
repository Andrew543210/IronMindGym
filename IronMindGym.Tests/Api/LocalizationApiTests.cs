using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using IronMindGym.Api;
using IronMindGym.Contracts;

namespace IronMindGym.Tests.Api;

public partial class LocalizationApiTests
{
    [GeneratedRegex("[А-Яа-яІіЇїЄєҐґ]")]
    private static partial Regex Cyrillic();

    private static void AllTranslated<T>(List<T> rows, Func<T, string> english, Func<T, string?> ukrainian, string what)
    {
        var missing = rows.Where(r => ukrainian(r) is not { } text || !Cyrillic().IsMatch(text)).Select(english).ToList();
        Assert.True(missing.Count == 0, $"{what} without Ukrainian text: {string.Join(", ", missing)}");
    }

    [Fact]
    public void EverySeededReferenceNameHasAUkrainianTranslation()
    {
        using var app = new TestApplication();

        var exercises = app.Query(db => db.Exercises.ToList());
        AllTranslated(exercises, e => e.Name, e => e.NameUk, "Exercises");
        AllTranslated(exercises, e => e.Name, e => e.DescriptionUk, "Exercise descriptions");
        AllTranslated(app.Query(db => db.MuscleGroups.ToList()), e => e.Name, e => e.NameUk, "Muscle groups");
        AllTranslated(app.Query(db => db.MuscleSubGroups.ToList()), e => e.Name, e => e.NameUk, "Muscle subgroups");
        AllTranslated(app.Query(db => db.SetTypes.ToList()), e => e.Name, e => e.NameUk, "Set types");
        AllTranslated(app.Query(db => db.SplitTypes.ToList()), e => e.Name, e => e.NameUk, "Split types");
        AllTranslated(app.Query(db => db.Avatars.ToList()), e => e.Name, e => e.NameUk, "Ranks");
        var awards = app.Query(db => db.Achievements.ToList());
        AllTranslated(awards, e => e.Name, e => e.NameUk, "Awards");
        AllTranslated(awards, e => e.Name, e => e.ConditionDescriptionUk, "Award conditions");
        var research = app.Query(db => db.ResearchItems.ToList());
        AllTranslated(research, e => e.Title, e => e.TitleUk, "Research titles");
        AllTranslated(research, e => e.Title, e => e.ThesisTextUk, "Research summaries");
        Assert.Empty(exercises.GroupBy(e => e.NameUk).Where(g => g.Count() > 1).Select(g => g.Key));
    }

    [Fact]
    public async Task LoggerShowsUkrainianNamesButKeepsEnglishKeys()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        var upperLower = app.Query(db => db.SplitTypes.Single(s => s.Name == "Upper-Lower").Id);
        await api.RegisterWithSetup(splitTypeId: upperLower);
        api.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uk-UA");

        var logger = await api.Get<WorkoutLoggerResponse>("/api/workouts/logger");

        var bench = logger.Exercises.Single(e => e.EnglishName == "Flat Bench Press");
        Assert.Equal("Жим штанги лежачи", bench.ExerciseName);
        Assert.Equal("Груди", bench.MuscleGroupName);
        Assert.Equal("Груди загалом", bench.MuscleSubGroupName);
        Assert.Contains(logger.MuscleGroups, g => g.Name == "Плечі");
        Assert.Equal("Робочий", logger.SetTypes[0].Name);
        var split = Assert.Single(logger.SplitTypes);
        Assert.Equal("Верх / низ", split.Name);
        Assert.Equal([new SplitDayOption("Upper", "Верх тіла"), new SplitDayOption("Lower", "Низ тіла")], split.Days);
        var names = logger.Exercises.Select(e => e.ExerciseName).ToList();
        Assert.Equal(names.Order(StringComparer.Create(new CultureInfo("uk"), ignoreCase: true)), names);
    }

    [Fact]
    public async Task WorkoutIsSavedInEnglishAndShownInTheRequestLanguage()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();
        api.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uk");
        var bench = app.Query(db => db.Exercises.Single(e => e.Name == "Flat Bench Press").Id);
        var working = app.Query(db => db.SetTypes.Single(s => s.Name == "Working").Id);

        var response = await api.Post("/api/workouts", new
        {
            splitDayLabel = "Legs",
            durationMinutes = 45,
            reviewRating = 4,
            sets = new[] { new { exerciseId = bench, setTypeId = working, weightKg = 60, reps = 5 } }
        });
        await ApiClient.Ok(response);
        var id = (await response.Content.ReadFromJsonAsync<SavedWorkoutResponse>(ApiClient.Json))!.Id;

        Assert.Equal("Legs day", app.Query(db => db.WorkoutSessions.Single().Name));
        var details = await api.Get<WorkoutDetailsResponse>($"/api/workouts/{id}");
        Assert.Equal("День ніг", details.Name);
        Assert.Equal("Жим штанги лежачи", Assert.Single(details.Exercises).ExerciseName);
        Assert.Equal("Робочий", details.Exercises[0].Sets[0].SetTypeName);
        Assert.Equal("Жим штанги лежачи", Assert.Single(details.ExerciseStats).ExerciseName);
        Assert.Equal("День ніг", Assert.Single((await api.Get<WorkoutHistoryResponse>("/api/workouts")).Sessions).Name);

        api.Http.DefaultRequestHeaders.AcceptLanguage.Clear();
        api.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
        Assert.Equal("Legs day", (await api.Get<WorkoutDetailsResponse>($"/api/workouts/{id}")).Name);
    }

    [Fact]
    public async Task AccountRemembersTheInterfaceLanguage()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        api.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uk");
        await api.Register();
        Assert.Equal("uk", (await api.Get<SessionResponse>("/api/session")).Language);

        await ApiClient.Ok(await api.Put("/api/profile/language", new { language = "en" }));
        Assert.Equal("en", (await api.Get<SessionResponse>("/api/session")).Language);
        Assert.NotEmpty(await ApiClient.Errors(await api.Put("/api/profile/language", new { language = "de" })));

        using var phone = app.Api();
        await phone.SignIn("athlete@example.com");
        Assert.Equal("en", (await phone.Get<SessionResponse>("/api/session")).Language);
    }

    [Fact]
    public async Task LanguageIsSavedOnlyForASignedInAthlete()
    {
        using var app = new TestApplication();
        using var api = app.Api();

        var response = await api.Put("/api/profile/language", new { language = "uk" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
