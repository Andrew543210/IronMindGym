namespace IronMindGym.Contracts;

public sealed record ProfileOverview(
    string Username,
    string Email,
    int Age,
    string AvatarName,
    double StrengthCoefficient,
    double? WeightKg,
    double? HeightCm,
    DateTime? MeasuredAt,
    double? BenchPressKg,
    double? SquatKg,
    double? DeadliftKg,
    List<WeightEntry> WeightHistory,
    List<ProfileAchievement> Achievements);

public sealed record WeightEntry(DateTime Date, double ValueKg);

public sealed record ProfileAchievement(string Name, string ConditionDescription, DateTime AchievedAt);
