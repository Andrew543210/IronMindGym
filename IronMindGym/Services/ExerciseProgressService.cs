using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class ExerciseProgressService(IronMindDbContext db, TimeProvider clock, FriendshipService friendships, StrengthService strength)
{
    public async Task<List<ExerciseProgressEntry>> ListAsync(int userId, CancellationToken cancellationToken)
    {
        var entries = await db.ExerciseProgress.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new ExerciseProgressEntry
            {
                ExerciseId = p.ExerciseId,
                ExerciseName = Localized.Pick(p.Exercise.Name, p.Exercise.NameUk),
                MuscleSubGroup = Localized.Pick(p.Exercise.MuscleSubGroup.Name, p.Exercise.MuscleSubGroup.NameUk),
                MuscleGroup = Localized.Pick(p.Exercise.MuscleSubGroup.MuscleGroup.Name, p.Exercise.MuscleSubGroup.MuscleGroup.NameUk),
                ExerciseType = p.Exercise.Type,
                InitialWeightKg = p.InitialWeightKg,
                InitialReps = p.InitialReps,
                InitialDurationSeconds = p.InitialDurationSeconds,
                InitialIntensity = p.InitialIntensity,
                CurrentWeightKg = p.CurrentWeightKg,
                CurrentReps = p.CurrentReps,
                CurrentDurationSeconds = p.CurrentDurationSeconds,
                CurrentIntensity = p.CurrentIntensity
            })
            .ToListAsync(cancellationToken);
        return entries.OrderBy(p => p.ExerciseName, Localized.Order).ToList();
    }

    public async Task<ProgressPair?> ResultsAsync(int userId, int exerciseId, CancellationToken cancellationToken)
    {
        var saved = await db.ExerciseProgress.AsNoTracking().Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == exerciseId, cancellationToken);
        return saved?.Exercise.Type switch
        {
            null => null,
            ExerciseType.StrengthTraining => new(exerciseId, saved.InitialWeightKg, saved.InitialReps, saved.CurrentWeightKg, saved.CurrentReps),
            ExerciseType.Cardio => new(exerciseId, saved.InitialDurationSeconds, saved.InitialIntensity, saved.CurrentDurationSeconds, saved.CurrentIntensity),
            _ => new(exerciseId, saved.InitialDurationSeconds, saved.InitialWeightKg, saved.CurrentDurationSeconds, saved.CurrentWeightKg)
        };
    }

    // Both results are saved together: when either is invalid, neither changes.
    public async Task SaveResultsAsync(int userId, int exerciseId, ProgressResultsRequest request, CancellationToken cancellationToken)
    {
        var exercise = await FindExerciseAsync(exerciseId, cancellationToken);
        var initial = Result.FromPair(exercise.Type, request.InitialValue1!.Value, request.InitialValue2!.Value);
        var current = Result.FromPair(exercise.Type, request.CurrentValue1!.Value, request.CurrentValue2!.Value);
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        var progress = await db.ExerciseProgress.FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == exercise.Id, cancellationToken);
        if (progress is null)
            db.ExerciseProgress.Add(progress = new ExerciseProgress { UserId = userId, ExerciseId = exercise.Id });
        var now = clock.UtcNow();
        Record(progress, initial: true, initial, now);
        Record(progress, initial: false, current, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveCurrentAsync(int userId, int exerciseId, CurrentResultRequest request, CancellationToken cancellationToken)
    {
        var exercise = await FindExerciseAsync(exerciseId, cancellationToken);
        var result = TrackingRules.Check(exercise.Type, request.WeightKg ?? 0, request.Reps ?? 0, request.DurationSeconds ?? 0, request.Intensity ?? 0);
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        var progress = await db.ExerciseProgress.FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == exerciseId, cancellationToken)
            ?? throw new DomainValidationException("Save initial results first.");
        Record(progress, initial: false, result, clock.UtcNow());
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<FriendProgressResponse?> OfFriendAsync(int viewerId, int friendId, CancellationToken cancellationToken)
    {
        if (!await friendships.AreFriendsAsync(viewerId, friendId, cancellationToken))
            return null;
        var username = await db.Users.Where(u => u.Id == friendId).Select(u => u.Username).SingleAsync(cancellationToken);
        var rank = await strength.RankAsync(friendId, cancellationToken);
        return new FriendProgressResponse(username, rank.Coefficient, await ListAsync(friendId, cancellationToken));
    }

    public async Task<bool> DeleteAsync(int userId, int exerciseId, CancellationToken cancellationToken) =>
        await db.ExerciseProgress.Where(p => p.UserId == userId && p.ExerciseId == exerciseId).ExecuteDeleteAsync(cancellationToken) > 0;

    private async Task<Exercise> FindExerciseAsync(int exerciseId, CancellationToken cancellationToken) =>
        await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken)
        ?? throw new DomainValidationException("Choose a valid exercise.", "ExerciseId");

    private static void Record(ExerciseProgress progress, bool initial, TrackedResult result, DateTime now)
    {
        if (initial)
        {
            progress.InitialWeightKg = result.WeightKg;
            progress.InitialReps = result.Reps;
            progress.InitialDurationSeconds = result.DurationSeconds;
            progress.InitialIntensity = result.Intensity;
            progress.InitialRecordedAt = now;
        }
        else
        {
            progress.CurrentWeightKg = result.WeightKg;
            progress.CurrentReps = result.Reps;
            progress.CurrentDurationSeconds = result.DurationSeconds;
            progress.CurrentIntensity = result.Intensity;
            progress.CurrentRecordedAt = now;
        }
    }

    // A result arrives as two numbers whose meaning depends on the exercise type.
    private static class Result
    {
        public static TrackedResult FromPair(ExerciseType type, double first, double second) => type switch
        {
            ExerciseType.StrengthTraining => TrackingRules.Check(type, first, WholeNumber(second), 0, 0),
            ExerciseType.Cardio => TrackingRules.Check(type, 0, 0, WholeNumber(first), WholeNumber(second)),
            _ => TrackingRules.Check(type, second, 0, WholeNumber(first), 0)
        };

        private static int WholeNumber(double value)
        {
            if (!double.IsFinite(value) || value != Math.Truncate(value) || value < 0 || value > TrackingRules.MaxDurationSeconds)
                throw new DomainValidationException("Repetitions, duration and intensity must be whole numbers within their allowed range.");
            return (int)value;
        }
    }
}
