using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using IronMindGym.Contracts;
using IronMindGym.Localization;
using IronMindGym.Models;

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
        AllTranslated(app.Query(db => db.SplitDays.ToList()), e => e.Name, e => e.NameUk, "Split days");
        AllTranslated(app.Query(db => db.SplitDays.ToList()), e => e.WorkoutName, e => e.WorkoutNameUk, "Workout titles");
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
        var upperLower = app.Query(db => db.SplitTypes.Single(s => s.Code == SplitTypeCode.UpperLower).Id);
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
        Assert.Equal(["Верх тіла", "Низ тіла"], split.Days.Select(day => day.Name));
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
        var working = app.Query(db => db.SetTypes.Single(s => s.Code == SetTypeCode.Working).Id);

        var response = await api.Post("/api/workouts", new
        {
            splitDayId = app.Query(db => db.SplitDays.Single(d => d.Code == "LEGS").Id),
            durationMinutes = 45,
            reviewRating = 4,
            sets = new[] { new { exerciseId = bench, setTypeId = working, weightKg = 60, reps = 5 } }
        });
        await ApiClient.Ok(response);
        var id = (await response.Content.ReadFromJsonAsync<SavedWorkoutResponse>(ApiClient.Json))!.Id;

        Assert.Equal("LEGS", app.Query(db => db.WorkoutSessions.Select(s => s.SplitDay!.Code).Single()));
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
        Assert.Equal(Language.Ukrainian, (await api.Get<SessionResponse>("/api/session")).Language);

        await ApiClient.Ok(await api.Put("/api/profile/language", new { language = "en" }));
        Assert.Equal(Language.English, (await api.Get<SessionResponse>("/api/session")).Language);
        Assert.NotEmpty(await ApiClient.Errors(await api.Put("/api/profile/language", new { language = "de" })));

        using var phone = app.Api();
        await phone.SignIn("athlete@example.com");
        Assert.Equal(Language.English, (await phone.Get<SessionResponse>("/api/session")).Language);
    }

    [Fact]
    public async Task LanguageIsSavedOnlyForASignedInAthlete()
    {
        using var app = new TestApplication();
        using var api = app.Api();

        var response = await api.Put("/api/profile/language", new { language = "uk" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void EveryValidationRuleOfTheApiHasAMessageWithAUkrainianTranslation()
    {
        var rules = typeof(SessionResponse).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(SessionResponse).Namespace)
            .SelectMany(t => t.GetProperties().Select(p => (Member: $"{t.Name}.{p.Name}", Attributes: p.GetCustomAttributes<ValidationAttribute>(true)))
                .Concat(t.GetConstructors().SelectMany(c => c.GetParameters())
                    .Select(p => (Member: $"{t.Name}({p.Name})", Attributes: p.GetCustomAttributes<ValidationAttribute>(true)))))
            .SelectMany(m => m.Attributes.Select(a => (m.Member, a.ErrorMessage)))
            .ToList();

        Assert.NotEmpty(rules);
        var missing = rules.Where(r => r.ErrorMessage is null || !ServerMessages.English.Contains(r.ErrorMessage)).Select(r => r.Member).Distinct().ToList();
        Assert.True(missing.Count == 0, $"Rules without a translated message: {string.Join(", ", missing)}");
    }

    [Fact]
    public async Task RefusalsAreWordedInTheRequestLanguage()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        api.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uk");

        var invalid = await ApiClient.Errors(await api.Post("/api/session/register", new
        {
            username = "a",
            email = "athlete@example.com",
            age = 30,
            password = "TrainingPass123!",
            confirmPassword = "TrainingPass123!"
        }));
        var refused = await ApiClient.Errors(await api.Post("/api/session/login", new { email = "nobody@example.com", password = "TrainingPass123!" }));
        var malformed = await ApiClient.Errors(await api.Post("/api/session/login", new { email = 5, password = "x" }));

        Assert.Equal(["Використовуйте 2–50 латинських літер, цифр, підкреслень, крапок або дефісів."], invalid.Distinct());
        Assert.Equal(["Неправильна пошта або пароль."], refused);
        Assert.All(malformed, message => Assert.Matches(Cyrillic(), message));
        Assert.DoesNotContain(malformed, message => message.Contains("System.", StringComparison.Ordinal));
    }
}
