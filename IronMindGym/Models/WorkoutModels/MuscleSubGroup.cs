using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class MuscleSubGroup
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
                throw new ArgumentException("Muscle subgroup name is required.");
            }

            _name = value.Trim();
        }
    }

    public int MuscleGroupId { get; set; }
    public MuscleGroup MuscleGroup { get; set; } = null!;

    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    public void Rename(string newName) => Name = newName;

    public override string ToString() => $"MuscleSubGroup: {Name}";
}
