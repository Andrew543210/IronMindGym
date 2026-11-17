using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.Services;

namespace IronMindGym.Seed;

public static class DataSeeder
{
    public static void Seed(IronMindDbContext db, bool includeDemoData = false)
    {
        using var transaction = db.Database.BeginTransaction();
        if (!db.SplitTypes.Any())
            db.SplitTypes.AddRange(new[] { "Full-body", "Upper-Lower", "Legs-Back-Chest" }.Select(name => new SplitType { Name = name }));
        if (!db.WeekDays.Any())
            db.WeekDays.AddRange(new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }.Select(name => new WeekDay { Name = name }));
        if (!db.SetTypes.Any())
            db.SetTypes.AddRange(new[] { "Working", "Warm-up", "Failure" }.Select(name => new SetType { Name = name }));
        if (!db.Exercises.Any())
            ExerciseCatalogSeeder.Seed(db);
        EnsureAvatarLadder(db);
        db.SaveChanges();
        EnsureResearchFeed(db);
        AchievementEngine.EnsureCatalog(db);
        if (includeDemoData)
            DemoDataSeeder.Seed(db);
        transaction.Commit();
    }

    private static void EnsureAvatarLadder(IronMindDbContext db)
    {
        var targetAvatars = new[]
        {
            ("Noodle Nomad", 0.0, "/images/avatar-novice.png"),
            ("Couch Crusader", 0.5, "/images/avatar-novice.png"),
            ("Barbell Intern", 1.0, "/images/avatar-novice.png"),
            ("Dumbbell Bard", 1.5, "/images/avatar-novice.png"),
            ("Iron Squire", 2.0, "/images/avatar-athlete.png"),
            ("Steel Disciple", 2.5, "/images/avatar-athlete.png"),
            ("Gym Centurion", 3.0, "/images/avatar-athlete.png"),
            ("Berserker Prime", 3.5, "/images/avatar-athlete.png"),
            ("Titanium Overlord", 4.0, "/images/avatar-titan.png"),
            ("Celestial Colossus", 4.5, "/images/avatar-titan.png"),
            ("Omnipotent Origin", 5.0, "/images/avatar-titan.png")
        };


        var existingNames = db.Avatars.Select(a => a.Name).ToHashSet();
        db.Avatars.AddRange(targetAvatars.Where(x => !existingNames.Contains(x.Item1)).Select(x => new Avatar
        {
            Name = x.Item1,
            MinStrengthCoefficient = x.Item2,
            IconPath = x.Item3
        }));
    }

    private static void EnsureResearchFeed(IronMindDbContext db)
    {
        var articles = new[]
        {
            new { Title = "Build movement into your week", Summary = "WHO guidance counts everyday movement as well as structured exercise. Walking, cycling and strength work can all contribute to an active week. Recommendations differ by age and circumstances.", Source = "WHO · Public health guidance|https://www.who.int/news-room/fact-sheets/detail/physical-activity", Published = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc) },
            new { Title = "Recovery includes consistent sleep", Summary = "CDC guidance emphasizes both sleep duration and quality. A consistent bedtime and wake-up time are practical starting habits; sleep requirements vary with age.", Source = "CDC · Sleep guidance|https://www.cdc.gov/sleep/about/index.html", Published = new DateTime(2024, 5, 15, 0, 0, 0, DateTimeKind.Utc) },
            new { Title = "Protein and resistance training", Summary = "The ISSN position stand reviews how dietary protein supports adaptations to exercise in healthy, active people. It discusses total daily intake, protein quality and timing, rather than a single rule for everyone.", Source = "Jäger et al. · ISSN position stand, 2017|https://pmc.ncbi.nlm.nih.gov/articles/PMC5477153/", Published = new DateTime(2017, 6, 20, 0, 0, 0, DateTimeKind.Utc) },
            new { Title = "Weekly training volume and muscle growth", Summary = "This systematic review and meta-analysis found a dose-response relationship between weekly resistance-training sets and muscle growth in the included studies. It does not establish one optimal set count for every lifter.", Source = "Schoenfeld et al. · Journal of Sports Sciences, 2017|https://pubmed.ncbi.nlm.nih.gov/27433992/", Published = new DateTime(2017, 6, 1, 0, 0, 0, DateTimeKind.Utc) }
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
