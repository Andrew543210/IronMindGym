using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class ResearchItem
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(1200)]
    public string ThesisText { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TitleUk { get; set; }

    [MaxLength(1200)]
    public string? ThesisTextUk { get; set; }

    [MaxLength(300)]
    public string Source { get; set; } = string.Empty;

    public ICollection<ActivityFeed> FeedItems { get; set; } = [];
}
