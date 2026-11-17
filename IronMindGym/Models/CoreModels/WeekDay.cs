using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class WeekDay
{
    private string _name = string.Empty;

    public int Id
    {
        get; set;
    }

    [Required, MaxLength(20)]
    public string Name
    {
        get => _name;
        set => _name = (value ?? string.Empty).Trim();
    }

    public ICollection<ScheduledDay> ScheduledDays { get; set; } = new List<ScheduledDay>();

    public void Rename(string newName) => Name = newName;

    public override string ToString() => $"WeekDay: {Name}";
}
