using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class MuscleGroup
{
    private string _name = string.Empty;

    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Muscle group name is required.");
            }

            _name = value.Trim();
        }
    }

    public ICollection<MuscleSubGroup> SubGroups { get; set; } = new List<MuscleSubGroup>();

    public void Rename(string newName) => Name = newName;

    public override string ToString() => $"MuscleGroup: {Name}";
}
