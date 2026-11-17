using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class SetType
{
    private string _name = string.Empty;

    public int Id
    {
        get; set;
    }

    [Required, MaxLength(40)]
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Set type name is required.");
            }

            _name = value.Trim();
        }
    }

    public ICollection<WorkoutSet> Sets { get; set; } = new List<WorkoutSet>();

    public void Rename(string newName) => Name = newName;

    public override string ToString() => $"SetType: {Name}";
}
