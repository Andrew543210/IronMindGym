using System.Text.Json.Serialization;

namespace IronMindGym.Models;

public enum Language
{
    [JsonStringEnumMemberName("en")]
    English,

    [JsonStringEnumMemberName("uk")]
    Ukrainian
}
