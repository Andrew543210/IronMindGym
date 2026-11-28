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

    public DateOnly PublishedAt { get; set; }

    [Required, MaxLength(200)]
    public string SourceName { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string SourceUrl { get; set; } = string.Empty;
}
