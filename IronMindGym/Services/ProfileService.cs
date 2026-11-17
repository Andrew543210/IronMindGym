using System.ComponentModel.DataAnnotations;
using IronMindGym.Data;
using IronMindGym.Models;
using IronMindGym.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class ProfileService(IronMindDbContext db)
{
    public EditProfileViewModel BuildEdit(int userId)
    {
        var user = db.Users.AsNoTracking().Include(u => u.Profile).Include(u => u.Benchmarks).Single(u => u.Id == userId);
        var body = db.AnthropometryLogs.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.RecordedAt).FirstOrDefault();
        var plan = db.WorkoutPlans.AsNoTracking().FirstOrDefault(p => p.UserId == userId && p.IsActive);
        return new EditProfileViewModel
        {
            CalorieTarget = user.Profile?.CalorieTarget,
            ProteinTarget = user.Profile?.ProteinTarget,
            FatTarget = user.Profile?.FatTarget,
            CarbsTarget = user.Profile?.CarbsTarget,
            EnergyEquation = user.Profile?.EnergyEquation ?? "None",
            Username = user.Username,
            Age = user.Profile?.Age ?? 19,
            WeightKg = body?.WeightKg ?? 70,
            HeightCm = body?.HeightCm ?? 175,
            BenchPressKg = user.Benchmarks?.BenchPressKg ?? 0,
            SquatKg = user.Benchmarks?.SquatKg ?? 0,
            DeadliftKg = user.Benchmarks?.DeadliftKg ?? 0,
            WorkoutsPerWeek = plan?.WorkoutsPerWeek ?? 3,
            SplitTypeId = plan?.SplitTypeId ?? db.SplitTypes.Select(s => s.Id).First()
        };
    }

    public void Update(int userId, EditProfileViewModel model)
    {
        if (!db.SplitTypes.Any(s => s.Id == model.SplitTypeId))
            throw new ValidationException("Choose a valid training split.");
        var username = model.Username.Trim();
        if (db.Users.Any(u => u.Id != userId && u.Username == username))
            throw new ValidationException("This username is already taken.");
        var user = db.Users.Include(u => u.Profile).Include(u => u.Benchmarks).Single(u => u.Id == userId);
        user.Username = username;
        user.Profile ??= new Profile { UserId = userId };
        user.Profile.Age = model.Age;
        user.Profile.CalorieTarget = model.CalorieTarget;
        user.Profile.ProteinTarget = model.ProteinTarget;
        user.Profile.FatTarget = model.FatTarget;
        user.Profile.CarbsTarget = model.CarbsTarget;
        user.Profile.EnergyEquation = model.EnergyEquation;
        user.Benchmarks ??= new InitialBenchmarks { UserId = userId };
        user.Benchmarks.BenchPressKg = model.BenchPressKg;
        user.Benchmarks.SquatKg = model.SquatKg;
        user.Benchmarks.DeadliftKg = model.DeadliftKg;
        db.AnthropometryLogs.Add(new Anthropometry { UserId = userId, WeightKg = model.WeightKg, HeightCm = model.HeightCm, RecordedAt = DateTime.UtcNow });
        var plan = db.WorkoutPlans.FirstOrDefault(p => p.UserId == userId && p.IsActive);
        if (plan is null)
            db.WorkoutPlans.Add(new WorkoutPlan { UserId = userId, SplitTypeId = model.SplitTypeId, WorkoutsPerWeek = model.WorkoutsPerWeek, IsActive = true });
        else
        {
            plan.SplitTypeId = model.SplitTypeId;
            plan.WorkoutsPerWeek = model.WorkoutsPerWeek;
        }
        UpsertWeight(userId, model.WeightKg, DateTime.UtcNow.Date);
        db.SaveChanges();
        AchievementEngine.AssignAchievements(db, userId);
    }

    public void LogWeight(int userId, LogWeightViewModel model)
    {
        if (model.Date.Date > DateTime.UtcNow.Date || model.Date.Date < DateTime.UtcNow.Date.AddYears(-5))
            throw new ValidationException("Choose a date within the last five years, up to today.");
        var latest = db.AnthropometryLogs.Where(a => a.UserId == userId).OrderByDescending(a => a.RecordedAt).FirstOrDefault();
        if (latest is null)
            throw new ValidationException("Complete your profile first.");
        UpsertWeight(userId, model.WeightKg, model.Date.Date);
        if (model.Date.Date == DateTime.UtcNow.Date)
            db.AnthropometryLogs.Add(new Anthropometry { UserId = userId, WeightKg = model.WeightKg, HeightCm = latest.HeightCm, RecordedAt = DateTime.UtcNow });
        db.SaveChanges();
    }

    public void ChangePassword(int userId, ChangePasswordViewModel model)
    {
        var user = db.Users.Single(u => u.Id == userId);
        if (!PasswordHashUtility.Verify(model.CurrentPassword, user.PasswordHash))
            throw new ValidationException("Current password is incorrect.");
        user.PasswordHash = PasswordHashUtility.Hash(model.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        db.SaveChanges();
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
