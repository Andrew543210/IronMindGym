using System.Text.Json;
using System.Text.Json.Serialization;
using IronMindGym.Data;
using IronMindGym.Models;

namespace IronMindGym.Seed;

/// <summary>
/// Fills an empty database with the exercise catalog from exercises.json and keeps the Ukrainian names
/// of an existing catalog up to date. Rows are matched by their English name, which never changes here,
/// so logged workouts, records and awards keep pointing at the same exercises.
/// </summary>
public static class ExerciseCatalogSeeder
{
    public static readonly IReadOnlyDictionary<string, string> MuscleGroupNamesUk = new Dictionary<string, string>
    {
        ["Arms"] = "Руки",
        ["Back"] = "Спина",
        ["Chest"] = "Груди",
        ["Conditioning"] = "Функціональна підготовка",
        ["Core"] = "Кор",
        ["Legs"] = "Ноги",
        ["Mobility"] = "Мобільність",
        ["Shoulders"] = "Плечі"
    };

    public static readonly IReadOnlyDictionary<string, string> MuscleSubGroupNamesUk = new Dictionary<string, string>
    {
        ["Biceps"] = "Біцепс",
        ["Forearms"] = "Передпліччя",
        ["Triceps"] = "Трицепс",
        ["Lats"] = "Найширші м’язи",
        ["Rows & upper back"] = "Тяги та верх спини",
        ["Spinal erectors"] = "Розгиначі спини",
        ["Upper back & traps"] = "Верх спини й трапеції",
        ["General chest"] = "Груди загалом",
        ["Lower chest emphasis"] = "Акцент на низ грудей",
        ["Upper chest emphasis"] = "Акцент на верх грудей",
        ["Bodyweight circuits"] = "Кругові тренування з власною вагою",
        ["Intervals"] = "Інтервали",
        ["Loaded conditioning"] = "Функціональна робота з обтяженням",
        ["Steady cardio"] = "Рівномірне кардіо",
        ["General mobility"] = "Загальна мобільність",
        ["Anti-extension"] = "Протидія розгинанню",
        ["Hip flexion & trunk control"] = "Згинання стегна й контроль корпусу",
        ["Lateral stability"] = "Бічна стабільність",
        ["Rotation & lateral stability"] = "Обертання й бічна стабільність",
        ["Trunk flexion"] = "Згинання корпусу",
        ["Calves"] = "Литки",
        ["Glutes"] = "Сідниці",
        ["Hamstrings"] = "Задня поверхня стегна",
        ["Posterior chain"] = "Задній ланцюг",
        ["Quadriceps"] = "Квадрицепси",
        ["Hip mobility"] = "Мобільність кульшових суглобів",
        ["Lower body stretches"] = "Розтяжка нижньої частини тіла",
        ["Spine mobility"] = "Мобільність хребта",
        ["Front Delts"] = "Передні дельти",
        ["Rear Delts"] = "Задні дельти",
        ["Side Delts"] = "Середні дельти"
    };

    private static readonly JsonSerializerOptions DefinitionJson = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void Seed(IronMindDbContext db)
    {
        foreach (var group in Definitions().GroupBy(e => e.Group))
        {
            var muscleGroup = new MuscleGroup { Name = group.Key, NameUk = MuscleGroupNamesUk[group.Key] };
            db.MuscleGroups.Add(muscleGroup);
            foreach (var subgroup in group.GroupBy(e => e.Subgroup))
            {
                var muscleSubGroup = new MuscleSubGroup { Name = subgroup.Key, NameUk = MuscleSubGroupNamesUk[subgroup.Key], MuscleGroup = muscleGroup };
                db.MuscleSubGroups.Add(muscleSubGroup);
                foreach (var e in subgroup)
                {
                    db.Exercises.Add(new Exercise
                    {
                        Name = e.Name,
                        NameUk = e.NameUk,
                        Description = e.Description,
                        DescriptionUk = e.DescriptionUk,
                        Type = e.Type,
                        MuscleSubGroup = muscleSubGroup
                    });
                }
            }
        }
    }

    /// <summary>
    /// Brings the Ukrainian names of existing catalog rows in line with exercises.json. Rows the catalog does not
    /// know keep their values; a description is only translated while it is still the catalog's English text.
    /// </summary>
    public static void EnsureTranslations(IronMindDbContext db)
    {
        foreach (var group in db.MuscleGroups)
        {
            if (MuscleGroupNamesUk.TryGetValue(group.Name, out var nameUk))
                group.NameUk = nameUk;
        }
        foreach (var subgroup in db.MuscleSubGroups)
        {
            if (MuscleSubGroupNamesUk.TryGetValue(subgroup.Name, out var nameUk))
                subgroup.NameUk = nameUk;
        }

        var definitions = Definitions().ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var exercise in db.Exercises)
        {
            if (!definitions.TryGetValue(exercise.Name, out var definition))
                continue;
            exercise.NameUk = definition.NameUk;
            exercise.DescriptionUk = exercise.Description == definition.Description ? definition.DescriptionUk : null;
        }
    }

    public static List<ExerciseDefinition> Definitions()
    {
        using var source = typeof(ExerciseCatalogSeeder).Assembly.GetManifestResourceStream("IronMindGym.Seed.exercises.json")!;
        return JsonSerializer.Deserialize<List<ExerciseDefinition>>(source, DefinitionJson)!;
    }

    public sealed record ExerciseDefinition(
        string Name, string NameUk, string Group, string Subgroup, ExerciseType Type, string Description, string DescriptionUk);
}
