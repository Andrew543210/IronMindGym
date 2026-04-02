namespace IronMindGym.Models;

public class ActivityFeed
{
    public int Id { get; set; }
    public int ResearchItemId { get; set; }
    public ResearchItem ResearchItem { get; set; } = null!;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
