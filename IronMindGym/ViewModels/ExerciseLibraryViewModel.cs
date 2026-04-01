using IronMindGym.Models;

namespace IronMindGym.ViewModels;

public class ExerciseLibraryViewModel
{
    public List<MuscleGroup> MuscleGroups { get; set; } = new();
    public List<MuscleSubGroup> MuscleSubGroups { get; set; } = new();
    public List<Exercise> Exercises { get; set; } = new();
    public int? SelectedGroupId { get; set; }
    public int? SelectedSubGroupId { get; set; }
    public string Search { get; set; } = string.Empty;
}
