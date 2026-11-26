using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Seed;

public static class DataSeeder
{
    public sealed record SplitDayDefinition(string Code, string Name, string NameUk, string WorkoutName, string WorkoutNameUk);

    public sealed record SplitTypeDefinition(string Code, string Name, string NameUk, SplitDayDefinition[] Days);

    // Rows are matched by code and their names are refreshed on every start.
    // The order is the order of the ids in a new database.
    public static readonly SplitTypeDefinition[] SplitTypes =
    [
        new(SplitTypeCode.FullBody, "Full-body", "Усе тіло",
            [new("FULL_BODY", "Full Body", "Усе тіло", "Full Body day", "День на все тіло")]),
        new(SplitTypeCode.UpperLower, "Upper-Lower", "Верх / низ",
        [
            new("UPPER", "Upper", "Верх тіла", "Upper day", "День верху тіла"),
            new("LOWER", "Lower", "Низ тіла", "Lower day", "День низу тіла")
        ]),
        new(SplitTypeCode.LegsBackChest, "Legs-Back-Chest", "Ноги / спина / груди",
        [
            new("LEGS", "Legs", "Ноги", "Legs day", "День ніг"),
            new("BACK", "Back", "Спина", "Back day", "День спини"),
            new("CHEST", "Chest", "Груди", "Chest day", "День грудей")
        ])
    ];

    public static readonly (string Code, string Name, string NameUk)[] SetTypes =
    [
        (SetTypeCode.Working, "Working", "Робочий"),
        (SetTypeCode.WarmUp, "Warm-up", "Розминковий"),
        (SetTypeCode.Failure, "Failure", "До відмови")
    ];

    public static void Seed(IronMindDbContext db, bool includeDemoData = false)
    {
        using var transaction = WriteLock.BeginForSeeding(db);
        EnsureSplitTypes(db);
        EnsureSetTypes(db);
        ExerciseCatalogSeeder.Sync(db);
        EnsureAvatarLadder(db);
        db.SaveChanges();
        EnsureResearchFeed(db);
        EnsureAchievements(db);
        if (includeDemoData)
            DemoDataSeeder.Seed(db);
        transaction.Commit();
    }

    private static void EnsureAchievements(IronMindDbContext db)
    {
        var existing = db.Achievements.ToDictionary(a => a.Code);
        foreach (var award in AchievementCatalog.All)
        {
            if (!existing.TryGetValue(award.Code, out var achievement))
                db.Achievements.Add(achievement = new Achievement { Code = award.Code });
            achievement.Name = award.Name;
            achievement.NameUk = award.NameUk;
            achievement.ConditionDescription = award.ConditionDescription;
            achievement.ConditionDescriptionUk = award.ConditionDescriptionUk;
        }
        db.SaveChanges();
    }

    private static void EnsureSplitTypes(IronMindDbContext db)
    {
        var existing = db.SplitTypes.ToDictionary(s => s.Code);
        var existingDays = db.SplitDays.ToDictionary(d => d.Code);
        foreach (var definition in SplitTypes)
        {
            if (!existing.TryGetValue(definition.Code, out var split))
                db.SplitTypes.Add(split = new SplitType { Code = definition.Code });
            (split.Name, split.NameUk) = (definition.Name, definition.NameUk);
            foreach (var (day, position) in definition.Days.Select((day, index) => (day, index + 1)))
            {
                if (!existingDays.TryGetValue(day.Code, out var splitDay))
                    db.SplitDays.Add(splitDay = new SplitDay { Code = day.Code });
                splitDay.SplitType = split;
                splitDay.Position = position;
                (splitDay.Name, splitDay.NameUk) = (day.Name, day.NameUk);
                (splitDay.WorkoutName, splitDay.WorkoutNameUk) = (day.WorkoutName, day.WorkoutNameUk);
            }
        }
    }

    private static void EnsureSetTypes(IronMindDbContext db)
    {
        var existing = db.SetTypes.ToDictionary(s => s.Code);
        foreach (var (code, name, nameUk) in SetTypes)
        {
            if (!existing.TryGetValue(code, out var setType))
                db.SetTypes.Add(setType = new SetType { Code = code });
            (setType.Name, setType.NameUk) = (name, nameUk);
        }
    }

