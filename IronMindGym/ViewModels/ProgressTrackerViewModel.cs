namespace IronMindGym.ViewModels;

using IronMindGym.Models;

public class SaveProgressViewModel
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public int ExerciseId { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public double InitialValue1 { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public double InitialValue2 { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public double CurrentValue1 { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public double CurrentValue2 { get; set; }
}

public class ProgressTrackerViewModel
{
    public List<ExerciseProgressEntry> Exercises { get; set; } = new();
    public int? SelectedExerciseIdForEdit
    {
        get; set;
    }
    public ExerciseProgressEntry? EditingEntry
    {
        get; set;
    }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ExerciseProgressEntry
{
    public bool HasCurrentResult => ExerciseType == ExerciseType.StrengthTraining ? CurrentReps > 0 : CurrentDurationSeconds > 0;

    public int Id
    {
        get; set;
    }
    public int ExerciseProgressId
    {
        get; set;
    }
    public int ExerciseId
    {
        get; set;
    }
    public string ExerciseName { get; set; } = string.Empty;
    public string MuscleSubGroup { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public ExerciseType ExerciseType
    {
        get; set;
    }

    public double InitialWeightKg
    {
        get; set;
    }
    public int InitialReps
    {
        get; set;
    }
    public int InitialDurationSeconds
    {
        get; set;
    }
    public int InitialIntensity
    {
        get; set;
    }
    public DateTime InitialRecordedAt
    {
        get; set;
    }

    public double CurrentWeightKg
    {
        get; set;
    }
    public int CurrentReps
    {
        get; set;
    }
    public int CurrentDurationSeconds
    {
        get; set;
    }
    public int CurrentIntensity
    {
        get; set;
    }
    public DateTime CurrentRecordedAt
    {
        get; set;
    }

    public double WeightProgressKg
    {
        get; set;
    }
    public double WeightProgressPercent
    {
        get; set;
    }
    public int RepsProgressCount
    {
        get; set;
    }
    public double RepsProgressPercent
    {
        get; set;
    }
    public int CardioDurationProgressSeconds
    {
        get; set;
    }
    public int CardioIntensityProgressCount
    {
        get; set;
    }
}

public class FriendProgressTrackerViewModel
{
    public int FriendId
    {
        get; set;
    }
    public string FriendUsername { get; set; } = string.Empty;
    public string CurrentAvatarStatus { get; set; } = string.Empty;
    public double CurrentStrengthCoefficient
    {
        get; set;
    }

    public List<ExerciseProgressEntry> Exercises { get; set; } = new();
}
