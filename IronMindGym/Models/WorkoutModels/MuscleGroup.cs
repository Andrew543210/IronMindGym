using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class MuscleGroup
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public ICollection<MuscleSubGroup> SubGroups { get; set; } = new List<MuscleSubGroup>();

    // Validation Methods
    public bool IsValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 80;

    public void ValidateAndSetName(string name)
    {
        if (!IsValidName(name))
            throw new ArgumentException("Muscle group name must be between 1 and 80 characters.");
        Name = name.Trim();
    }

    public void Rename(string newName) => ValidateAndSetName(newName);

    public override string ToString() => $"MuscleGroup: {Name}";
}
