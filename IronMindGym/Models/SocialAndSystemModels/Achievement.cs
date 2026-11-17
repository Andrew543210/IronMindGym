using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Achievement
{
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _conditionDescription = string.Empty;

    public int Id
    {
        get; set;
    }

    [Required, MaxLength(80)]
    public string Code
    {
        get => _code;
        set => _code = (value ?? string.Empty).Trim();
    }

    [Required, MaxLength(100)]
    public string Name
    {
        get => _name;
        set => _name = (value ?? string.Empty).Trim();
    }

    [MaxLength(500)]
    public string ConditionDescription
    {
        get => _conditionDescription;
        set => _conditionDescription = (value ?? string.Empty).Trim();
    }

    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

    public void Update(string code, string name, string conditionDescription)
    {
        Code = code;
        Name = name;
        ConditionDescription = conditionDescription;
    }

    public override string ToString() => $"Achievement: {Code} - {Name}";
}
