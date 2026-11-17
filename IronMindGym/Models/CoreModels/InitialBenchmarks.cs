using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class InitialBenchmarks
{
    public int Id
    {
        get; set;
    }

    [Range(0, 500)]
    public double BenchPressKg
    {
        get; set;
    }

    [Range(0, 600)]
    public double SquatKg
    {
        get; set;
    }

    [Range(0, 600)]
    public double DeadliftKg
    {
        get; set;
    }

    public int UserId
    {
        get; set;
    }
    public User User { get; set; } = null!;

    public bool IsValidBedBench(double weight) => weight >= 0 && weight <= 500;
    public bool IsValidSquat(double weight) => weight >= 0 && weight <= 600;
    public bool IsValidDeadlift(double weight) => weight >= 0 && weight <= 600;

    public void ValidateAndUpdate(double bench, double squat, double deadlift)
    {
        if (!IsValidBedBench(bench))
            throw new ArgumentException("Bench press must be between 0 and 500 kg.");
        if (!IsValidSquat(squat))
            throw new ArgumentException("Squat must be between 0 and 600 kg.");
        if (!IsValidDeadlift(deadlift))
            throw new ArgumentException("Deadlift must be between 0 and 600 kg.");

        BenchPressKg = bench;
        SquatKg = squat;
        DeadliftKg = deadlift;
    }

    public void Update(double bench, double squat, double deadlift) => ValidateAndUpdate(bench, squat, deadlift);

    public override string ToString() => $"Benchmarks: BP={BenchPressKg}, SQ={SquatKg}, DL={DeadliftKg}";
}
