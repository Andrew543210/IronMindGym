using IronMindGym.Data;
using IronMindGym.Models;
using Microsoft.EntityFrameworkCore;

namespace IronMindGym.Services;

public class StrengthService(IronMindDbContext db)
{
    public InitialBenchmarks Current(int userId, InitialBenchmarks? baseline)
    {
        var records = db.PersonalRecords.AsNoTracking().Where(r => r.UserId == userId)
            .Select(r => new { r.Exercise.Name, r.MaxWeightKg }).ToList();
        double Best(string name, double initial) => Math.Max(initial, records.Where(r => r.Name == name).Select(r => r.MaxWeightKg).DefaultIfEmpty(0).Max());
        return new InitialBenchmarks
        {
            BenchPressKg = Best("Flat Bench Press", baseline?.BenchPressKg ?? 0),
            SquatKg = Best("Barbell Back Squat", baseline?.SquatKg ?? 0),
            DeadliftKg = Best("Deadlift", baseline?.DeadliftKg ?? 0)
        };
    }
}
