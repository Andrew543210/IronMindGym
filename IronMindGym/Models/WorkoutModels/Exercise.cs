using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class Exercise
{
    private string _name = string.Empty;
    private string _description = string.Empty;

    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Exercise name is required.");
            }

            _name = value.Trim();
        }
    }

    [MaxLength(1000)]
    public string Description
    {
        get => _description;
        set => _description = value.Trim();
    }

    public int MuscleSubGroupId { get; set; }
    public MuscleSubGroup MuscleSubGroup { get; set; } = null!;

    public ICollection<PerformedExercise> PerformedExercises { get; set; } = new List<PerformedExercise>();

    public void Rename(string newName) => Name = newName;
    public void UpdateDescription(string newDescription) => Description = newDescription;

    public override string ToString() => $"Exercise: {Name}";
}
