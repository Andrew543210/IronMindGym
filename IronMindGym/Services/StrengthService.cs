using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class StrengthService(IronMindDbContext db)
{
    public InitialBenchmarks Current(int userId, InitialBenchmarks? baseline)
    {
        var records = db.PersonalRecords.AsNoTracking().Where(r => r.UserId == userId && r.Exercise.KeyLift != null)
            .Select(r => new { Lift = r.Exercise.KeyLift!.Value, r.MaxWeightKg })
            .ToDictionary(r => r.Lift, r => r.MaxWeightKg);
        double Best(KeyLift lift, double initial) => Math.Max(initial, records.GetValueOrDefault(lift));
        return new InitialBenchmarks
        {
            BenchPressKg = Best(KeyLift.BenchPress, baseline?.BenchPressKg ?? 0),
            SquatKg = Best(KeyLift.Squat, baseline?.SquatKg ?? 0),
            DeadliftKg = Best(KeyLift.Deadlift, baseline?.DeadliftKg ?? 0)
        };
    }
}
