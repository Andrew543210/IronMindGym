using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public enum ExerciseType
{
    StrengthTraining = 0,
    Cardio = 1
}

public class Exercise
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public ExerciseType Type { get; set; } = ExerciseType.StrengthTraining;

    public int MuscleSubGroupId { get; set; }
    public MuscleSubGroup MuscleSubGroup { get; set; } = null!;

    public ICollection<PerformedExercise> PerformedExercises { get; set; } = new List<PerformedExercise>();

    // Validation Methods
    public bool IsValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 120;

    public void ValidateAndSetName(string name)
    {
        if (!IsValidName(name))
            throw new ArgumentException("Exercise name must be between 1 and 120 characters.");
        Name = name.Trim();
    }

    public void ValidateAndSetDescription(string description)
    {
        if (description != null && description.Length > 1000)
            throw new ArgumentException("Description cannot exceed 1000 characters.");
        Description = description?.Trim() ?? string.Empty;
    }

    public void Rename(string newName) => ValidateAndSetName(newName);
    public void UpdateDescription(string newDescription) => ValidateAndSetDescription(newDescription);

    public override string ToString() => $"Exercise: {Name}";
}
