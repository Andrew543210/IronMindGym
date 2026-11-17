using System.Text.Json;
using System.Text.Json.Serialization;
using IronMindGym.Data;
using IronMindGym.Models;

namespace IronMindGym.Seed;

public static class ExerciseCatalogSeeder
{
    public static void Seed(IronMindDbContext db)
    {
        using var source = typeof(ExerciseCatalogSeeder).Assembly.GetManifestResourceStream("IronMindGym.Seed.exercises.json")!;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var definitions = JsonSerializer.Deserialize<List<ExerciseDefinition>>(source, options)!;
        foreach (var group in definitions.GroupBy(e => e.Group))
        {
            var muscleGroup = new MuscleGroup { Name = group.Key };
            db.MuscleGroups.Add(muscleGroup);
            foreach (var subgroup in group.GroupBy(e => e.Subgroup))
            {
                var muscleSubGroup = new MuscleSubGroup { Name = subgroup.Key, MuscleGroup = muscleGroup };
                db.MuscleSubGroups.Add(muscleSubGroup);
                foreach (var e in subgroup)
                    db.Exercises.Add(new Exercise { Name = e.Name, Description = e.Description, Type = e.Type, MuscleSubGroup = muscleSubGroup });
            }
        }
    }

    private sealed record ExerciseDefinition(string Name, string Group, string Subgroup, ExerciseType Type, string Description);
}
