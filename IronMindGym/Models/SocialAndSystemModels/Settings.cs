using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Settings
{
    private string _language = "en";

    public int Id
    {
        get; set;
    }

    public int UserId
    {
        get; set;
    }
    public User User { get; set; } = null!;

    [MaxLength(10)]
    public string Language
    {
        get => _language;
        set => _language = string.IsNullOrWhiteSpace(value) ? "en" : value.Trim().ToLowerInvariant();
    }

    public int UIThemeId
    {
        get; set;
    }
    public UITheme UITheme { get; set; } = null!;

    public void ChangeLanguage(string language) => Language = language;

    public override string ToString() => $"Settings: Language={Language}, ThemeId={UIThemeId}";
}
