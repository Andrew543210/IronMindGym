using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class ResearchItem
{
    private string _title = string.Empty;
    private string _thesisText = string.Empty;
    private string _source = string.Empty;

    public int Id
    {
        get; set;
    }

    [Required, MaxLength(200)]
    public string Title
    {
        get => _title;
        set => _title = (value ?? string.Empty).Trim();
    }

    [Required, MaxLength(1200)]
    public string ThesisText
    {
        get => _thesisText;
        set => _thesisText = (value ?? string.Empty).Trim();
    }

    [MaxLength(300)]
    public string Source
    {
        get => _source;
        set => _source = (value ?? string.Empty).Trim();
    }

    public ICollection<ActivityFeed> FeedItems { get; set; } = new List<ActivityFeed>();

    public void UpdateContent(string title, string thesisText, string source)
    {
        Title = title;
        ThesisText = thesisText;
        Source = source;
    }

    public override string ToString() => $"ResearchItem: {Title}";
}
