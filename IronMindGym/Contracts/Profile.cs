using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Contracts;

public sealed record ProfileOverview(
    string Username,
    string Email,
    int Age,
    string AvatarName,
    double StrengthCoefficient,
    double? WeightKg,
    double? HeightCm,
    DateOnly? WeighedOn,
    double? BenchPressKg,
    double? SquatKg,
    double? DeadliftKg,
    List<WeightEntry> WeightHistory,
    List<ProfileAchievement> Achievements);

public sealed record WeightEntry(DateOnly Date, double ValueKg);

public sealed record ProfileAchievement(string Name, string ConditionDescription, DateTime AchievedAt);

public sealed record ProfileSettingsResponse(ProfileSettings Settings, List<OptionResponse> SplitTypes);

public sealed record WeightRequest([Range(30, 250, ErrorMessage = "Enter a weight between 30 and 250 kg.")] double WeightKg);

public sealed record LanguageRequest(
    [Required, RegularExpression("^(en|uk)$", ErrorMessage = "Choose English or Ukrainian.")] string Language);
