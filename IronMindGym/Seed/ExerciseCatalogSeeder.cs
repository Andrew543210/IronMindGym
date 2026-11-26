using System.Text.Json;
using System.Text.Json.Serialization;
using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Seed;

/// <summary>
/// The exercise catalog from exercises.json. Rows are matched by their English name, which never changes here,
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

    /// <summary>
    /// Applies exercises.json on every start. Rows are matched by their English name: missing groups, subgroups
    /// and exercises are added, and the Ukrainian names and key-lift markers are refreshed. Nothing is deleted,
    /// and an existing exercise keeps its type and group, because logged sets point at it.
    /// A description is only translated while it is still the catalog's English text.
    /// </summary>
    public static void Sync(IronMindDbContext db)
    {
        var groups = db.MuscleGroups.ToDictionary(g => g.Name);
        var subGroups = db.MuscleSubGroups.Include(s => s.MuscleGroup).ToDictionary(s => (Group: s.MuscleGroup.Name, s.Name));
        var exercises = db.Exercises.ToDictionary(e => e.Name);
        foreach (var definition in Definitions())
        {
            if (!groups.TryGetValue(definition.Group, out var group))
                db.MuscleGroups.Add(groups[definition.Group] = group = new MuscleGroup { Name = definition.Group });
            group.NameUk = MuscleGroupNamesUk[definition.Group];

            if (!subGroups.TryGetValue((definition.Group, definition.Subgroup), out var subGroup))
            {
                subGroup = new MuscleSubGroup { Name = definition.Subgroup, MuscleGroup = group };
                db.MuscleSubGroups.Add(subGroups[(definition.Group, definition.Subgroup)] = subGroup);
            }
            subGroup.NameUk = MuscleSubGroupNamesUk[definition.Subgroup];

            if (!exercises.TryGetValue(definition.Name, out var exercise))
            {
                exercise = new Exercise { Name = definition.Name, Description = definition.Description, Type = definition.Type, MuscleSubGroup = subGroup };
                db.Exercises.Add(exercises[definition.Name] = exercise);
            }
            exercise.NameUk = definition.NameUk;
            exercise.KeyLift = definition.KeyLift;
            exercise.DescriptionUk = exercise.Description == definition.Description ? definition.DescriptionUk : null;
        }
    }

    public static List<ExerciseDefinition> Definitions()
    {
        using var source = typeof(ExerciseCatalogSeeder).Assembly.GetManifestResourceStream("IronMindGym.Seed.exercises.json")!;
        return JsonSerializer.Deserialize<List<ExerciseDefinition>>(source, DefinitionJson)!;
    }

    public sealed record ExerciseDefinition(
        string Name, string NameUk, string Group, string Subgroup, ExerciseType Type, string Description, string DescriptionUk,
        KeyLift? KeyLift = null);
}
