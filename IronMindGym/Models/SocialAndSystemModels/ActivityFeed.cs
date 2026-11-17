namespace IronMindGym.Models;

public class ActivityFeed
{
    public int Id
    {
        get; set;
    }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public int ResearchItemId
    {
        get; set;
    }
    public ResearchItem ResearchItem { get; set; } = null!;

    public void Reschedule(DateTime publishedAt) => PublishedAt = publishedAt;

    public override string ToString() => $"ActivityFeed: ResearchItemId={ResearchItemId}, PublishedAt={PublishedAt:yyyy-MM-dd}";
}