    public static readonly (string Name, string NameUk, double MinStrengthCoefficient)[] AvatarLadder =
    [
        ("Noodle Nomad", "Макаронний мандрівник", 0.0),
        ("Couch Crusader", "Диванний хрестоносець", 0.5),
        ("Barbell Intern", "Стажер штанги", 1.0),
        ("Dumbbell Bard", "Гантельний бард", 1.5),
        ("Iron Squire", "Залізний зброєносець", 2.0),
        ("Steel Disciple", "Сталевий учень", 2.5),
        ("Gym Centurion", "Центуріон залу", 3.0),
        ("Berserker Prime", "Верховний берсерк", 3.5),
        ("Titanium Overlord", "Титановий володар", 4.0),
        ("Celestial Colossus", "Небесний колос", 4.5),
        ("Omnipotent Origin", "Всемогутнє першоджерело", 5.0)
    ];

    private static void EnsureAvatarLadder(IronMindDbContext db)
    {
        var existing = db.Avatars.ToList();
        foreach (var rank in AvatarLadder)
        {
            var avatar = existing.FirstOrDefault(a => a.Name == rank.Name);
            if (avatar is null)
                db.Avatars.Add(avatar = new Avatar { Name = rank.Name });
            (avatar.NameUk, avatar.MinStrengthCoefficient) = (rank.NameUk, rank.MinStrengthCoefficient);
        }
    }

    public sealed record ResearchNote(
        string Title, string Summary, string TitleUk, string SummaryUk, string SourceName, string SourceUrl, DateOnly PublishedAt);

    // The research notes are content rather than data anything points at: a note left out of this list is removed.
    public static readonly ResearchNote[] ResearchNotes =
    [
        new("Build movement into your week",
            "WHO guidance counts everyday movement as well as structured exercise. Walking, cycling and strength work can all contribute to an active week. Recommendations differ by age and circumstances.",
            "Додайте рух у свій тиждень",
            "Рекомендації ВООЗ враховують як повсякденний рух, так і структуровані тренування. Ходьба, їзда на велосипеді й силові вправи — усе це робить тиждень активним. Рекомендації залежать від віку та обставин.",
            "WHO · Public health guidance", "https://www.who.int/news-room/fact-sheets/detail/physical-activity", new DateOnly(2026, 11, 4)),
        new("Recovery includes consistent sleep",
            "CDC guidance emphasizes both sleep duration and quality. A consistent bedtime and wake-up time are practical starting habits; sleep requirements vary with age.",
            "Відновлення — це й регулярний сон",
            "Рекомендації CDC наголошують і на тривалості, і на якості сну. Сталий час відходу до сну й пробудження — практичні звички для початку; потреба уві сні залежить від віку.",
            "CDC · Sleep guidance", "https://www.cdc.gov/sleep/about/index.html", new DateOnly(2024, 5, 15)),
        new("Protein and resistance training",
            "The ISSN position stand reviews how dietary protein supports adaptations to exercise in healthy, active people. It discusses total daily intake, protein quality and timing, rather than a single rule for everyone.",
            "Білок і силові тренування",
            "Позиційна заява ISSN розглядає, як харчовий білок підтримує адаптацію до тренувань у здорових активних людей. Ідеться про загальне добове споживання, якість і час прийому білка, а не про одне правило для всіх.",
            "Jäger et al. · ISSN position stand, 2017", "https://pmc.ncbi.nlm.nih.gov/articles/PMC5477153/", new DateOnly(2017, 6, 20)),
        new("Weekly training volume and muscle growth",
            "This systematic review and meta-analysis found a dose-response relationship between weekly resistance-training sets and muscle growth in the included studies. It does not establish one optimal set count for every lifter.",
            "Тижневий обсяг тренувань і ріст м’язів",
            "Цей систематичний огляд і метааналіз виявив дозозалежний зв’язок між кількістю тижневих підходів силових тренувань і ростом м’язів у включених дослідженнях. Єдиної оптимальної кількості підходів для кожного він не встановлює.",
            "Schoenfeld et al. · Journal of Sports Sciences, 2017", "https://pubmed.ncbi.nlm.nih.gov/27433992/", new DateOnly(2017, 6, 1))
    ];

    private static void EnsureResearchFeed(IronMindDbContext db)
    {
        var titles = ResearchNotes.Select(n => n.Title).ToList();
        db.ResearchItems.Where(r => !titles.Contains(r.Title)).ExecuteDelete();
        var existing = db.ResearchItems.ToDictionary(r => r.Title);
        foreach (var note in ResearchNotes)
        {
            if (!existing.TryGetValue(note.Title, out var item))
                db.ResearchItems.Add(item = new ResearchItem { Title = note.Title });
            (item.ThesisText, item.TitleUk, item.ThesisTextUk) = (note.Summary, note.TitleUk, note.SummaryUk);
            (item.SourceName, item.SourceUrl, item.PublishedAt) = (note.SourceName, note.SourceUrl, note.PublishedAt);
        }
        db.SaveChanges();
    }
}
