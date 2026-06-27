using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;

namespace IronMindGym.Seed;

public static class DataSeeder
{
    // The English names are the keys the code matches on; the Ukrainian ones are only shown.
    // The order is the order of the ids in a new database.
    public static readonly (string Name, string NameUk)[] SplitTypes =
    [
        ("Full-body", "Усе тіло"),
        ("Upper-Lower", "Верх / низ"),
        ("Legs-Back-Chest", "Ноги / спина / груди")
    ];

    public static readonly (string Name, string NameUk)[] SetTypes =
    [
        ("Working", "Робочий"),
        ("Warm-up", "Розминковий"),
        ("Failure", "До відмови")
    ];

    public static void Seed(IronMindDbContext db, bool includeDemoData = false)
    {
        using var transaction = db.Database.BeginTransaction();
        WriteLock.ForSeeding(db);
        if (!db.SplitTypes.Any())
            db.SplitTypes.AddRange(SplitTypes.Select(x => new SplitType { Name = x.Name, NameUk = x.NameUk }));
        if (!db.SetTypes.Any())
            db.SetTypes.AddRange(SetTypes.Select(x => new SetType { Name = x.Name, NameUk = x.NameUk }));
        if (!db.Exercises.Any())
            ExerciseCatalogSeeder.Seed(db);
        EnsureTranslations(db);
        ExerciseCatalogSeeder.EnsureTranslations(db);
        EnsureAvatarLadder(db);
        db.SaveChanges();
        EnsureResearchFeed(db);
        AchievementEngine.EnsureCatalog(db);
        if (includeDemoData)
            DemoDataSeeder.Seed(db);
        transaction.Commit();
    }

    private static void EnsureTranslations(IronMindDbContext db)
    {
        foreach (var splitType in db.SplitTypes)
            foreach (var known in SplitTypes.Where(x => x.Name == splitType.Name))
                splitType.NameUk = known.NameUk;
        foreach (var setType in db.SetTypes)
            foreach (var known in SetTypes.Where(x => x.Name == setType.Name))
                setType.NameUk = known.NameUk;
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
                db.Avatars.Add(new Avatar { Name = rank.Name, NameUk = rank.NameUk, MinStrengthCoefficient = rank.MinStrengthCoefficient });
            else
                avatar.NameUk = rank.NameUk;
        }
    }

    private static void EnsureResearchFeed(IronMindDbContext db)
    {
        var articles = new[]
        {
            new { Title = "Build movement into your week", Summary = "WHO guidance counts everyday movement as well as structured exercise. Walking, cycling and strength work can all contribute to an active week. Recommendations differ by age and circumstances.", TitleUk = "Додайте рух у свій тиждень", SummaryUk = "Рекомендації ВООЗ враховують як повсякденний рух, так і структуровані тренування. Ходьба, їзда на велосипеді й силові вправи — усе це робить тиждень активним. Рекомендації залежать від віку та обставин.", Source = "WHO · Public health guidance|https://www.who.int/news-room/fact-sheets/detail/physical-activity", Published = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc) },
            new { Title = "Recovery includes consistent sleep", Summary = "CDC guidance emphasizes both sleep duration and quality. A consistent bedtime and wake-up time are practical starting habits; sleep requirements vary with age.", TitleUk = "Відновлення — це й регулярний сон", SummaryUk = "Рекомендації CDC наголошують і на тривалості, і на якості сну. Сталий час відходу до сну й пробудження — практичні звички для початку; потреба уві сні залежить від віку.", Source = "CDC · Sleep guidance|https://www.cdc.gov/sleep/about/index.html", Published = new DateTime(2024, 5, 15, 0, 0, 0, DateTimeKind.Utc) },
            new { Title = "Protein and resistance training", Summary = "The ISSN position stand reviews how dietary protein supports adaptations to exercise in healthy, active people. It discusses total daily intake, protein quality and timing, rather than a single rule for everyone.", TitleUk = "Білок і силові тренування", SummaryUk = "Позиційна заява ISSN розглядає, як харчовий білок підтримує адаптацію до тренувань у здорових активних людей. Ідеться про загальне добове споживання, якість і час прийому білка, а не про одне правило для всіх.", Source = "Jäger et al. · ISSN position stand, 2017|https://pmc.ncbi.nlm.nih.gov/articles/PMC5477153/", Published = new DateTime(2017, 6, 20, 0, 0, 0, DateTimeKind.Utc) },
            new { Title = "Weekly training volume and muscle growth", Summary = "This systematic review and meta-analysis found a dose-response relationship between weekly resistance-training sets and muscle growth in the included studies. It does not establish one optimal set count for every lifter.", TitleUk = "Тижневий обсяг тренувань і ріст м’язів", SummaryUk = "Цей систематичний огляд і метааналіз виявив дозозалежний зв’язок між кількістю тижневих підходів силових тренувань і ростом м’язів у включених дослідженнях. Єдиної оптимальної кількості підходів для кожного він не встановлює.", Source = "Schoenfeld et al. · Journal of Sports Sciences, 2017|https://pubmed.ncbi.nlm.nih.gov/27433992/", Published = new DateTime(2017, 6, 1, 0, 0, 0, DateTimeKind.Utc) }
        };
        var titles = articles.Select(a => a.Title).ToList();
        db.ActivityFeeds.RemoveRange(db.ActivityFeeds.Where(f => !titles.Contains(f.ResearchItem.Title)));
        db.ResearchItems.RemoveRange(db.ResearchItems.Where(r => !titles.Contains(r.Title)));
        foreach (var article in articles)
        {
            var item = db.ResearchItems.FirstOrDefault(r => r.Title == article.Title);
            if (item is null)
            {
                item = new ResearchItem { Title = article.Title };
                db.ResearchItems.Add(item);
            }
            item.ThesisText = article.Summary;
            item.TitleUk = article.TitleUk;
            item.ThesisTextUk = article.SummaryUk;
            item.Source = article.Source;
            var feed = item.Id == 0 ? null : db.ActivityFeeds.FirstOrDefault(f => f.ResearchItemId == item.Id);
            if (feed is null)
                db.ActivityFeeds.Add(new ActivityFeed { ResearchItem = item, PublishedAt = article.Published });
            else
                feed.PublishedAt = article.Published;
        }
        db.SaveChanges();
    }
}
