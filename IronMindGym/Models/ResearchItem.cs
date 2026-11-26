using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

/// <summary>A short research note on the dashboard, with the source it summarizes.</summary>
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

    /// <summary>The publication date of the source.</summary>
    public DateOnly PublishedAt { get; set; }

    [Required, MaxLength(200)]
    public string SourceName { get; set; } = string.Empty;

    /// <summary>An https link to the source.</summary>
    [Required, MaxLength(300)]
    public string SourceUrl { get; set; } = string.Empty;
}
