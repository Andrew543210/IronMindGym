using System.Text.Json.Serialization;

namespace IronMindGym.Models;

/// <summary>The interface language. The API and the database use the language codes.</summary>
public enum Language
{
    [JsonStringEnumMemberName("en")]
    English,

    [JsonStringEnumMemberName("uk")]
    Ukrainian
}
