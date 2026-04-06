using System.Globalization;

namespace IronMindGym.Localization;

/// <summary>
/// Picks the text for the language of the current request. The client sends Accept-Language, and the
/// request localization middleware turns it into CurrentUICulture ("en" or "uk"). Numbers and dates are
/// still parsed with the invariant English culture, so only the wording changes.
/// </summary>
public static class Localized
{
    public static bool IsUkrainian => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "uk";

    public static string Pick(string english, string? ukrainian) =>
        IsUkrainian && !string.IsNullOrWhiteSpace(ukrainian) ? ukrainian : english;

    public static StringComparer Order => StringComparer.Create(CultureInfo.CurrentUICulture, ignoreCase: true);
}
