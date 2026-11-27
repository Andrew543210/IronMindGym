using IronMindGym.Contracts;
using IronMindGym.Data;
using IronMindGym.Localization;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public sealed class ProfileService(IronMindDbContext db, TimeProvider clock, AthleteCalendars calendars, StrengthService strength, SetupService setup, AchievementService achievements)
{
    public async Task<ProfileOverview> BuildOverviewAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Profile).Include(u => u.Benchmarks).SingleAsync(u => u.Id == userId, cancellationToken);
        var weighIn = await db.WeightLogs.LatestWeighInAsync(userId, cancellationToken);
        var rank = await strength.RankAsync(userId, cancellationToken);
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
            await db.WeightLogs.AsNoTracking().Where(w => w.UserId == userId).OrderByDescending(w => w.Date).Take(30)
                .Select(w => new WeightEntry(w.Date, w.ValueKg)).ToListAsync(cancellationToken),
            await db.UserAchievements.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.AchievedAt)
                .Select(a => new ProfileAchievement(
                    Localized.Pick(a.Achievement.Name, a.Achievement.NameUk),
                    Localized.Pick(a.Achievement.ConditionDescription, a.Achievement.ConditionDescriptionUk),
                    a.AchievedAt))
                .ToListAsync(cancellationToken));
    }

    public async Task<ProfileSettingsResponse> SettingsAsync(int userId, CancellationToken cancellationToken) =>
        new(await BuildEditAsync(userId, cancellationToken), await setup.SplitTypesAsync(cancellationToken));

    /// <summary>The interface language, kept so the athlete sees it again on another device.</summary>
    public async Task SaveLanguageAsync(int userId, Language language, CancellationToken cancellationToken)
    {
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
            db.Profiles.Add(profile = new Profile { UserId = userId });
        profile.Language = language;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<ProfileSettings> BuildEditAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Profile).Include(u => u.Benchmarks).SingleAsync(u => u.Id == userId, cancellationToken);
        var bodyweightKg = (await db.WeightLogs.LatestWeighInAsync(userId, cancellationToken))?.ValueKg;
        var plan = await db.WorkoutPlans.PlanOfAsync(userId, cancellationToken);
        return new ProfileSettings
        {
            Username = user.Username,
            Age = user.Profile?.Age ?? 19,
            TimeZone = user.Profile?.TimeZone,
            CalorieTarget = user.Profile?.CalorieTarget,
            ProteinTarget = user.Profile?.ProteinTarget,
            FatTarget = user.Profile?.FatTarget,
            CarbsTarget = user.Profile?.CarbsTarget,
            EnergyEquation = user.Profile?.EnergyEquation ?? EnergyEquation.None,
            WeightKg = bodyweightKg ?? 70,
            HeightCm = user.Profile?.HeightCm ?? 175,
            BenchPressKg = user.Benchmarks?.BenchPressKg ?? 0,
            SquatKg = user.Benchmarks?.SquatKg ?? 0,
            DeadliftKg = user.Benchmarks?.DeadliftKg ?? 0,
            WorkoutsPerWeek = plan?.WorkoutsPerWeek ?? 3,
            SplitTypeId = plan?.SplitTypeId ?? await db.SplitTypes.OrderBy(s => s.Id).Select(s => s.Id).FirstAsync(cancellationToken)
        };
    }

    public async Task UpdateAsync(int userId, ProfileSettings settings, CancellationToken cancellationToken)
    {
        await AthleteSetup.EnsureSplitExistsAsync(db, settings.SplitTypeId, cancellationToken);
        var timeZone = string.IsNullOrWhiteSpace(settings.TimeZone) ? null : settings.TimeZone.Trim();
        if (timeZone is not null && !AthleteCalendar.TryFindZone(timeZone, out _))
            throw new DomainValidationException("Enter a time zone such as Europe/Kyiv, or leave it empty to use UTC.", nameof(settings.TimeZone));
        var username = settings.Username.Trim();
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        if (await db.Users.AnyAsync(u => u.Id != userId && u.Username == username, cancellationToken))
            throw new DomainValidationException("This username is already taken.", nameof(ProfileSettings.Username));
        var user = await db.Users.Include(u => u.Profile).Include(u => u.Benchmarks).SingleAsync(u => u.Id == userId, cancellationToken);
        user.Username = username;
        await AthleteSetup.ApplyAsync(db, user, settings, cancellationToken);
        user.Profile!.Age = settings.Age;
        user.Profile.TimeZone = timeZone;
        user.Profile.CalorieTarget = settings.CalorieTarget;
        user.Profile.ProteinTarget = settings.ProteinTarget;
        user.Profile.FatTarget = settings.FatTarget;
        user.Profile.CarbsTarget = settings.CarbsTarget;
        user.Profile.EnergyEquation = settings.EnergyEquation;
        await RecordWeightIfChangedAsync(userId, settings.WeightKg, calendars.InZone(timeZone).Today, cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // Another account took the username between the check above and the update.
            throw new DomainValidationException("This username is already taken.", nameof(ProfileSettings.Username));
        }
        calendars.Forget(userId);
        await achievements.AwardEarnedAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>A weigh-in for any day in the last five years. The latest one is the current bodyweight.</summary>
    public async Task LogWeightAsync(int userId, DateOnly date, double weightKg, CancellationToken cancellationToken)
    {
        (await calendars.ForAsync(userId, cancellationToken)).EnsureJournalDay(date);
        await using var transaction = await WriteLock.BeginForAthleteAsync(db, userId, cancellationToken);
        if (!await AthleteSetup.IsCompleteAsync(db, userId, cancellationToken))
            throw new DomainValidationException("Complete your profile first.");
        await AthleteSetup.SaveWeighInAsync(db, userId, weightKg, date, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>A new security stamp ends every other session of the account.</summary>
    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleAsync(u => u.Id == userId, cancellationToken);
        if (!PasswordHashUtility.Verify(request.CurrentPassword, user.PasswordHash))
            throw new DomainValidationException("Current password is incorrect.", nameof(request.CurrentPassword));
        user.PasswordHash = PasswordHashUtility.Hash(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Everything the athlete recorded, without credentials, for keeping a personal copy.</summary>
    public async Task<DataExport> ExportAsync(int userId, CancellationToken cancellationToken) => new(
        clock.UtcNow(),
        await db.Users.Where(u => u.Id == userId)
            .Select(u => new ExportedAccount(u.Username, u.Email, u.Profile == null ? 0 : u.Profile.Age, u.Profile == null ? null : u.Profile.HeightCm))
            .SingleAsync(cancellationToken),
        await db.WeightLogs.Where(w => w.UserId == userId).OrderBy(w => w.Date).Select(w => new ExportedWeighIn(w.Date, w.ValueKg)).ToListAsync(cancellationToken),
        await db.DailyNutritions.Where(n => n.UserId == userId).OrderBy(n => n.Date)
            .Select(n => new ExportedNutritionDay(n.Date, n.CaloriesKcal, n.ProteinG, n.FatG, n.CarbsG)).ToListAsync(cancellationToken),
        await db.WorkoutSessions.Where(w => w.UserId == userId).OrderBy(w => w.SessionDate).Select(w => new ExportedWorkout(
            w.SplitDay == null ? "Training Day" : w.SplitDay.WorkoutName,
            w.SessionDate,
            w.DurationMinutes,
            w.Rating,
            w.Notes,
            w.PerformedExercises.OrderBy(e => e.OrderInWorkout).Select(e => new ExportedExercise(
                e.Exercise.Name,
                e.Sets.OrderBy(s => s.SetNumber)
                    .Select(s => new ExportedSet(s.SetNumber, s.SetType.Name, s.WeightKg, s.Reps, s.DurationSeconds, s.Intensity, s.RestSeconds))
                    .ToList())).ToList())).ToListAsync(cancellationToken),
        await db.ExerciseProgress.Where(p => p.UserId == userId).OrderBy(p => p.Exercise.Name).Select(p => new ExportedProgress(
            p.Exercise.Name, p.InitialWeightKg, p.CurrentWeightKg, p.InitialReps, p.CurrentReps,
            p.InitialDurationSeconds, p.CurrentDurationSeconds, p.InitialIntensity, p.CurrentIntensity)).ToListAsync(cancellationToken),
        await db.UserGoals.Where(g => g.UserId == userId).OrderBy(g => g.Deadline)
            .Select(g => new ExportedGoal(g.Title, g.Deadline, g.IsCompleted)).ToListAsync(cancellationToken));

    /// <summary>The settings form always sends the bodyweight; only a real change is today's weigh-in.</summary>
    private async Task RecordWeightIfChangedAsync(int userId, double weightKg, DateOnly today, CancellationToken cancellationToken)
    {
        var latest = await db.WeightLogs.LatestWeighInAsync(userId, cancellationToken);
        if (latest is null || Math.Abs(latest.ValueKg - weightKg) > 0.001)
            await AthleteSetup.SaveWeighInAsync(db, userId, weightKg, today, cancellationToken);
    }
}
