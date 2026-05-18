using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class MuscleSubGroup
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public int MuscleGroupId { get; set; }
    public MuscleGroup MuscleGroup { get; set; } = null!;

    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    // Validation Methods
    public bool IsValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 80;

    public void ValidateAndSetName(string name)
    {
        if (!IsValidName(name))
            throw new ArgumentException("Muscle subgroup name must be between 1 and 80 characters.");
        Name = name.Trim();
    }

    public void Rename(string newName) => ValidateAndSetName(newName);

    public override string ToString() => $"MuscleSubGroup: {Name}";
}
