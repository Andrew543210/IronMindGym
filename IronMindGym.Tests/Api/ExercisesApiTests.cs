using System.Globalization;
using IronMindGym.Api;
using IronMindGym.Models;

namespace IronMindGym.Tests.Api;

public class ExercisesApiTests
{
    [Fact]
    public async Task LibraryListsTheWholeCatalogWithGroupsInTheRequestLanguage()
    {
        using var app = new TestApplication();
        using var api = app.Api();
        await api.Register();

        var library = await api.Get<ExerciseLibraryResponse>("/api/exercises");
        api.Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("uk");
        var ukrainian = await api.Get<ExerciseLibraryResponse>("/api/exercises");

        Assert.Equal(app.Query(db => db.Exercises.Count()), library.Exercises.Count);
        Assert.Equal(8, library.MuscleGroups.Count);
        Assert.Equal(library.Exercises.Select(e => e.Name).Order(StringComparer.Create(CultureInfo.GetCultureInfo("en"), true)), library.Exercises.Select(e => e.Name));
        var plank = library.Exercises.Single(e => e.Name == "Weighted Plank");
        Assert.Equal(ExerciseType.Timed, plank.Type);
        Assert.Contains(library.MuscleGroups, g => g.Id == plank.MuscleGroupId && g.Name == "Core");
        Assert.NotEmpty(plank.Description);
        Assert.Contains(ukrainian.Exercises, e => e.Name == "Жим штанги лежачи");
        Assert.Contains(ukrainian.MuscleGroups, g => g.Name == "Груди");
    }
}
