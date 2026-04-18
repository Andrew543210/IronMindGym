using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Controllers;

public class ExerciseLibraryController(IronMindDbContext db) : Controller
{
    public static string TranslateExerciseNameForDisplay(string value) => TranslateExerciseName(value);
    public static string TranslateMuscleGroupForDisplay(string value) => TranslateMuscleGroup(value);
    public static string TranslateMuscleSubGroupForDisplay(string value) => TranslateMuscleSubGroup(value);

    public static string TranslateSessionNameForDisplay(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Replace("День ", "Day ", StringComparison.OrdinalIgnoreCase);
    }

    public IActionResult Index(int? muscleGroupId, int? muscleSubGroupId, string? search)
    {
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var groups = db.MuscleGroups
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToList();

        var subGroupsQuery = db.MuscleSubGroups
            .AsNoTracking()
            .OrderBy(sg => sg.Name)
            .AsQueryable();

        if (muscleGroupId.HasValue)
        {
            subGroupsQuery = subGroupsQuery.Where(sg => sg.MuscleGroupId == muscleGroupId.Value);
        }

        var exercisesQuery = db.Exercises
            .Include(e => e.MuscleSubGroup)
            .ThenInclude(sg => sg.MuscleGroup)
            .AsNoTracking()
            .AsQueryable();

        if (muscleGroupId.HasValue)
        {
            exercisesQuery = exercisesQuery.Where(e => e.MuscleSubGroup.MuscleGroupId == muscleGroupId.Value);
        }

        if (muscleSubGroupId.HasValue)
        {
            exercisesQuery = exercisesQuery.Where(e => e.MuscleSubGroupId == muscleSubGroupId.Value);
        }

        var groupsForView = groups
            .Select(g => new MuscleGroup { Id = g.Id, Name = TranslateMuscleGroup(g.Name) })
            .ToList();

        var subGroupsForView = subGroupsQuery
            .ToList()
            .Select(sg => new MuscleSubGroup
            {
                Id = sg.Id,
                Name = TranslateMuscleSubGroup(sg.Name),
                MuscleGroupId = sg.MuscleGroupId
            })
            .ToList();

        var exercisesForView = exercisesQuery
            .OrderBy(e => e.Name)
            .ToList()
            .Select(e =>
            {
                var translatedName = TranslateExerciseName(e.Name);
                return new Exercise
                {
                    Id = e.Id,
                    Name = translatedName,
                    Description = TranslateExerciseDescription(translatedName, e.Description, e.Name),
                    MuscleSubGroupId = e.MuscleSubGroupId,
                    MuscleSubGroup = new MuscleSubGroup
                    {
                        Id = e.MuscleSubGroup.Id,
                        Name = TranslateMuscleSubGroup(e.MuscleSubGroup.Name),
                        MuscleGroupId = e.MuscleSubGroup.MuscleGroupId,
                        MuscleGroup = new MuscleGroup
                        {
                            Id = e.MuscleSubGroup.MuscleGroup.Id,
                            Name = TranslateMuscleGroup(e.MuscleSubGroup.MuscleGroup.Name)
                        }
                    }
                };
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            exercisesForView = exercisesForView
                .Where(e =>
                    e.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    e.Description.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var vm = new ExerciseLibraryViewModel
        {
            MuscleGroups = groupsForView,
            MuscleSubGroups = subGroupsForView,
            Exercises = exercisesForView,
            SelectedGroupId = muscleGroupId,
            SelectedSubGroupId = muscleSubGroupId,
            Search = search ?? string.Empty
        };

        return View(vm);
    }

    private static string TranslateMuscleGroup(string name)
    {
        var mapped = name switch
        {
            "Груди" => "Chest",
            "Спина" => "Back",
            "Плечі" => "Shoulders",
            "Руки" => "Arms",
            "Ноги" => "Legs",
            "Прес" => "Abs",
            "Кардіо та функціонал" => "Cardio & Functional",
            _ => TranslateByReplacements(name, BaseReplacements)
        };

        return EnsureEnglish(mapped, "Muscle Group");
    }

    private static string TranslateMuscleSubGroup(string name)
    {
        var mapped = name switch
        {
            "Верх грудей" => "Upper Chest",
            "Середина грудей" => "Middle Chest",
            "Низ грудей" => "Lower Chest",
            "Широчайші" => "Lats",
            "Трапеції" => "Traps",
            "Поперек" => "Lower Back",
            "Передня дельта" => "Front Delts",
            "Середня дельта" => "Side Delts",
            "Задня дельта" => "Rear Delts",
            "Біцепс" => "Biceps",
            "Трицепс" => "Triceps",
            "Передпліччя" => "Forearms",
            "Квадрицепс" => "Quadriceps",
            "Біцепс стегна" => "Hamstrings",
            "Сідниці" => "Glutes",
            "Литки" => "Calves",
            "Верхній прес" => "Upper Abs",
            "Нижній прес" => "Lower Abs",
            "Косі м'язи живота" => "Obliques",
            "Мобільність" => "Mobility",
            _ => TranslateByReplacements(name, BaseReplacements)
        };

        return EnsureEnglish(mapped, "Muscle Subgroup");
    }

    private static string TranslateExerciseName(string name)
    {
        var directMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Жим лежачи"] = "Flat Bench Press",
            ["Жим гантелей лежачи"] = "Flat Dumbbell Press",
            ["Жим штанги на похилій лаві"] = "Incline Barbell Bench Press",
            ["Жим гантелей на похилій лаві"] = "Incline Dumbbell Bench Press",
            ["Зведення гантелей на похилій"] = "Incline Dumbbell Flyes",
            ["Кросовер з нижніх блоків"] = "Low Cable Crossover",
            ["Віджимання з ногами на опорі"] = "Decline Push-ups (Feet on elevation)",
            ["Жим у Сміті на похилій"] = "Incline Smith Machine Press",
            ["Віджимання на підлозі"] = "Push-Ups",
            ["Жим у тренажері сидячи"] = "Seated Machine Chest Press",
            ["Кросовер на рівні грудей"] = "Chest-Level Cable Flyes",
            ["Пек-дек"] = "Pec Deck Flyes",
            ["Жим штанги вниз головою"] = "Decline Barbell Press",
            ["Жим гантелей вниз головою"] = "Decline Dumbbell Press",
            ["Віджимання на брусах (грудний стиль)"] = "Chest Dips",
            ["Кросовер з верхніх блоків"] = "High Cable Crossover",
            ["Віджимання від лави"] = "Incline Push-ups (Hands on bench)",
            ["Жим у Сміті вниз головою"] = "Decline Smith Machine Press",
            ["Присідання зі штангою"] = "Barbell Squats",
            ["Фронтальні присідання"] = "Front Squats",
            ["Станова тяга"] = "Deadlift",
            ["Румунська тяга"] = "Romanian Deadlift",
            ["Французький жим лежачи"] = "Lying French Press",
            ["Підтягування широким хватом"] = "Wide-Grip Pull-ups",
            ["Тяга верхнього блока"] = "Lat Pulldown",
            ["Тяга гантелі в упорі"] = "One-Arm Dumbbell Row",
            ["Тяга Т-грифа"] = "T-Bar Row",
            ["Пуловер на верхньому блоці"] = "Straight-Arm Cable Pulldown",
            ["Тяга штанги в нахилі"] = "Bent-Over Barbell Row",
            ["Шраги зі штангою"] = "Barbell Shrugs",
            ["Шраги з гантелями"] = "Dumbbell Shrugs",
            ["Тяга до підборіддя"] = "Upright Row",
            ["Тяга канату до обличчя"] = "Face Pull",
            ["Зворотні розведення в тренажері"] = "Machine Reverse Flyes",
            ["Y-рейзи на лаві"] = "Incline Bench Y-Raises",
            ["Гіперекстензія"] = "Hyperextension",
            ["Good Morning"] = "Good Morning",
            ["Зворотна гіперекстензія"] = "Reverse Hyperextension",
            ["Планка з обтяженням"] = "Weighted Plank",
            ["Жим штанги стоячи"] = "Standing Overhead Press",
            ["Жим гантелей сидячи"] = "Seated Dumbbell Press",
            ["Підйом гантелей перед собою"] = "Dumbbell Front Raise",
            ["Арнольд-жим"] = "Arnold Press",
            ["Жим у тренажері на плечі"] = "Shoulder Press Machine",
            ["Підйом диска перед собою"] = "Front Plate Raises",
            ["Махи гантелями в сторони"] = "Dumbbell Lateral Raises",
            ["Махи в кросовері"] = "Cable Lateral Raise",
            ["Протяжка гантелей"] = "Dumbbell Upright Row",
            ["Жим гантелей стоячи"] = "Standing Dumbbell Press",
            ["Підйом однієї руки в сторони"] = "Single-Arm Lateral Raise",
            ["Махи в тренажері"] = "Machine Lateral Raise",
            ["Зворотні махи гантелями"] = "Reverse Dumbbell Flyes",
            ["Face Pull"] = "Face Pull",
            ["Зворотний пек-дек"] = "Reverse Pec Deck",
            ["Тяга гантелей лежачи на лаві"] = "Prone Dumbbell Rear Delt Row",
            ["Тяга канату до лиця з паузою"] = "Face Pull with Pause",
            ["Тяга штанги до грудей з широким хватом"] = "Wide-Grip Barbell Row to Chest",
            ["Підйом штанги на біцепс"] = "Barbell Bicep Curls",
            ["Підйом EZ-штанги"] = "EZ-Bar Curl",
            ["Підйом гантелей сидячи"] = "Seated Dumbbell Curl",
            ["Молоткові згинання"] = "Hammer Curls",
            ["Концентровані згинання"] = "Concentration Curl",
            ["Згинання на лаві Скотта"] = "Preacher Curl",
            ["Розгинання рук на блоці"] = "Triceps Pushdown",
            ["Розгинання над головою з канатом"] = "Overhead Rope Extension",
            ["Віджимання вузьким хватом"] = "Close-Grip Push-Ups",
            ["Жим вузьким хватом"] = "Close-Grip Bench Press",
            ["Розгинання гантелі з-за голови"] = "Overhead Dumbbell Extension",
            ["Згинання кистей сидячи"] = "Seated Wrist Curls",
            ["Розгинання кистей сидячи"] = "Seated Wrist Extensions",
            ["Фермерська хода"] = "Farmer's Walk",
            ["Зворотний підйом штанги"] = "Reverse Barbell Curl",
            ["Вис на турніку на час"] = "Timed Dead Hang",
            ["Ролер для передпліч"] = "Wrist Roller",
            ["Жим ногами"] = "Leg Press",
            ["Випади з гантелями"] = "Dumbbell Lunges",
            ["Болгарські спліт-присідання"] = "Bulgarian Split Squat",
            ["Розгинання ніг у тренажері"] = "Leg Extension",
            ["Згинання ніг лежачи"] = "Lying Leg Curl",
            ["Згинання ніг сидячи"] = "Seated Leg Curl",
            ["Румунська тяга з гантелями"] = "Dumbbell Romanian Deadlift",
            ["Good Morning з легкою вагою"] = "Light Good Morning",
            ["Глют-хем рейз"] = "Glute-Ham Raise",
            ["Тяга на прямих ногах"] = "Stiff-Leg Deadlift",
            ["Хіп-траст"] = "Hip Thrust",
            ["Місток зі штангою"] = "Barbell Hip Thrust",
            ["Відведення ноги в кросовері"] = "Cable Hip Abduction",
            ["Кроки на платформу"] = "Step-Ups",
            ["Зворотні випади"] = "Reverse Lunges",
            ["Розведення ніг у тренажері"] = "Machine Hip Abduction",
            ["Підйом на носки стоячи"] = "Standing Calf Raise",
            ["Підйом на носки сидячи"] = "Seated Calf Raise",
            ["Підйом на носки в жимі ногами"] = "Leg Press Calf Raise",
            ["Підйоми на одній нозі"] = "Single-Leg Calf Raise",
            ["Стрибки на скакалці"] = "Jump Rope",
            ["Фермерська хода на носках"] = "Tip-Toe Farmer's Walk",
            ["Скручування на підлозі"] = "Floor Crunches",
            ["Скручування на лаві"] = "Incline Bench Crunches",
            ["Кранч у тренажері"] = "Machine Crunch",
            ["Скручування з диском"] = "Weighted Crunches",
            ["Скручування на фітболі"] = "Fitball Crunches",
            ["Кабельні скручування"] = "Cable Crunch",
            ["Підйом ніг у висі"] = "Hanging Leg Raises",
            ["Підйом колін у упорі"] = "Captain's Chair Knee Raises",
            ["Зворотні скручування"] = "Reverse Crunch",
            ["Підйом ніг лежачи"] = "Lying Leg Raise",
            ["Ножиці"] = "Scissor Kicks",
            ["Планка з підтягуванням колін"] = "Plank Knee Tucks",
            ["Бічні скручування"] = "Side Crunches",
            ["Російські повороти"] = "Russian Twists",
            ["Woodchopper у кросовері"] = "Cable Woodchopper",
            ["Бічна планка"] = "Side Plank",
            ["Підйом коліна до ліктя"] = "Knee to Elbow Crunches",
            ["Повороти корпусу з млинцем"] = "Weighted Russian Twists",
            ["Спринти 30/30"] = "Sprints 30/30",
            ["Бьорпі"] = "Burpees",
            ["Гребний тренажер інтервали"] = "Rowing Machine Intervals",
            ["Air Bike інтервали"] = "Air Bike Intervals",
            ["Скакалка інтервали"] = "Jump Rope Intervals",
            ["Штовхання саней"] = "Sled Push",
            ["Розкриття грудного відділу"] = "Thoracic Opening",
            ["Мобілізація кульшового суглоба"] = "Hip Mobilization",
            ["Кішка-корова"] = "Cat-Cow"
            ,
            ["World's Greatest Stretch"] = "World's Greatest Stretch",
            ["Розтягнення біцепса стегна"] = "Hamstring Stretch",
            ["Розтягнення литок біля стіни"] = "Wall Calf Stretch"
        };

        if (directMap.TryGetValue(name, out var translated))
        {
            return translated;
        }

        return EnsureEnglish(TranslateByReplacements(name, BaseReplacements), "Exercise");
    }

    private static string TranslateExerciseDescription(string translatedExerciseName, string description, string originalExerciseName)
    {
        if (translatedExerciseName.Equals("Face Pull", StringComparison.OrdinalIgnoreCase))
        {
            if (originalExerciseName.Equals("Тяга канату до обличчя", StringComparison.OrdinalIgnoreCase))
            {
                return "Middle traps and rear delts";
            }

            if (originalExerciseName.Equals("Face Pull", StringComparison.OrdinalIgnoreCase))
            {
                return "Rear delts and upper back focus";
            }
        }

        if (ExerciseDescriptionMap.TryGetValue(translatedExerciseName, out var exactDescription))
        {
            return exactDescription;
        }

        var mapped = TranslateByReplacements(description, BaseReplacements);
        return EnsureEnglish(mapped, "Exercise description");
    }

    private static readonly IReadOnlyDictionary<string, string> ExerciseDescriptionMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Air Bike Intervals"] = "High-intensity workload",
            ["Good Morning"] = "Tilt control and back stabilization",
            ["Light Good Morning"] = "Hinge control and hamstring tension",
            ["Cable Woodchopper"] = "Diagonal rotational movement",
            ["World's Greatest Stretch"] = "Comprehensive full-body mobilization",
            ["Incline Bench Y-Raises"] = "Scapular stabilizers",
            ["Arnold Press"] = "Rotational movement for full shoulder development",
            ["Bulgarian Split Squat"] = "Deep range of motion for quads",
            ["Burpees"] = "Full-body functional movement",
            ["Side Plank"] = "Isometric oblique work",
            ["Side Crunches"] = "Emphasis on oblique muscles",
            ["Dumbbell Lunges"] = "Unilateral work and balance",
            ["Timed Dead Hang"] = "Grip endurance",
            ["Cable Hip Abduction"] = "Medius glute isolation",
            ["Close-Grip Push-Ups"] = "Basic bodyweight exercise",
            ["Incline Push-ups (Hands on bench)"] = "Easier variation for lower chest",
            ["Decline Push-ups (Feet on elevation)"] = "Bodyweight with upper chest focus",
            ["Chest Dips"] = "Forward lean for chest emphasis",
            ["Push-Ups"] = "Classic movement for chest and triceps",
            ["Glute-Ham Raise"] = "Strength exercise for hamstrings",
            ["Rowing Machine Intervals"] = "Cardio with endurance emphasis",
            ["Hyperextension"] = "Strengthening spinal erectors",
            ["Close-Grip Bench Press"] = "Strength exercise for triceps",
            ["Decline Dumbbell Press"] = "Increased range of motion for lower chest",
            ["Flat Dumbbell Press"] = "Balance of left and right sides",
            ["Incline Dumbbell Bench Press"] = "Stabilization and greater range of motion",
            ["Seated Dumbbell Press"] = "Stable version for delts",
            ["Standing Dumbbell Press"] = "Strength version for shoulders",
            ["Flat Bench Press"] = "Core exercise for overall chest strength",
            ["Leg Press"] = "Strength movement in machine",
            ["Decline Smith Machine Press"] = "Safe mechanics with fixed path",
            ["Incline Smith Machine Press"] = "Controlled trajectory",
            ["Shoulder Press Machine"] = "Safe trajectory control",
            ["Seated Machine Chest Press"] = "Stable work in a safe position",
            ["Decline Barbell Press"] = "Focus on lower chest fibers",
            ["Incline Barbell Press"] = "Focus on upper chest fibers",
            ["Standing Overhead Press"] = "Strength exercise for front delt",
            ["Incline Dumbbell Flyes"] = "Upper chest isolation",
            ["Reverse Hyperextension"] = "Lower back and glutes",
            ["Reverse Pec Deck"] = "Controlled machine movement",
            ["Reverse Barbell Curls"] = "Brachioradialis focus",
            ["Reverse Lunges"] = "Stabilization and glute emphasis",
            ["Reverse Dumbbell Flyes"] = "Rear delt isolation",
            ["Machine Reverse Flyes"] = "Controlled middle back work",
            ["Reverse Crunch"] = "Lower abs isolation",
            ["Seated Wrist Curls"] = "Forearm flexor development",
            ["Preacher Curl"] = "Fixed position for bicep peak",
            ["Lying Leg Curl"] = "Hamstring isolation",
            ["Seated Leg Curl"] = "Controlled hamstring isolation",
            ["Cable Crunch"] = "Constant tension on abs",
            ["Concentration Curl"] = "Isolated bicep work",
            ["Machine Crunch"] = "Controlled isolation",
            ["Step-Ups"] = "Functional single-leg work",
            ["High Cable Crossover"] = "Downward load direction",
            ["Low Cable Crossover"] = "Upper chest peak contraction",
            ["Chest-Level Cable Flyes"] = "Horizontal isolated movement",
            ["Cat-Cow"] = "Spinal mobility",
            ["Cable Lateral Raise"] = "Constant tension throughout the movement",
            ["Machine Lateral Raise"] = "Stabilized isolation",
            ["Dumbbell Lateral Raises"] = "Classic side delt isolation",
            ["Hip Mobilization"] = "Preparation for squats",
            ["Hammer Curls"] = "Brachialis emphasis",
            ["Barbell Hip Thrust"] = "Glute strength work",
            ["Scissor Kicks"] = "Statodynamic lower abs work",
            ["Pec Deck Flyes"] = "Chest isolation without heavy shoulder load",
            ["Weighted Plank"] = "Isometric core stabilization",
            ["Plank Knee Tucks"] = "Dynamic stabilization",
            ["Weighted Russian Twists"] = "Additional weight for obliques",
            ["Barbell Squats"] = "Base for leg strength and mass",
            ["Dumbbell Upright Row"] = "Alternative to upright barbell row",
            ["Straight-Arm Cable Pulldown"] = "Lat isolation without elbow flexion",
            ["EZ-Bar Curl"] = "Reduced wrist strain",
            ["Dumbbell Front Raise"] = "Front delt isolation",
            ["Seated Dumbbell Curl"] = "Range of motion control",
            ["Front Plate Raises"] = "Front delt load",
            ["Captain's Chair Knee Raises"] = "Easier hanging variation",
            ["Knee to Elbow Crunches"] = "Coordination and rotation",
            ["Leg Press Calf Raise"] = "Heavy weight for calves",
            ["Seated Calf Raise"] = "Soleus muscle emphasis",
            ["Standing Calf Raise"] = "Main movement for calves",
            ["Lying Leg Raise"] = "Basic exercise for lower abs",
            ["Hanging Leg Raises"] = "Strong emphasis on lower abs",
            ["Single-Arm Lateral Raise"] = "Symmetry focus",
            ["Barbell Bicep Curls"] = "Basic movement for bicep mass",
            ["Single-Leg Calf Raise"] = "Unilateral control",
            ["Wide-Grip Pull-ups"] = "Lat width development",
            ["Machine Hip Abduction"] = "Abductor muscle isolation",
            ["Overhead Dumbbell Extension"] = "Single-arm isolation",
            ["Seated Wrist Extensions"] = "Forearm extensor development",
            ["Overhead Rope Extension"] = "Long head emphasis",
            ["Leg Extension"] = "Quad isolation",
            ["Triceps Pushdown"] = "Classic triceps isolation",
            ["Thoracic Opening"] = "Shoulder girdle mobility improvement",
            ["Hamstring Stretch"] = "Reduced hamstring stiffness",
            ["Wall Calf Stretch"] = "Ankle mobility improvement",
            ["Wrist Roller"] = "Complex grip load",
            ["Russian Twists"] = "Core rotation and obliques",
            ["Romanian Deadlift"] = "Posterior chain and lower back",
            ["Dumbbell Romanian Deadlift"] = "Posterior chain work",
            ["Jump Rope Intervals"] = "Coordination and endurance development",
            ["Weighted Crunches"] = "Additional resistance",
            ["Incline Bench Crunches"] = "Increased range of motion",
            ["Floor Crunches"] = "Classic upper abs movement",
            ["Fitball Crunches"] = "Stretch and core control",
            ["Sprints 30/30"] = "High-intensity interval cardio",
            ["Deadlift"] = "Complex strength exercise",
            ["Jump Rope"] = "Dynamic calf work",
            ["T-Bar Row"] = "Strength movement for thickness and width",
            ["Lat Pulldown"] = "Controlled vertical pulling movement",
            ["Prone Dumbbell Rear Delt Row"] = "Rear delt work without momentum",
            ["One-Arm Dumbbell Row"] = "Unilateral lat load",
            ["Upright Row"] = "Traps and delts",
            ["Face Pull with Pause"] = "Rear delt peak contraction",
            ["Stiff-Leg Deadlift"] = "Tension and strengthening of the posterior chain",
            ["Bent-Over Barbell Row"] = "Basic movement for back mass",
            ["Wide-Grip Barbell Row to Chest"] = "Rear delts and traps",
            ["Farmer's Walk"] = "Grip strength and static load",
            ["Tip-Toe Farmer's Walk"] = "Statodynamic load",
            ["Lying French Press"] = "Triceps long head development",
            ["Front Squats"] = "Greater quad emphasis",
            ["Hip Thrust"] = "Maximum glute activation",
            ["Dumbbell Shrugs"] = "Free range of motion variation",
            ["Barbell Shrugs"] = "Upper traps focus",
            ["Sled Push"] = "Strength endurance"
        };

    private static readonly IReadOnlyDictionary<string, string> BaseReplacements =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Акцент на"] = "Emphasis on",
            ["верх"] = "upper",
            ["ниж"] = "lower",
            ["серед"] = "mid",
            ["груд"] = "chest",
            ["спин"] = "back",
            ["плеч"] = "shoulder",
            ["дельт"] = "delt",
            ["біцепс"] = "biceps",
            ["трицепс"] = "triceps",
            ["передплічч"] = "forearm",
            ["литк"] = "calf",
            ["квадрицепс"] = "quadriceps",
            ["сідниц"] = "glute",
            ["м'яз"] = "muscle",
            ["м'язів"] = "muscles",
            ["вправа"] = "exercise",
            ["вправ"] = "exercise",
            ["базова"] = "basic",
            ["базовий"] = "compound",
            ["ізоляція"] = "isolation",
            ["ізольована"] = "isolated",
            ["контрольована"] = "controlled",
            ["контроль"] = "control",
            ["траєкторія"] = "path",
            ["траєкторії"] = "path",
            ["сила"] = "strength",
            ["силова"] = "strength",
            ["витривалість"] = "endurance",
            ["навантаження"] = "load",
            ["навантага"] = "load",
            ["вагою тіла"] = "bodyweight",
            ["вага тіла"] = "bodyweight",
            ["амплітуда"] = "range of motion",
            ["стабілізація"] = "stability",
            ["стабілізована"] = "stabilized",
            ["стабільна"] = "stable",
            ["одностороння"] = "unilateral",
            ["однією рукою"] = "single-arm",
            ["однією ногою"] = "single-leg",
            ["поверхні стегна"] = "thigh region",
            ["корпусу"] = "core",
            ["хвата"] = "grip",
            ["рух"] = "movement",
            ["рухливості"] = "mobility",
            ["рухливість"] = "mobility",
            ["розтягнення"] = "stretch",
            ["підготовка"] = "preparation",
            ["покращення"] = "improvement",
            ["зниження"] = "reduction",
            ["скутіст"] = "stiffness",
            ["постійна напруга"] = "constant tension",
            ["пікове скорочення"] = "peak contraction",
            ["функціональна"] = "functional",
            ["повнотіловий"] = "full-body",
            ["інтервальне кардіо високої інтенсивності"] = "high-intensity interval cardio",
            ["комплексна"] = "complex",
            ["без інерції"] = "without momentum",
            ["безпечний"] = "safe",
            ["безпечна"] = "safe",
            ["класична"] = "classic",
            ["класичний"] = "classic",
            ["розвиток"] = "development",
            ["зміцнення"] = "strengthening",
            ["координації"] = "coordination",
            ["та"] = "and",
            ["з"] = "with",
            ["для"] = "for",
            ["у"] = "in"
        };

    private static string TranslateByReplacements(string value, IReadOnlyDictionary<string, string> replacements)
    {
        var translated = value;
        foreach (var pair in replacements.OrderByDescending(x => x.Key.Length))
        {
            translated = translated.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
        }

        return translated.Trim();
    }

    private static string EnsureEnglish(string value, string fallbackLabel)
    {
        return ContainsCyrillic(value) ? fallbackLabel : value;
    }

    private static bool ContainsCyrillic(string value)
    {
        foreach (var ch in value)
        {
            if ((ch >= '\u0400' && ch <= '\u04FF') || ch == 'і' || ch == 'ї' || ch == 'є' || ch == 'ґ')
            {
                return true;
            }
        }

        return false;
    }
}
