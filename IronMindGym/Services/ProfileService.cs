using System.ComponentModel.DataAnnotations;
using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class ProfileService(IronMindDbContext db, CurrentCalendar calendar, StrengthService strength, AvatarService avatars)
{
    public ProfileOverview BuildOverview(int userId)
    {
        var user = db.Users.AsNoTracking().Include(u => u.Profile).Include(u => u.Benchmarks).Single(u => u.Id == userId);
        var body = LatestBody(userId);
        var current = strength.Current(userId, user.Benchmarks);
        var avatar = avatars.ResolveAvatar(db.Avatars.AsNoTracking().ToList(), current, body);
        return new ProfileOverview(
            user.Username,
            user.Email,
            user.Profile?.Age ?? 0,
            Localized.Pick(avatar.Name, avatar.NameUk),
            avatars.CalculateStrengthCoefficient(current, body),
            body?.WeightKg,
            body?.HeightCm,
            body?.RecordedAt,
            user.Benchmarks?.BenchPressKg,
            user.Benchmarks?.SquatKg,
            user.Benchmarks?.DeadliftKg,
            db.WeightLogs.AsNoTracking().Where(w => w.UserId == userId).OrderByDescending(w => w.Date).Take(30)
                .Select(w => new WeightEntry(w.Date, w.ValueKg)).ToList(),
            db.UserAchievements.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.AchievedAt)
                .Select(a => new ProfileAchievement(
                    Localized.Pick(a.Achievement.Name, a.Achievement.NameUk),
                    Localized.Pick(a.Achievement.ConditionDescription, a.Achievement.ConditionDescriptionUk),
                    a.AchievedAt))
                .ToList());
    }

    public ProfileSettings BuildEdit(int userId)
    {
        var user = db.Users.AsNoTracking().Include(u => u.Profile).Include(u => u.Benchmarks).Single(u => u.Id == userId);
        var body = LatestBody(userId);
        var plan = db.WorkoutPlans.AsNoTracking().FirstOrDefault(p => p.UserId == userId && p.IsActive);
        return new ProfileSettings
        {
            Username = user.Username,
            Age = user.Profile?.Age ?? 19,
            TimeZone = user.Profile?.TimeZone,
            CalorieTarget = user.Profile?.CalorieTarget,
            ProteinTarget = user.Profile?.ProteinTarget,
            FatTarget = user.Profile?.FatTarget,
            CarbsTarget = user.Profile?.CarbsTarget,
            EnergyEquation = user.Profile?.EnergyEquation ?? "None",
            WeightKg = body?.WeightKg ?? 70,
            HeightCm = body?.HeightCm ?? 175,
            BenchPressKg = user.Benchmarks?.BenchPressKg ?? 0,
            SquatKg = user.Benchmarks?.SquatKg ?? 0,
            DeadliftKg = user.Benchmarks?.DeadliftKg ?? 0,
            WorkoutsPerWeek = plan?.WorkoutsPerWeek ?? 3,
            SplitTypeId = plan?.SplitTypeId ?? db.SplitTypes.OrderBy(s => s.Id).Select(s => s.Id).First()
        };
    }

    public void Update(int userId, ProfileSettings settings)
    {
        if (!db.SplitTypes.Any(s => s.Id == settings.SplitTypeId))
            throw new ValidationException("Choose a valid training split.");
        var timeZone = string.IsNullOrWhiteSpace(settings.TimeZone) ? null : settings.TimeZone.Trim();
        if (timeZone is not null && !AthleteCalendar.TryFindZone(timeZone, out _))
            throw new ValidationException("Enter a time zone such as Europe/Kyiv, or leave it empty to use UTC.");
        var username = settings.Username.Trim();
        using var transaction = WriteLock.BeginForAthletes(db, userId);
        if (db.Users.Any(u => u.Id != userId && u.Username == username))
            throw new ValidationException("This username is already taken.");
        var user = db.Users.Include(u => u.Profile).Include(u => u.Benchmarks).Single(u => u.Id == userId);
        user.Username = username;
        user.Profile ??= new Profile { UserId = userId };
        user.Profile.Age = settings.Age;
        user.Profile.TimeZone = timeZone;
        user.Profile.CalorieTarget = settings.CalorieTarget;
        user.Profile.ProteinTarget = settings.ProteinTarget;
        user.Profile.FatTarget = settings.FatTarget;
        user.Profile.CarbsTarget = settings.CarbsTarget;
        user.Profile.EnergyEquation = settings.EnergyEquation;
        user.Benchmarks ??= new InitialBenchmarks { UserId = userId };
        user.Benchmarks.BenchPressKg = settings.BenchPressKg;
        user.Benchmarks.SquatKg = settings.SquatKg;
        user.Benchmarks.DeadliftKg = settings.DeadliftKg;
        RecordBodyIfChanged(userId, settings.WeightKg, settings.HeightCm, AthleteCalendar.FromId(timeZone).Today);
        var plan = db.WorkoutPlans.FirstOrDefault(p => p.UserId == userId && p.IsActive);
        if (plan is null)
            db.WorkoutPlans.Add(plan = new WorkoutPlan { UserId = userId, IsActive = true });
        plan.SplitTypeId = settings.SplitTypeId;
        plan.WorkoutsPerWeek = settings.WorkoutsPerWeek;
        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, userId);
        transaction.Commit();
    }

    /// <summary>A weigh-in for any day in the last five years. Today's also becomes the current bodyweight.</summary>
    public void LogWeight(int userId, DateTime date, double weightKg)
    {
        var today = calendar.Today;
        if (date.Date > today || date.Date < today.AddYears(-5))
            throw new ValidationException("Choose a date within the last five years, up to today.");
        using var transaction = WriteLock.BeginForAthletes(db, userId);
        var latest = LatestBody(userId) ?? throw new ValidationException("Complete your profile first.");
        UpsertWeight(userId, weightKg, date.Date);
        if (date.Date == today)
            db.AnthropometryLogs.Add(new Anthropometry { UserId = userId, WeightKg = weightKg, HeightCm = latest.HeightCm, RecordedAt = DateTime.UtcNow });
        db.SaveChanges();
        transaction.Commit();
    }

    /// <summary>A new security stamp ends every other session of the account.</summary>
    public void ChangePassword(int userId, ChangePasswordRequest request)
    {
        var user = db.Users.Single(u => u.Id == userId);
        if (!PasswordHashUtility.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException("Current password is incorrect.");
        user.PasswordHash = PasswordHashUtility.Hash(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        db.SaveChanges();
    }

    /// <summary>Everything the athlete recorded, without credentials, for keeping a personal copy.</summary>
    public object Export(int userId) => new
    {
        ExportedAtUtc = DateTime.UtcNow,
        Account = db.Users.Where(u => u.Id == userId)
            .Select(u => new { u.Username, u.Email, Age = u.Profile == null ? 0 : u.Profile.Age }).Single(),
        Weight = db.WeightLogs.Where(w => w.UserId == userId).OrderBy(w => w.Date).Select(w => new { w.Date, w.ValueKg }).ToList(),
        Nutrition = db.DailyNutritions.Where(n => n.UserId == userId).OrderBy(n => n.Date)
            .Select(n => new { n.Date, n.CaloriesKcal, n.ProteinG, n.FatG, n.CarbsG }).ToList(),
        Workouts = db.WorkoutSessions.Where(w => w.UserId == userId).OrderBy(w => w.SessionDate).Select(w => new
        {
            w.Name,
            w.SessionDate,
            w.DurationMinutes,
            Exercises = w.PerformedExercises.OrderBy(e => e.OrderInWorkout).Select(e => new
            {
                e.Exercise.Name,
                Sets = e.Sets.OrderBy(s => s.SetNumber).Select(s => new { s.SetNumber, s.WeightKg, s.Reps, s.DurationSeconds, s.Intensity, s.RestSeconds })
            })
        }).ToList(),
        Progress = db.ExerciseProgress.Where(p => p.UserId == userId).Select(p => new
        {
            p.Exercise.Name,
            p.InitialWeightKg,
            p.CurrentWeightKg,
            p.InitialReps,
            p.CurrentReps,
            p.InitialDurationSeconds,
            p.CurrentDurationSeconds,
            p.InitialIntensity,
            p.CurrentIntensity
        }).ToList(),
        Goals = db.UserGoals.Where(g => g.UserId == userId).Select(g => new { Title = g.GoalType, g.Deadline, g.IsCompleted }).ToList()
    };

    private Anthropometry? LatestBody(int userId) => db.AnthropometryLogs.AsNoTracking()
        .Where(a => a.UserId == userId).OrderByDescending(a => a.RecordedAt).FirstOrDefault();

    /// <summary>The settings form always sends the body values; only a real change is a new measurement.</summary>
    private void RecordBodyIfChanged(int userId, double weightKg, double heightCm, DateTime today)
    {
        var latest = LatestBody(userId);
        var weightChanged = latest is null || Math.Abs(latest.WeightKg - weightKg) > 0.001;
        var heightChanged = latest is null || Math.Abs(latest.HeightCm - heightCm) > 0.001;
        if (!weightChanged && !heightChanged)
            return;
        db.AnthropometryLogs.Add(new Anthropometry { UserId = userId, WeightKg = weightKg, HeightCm = heightCm, RecordedAt = DateTime.UtcNow });
        if (weightChanged)
            UpsertWeight(userId, weightKg, today);
    }

    private void UpsertWeight(int userId, double value, DateTime date)
    {
        var log = db.WeightLogs.FirstOrDefault(w => w.UserId == userId && w.Date == date);
        if (log is null)
            db.WeightLogs.Add(new WeightLog { UserId = userId, ValueKg = value, Date = date });
        else
            log.ValueKg = value;
    }
}
