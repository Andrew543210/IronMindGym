using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class ProfileService(IronMindDbContext db, AthleteCalendars calendars, StrengthService strength, AchievementService achievements)
{
    public ProfileOverview BuildOverview(int userId)
    {
        var user = db.Users.AsNoTracking().Include(u => u.Profile).Include(u => u.Benchmarks).Single(u => u.Id == userId);
        var weighIn = db.WeightLogs.LatestWeighIn(userId);
        var rank = strength.Rank(userId);
        return new ProfileOverview(
            user.Username,
            user.Email,
            user.Profile?.Age ?? 0,
            Localized.Pick(rank.Rank.Name, rank.Rank.NameUk),
            rank.Coefficient,
            weighIn?.ValueKg,
            user.Profile?.HeightCm,
            weighIn?.Date,
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
        var bodyweightKg = db.WeightLogs.LatestWeighIn(userId)?.ValueKg;
        var plan = db.WorkoutPlans.PlanOf(userId);
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
            WeightKg = bodyweightKg ?? 70,
            HeightCm = user.Profile?.HeightCm ?? 175,
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
            throw new DomainValidationException("Choose a valid training split.", "SplitTypeId");
        var timeZone = string.IsNullOrWhiteSpace(settings.TimeZone) ? null : settings.TimeZone.Trim();
        if (timeZone is not null && !AthleteCalendar.TryFindZone(timeZone, out _))
            throw new DomainValidationException("Enter a time zone such as Europe/Kyiv, or leave it empty to use UTC.", "TimeZone");
        var username = settings.Username.Trim();
        using var transaction = WriteLock.BeginForAthletes(db, userId);
        if (db.Users.Any(u => u.Id != userId && u.Username == username))
            throw new DomainValidationException("This username is already taken.", "Username");
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
        user.Profile.HeightCm = settings.HeightCm;
        user.Benchmarks ??= new InitialBenchmarks { UserId = userId };
        user.Benchmarks.BenchPressKg = settings.BenchPressKg;
        user.Benchmarks.SquatKg = settings.SquatKg;
        user.Benchmarks.DeadliftKg = settings.DeadliftKg;
        RecordWeightIfChanged(userId, settings.WeightKg, calendars.InZone(timeZone).Today);
        var plan = db.WorkoutPlans.Find(userId);
        if (plan is null)
            db.WorkoutPlans.Add(plan = new WorkoutPlan { UserId = userId });
        plan.SplitTypeId = settings.SplitTypeId;
        plan.WorkoutsPerWeek = settings.WorkoutsPerWeek;
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // Another account took the username between the check above and the update.
            throw new DomainValidationException("This username is already taken.", "Username");
        }
        calendars.Forget(userId);
        achievements.AwardEarned(userId);
        transaction.Commit();
    }

    /// <summary>A weigh-in for any day in the last five years. The latest one is the current bodyweight.</summary>
    public void LogWeight(int userId, DateOnly date, double weightKg)
    {
        var today = calendars.For(userId).Today;
        if (date > today || date < today.AddYears(-5))
            throw new DomainValidationException("Choose a date within the last five years, up to today.", "Date");
        using var transaction = WriteLock.BeginForAthletes(db, userId);
        if (!AthleteSetup.IsComplete(db, userId))
            throw new DomainValidationException("Complete your profile first.");
        UpsertWeight(userId, weightKg, date);
        db.SaveChanges();
        transaction.Commit();
    }

    /// <summary>A new security stamp ends every other session of the account.</summary>
    public void ChangePassword(int userId, ChangePasswordRequest request)
    {
        var user = db.Users.Single(u => u.Id == userId);
        if (!PasswordHashUtility.Verify(request.CurrentPassword, user.PasswordHash))
            throw new DomainValidationException("Current password is incorrect.", "CurrentPassword");
        user.PasswordHash = PasswordHashUtility.Hash(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        db.SaveChanges();
    }

    /// <summary>Everything the athlete recorded, without credentials, for keeping a personal copy.</summary>
    public object Export(int userId) => new
    {
        ExportedAtUtc = calendars.For(userId).UtcNow,
        Account = db.Users.Where(u => u.Id == userId)
            .Select(u => new { u.Username, u.Email, Age = u.Profile == null ? 0 : u.Profile.Age, HeightCm = u.Profile == null ? null : u.Profile.HeightCm }).Single(),
        Weight = db.WeightLogs.Where(w => w.UserId == userId).OrderBy(w => w.Date).Select(w => new { w.Date, w.ValueKg }).ToList(),
        Nutrition = db.DailyNutritions.Where(n => n.UserId == userId).OrderBy(n => n.Date)
            .Select(n => new { n.Date, n.CaloriesKcal, n.ProteinG, n.FatG, n.CarbsG }).ToList(),
        Workouts = db.WorkoutSessions.Where(w => w.UserId == userId).OrderBy(w => w.SessionDate).Select(w => new
        {
            Name = w.SplitDay == null ? "Training Day" : w.SplitDay.WorkoutName,
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
        Goals = db.UserGoals.Where(g => g.UserId == userId).Select(g => new { g.Title, g.Deadline, g.IsCompleted }).ToList()
    };

    /// <summary>The settings form always sends the bodyweight; only a real change is today's weigh-in.</summary>
    private void RecordWeightIfChanged(int userId, double weightKg, DateOnly today)
    {
        var latest = db.WeightLogs.LatestWeighIn(userId);
        if (latest is null || Math.Abs(latest.ValueKg - weightKg) > 0.001)
            UpsertWeight(userId, weightKg, today);
    }

    private void UpsertWeight(int userId, double value, DateOnly date)
    {
        var log = db.WeightLogs.FirstOrDefault(w => w.UserId == userId && w.Date == date);
        if (log is null)
            db.WeightLogs.Add(new WeightLog { UserId = userId, ValueKg = value, Date = date });
        else
            log.ValueKg = value;
    }
}
