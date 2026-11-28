using System.Globalization;

namespace IronMindGym.Localization;

public static class Localized
{
    public static bool IsUkrainian => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "uk";

    public static string Pick(string english, string? ukrainian) =>
        IsUkrainian && !string.IsNullOrWhiteSpace(ukrainian) ? ukrainian : english;

    public static StringComparer Order => StringComparer.Create(CultureInfo.CurrentUICulture, ignoreCase: true);
}
